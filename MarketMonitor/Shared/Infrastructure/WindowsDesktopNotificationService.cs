using System.Drawing;
using System.Windows.Forms;

namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// Windows のバルーン通知を表示する。
/// </summary>
public sealed class WindowsDesktopNotificationService : IDesktopNotificationService, IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    /// <summary>
    /// サービスを初期化する。
    /// </summary>
    public WindowsDesktopNotificationService()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true,
            Text = "Tokyo Market Technical"
        };
    }

    /// <inheritdoc />
    public void ShowNotification(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(5000);
    }

    /// <summary>
    /// リソースを解放する。
    /// </summary>
    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}