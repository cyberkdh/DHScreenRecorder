//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: GraphicsCaptureService
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace DHScreenRecorder.Capture;

// Windows.Graphics.Capture API 기반 화면 캡처 서비스
// 요구 OS: Windows 10 1903 (build 18362) 이상
public class GraphicsCaptureService : IScreenCapture, IDisposable
{
    private ID3D11Device?               _d3dDevice;
    private ID3D11DeviceContext?        _d3dContext;
    private IDirect3DDevice?            _winrtDevice;
    private Direct3D11CaptureFramePool? _framePool;
    private GraphicsCaptureSession?     _session;
    private CaptureRegion?              _region;
    private bool                        _running;

    // WGC 텍스처는 캡처된 모니터의 로컬 좌표(0,0 기준)를 사용
    // region 은 가상 데스크톱 좌표이므로 추출 시 모니터 원점을 빼줘야 함
    private int _monitorOriginX;
    private int _monitorOriginY;

    public event EventHandler<byte[]>? FrameCaptured;

    // Start WGC capture session for the given region / 지정 영역에 WGC 캡처 세션 시작
    public void Start(CaptureRegion region)
    {
        if (_running) return;
        _region = region;

        InitD3D();

        // 가상 데스크톱에서 해당 모니터 찾기 → 원점 저장
        var monitorScreen = Screen.FromPoint(new Point(region.X, region.Y));
        _monitorOriginX   = monitorScreen.Bounds.Left;
        _monitorOriginY   = monitorScreen.Bounds.Top;

        var hmonitor = WinRTInteropHelper.MonitorFromPoint(
            new POINT(region.X, region.Y),
            WinRTInteropHelper.MONITOR_DEFAULTTOPRIMARY);

        var item = WinRTInteropHelper.CreateItemForMonitor(hmonitor);

        _framePool = Direct3D11CaptureFramePool.Create(
            _winrtDevice!,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            item.Size);

        _framePool.FrameArrived += OnFrameArrived;

        _session = _framePool.CreateCaptureSession(item);
        _session.StartCapture();
        _running = true;
    }

    // Stop capture session and release frame pool / 캡처 세션 중지 및 프레임 풀 해제
    public void Stop()
    {
        if (!_running) return;
        _running = false;

        _session?.Dispose();
        _framePool?.Dispose();
        _session   = null;
        _framePool = null;
    }

    // Called by WGC when a new frame is ready; extract bytes and fire FrameCaptured / 새 프레임 도착 시 바이트 추출 후 이벤트 발생
    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        using var frame = sender.TryGetNextFrame();
        if (frame == null) return;

        try
        {
            var bytes = ExtractFrameBytes(frame);
            FrameCaptured?.Invoke(this, bytes);
        }
        catch { }
    }

    // .NET 9 + CsWinRT: (IFoo)(object)winrtObj 캐스팅은 QI를 통하지 않아 InvalidCastException 발생
    // WinRT 네이티브 포인터를 직접 꺼내 QueryInterface → vtable 호출로 우회
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetInterfaceProc(IntPtr thisPtr, ref Guid iid, out IntPtr ppv);

    // Unwrap WinRT IDirect3DSurface to ID3D11Texture2D via vtable call (.NET 9 COM workaround) / WinRT 서피스에서 D3D11 텍스처 포인터를 vtable 직접 호출로 추출
    private ID3D11Texture2D GetTextureFromSurface(IDirect3DSurface surface)
    {
        // 1) WinRT 객체 → 네이티브 IInspectable 포인터 (AddRefs)
        IntPtr surfPtr = WinRT.MarshalInterface<IDirect3DSurface>.FromManaged(surface);
        try
        {
            // 2) QueryInterface → IDirect3DDxgiInterfaceAccess (순수 COM, IUnknown 기반)
            var iidAccess = new Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");
            int hr = Marshal.QueryInterface(surfPtr, in iidAccess, out IntPtr accessPtr);
            Marshal.ThrowExceptionForHR(hr);
            try
            {
                // 3) vtable 직접 호출: QI(0) AddRef(1) Release(2) GetInterface(3)
                IntPtr vtbl = Marshal.ReadIntPtr(accessPtr);
                IntPtr fn   = Marshal.ReadIntPtr(vtbl, 3 * IntPtr.Size);
                var proc    = Marshal.GetDelegateForFunctionPointer<GetInterfaceProc>(fn);

                var iidTex = WinRTInteropHelper.IID_ID3D11Texture2D;
                hr = proc(accessPtr, ref iidTex, out IntPtr texPtr);
                Marshal.ThrowExceptionForHR(hr);

                return new ID3D11Texture2D(texPtr); // Vortice가 소유권 인계, Dispose 시 Release
            }
            finally { Marshal.Release(accessPtr); }
        }
        finally { Marshal.Release(surfPtr); }
    }

    // Copy the target sub-region from GPU texture to a CPU-readable staging buffer / GPU 텍스처에서 지정 영역만 스테이징 버퍼로 복사해 BGRA 바이트 배열 반환
    private byte[] ExtractFrameBytes(Direct3D11CaptureFrame frame)
    {
        using var texture = GetTextureFromSurface(frame.Surface);

        var desc      = texture.Description;
        int monWidth  = (int)desc.Width;
        int monHeight = (int)desc.Height;

        // 가상 데스크톱 좌표 → 모니터 로컬 좌표 (WGC 텍스처는 0,0 기준)
        int srcX = Math.Clamp(_region!.X - _monitorOriginX, 0, monWidth);
        int srcY = Math.Clamp(_region.Y  - _monitorOriginY, 0, monHeight);

        // FFmpeg BuildArguments 의 `& ~1` 과 동일하게 짝수 강제
        // 홀수 픽셀이 넘어가면 행 바이트가 어긋나 영상이 SKEW 됨
        int outWidth  = Math.Min(_region.Width,  monWidth  - srcX) & ~1;
        int outHeight = Math.Min(_region.Height, monHeight - srcY) & ~1;

        // 전체 모니터 크기의 스테이징 텍스처(Staging Texture) 생성 — CPU 접근용
        var stagingDesc = new Texture2DDescription
        {
            Width             = desc.Width,
            Height            = desc.Height,
            MipLevels         = 1,
            ArraySize         = 1,
            Format            = desc.Format,
            SampleDescription = new SampleDescription(1, 0),
            Usage             = ResourceUsage.Staging,
            BindFlags         = BindFlags.None,
            CPUAccessFlags    = CpuAccessFlags.Read
        };
        using var staging = _d3dDevice!.CreateTexture2D(stagingDesc);
        _d3dContext!.CopyResource(staging, texture);

        // 스테이징 텍스처 → byte[] 변환 (BGRA 포맷, 지정 영역만 추출)
        var mapped   = _d3dContext.Map(staging, 0, MapMode.Read);
        int rowBytes = outWidth * 4;
        var result   = new byte[rowBytes * outHeight];

        for (int y = 0; y < outHeight; y++)
        {
            var src = new IntPtr((long)mapped.DataPointer
                      + (long)(srcY + y) * mapped.RowPitch
                      + (long)srcX * 4);
            Marshal.Copy(src, result, y * rowBytes, rowBytes);
        }

        _d3dContext.Unmap(staging, 0);
        return result;
    }

    // Create D3D11 hardware device and wrap it as WinRT IDirect3DDevice / D3D11 하드웨어 디바이스 생성 및 WinRT IDirect3DDevice로 래핑
    private void InitD3D()
    {
        D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            Array.Empty<FeatureLevel>(),
            out _d3dDevice!,
            out _d3dContext!);

        using var dxgiDevice = _d3dDevice.QueryInterface<IDXGIDevice>();
        int hr = WinRTInteropHelper.CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var devPtr);
        Marshal.ThrowExceptionForHR(hr);

        _winrtDevice = WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(devPtr);

        // FromAbi는 내부적으로 AddRef하므로 원본 참조 해제
        if (devPtr != IntPtr.Zero)
            Marshal.Release(devPtr);
    }

    public void Dispose()
    {
        Stop();
        _winrtDevice?.Dispose();
        _d3dContext?.Dispose();
        _d3dDevice?.Dispose();
    }
}
