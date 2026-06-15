//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: RegionSelectorForm
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using DHScreenRecorder.Capture;

namespace DHScreenRecorder.App;

// 화면 영역(Region) 선택을 위한 투명 오버레이(Overlay) 폼
// - 지정 모니터 전체를 덮어 표시 (멀티 모니터 지원)
// - 마우스 드래그로 녹화 영역 지정, ESC로 취소
// - SelectedRegion 좌표는 가상 데스크톱 기준 (모니터 원점 + 클라이언트 좌표)
public class RegionSelectorForm : Form
{
    private readonly Screen _monitor;

    private Point     _startPoint;
    private Rectangle _selectedRect;
    private bool      _isDragging;

    public CaptureRegion? SelectedRegion { get; private set; }

    public RegionSelectorForm(Screen monitor)
    {
        _monitor = monitor;

        FormBorderStyle = FormBorderStyle.None;
        BackColor       = Color.Black;
        Opacity         = 0.35;
        TopMost         = true;
        Cursor          = Cursors.Cross;
        ShowInTaskbar   = false;
        DoubleBuffered  = true;
        StartPosition   = FormStartPosition.Manual;

		// Maximized 대신 명시적 Bounds — 지정 모니터를 정확히 덮음
		Bounds = monitor.Bounds;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _startPoint   = e.Location;
        _isDragging   = true;
        _selectedRect = Rectangle.Empty;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_isDragging) return;
        _selectedRect = MakeRect(_startPoint, e.Location);
        Invalidate();
    }

    // Finalize selection; convert client coords to virtual-desktop coords and set SelectedRegion / 선택 완료 — 클라이언트 좌표를 가상 데스크톱 좌표로 변환해 SelectedRegion에 저장
    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!_isDragging || e.Button != MouseButtons.Left) return;
        _isDragging   = false;
        _selectedRect = MakeRect(_startPoint, e.Location);

        if (_selectedRect.Width > 10 && _selectedRect.Height > 10)
        {
            // 클라이언트 좌표(모니터 로컬) → 가상 데스크톱 좌표로 변환
            SelectedRegion = new CaptureRegion(
                _monitor.Bounds.Left + _selectedRect.X,
                _monitor.Bounds.Top  + _selectedRect.Y,
                _selectedRect.Width,
                _selectedRect.Height);
            DialogResult = DialogResult.OK;
        }
        else
        {
            DialogResult = DialogResult.Cancel;
        }
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_selectedRect.IsEmpty) return;

        using var fill = new SolidBrush(Color.FromArgb(60, Color.White));
        e.Graphics.FillRectangle(fill, _selectedRect);

        using var pen = new Pen(Color.FromArgb(255, 80, 200, 80), 2);
        e.Graphics.DrawRectangle(pen, _selectedRect);

        var label = $"{_selectedRect.Width} × {_selectedRect.Height}";
        using var font  = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        e.Graphics.DrawString(label, font, brush, _selectedRect.X + 4, _selectedRect.Y + 4);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    // Build a normalized Rectangle from two arbitrary corner points / 두 임의 꼭짓점으로 정규화된 Rectangle 생성 (음수 크기 방지)
    private static Rectangle MakeRect(Point a, Point b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
            Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
