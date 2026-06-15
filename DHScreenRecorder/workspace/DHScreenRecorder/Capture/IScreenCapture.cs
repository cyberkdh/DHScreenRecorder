//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: IScreenCapture
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
namespace DHScreenRecorder.Capture;

public interface IScreenCapture
{
    void Start(CaptureRegion region);
    void Stop();
    event EventHandler<byte[]> FrameCaptured;
}
