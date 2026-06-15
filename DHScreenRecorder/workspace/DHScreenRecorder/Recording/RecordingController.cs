//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: RecordingController
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using DHScreenRecorder.Capture;
using DHScreenRecorder.Encoding;

namespace DHScreenRecorder.Recording;

public class RecordingController : IDisposable
{
    private readonly IScreenCapture _capture;
    private readonly FfmpegEncoder _encoder;
    private bool _isRecording;

    public bool IsRecording => _isRecording;

    public RecordingController(IScreenCapture capture, FfmpegEncoder encoder)
    {
        _capture = capture;
        _encoder = encoder;
        _capture.FrameCaptured += OnFrameCaptured;
    }

    // Begin encoding then start capture so frames flow immediately / 인코더 먼저 기동 후 캡처 시작해 프레임이 즉시 흐르도록 함
    public void Start(CaptureRegion region, string outputPath, VideoSettings settings)
    {
        if (_isRecording) return;
        _encoder.Start(outputPath, settings);
        _capture.Start(region);
        _isRecording = true;
    }

    // Stop capture first, then finalize the output file via encoder / 캡처를 먼저 중지한 뒤 인코더로 출력 파일 최종화
    public void Stop()
    {
        if (!_isRecording) return;
        _capture.Stop();
        _encoder.Stop();
        _isRecording = false;
    }

    // Forward each captured frame directly to the encoder without buffering / 캡처된 프레임을 버퍼링 없이 즉시 인코더로 전달
    private void OnFrameCaptured(object? sender, byte[] frame)
    {
        _encoder.WriteFrame(frame);
    }

    public void Dispose()
    {
        Stop();
        _encoder.Dispose();
    }
}
