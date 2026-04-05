using MarketMonitor.Models;
using MarketMonitor.Services;
using MarketMonitor.ViewModels;

namespace MarketMonitorTest;

public class MainViewModelTest
{
    /// <summary>
    /// 初期化時に取得した値が表示用プロパティへ反映されることをテスト。
    /// 期待値: 為替・株価表示が取得値になる。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_ReflectsSnapshotValues()
    {
        // Arrange
        var fakeApi = new FakeApiService(new MarketSnapshot
        {
            Symbol = "IBM",
            ExchangeRate = 150.1234m,
            StockPrice = 210.50m,
            ExchangeUpdatedAt = DateTimeOffset.Now,
            StockUpdatedAt = DateTimeOffset.Now
        });
        var fakeLogger = new FakeLogger();
        var viewModel = new MainViewModel(fakeApi, fakeLogger);

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.Equal("150.1234", viewModel.ExchangeRateDisplay);
        Assert.Equal("210.50", viewModel.StockPriceDisplay);
        Assert.StartsWith("更新完了:", viewModel.StatusMessage);
    }

    /// <summary>
    /// 更新間隔に10秒未満を設定した場合に10秒へ補正されることをテスト。
    /// 期待値: AutoUpdateIntervalSeconds が 10。
    /// </summary>
    [Fact]
    public void AutoUpdateIntervalSeconds_ClampsToTen()
    {
        // Arrange
        var viewModel = new MainViewModel(new FakeApiService(), new FakeLogger());

        // Act
        viewModel.AutoUpdateIntervalSeconds = 1;

        // Assert
        Assert.Equal(10, viewModel.AutoUpdateIntervalSeconds);
    }

    /// <summary>
    /// 自動更新切替コマンド実行で有効/無効が切り替わることをテスト。
    /// 期待値: true から false へ遷移する。
    /// </summary>
    [Fact]
    public void ToggleAutoUpdateCommand_TogglesEnabledState()
    {
        // Arrange
        var viewModel = new MainViewModel(new FakeApiService(), new FakeLogger());

        // Act
        viewModel.ToggleAutoUpdateCommand.Execute(null);
        var first = viewModel.IsAutoUpdateEnabled;

        viewModel.ToggleAutoUpdateCommand.Execute(null);
        var second = viewModel.IsAutoUpdateEnabled;

        // Assert
        Assert.True(first);
        Assert.False(second);
    }

    /// <summary>
    /// API例外発生時に失敗メッセージが設定されることをテスト。
    /// 期待値: StatusMessage が "更新失敗:" で始まる。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SetsFailureMessage_WhenApiThrows()
    {
        // Arrange
        var viewModel = new MainViewModel(new ThrowingApiService(), new FakeLogger());

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.StartsWith("更新失敗:", viewModel.StatusMessage);
    }

    /// <summary>
    /// 日本株シンボル取得時に日本株表示フラグが有効になることをテスト。
    /// 期待値: IsJapaneseStock が true。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SetsJapaneseStockFlag_WhenTokyoSymbolReturned()
    {
        // Arrange
        var fakeApi = new FakeApiService(new MarketSnapshot
        {
            Symbol = "7011.T",
            ExchangeRate = 150.1234m,
            StockPrice = 210.50m,
            ExchangeUpdatedAt = DateTimeOffset.Now,
            StockUpdatedAt = DateTimeOffset.Now
        });
        var viewModel = new MainViewModel(fakeApi, new FakeLogger());

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.True(viewModel.IsJapaneseStock);
    }

    /// <summary>
    /// 米国株シンボル取得時に日本株表示フラグが無効のままであることをテスト。
    /// 期待値: IsJapaneseStock が false。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_KeepsJapaneseStockFlagFalse_WhenUsSymbolReturned()
    {
        // Arrange
        var fakeApi = new FakeApiService(new MarketSnapshot
        {
            Symbol = "IBM",
            ExchangeRate = 150.1234m,
            StockPrice = 210.50m,
            ExchangeUpdatedAt = DateTimeOffset.Now,
            StockUpdatedAt = DateTimeOffset.Now
        });
        var viewModel = new MainViewModel(fakeApi, new FakeLogger());

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.False(viewModel.IsJapaneseStock);
    }

    private sealed class FakeApiService : IApiService
    {
        private readonly MarketSnapshot _snapshot;

        public FakeApiService(MarketSnapshot? snapshot = null)
        {
            _snapshot = snapshot ?? new MarketSnapshot
            {
                Symbol = "IBM",
                ExchangeRate = 100m,
                StockPrice = 200m,
                ExchangeUpdatedAt = DateTimeOffset.Now,
                StockUpdatedAt = DateTimeOffset.Now
            };
        }

        public Task<MarketSnapshot> GetMarketSnapshotAsync(string symbol, CancellationToken cancellationToken)
        {
            var normalizedSymbol = string.IsNullOrWhiteSpace(symbol) ? _snapshot.Symbol : symbol.Trim().ToUpperInvariant();
            var result = new MarketSnapshot
            {
                Symbol = normalizedSymbol,
                ExchangeRate = _snapshot.ExchangeRate,
                StockPrice = _snapshot.StockPrice,
                ExchangeUpdatedAt = _snapshot.ExchangeUpdatedAt,
                StockUpdatedAt = _snapshot.StockUpdatedAt
            };

            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingApiService : IApiService
    {
        public Task<MarketSnapshot> GetMarketSnapshotAsync(string symbol, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("テスト用例外");
        }
    }

    private sealed class FakeLogger : IAppLogger
    {
        public void Info(string message)
        {
        }

        public void Error(Exception exception, string message)
        {
        }
    }
}
