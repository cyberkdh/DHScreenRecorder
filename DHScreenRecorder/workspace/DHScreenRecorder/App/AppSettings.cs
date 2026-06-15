//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: AppSettings
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using System.Text.Json;
using DHScreenRecorder.Encoding;

namespace DHScreenRecorder.App;

public enum TabEdge { Right, Left, Top, Bottom }

public class AppSettings
{
    // ── 탭 위치 ─────────────────────────────────────────
    public int     MonitorIndex { get; set; } = 0;
    public TabEdge TabEdge      { get; set; } = TabEdge.Right;
    public int     EdgeOffset   { get; set; } = 100;

    // ── 영상 설정 ────────────────────────────────────────
    public VideoFormat  VideoFormat  { get; set; } = VideoFormat.Mp4;
    public VideoQuality VideoQuality { get; set; } = VideoQuality.Medium;
    public int          Fps          { get; set; } = 30;
    public string       DefaultOutputPath { get; set; } =
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    private static string FilePath =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    // Deserialize settings.json; return defaults if file is missing or corrupt / settings.json 역직렬화, 없거나 손상 시 기본값 반환
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath))
                       ?? new AppSettings();
        }
        catch { }
        return new AppSettings();
    }

    // Serialize current settings to settings.json with indented formatting / 현재 설정을 들여쓰기 JSON으로 settings.json에 저장
    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
