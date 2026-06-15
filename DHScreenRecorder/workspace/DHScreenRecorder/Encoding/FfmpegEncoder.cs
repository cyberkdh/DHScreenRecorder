//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: FfmpegEncoder
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using System.Diagnostics;

namespace DHScreenRecorder.Encoding;

public class FfmpegEncoder : IDisposable
{
    private Process? _process;
    private readonly string _ffmpegPath;

    public FfmpegEncoder(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }

    // Launch FFmpeg process with stdin/stderr redirected and begin reading stderr asynchronously / FFmpeg 프로세스 시작, stdin·stderr 리디렉션 후 stderr 비동기 소비 개시
    public void Start(string outputPath, VideoSettings settings)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName              = _ffmpegPath,
                Arguments             = BuildArguments(outputPath, settings),
                UseShellExecute       = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,  // stderr 버퍼 블록 방지
                CreateNoWindow        = true
            }
        };
        _process.ErrorDataReceived += (_, _) => { }; // stderr 소비 (버리기)
        _process.Start();
        _process.BeginErrorReadLine();
    }

    // Push one raw BGRA frame into FFmpeg's stdin pipe / BGRA 원본 프레임 1장을 FFmpeg stdin으로 전송
    public void WriteFrame(byte[] frameData)
    {
        if (_process is null || _process.HasExited) return;
        _process.StandardInput.BaseStream.Write(frameData, 0, frameData.Length);
    }

    // Send EOF to stdin so FFmpeg finalizes the file (moov atom), then wait for exit / stdin EOF 전송으로 FFmpeg가 파일을 완성하도록 한 뒤 종료 대기
    public void Stop()
    {
        if (_process is null) return;
        try
        {
            _process.StandardInput.BaseStream.Close(); // EOF → ffmpeg 파일 종료
            _process.WaitForExit(10_000);
        }
        catch { }
    }

    // Build the FFmpeg command-line for the selected format and quality / 선택된 포맷·화질에 맞는 FFmpeg 명령행 인수 생성
    private static string BuildArguments(string outputPath, VideoSettings settings)
    {
        // 짝수 해상도 강제 (H.264/mpeg4 요구사항)
        int w = settings.Width  & ~1;
        int h = settings.Height & ~1;

        string codec, ext, qualityArgs;
        switch (settings.Format)
        {
            case VideoFormat.Mp4:
                codec = "libx264";
                ext   = "mp4";
                int crf = settings.Quality switch
                {
                    VideoQuality.High   => 18,
                    VideoQuality.Medium => 23,
                    VideoQuality.Low    => 28,
                    _                   => 23
                };
                qualityArgs = $"-crf {crf} -preset fast";
                break;
            case VideoFormat.Avi:
                codec = "mpeg4";
                ext   = "avi";
                int qs = settings.Quality switch
                {
                    VideoQuality.High   => 2,
                    VideoQuality.Medium => 5,
                    VideoQuality.Low    => 10,
                    _                   => 5
                };
                qualityArgs = $"-qscale:v {qs}";
                break;
            case VideoFormat.Mpeg:
                codec       = "mpeg1video";
                ext         = "mpeg";
                qualityArgs = "";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return $"-y -loglevel quiet " +
               $"-f rawvideo -pix_fmt bgra -s {w}x{h} -r {settings.Fps} -i pipe:0 " +
               $"-vcodec {codec} {qualityArgs} -pix_fmt yuv420p \"{outputPath}.{ext}\"";
    }

    public void Dispose()
    {
        Stop();
        _process?.Dispose();
    }
}
