using MarketMonitor.Composition;

namespace MarketMonitorTest;

/// <summary>
/// MainWindowLifecycleService の初期化委譲を検証するテストクラス。
/// </summary>
public sealed class MainWindowLifecycleServiceTest
{
    /// <summary>
    /// InitializeAsync が ViewModel の初期化処理を呼び出すことをテスト。
    /// 期待値: InitializeAsync が 1 回呼ばれる。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_DelegatesToViewModel()
    {
        var viewModel = new FakeMainWindowViewModel();

        await MainWindowLifecycleService.InitializeAsync(viewModel);

        Assert.True(viewModel.InitializeCalled);
    }

    private sealed class FakeMainWindowViewModel : IMainWindowViewModel
    {
        public bool InitializeCalled { get; private set; }

        public Task InitializeAsync()
        {
            InitializeCalled = true;
            return Task.CompletedTask;
        }
    }
}