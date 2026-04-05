using MarketMonitor.Composition;

namespace MarketMonitorTest;

/// <summary>
/// AppLifecycleService の起動終了制御を検証するテストクラス。
/// </summary>
public sealed class AppLifecycleServiceTest
{
    /// <summary>
    /// Start がメインウィンドウを表示して返すことをテスト。
    /// 期待値: Show が 1 回呼ばれる。
    /// </summary>
    [Fact]
    public void Start_ShowsMainWindow()
    {
        var window = new FakeMainWindowShell();

        var result = AppLifecycleService.Start(() => window);

        Assert.Same(window, result);
        Assert.True(window.ShowCalled);
    }

    /// <summary>
    /// Stop が DataContext の IDisposable を破棄することをテスト。
    /// 期待値: Dispose が 1 回呼ばれる。
    /// </summary>
    [Fact]
    public void Stop_DisposesDataContext_WhenDisposable()
    {
        var disposable = new FakeDisposable();
        var window = new FakeMainWindowShell { DataContext = disposable };

        AppLifecycleService.Stop(window);

        Assert.True(disposable.IsDisposed);
    }

    private sealed class FakeMainWindowShell : IMainWindowShell
    {
        public object? DataContext { get; set; }

        public bool ShowCalled { get; private set; }

        public void Show()
        {
            ShowCalled = true;
        }
    }

    private sealed class FakeDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}