using System.Windows.Threading;

namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// DispatcherTimer を UI タイマー抽象へ適合させる。
/// </summary>
public sealed class WpfDispatcherTimerAdapter : IUiDispatcherTimer
{
    private readonly DispatcherTimer _dispatcherTimer = new();

    /// <inheritdoc />
    public TimeSpan Interval
    {
        get => _dispatcherTimer.Interval;
        set => _dispatcherTimer.Interval = value;
    }

    /// <inheritdoc />
    public event EventHandler? Tick
    {
        add => _dispatcherTimer.Tick += value;
        remove => _dispatcherTimer.Tick -= value;
    }

    /// <inheritdoc />
    public void Start()
    {
        _dispatcherTimer.Start();
    }

    /// <inheritdoc />
    public void Stop()
    {
        _dispatcherTimer.Stop();
    }
}