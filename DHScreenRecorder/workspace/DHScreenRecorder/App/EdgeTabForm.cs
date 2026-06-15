//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: EdgeTabForm
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using System.Runtime.InteropServices;
using DHScreenRecorder.Recording;

namespace DHScreenRecorder.App;

// 화면 엣지에 항상 표시되는 탭
// - 어느 모니터의 Left / Top / Right / Bottom 엣지에도 부착 가능
// - 드래그 → 마우스 업 시 가장 가까운 엣지로 스냅 + 설정 저장
// - 클릭으로 PopupMenuForm 토글
public class EdgeTabForm : Form
{
    private readonly PopupMenuForm _popup;
    private readonly AppSettings   _settings;

    // 현재 붙어있는 엣지 (OnPaint, AlignToTab 에서 사용)
    public TabEdge CurrentEdge { get; private set; }

    // 탭의 얇은 쪽 / 긴 쪽 크기 (px)
    private const int SHORT = 24;
    private const int LONG  = 88;

    private Point _dragOrigin;    // 드래그 시작 커서 위치 (스크린 좌표)
    private Point _dragStartPos;  // 드래그 시작 폼 위치
    private bool  _isDragging;

    public EdgeTabForm(RecordingController controller, AppSettings settings)
    {
        _settings   = settings;
        CurrentEdge = settings.TabEdge;
        _popup      = new PopupMenuForm(controller, this);

        InitForm();
        ApplyEdgePosition(GetSavedMonitor(), settings.TabEdge, settings.EdgeOffset);
    }

    private void InitForm()
    {   
        AutoScaleMode   = AutoScaleMode.None;  // DPI 자동 스케일 비활성화
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar   = false;
        TopMost         = true;
        BackColor       = Color.FromArgb(38, 120, 210);
        Opacity         = 0.85;
        StartPosition   = FormStartPosition.Manual;
        Cursor          = Cursors.SizeAll;
        MinimumSize     = Size.Empty;  // Windows 최소 크기 제한 해제
        MaximumSize     = Size.Empty;  // Windows 최대 크기 제한 해제

        // CurrentEdge(= settings.TabEdge)가 이미 설정된 상태이므로
        // 엣지 방향에 맞는 정확한 초기 크기를 여기서 결정
        bool horiz = CurrentEdge == TabEdge.Top || CurrentEdge == TabEdge.Bottom;
        ClientSize = horiz ? new Size(LONG, SHORT) : new Size(SHORT, LONG);
        Program.Log($"[Tab] InitForm  Edge={CurrentEdge} ClientSize={ClientSize}");

        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp   += OnMouseUp;
    }

    // ── 위치 적용 ────────────────────────────────────────────────────────────

    // Return the monitor stored in settings, clamped to currently available screens / 설정에 저장된 모니터를 반환, 없으면 유효 범위로 클램핑
    private Screen GetSavedMonitor()
    {
        var screens = Screen.AllScreens;
        int idx     = Math.Clamp(_settings.MonitorIndex, 0, screens.Length - 1);
        return screens[idx];
    }

    // Resize the tab for the given edge direction and snap it to the monitor boundary / 탭 크기를 엣지 방향에 맞게 조정하고 해당 모니터 경계에 위치 고정
    private void ApplyEdgePosition(Screen monitor, TabEdge edge, int offset)
    {
        CurrentEdge = edge;

        bool horiz = (edge == TabEdge.Top || edge == TabEdge.Bottom);
        var  b     = monitor.Bounds;
        int  max   = horiz ? b.Width - LONG : b.Height - LONG;
        int  off   = Math.Clamp(offset, 0, Math.Max(0, max));

        // 엣지 방향에 따라 폼 크기 변경 (ClientSize = 테두리 없는 폼에서 실제 픽셀 크기)
        ClientSize = horiz ? new Size(LONG, SHORT) : new Size(SHORT, LONG);
        Program.Log($"[Tab] ApplyEdge Edge={edge} ClientSize={ClientSize}");

        Location = edge switch
        {
            TabEdge.Right  => new Point(b.Right - SHORT, b.Top  + off),
            TabEdge.Left   => new Point(b.Left,          b.Top  + off),
            TabEdge.Top    => new Point(b.Left  + off,   b.Top),
            TabEdge.Bottom => new Point(b.Left  + off,   b.Bottom - SHORT),
            _              => Location
        };

        if (_popup.Visible) _popup.AlignToTab(this);
        Invalidate();
    }

    // Show() 이후 실제 크기 확인용 로그 (Size == ClientSize for borderless form)
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Program.Log($"[Tab] OnShown   Edge={CurrentEdge} ClientSize={ClientSize} Bounds={Bounds}");
    }

    // Windows가 WM_GETMINMAXINFO로 강제하는 최소 창 크기(~136px)를 무력화
    private const int WM_GETMINMAXINFO = 0x0024;

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public Point ptReserved;
        public Point ptMaxSize;
        public Point ptMaxPosition;
        public Point ptMinTrackSize;
        public Point ptMaxTrackSize;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_GETMINMAXINFO)
        {
            var info = Marshal.PtrToStructure<MINMAXINFO>(m.LParam);
            info.ptMinTrackSize = new Point(1, 1);
            Marshal.StructureToPtr(info, m.LParam, false);
        }
        base.WndProc(ref m);
    }

    // ── 그리기 ──────────────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);

        bool horiz = (CurrentEdge == TabEdge.Top || CurrentEdge == TabEdge.Bottom);

        using var barBrush = new SolidBrush(Color.FromArgb(200, Color.White));

        if (horiz)
        {
            // Top/Bottom: 세로 바 3개 (왼쪽 1/4 영역)
            for (int i = 0; i < 3; i++)
                g.FillRectangle(barBrush, 12 + i * 6, 5, 2, SHORT - 10);
        }
        else
        {
            // Left/Right: 가로 바 3개 (위쪽 1/3 영역)
            for (int i = 0; i < 3; i++)
                g.FillRectangle(barBrush, 5, 16 + i * 6, SHORT - 10, 2);
        }

        // 팝업 상태에 따른 화살표 방향
        bool open  = _popup.Visible;
        string arrow = CurrentEdge switch
        {
            TabEdge.Right  => open ? "▶" : "◀",
            TabEdge.Left   => open ? "◀" : "▶",
            TabEdge.Top    => open ? "▲" : "▼",
            TabEdge.Bottom => open ? "▼" : "▲",
            _              => "◀"
        };

        using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
        var tf = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        RectangleF arrowRect = horiz
            ? new RectangleF(LONG / 2f + 4, 0, LONG / 2f - 8, SHORT)
            : new RectangleF(0, LONG / 2f + 4, SHORT, LONG / 2f - 8);

        g.DrawString(arrow, font, Brushes.White, arrowRect, tf);
    }

    // ── 마우스 이벤트 ────────────────────────────────────────────────────────

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragOrigin   = Cursor.Position;
        _dragStartPos = Location;
        _isDragging   = false;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        int dx = Cursor.Position.X - _dragOrigin.X;
        int dy = Cursor.Position.Y - _dragOrigin.Y;

        if (!_isDragging && (Math.Abs(dx) > 5 || Math.Abs(dy) > 5))
            _isDragging = true;

        if (_isDragging)
        {
            // 드래그 중 자유 이동
            Location = new Point(_dragStartPos.X + dx, _dragStartPos.Y + dy);
            if (_popup.Visible) _popup.AlignToTab(this);
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        if (_isDragging)
        {
            // 가장 가까운 엣지로 스냅
            var center              = new Point(Left + Width / 2, Top + Height / 2);
            var (monitor, edge, offset) = FindNearestEdge(center);

            _settings.MonitorIndex = Array.IndexOf(Screen.AllScreens, monitor);
            _settings.TabEdge      = edge;
            _settings.EdgeOffset   = offset;
            _settings.Save();

            ApplyEdgePosition(monitor, edge, offset);
        }
        else
        {
            TogglePopup();
        }
        _isDragging = false;
    }

    // ── 엣지 스냅 계산 ──────────────────────────────────────────────────────

    // Find the nearest monitor edge to the given center point and return snap parameters / 드래그 후 중심점과 가장 가까운 모니터 엣지를 찾아 스냅 파라미터 반환
    private static (Screen monitor, TabEdge edge, int offset) FindNearestEdge(Point center)
    {
        // 가장 가까운 모니터 선택
        var monitor = Screen.AllScreens
            .OrderBy(s => DistToRect(center, s.Bounds))
            .First();

        var b = monitor.Bounds;

        int dRight  = Math.Abs(center.X - b.Right);
        int dLeft   = Math.Abs(center.X - b.Left);
        int dTop    = Math.Abs(center.Y - b.Top);
        int dBottom = Math.Abs(center.Y - b.Bottom);
        int minD    = Math.Min(Math.Min(dRight, dLeft), Math.Min(dTop, dBottom));

        TabEdge edge;
        int     offset;

        if      (minD == dRight)  { edge = TabEdge.Right;  offset = center.Y - b.Top;  }
        else if (minD == dLeft)   { edge = TabEdge.Left;   offset = center.Y - b.Top;  }
        else if (minD == dTop)    { edge = TabEdge.Top;    offset = center.X - b.Left; }
        else                      { edge = TabEdge.Bottom; offset = center.X - b.Left; }

        return (monitor, edge, Math.Max(0, offset));
    }

    // Manhattan distance from point to the nearest edge of a rectangle (0 if inside) / 점에서 사각형 가장 가까운 변까지의 맨해튼 거리 (내부면 0)
    private static int DistToRect(Point p, Rectangle r)
    {
        int dx = Math.Max(0, Math.Max(r.Left - p.X, p.X - r.Right));
        int dy = Math.Max(0, Math.Max(r.Top  - p.Y, p.Y - r.Bottom));
        return dx + dy;
    }

    // ── 팝업 제어 ────────────────────────────────────────────────────────────

    // Show popup if hidden, hide it if visible / 팝업이 숨겨져 있으면 표시, 보이면 숨김 토글
    public void TogglePopup()
    {
        if (_popup.Visible) ClosePopup();
        else OpenPopup();
    }

    public void OpenPopup()
    {
        _popup.AlignToTab(this);
        _popup.Show(this);  // owner = this → 항상 탭 위에 표시
        Invalidate();
    }

    public void ClosePopup()
    {
        _popup.Hide();
        Invalidate();
    }

    // PopupMenuForm의 × 클릭 시 호출
    internal void OnPopupClosed() => Invalidate();

    // OptionsForm에서 설정 변경 후 즉시 위치/크기 반영
    // Re-snap the tab using the current settings values (called after OptionsForm saves) / 설정 저장 후 호출 — 현재 설정값으로 탭 위치를 즉시 재적용
    public void ApplyPositionFromSettings()
    {
        CurrentEdge = _settings.TabEdge;
        ApplyEdgePosition(GetSavedMonitor(), _settings.TabEdge, _settings.EdgeOffset);
    }

    // OptionsForm이 같은 settings 인스턴스를 공유하기 위해 노출
    public AppSettings Settings => _settings;

    protected override void Dispose(bool disposing)
    {
        if (disposing) _popup.Dispose();
        base.Dispose(disposing);
    }
}
