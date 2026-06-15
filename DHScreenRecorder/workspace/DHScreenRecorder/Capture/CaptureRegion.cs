//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: CaptureRegion
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
namespace DHScreenRecorder.Capture;

public record CaptureRegion(int X, int Y, int Width, int Height)
{
    // 주 모니터 전체 화면 (하위 호환용)
    public static CaptureRegion FullScreen() =>
        FromScreen(Screen.PrimaryScreen!);

    // 지정 모니터 전체 화면
    // X, Y 는 가상 데스크톱 기준 좌표 (GraphicsCaptureService 에서 원점 보정)
    public static CaptureRegion FromScreen(Screen screen)
    {
        var b = screen.Bounds;
        return new CaptureRegion(b.Left, b.Top, b.Width, b.Height);
    }
}
