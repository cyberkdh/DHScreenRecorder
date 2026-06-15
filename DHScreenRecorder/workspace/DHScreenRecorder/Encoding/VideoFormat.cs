//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: VideoFormat
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
namespace DHScreenRecorder.Encoding;

public enum VideoFormat
{
    Mp4,   // H.264 (권장)
    Avi,   // MPEG-4
    Mpeg   // MPEG-1 (레거시)
}

public enum VideoQuality
{
    High,    // CRF 18 / qscale 2
    Medium,  // CRF 23 / qscale 5
    Low      // CRF 28 / qscale 10
}

public record VideoSettings(
    VideoFormat  Format,
    int          Fps,
    int          Width,
    int          Height,
    VideoQuality Quality = VideoQuality.Medium
);
