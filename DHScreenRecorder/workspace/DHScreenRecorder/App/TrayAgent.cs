//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: DHScreenRecorder
//	Author			: CYBERKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com), AI(Claude)
//	Module			: TrayAgent
//	History			:
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////
using DHScreenRecorder.Recording;

namespace DHScreenRecorder.App;

// 시스템 트레이(System Tray) 에이전트 — 앱의 메인 컨텍스트
// 창 없이 백그라운드로 실행, EdgeTabForm과 트레이 아이콘으로 제어
public class TrayAgent : ApplicationContext
{
    private readonly NotifyIcon          _trayIcon;
    private readonly EdgeTabForm         _edgeTab;
    private readonly RecordingController _controller;

    public TrayAgent()
    {
        _controller = RecordingControllerFactory.Create();

        var settings = AppSettings.Load();
        _edgeTab = new EdgeTabForm(_controller, settings);

        _trayIcon = new NotifyIcon
        {
            Icon             = SystemIcons.Application,
            Visible          = true,
            Text             = "DHScreenRecorder",
            ContextMenuStrip = BuildTrayMenu()
        };

        _trayIcon.DoubleClick += (_, _) => _edgeTab.TogglePopup();

        _edgeTab.Show();
    }

    // Build the right-click context menu for the system tray icon / 시스템 트레이 아이콘의 오른쪽 클릭 컨텍스트 메뉴 생성
    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("메뉴 열기", null, (_, _) => _edgeTab.OpenPopup());
        menu.Items.Add("설정",      null, (_, _) => OpenOptions());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료",      null, (_, _) => ExitApp());
        return menu;
    }

    // Open OptionsForm as a modal dialog sharing the EdgeTab's settings instance / OptionsForm을 모달로 열어 EdgeTab 설정 인스턴스 공유
    private void OpenOptions()
    {
        using var dlg = new OptionsForm(_edgeTab.Settings, _edgeTab);
        dlg.ShowDialog();
    }

    // Stop any active recording and terminate the application cleanly / 진행 중인 녹화를 중단하고 애플리케이션을 정상 종료
    private void ExitApp()
    {
        _controller.Stop();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _edgeTab.Dispose();
            _controller.Dispose();
        }
        base.Dispose(disposing);
    }
}
