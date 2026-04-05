namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// UI スレッド上で動作するタイマーの抽象を表す。
/// </summary>
public interface IUiDispatcherTimer
{
    /// <summary>
    /// タイマーの発火間隔。
    /// </summary>
    TimeSpan Interval { get; set; }

    /// <summary>
    /// タイマー発火時のイベント。
    /// </summary>
    event EventHandler? Tick;

    /// <summary>
    /// タイマーを開始する。
    /// </summary>
    void Start();

    /// <summary>
    /// タイマーを停止する。
    /// </summary>
    void Stop();
}