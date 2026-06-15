//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: PopupMenuForm
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using DHScreenRecorder.Capture;
using DHScreenRecorder.Encoding;
using DHScreenRecorder.Recording;

namespace DHScreenRecorder.App;

// EdgeTabForm 클릭 시 표시되는 팝업 메뉴 폼
// - × 버튼으로만 닫힘 (포커스를 잃어도 유지)
// - EdgeTabForm 왼쪽에 위치 정렬
public class PopupMenuForm : Form
{
    private readonly RecordingController _controller;
    private readonly EdgeTabForm         _edgeTab;

    private const int FormWidth = 175;
    private const int BtnHeight = 38;
    private const int BtnMargin = 6;
    private const int BtnPad    = 10;
    private const int TitleH    = 32;
    private const int StatusH   = 26;

    private Button _btnFullScreen = null!;
    private Button _btnRegion     = null!;
    private Button _btnStop       = null!;
    private Label  _lblStatus     = null!;

    public PopupMenuForm(RecordingController controller, EdgeTabForm edgeTab)
    {
        _controller = controller;
        _edgeTab    = edgeTab;
        InitForm();
        InitControls();
    }

    private void InitForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar   = false;
        TopMost         = true;
        BackColor       = Color.FromArgb(30, 30, 30);
        Opacity         = 0.95;
        Width           = FormWidth;
        Height          = TitleH + StatusH + BtnHeight * 5 + BtnMargin * 4 + BtnPad * 2;
        StartPosition   = FormStartPosition.Manual;
    }

    private void InitControls()
    {
        // ── 타이틀 바 ──────────────────────────────
        var titlePanel = new Panel
        {
            Dock      = DockStyle.None,
            Bounds    = new Rectangle(0, 0, FormWidth, TitleH),
            BackColor = Color.FromArgb(20, 20, 20)
        };

        var lblTitle = new Label
        {
            Text      = "DHScreenRecorder",
            ForeColor = Color.FromArgb(200, 200, 200),
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 8.5f),
            Bounds    = new Rectangle(BtnPad, 0, FormWidth - 40, TitleH),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var btnClose = new Button
        {
            Text      = "×",
            ForeColor = Color.FromArgb(180, 180, 180),
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 11f),
            Bounds    = new Rectangle(FormWidth - 32, 2, 28, TitleH - 4),
            Cursor    = Cursors.Hand,
            TabStop   = false
        };
        btnClose.FlatAppearance.BorderSize    = 0;
        btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 50, 50);
        btnClose.Click += (_, _) =>
        {
            Hide();
            _edgeTab.OnPopupClosed();
        };

        titlePanel.Controls.AddRange([lblTitle, btnClose]);

        // ── 상태 레이블 ────────────────────────────
        int y = TitleH + 4;
        _lblStatus = new Label
        {
            Text      = "● 대기 중",
            ForeColor = Color.FromArgb(140, 140, 140),
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 8.5f),
            Bounds    = new Rectangle(BtnPad, y, FormWidth - BtnPad * 2, StatusH),
            TextAlign = ContentAlignment.MiddleLeft
        };
        y += StatusH;

        // ── 버튼 ───────────────────────────────────
        _btnFullScreen = MakeButton("전체화면 녹화", y, Color.FromArgb(0, 120, 215));
        y += BtnHeight + BtnMargin;
        _btnRegion = MakeButton("영역 선택 녹화", y, Color.FromArgb(0, 153, 76));
        y += BtnHeight + BtnMargin;
        _btnStop   = MakeButton("■  정 지",       y, Color.FromArgb(180, 50, 50));
        y += BtnHeight + BtnMargin;
        var btnOptions = MakeButton("⚙  설 정",   y, Color.FromArgb(80, 80, 80));
        y += BtnHeight + BtnMargin;
        var btnExit    = MakeButton("종 료",       y, Color.FromArgb(60, 60, 60));

        _btnFullScreen.Click += OnFullScreenClick;
        _btnRegion.Click     += OnRegionClick;
        _btnStop.Click       += (_, _) => StopRecording();
        btnOptions.Click     += OnOptionsClick;
        btnExit.Click        += (_, _) => Application.Exit();

        Controls.AddRange([titlePanel, _lblStatus,
                           _btnFullScreen, _btnRegion, _btnStop, btnOptions, btnExit]);
        UpdateButtonState();
    }

    private Button MakeButton(string text, int y, Color backColor) => new()
    {
        Text      = text,
        ForeColor = Color.White,
        BackColor = backColor,
        FlatStyle = FlatStyle.Flat,
        Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
        Bounds    = new Rectangle(BtnPad, y, FormWidth - BtnPad * 2, BtnHeight),
        Cursor    = Cursors.Hand
    };

    // EdgeTabForm의 현재 엣지에 따라 팝업 위치 정렬
    // Position popup adjacent to the tab's current edge, clamped inside the working area / 탭의 현재 엣지 방향에 맞게 팝업을 인접 배치, 작업 영역 밖으로 나가지 않게 클램핑
    public void AlignToTab(EdgeTabForm tab)
    {
        // 탭이 속한 모니터의 작업 영역(TaskBar 제외)
        var wa = Screen.FromPoint(new Point(tab.Left + tab.Width / 2, tab.Top + tab.Height / 2))
                       .WorkingArea;

        int x, y;
        switch (tab.CurrentEdge)
        {
            case TabEdge.Right:
                x = tab.Left - Width - 4;
                y = Math.Clamp(tab.Top, wa.Top, wa.Bottom - Height);
                break;
            case TabEdge.Left:
                x = tab.Right + 4;
                y = Math.Clamp(tab.Top, wa.Top, wa.Bottom - Height);
                break;
            case TabEdge.Top:
                x = Math.Clamp(tab.Left, wa.Left, wa.Right - Width);
                y = tab.Bottom + 4;
                break;
            case TabEdge.Bottom:
                x = Math.Clamp(tab.Left, wa.Left, wa.Right - Width);
                y = tab.Top - Height - 4;
                break;
            default:
                x = tab.Left - Width - 4;
                y = tab.Top;
                break;
        }
        Location = new Point(x, y);
    }

    // EdgeTabForm이 위치한 모니터를 기준으로 캡처 대상 결정
    // Identify which monitor the EdgeTab currently sits on by its center point / EdgeTab 중심점 기준으로 현재 위치한 모니터를 반환
    private Screen GetTabMonitor() =>
        Screen.FromPoint(new Point(_edgeTab.Left + _edgeTab.Width / 2,
                                   _edgeTab.Top  + _edgeTab.Height / 2));

    private void OnFullScreenClick(object? sender, EventArgs e)
    {
        StartRecording(CaptureRegion.FromScreen(GetTabMonitor()));
    }

    private void OnRegionClick(object? sender, EventArgs e)
    {
        Hide();
        _edgeTab.OnPopupClosed();

        // 영역 선택 오버레이를 EdgeTab이 있는 모니터에 표시
        using var selector = new RegionSelectorForm(GetTabMonitor());
        if (selector.ShowDialog() == DialogResult.OK && selector.SelectedRegion is { } region)
            StartRecording(region);
    }

    private void OnOptionsClick(object? sender, EventArgs e)
    {
        using var dlg = new OptionsForm(_edgeTab.Settings, _edgeTab);
        dlg.ShowDialog();
    }

    // Show save dialog then start the recording controller with the resolved settings / 저장 경로 선택 후 설정값으로 RecordingController 녹화 시작
    private void StartRecording(CaptureRegion region)
    {
        var appSettings = _edgeTab.Settings;

        string ext = appSettings.VideoFormat switch
        {
            VideoFormat.Mp4  => "mp4",
            VideoFormat.Avi  => "avi",
            VideoFormat.Mpeg => "mpeg",
            _                => "mp4"
        };
        string filter = appSettings.VideoFormat switch
        {
            VideoFormat.Mp4  => "MP4 파일 (*.mp4)|*.mp4",
            VideoFormat.Avi  => "AVI 파일 (*.avi)|*.avi",
            VideoFormat.Mpeg => "MPEG 파일 (*.mpeg)|*.mpeg",
            _                => "MP4 파일 (*.mp4)|*.mp4"
        };

        using var dlg = new SaveFileDialog
        {
            Title            = "저장 위치 선택",
            Filter           = filter,
            DefaultExt       = ext,
            FileName         = $"record_{DateTime.Now:yyyyMMdd_HHmmss}",
            InitialDirectory = Directory.Exists(appSettings.DefaultOutputPath)
                                   ? appSettings.DefaultOutputPath
                                   : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        var path        = Path.ChangeExtension(dlg.FileName, null);
        var vidSettings = new VideoSettings(
            appSettings.VideoFormat,
            appSettings.Fps,
            region.Width,
            region.Height,
            appSettings.VideoQuality);

        _controller.Start(region, path, vidSettings);
        UpdateButtonState();
    }

    // Stop the recording controller and refresh button states / 녹화 컨트롤러 중지 후 버튼 상태 갱신
    private void StopRecording()
    {
        _controller.Stop();
        UpdateButtonState();
    }

    // Enable/disable buttons and update status label to reflect current recording state / 녹화 상태에 따라 버튼 활성화/비활성화 및 상태 레이블 갱신
    private void UpdateButtonState()
    {
        bool rec           = _controller.IsRecording;
        _btnFullScreen.Enabled = !rec;
        _btnRegion.Enabled     = !rec;
        _btnStop.Enabled       = rec;
        _lblStatus.Text        = rec ? "● 녹화 중..." : "● 대기 중";
        _lblStatus.ForeColor   = rec
            ? Color.FromArgb(255, 80, 80)
            : Color.FromArgb(140, 140, 140);
    }
}
