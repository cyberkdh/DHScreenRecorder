//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: WinRTInterop
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace DHScreenRecorder.Capture;

// GraphicsCaptureItem을 모니터/창 핸들에서 생성하기 위한 COM 인터페이스
[ComImport]
[Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGraphicsCaptureItemInterop
{
    [PreserveSig]
    int CreateForWindow(IntPtr hwnd, [In] ref Guid iid, out IntPtr ppv);

    [PreserveSig]
    int CreateForMonitor(IntPtr hmonitor, [In] ref Guid iid, out IntPtr ppv);
}

// IDirect3DSurface → ID3D11Texture2D 변환을 위한 COM 인터페이스
[ComImport]
[Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDirect3DDxgiInterfaceAccess
{
    [PreserveSig]
    int GetInterface([In] ref Guid iid, out IntPtr ppv);
}

internal static class WinRTInteropHelper
{
    // ID3D11Texture2D 인터페이스 GUID
    internal static readonly Guid IID_ID3D11Texture2D = new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

    // IActivationFactory 표준 WinRT 팩토리 인터페이스 GUID
    private static readonly Guid IID_IActivationFactory = new("00000035-0000-0000-C000-000000000046");

    // DXGI 디바이스 → WinRT IDirect3DDevice 변환
    [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", PreserveSig = true)]
    internal static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    // HSTRING 수동 생성/해제 (.NET 5+ 에서 UnmanagedType.HString P/Invoke 미지원)
    [DllImport("combase.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int WindowsCreateString(string str, int length, out IntPtr hstring);

    [DllImport("combase.dll", PreserveSig = true)]
    private static extern int WindowsDeleteString(IntPtr hstring);

    // WinRT 활성화 팩토리 획득
    [DllImport("combase.dll", PreserveSig = true)]
    private static extern int RoGetActivationFactory(
        IntPtr activatableClassId,
        [In] ref Guid iid,
        out IntPtr factory);

    // 모니터 핸들 획득
    [DllImport("user32.dll")]
    internal static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    internal const uint MONITOR_DEFAULTTOPRIMARY = 1;

    // Create a GraphicsCaptureItem for the given monitor HMONITOR via WinRT activation factory / WinRT 활성화 팩토리를 통해 모니터 HMONITOR로 GraphicsCaptureItem 생성
    internal static GraphicsCaptureItem CreateItemForMonitor(IntPtr hmonitor)
    {
        // 1단계: IActivationFactory로 팩토리 획득
        const string className = "Windows.Graphics.Capture.GraphicsCaptureItem";
        WindowsCreateString(className, className.Length, out var hstring);

        IntPtr factoryPtr;
        try
        {
            var iid = IID_IActivationFactory;
            int hr  = RoGetActivationFactory(hstring, ref iid, out factoryPtr);
            Marshal.ThrowExceptionForHR(hr);
        }
        finally
        {
            WindowsDeleteString(hstring);
        }

        // 2단계: IGraphicsCaptureItemInterop으로 QueryInterface
        var factory = (IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(factoryPtr);
        Marshal.Release(factoryPtr);

        // 3단계: 모니터 핸들로 GraphicsCaptureItem 생성
        // IGraphicsCaptureItem 인터페이스 IID (클래스 GUID와 다름)
        var itemIid  = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");
        int createHr = factory.CreateForMonitor(hmonitor, ref itemIid, out var itemPtr);
        Marshal.ThrowExceptionForHR(createHr);

        var item = WinRT.MarshalInterface<GraphicsCaptureItem>.FromAbi(itemPtr);
        // FromAbi는 AddRef하므로 원본 참조 해제
        if (itemPtr != IntPtr.Zero)
            Marshal.Release(itemPtr);

        return item ?? throw new InvalidOperationException("GraphicsCaptureItem 생성 실패");
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
    public POINT(int x, int y) { X = x; Y = y; }
}
