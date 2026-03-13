using System.Windows.Forms;

namespace BatteryManager.Services;

public sealed class NotificationService
{
    public void Show(NotifyIcon trayIcon, string title, string body, ToolTipIcon icon = ToolTipIcon.Info)
    {
        trayIcon.BalloonTipTitle = title;
        trayIcon.BalloonTipText = body;
        trayIcon.BalloonTipIcon = icon;
        trayIcon.ShowBalloonTip(4000);
    }
}
