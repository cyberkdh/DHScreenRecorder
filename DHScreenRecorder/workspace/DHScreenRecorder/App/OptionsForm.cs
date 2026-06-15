//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: OptionsForm
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using DHScreenRecorder.Encoding;

namespace DHScreenRecorder.App;

// 설정 대화 상자
// - GroupBox + Transparent 조합은 WinForms 렌더링 버그 발생
//   → GroupBox 제거, 섹션 헤더(Label) + 구분선(Panel) 방식 사용
public class OptionsForm : Form
{
    private readonly AppSettings _settings;
    private readonly EdgeTabForm _edgeTab;

    private RadioButton   _rbMp4    = null!;
    private RadioButton   _rbAvi    = null!;
    private RadioButton   _rbMpeg   = null!;
    private RadioButton   _rbHigh   = null!;
    private RadioButton   _rbMed    = null!;
    private RadioButton   _rbLow    = null!;
    private NumericUpDown _nudFps   = null!;
    private TextBox       _txtPath  = null!;
    private ComboBox      _cmbMonitor = null!;
    private ComboBox      _cmbEdge    = null!;
    private NumericUpDown _nudOffset  = null!;

    private const int FormW = 430;
    private static readonly Color BgColor  = Color.FromArgb(45,  45,  45);
    private static readonly Color FgColor  = Color.FromArgb(215, 215, 215);
    private static readonly Color FgHint   = Color.FromArgb(140, 140, 140);
    private static readonly Color CtrlBg   = Color.FromArgb(62,  62,  62);
    private static readonly Color SepColor = Color.FromArgb(75,  75,  75);

    public OptionsForm(AppSettings settings, EdgeTabForm edgeTab)
    {
        _settings = settings;
        _edgeTab  = edgeTab;
        AutoScaleMode = AutoScaleMode.None;
        InitForm();
        InitControls();
        LoadFromSettings();
    }

    private void InitForm()
    {
        Text            = "DHScreenRecorder 설정";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.CenterScreen;
        ClientSize      = new Size(FormW, 440);
        BackColor       = BgColor;
        ForeColor       = FgColor;
        Font            = new Font("Segoe UI", 9f);
    }

    private void InitControls()
    {
        var all = new List<Control>();

        // ── 영상 설정 ────────────────────────────────────────────────
        all.Add(SectionHeader("영상 설정", 10));
        all.Add(Separator(32));

        // 형식/화질을 별도 Panel로 분리 — 같은 Form에 직접 추가 시 단일 그룹으로 묶임
        all.Add(FieldLabel("형식:", 50, 16));
        var pnlFormat = RadioPanel(46, 72, 342);
        _rbMp4  = PanelRadio("MP4 (H.264)",  2,   0, 122);
        _rbAvi  = PanelRadio("AVI (MPEG-4)", 2, 126, 122);
        _rbMpeg = PanelRadio("MPEG",         2, 252, 72);
        pnlFormat.Controls.AddRange(new Control[] { _rbMp4, _rbAvi, _rbMpeg });
        all.Add(pnlFormat);

        all.Add(FieldLabel("화질:", 82, 16));
        var pnlQuality = RadioPanel(78, 72, 270);
        _rbHigh = PanelRadio("고화질", 2,   0, 82);
        _rbMed  = PanelRadio("보통",   2,  88, 62);
        _rbLow  = PanelRadio("저화질", 2, 156, 82);
        pnlQuality.Controls.AddRange(new Control[] { _rbHigh, _rbMed, _rbLow });
        all.Add(pnlQuality);

        var hint = Hint("(고: CRF 18 / 보통: CRF 23 / 저: CRF 28)", 104, 72);
        all.Add(hint);

        all.Add(FieldLabel("FPS:", 130, 16));
        _nudFps = Nud(1, 120, 30, 128, 72, 72); all.Add(_nudFps);
        all.Add(FieldLabel("fps  (1 ~ 120)", 130, 152));

        // ── 기본 저장 경로 ───────────────────────────────────────────
        all.Add(Separator(162));
        all.Add(SectionHeader("기본 저장 경로", 168));
        all.Add(Separator(190));

        _txtPath = new TextBox
        {
            Bounds      = new Rectangle(16, 202, 305, 24),
            BackColor   = CtrlBg,
            ForeColor   = FgColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        all.Add(_txtPath);

        var btnBrowse = DarkButton("찾아보기...", 201, 326, 88);
        btnBrowse.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog
            {
                Description          = "기본 저장 경로 선택",
                SelectedPath         = _txtPath.Text,
                UseDescriptionForTitle = true
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                _txtPath.Text = dlg.SelectedPath;
        };
        all.Add(btnBrowse);

        // ── 탭 위치 ──────────────────────────────────────────────────
        all.Add(Separator(240));
        all.Add(SectionHeader("탭 위치", 246));
        all.Add(Separator(268));

        all.Add(FieldLabel("모니터:", 284, 16));
        _cmbMonitor = Combo(282, 72, 340);
        foreach (var s in Screen.AllScreens)
            _cmbMonitor.Items.Add(s.DeviceName.Replace("\\\\.", "").TrimStart('\\'));
        all.Add(_cmbMonitor);

        all.Add(FieldLabel("엣지:", 316, 16));
        _cmbEdge = Combo(314, 72, 160);
        _cmbEdge.Items.AddRange(new object[] { "Right (오른쪽)", "Left (왼쪽)", "Top (위)", "Bottom (아래)" });
        all.Add(_cmbEdge);

        all.Add(FieldLabel("오프셋:", 348, 16));
        _nudOffset = Nud(0, 9999, 100, 346, 72, 84); all.Add(_nudOffset);
        all.Add(FieldLabel("px", 348, 162));

        // ── 버튼 ─────────────────────────────────────────────────────
        all.Add(Separator(390));

        var btnOk = new Button
        {
            Text         = "확인",
            Bounds       = new Rectangle(FormW - 190, 400, 84, 28),
            FlatStyle    = FlatStyle.Flat,
            BackColor    = Color.FromArgb(0, 120, 215),
            ForeColor    = Color.White,
            DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += OnOkClick;

        var btnCancel = new Button
        {
            Text         = "취소",
            Bounds       = new Rectangle(FormW - 100, 400, 84, 28),
            FlatStyle    = FlatStyle.Flat,
            BackColor    = Color.FromArgb(70, 70, 70),
            ForeColor    = Color.White,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        all.Add(btnOk);
        all.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        Controls.AddRange(all.ToArray());
    }

    private void LoadFromSettings()
    {
        _rbMp4.Checked  = _settings.VideoFormat == VideoFormat.Mp4;
        _rbAvi.Checked  = _settings.VideoFormat == VideoFormat.Avi;
        _rbMpeg.Checked = _settings.VideoFormat == VideoFormat.Mpeg;
        if (!_rbMp4.Checked && !_rbAvi.Checked && !_rbMpeg.Checked) _rbMp4.Checked = true;

        _rbHigh.Checked = _settings.VideoQuality == VideoQuality.High;
        _rbMed.Checked  = _settings.VideoQuality == VideoQuality.Medium;
        _rbLow.Checked  = _settings.VideoQuality == VideoQuality.Low;
        if (!_rbHigh.Checked && !_rbMed.Checked && !_rbLow.Checked) _rbMed.Checked = true;

        _nudFps.Value = Math.Clamp(_settings.Fps, 1, 120);

        _txtPath.Text = Directory.Exists(_settings.DefaultOutputPath)
            ? _settings.DefaultOutputPath
            : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        if (_cmbMonitor.Items.Count > 0)
            _cmbMonitor.SelectedIndex = Math.Clamp(_settings.MonitorIndex, 0, _cmbMonitor.Items.Count - 1);

        _cmbEdge.SelectedIndex = Math.Clamp((int)_settings.TabEdge, 0, 3);

        _nudOffset.Value = Math.Clamp(_settings.EdgeOffset, 0, 9999);
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        _settings.VideoFormat  = _rbMp4.Checked  ? VideoFormat.Mp4  :
                                 _rbAvi.Checked  ? VideoFormat.Avi  : VideoFormat.Mpeg;
        _settings.VideoQuality = _rbHigh.Checked ? VideoQuality.High :
                                 _rbMed.Checked  ? VideoQuality.Medium : VideoQuality.Low;
        _settings.Fps               = (int)_nudFps.Value;
        _settings.DefaultOutputPath = _txtPath.Text;
        _settings.MonitorIndex      = _cmbMonitor.SelectedIndex;
        _settings.TabEdge           = (TabEdge)_cmbEdge.SelectedIndex;
        _settings.EdgeOffset        = (int)_nudOffset.Value;
        _settings.Save();

        _edgeTab.ApplyPositionFromSettings();
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────

    private Label SectionHeader(string text, int y) => new()
    {
        Text      = text,
        Bounds    = new Rectangle(16, y, FormW - 32, 18),
        ForeColor = Color.FromArgb(100, 170, 240),
        BackColor = BgColor,
        Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private Panel Separator(int y) => new()
    {
        Bounds    = new Rectangle(16, y, FormW - 32, 1),
        BackColor = SepColor
    };

    private Label FieldLabel(string text, int y, int x) => new()
    {
        Text      = text,
        Location  = new Point(x, y + 5),
        AutoSize  = true,   // 텍스트 폭에 맞게 자동 - 고정 Width=130이 인접 컨트롤 덮는 문제 해결
        ForeColor = FgColor,
        BackColor = BgColor
    };

    private Label Hint(string text, int y, int x) => new()
    {
        Text      = text,
        Bounds    = new Rectangle(x, y, 330, 18),
        ForeColor = FgHint,
        BackColor = BgColor,
        Font      = new Font("Segoe UI", 8f)
    };

    // Panel 내부용 RadioButton (좌표는 패널 기준)
    private RadioButton PanelRadio(string text, int y, int x, int w) => new()
    {
        Text      = text,
        Bounds    = new Rectangle(x, y, w, 22),
        ForeColor = FgColor,
        BackColor = BgColor
    };

    // RadioButton 그룹 컨테이너 — 별도 Panel로 묶어야 독립 그룹으로 인식
    private Panel RadioPanel(int y, int x, int w) => new()
    {
        Bounds    = new Rectangle(x, y, w, 26),
        BackColor = BgColor
    };

    private NumericUpDown Nud(int min, int max, int def, int y, int x, int w) => new()
    {
        Minimum   = min,
        Maximum   = max,
        Value     = def,
        Bounds    = new Rectangle(x, y, w, 24),
        BackColor = CtrlBg,
        ForeColor = FgColor
    };

    private ComboBox Combo(int y, int x, int w) => new()
    {
        Bounds        = new Rectangle(x, y, w, 24),
        DropDownStyle = ComboBoxStyle.DropDownList,
        BackColor     = CtrlBg,
        ForeColor     = FgColor,
        FlatStyle     = FlatStyle.Flat
    };

    private Button DarkButton(string text, int y, int x, int w)
    {
        var b = new Button
        {
            Text      = text,
            Bounds    = new Rectangle(x, y, w, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(70, 70, 70),
            ForeColor = FgColor
        };
        b.FlatAppearance.BorderColor = SepColor;
        return b;
    }
}
