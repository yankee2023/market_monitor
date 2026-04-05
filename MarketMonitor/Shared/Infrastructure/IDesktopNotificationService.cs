namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// デスクトップ通知を表示する抽象を表す。
/// </summary>
public interface IDesktopNotificationService
{
    /// <summary>
    /// 通知を表示する。
    /// </summary>
    void ShowNotification(string title, string message);
}