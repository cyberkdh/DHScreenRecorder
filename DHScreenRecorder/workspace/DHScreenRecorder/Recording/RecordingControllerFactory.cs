//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: RecordingControllerFactory
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using DHScreenRecorder.Capture;
using DHScreenRecorder.Encoding;

namespace DHScreenRecorder.Recording;

public static class RecordingControllerFactory
{
    public static RecordingController Create()
    {
        var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ffmpeg", "ffmpeg.exe");
        var capture = new GraphicsCaptureService();
        var encoder = new FfmpegEncoder(ffmpegPath);
        return new RecordingController(capture, encoder);
    }
}
