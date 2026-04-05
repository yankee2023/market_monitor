using MarketMonitor.Features.Dashboard.ViewModels;
using MarketMonitor.Features.JapaneseStockChart.Services;
using MarketMonitor.Features.MarketSnapshot.Services;
using MarketMonitor.Features.PriceHistory.Services;
using MarketMonitor.Shared.Infrastructure;
using MarketMonitor.Shared.Logging;
using MarketMonitor.Shared.MarketData;

namespace MarketMonitor.Composition;

/// <summary>
/// アプリケーション起動時のオブジェクト構成を組み立てる。
/// </summary>
internal static class AppBootstrapper
{
    /// <summary>
    /// メイン画面用の ViewModel を生成する。
    /// </summary>
    /// <returns>初期化済みの ViewModel。</returns>
    internal static MainViewModel CreateMainViewModel()
    {
        var logger = new SerilogAppLogger();
        var httpService = new RateLimitedHttpService();
        var cache = new MarketDataCache();
        var tokyoPrimeSymbolResolver = new TokyoPrimeSymbolResolver(httpService);
        var marketSymbolResolver = new MarketSymbolResolver(tokyoPrimeSymbolResolver, logger);

        return new MainViewModel(
            new MarketSnapshotService(logger, httpService, cache, marketSymbolResolver),
            new PriceHistoryFeatureService(new SqlitePriceHistoryRepository(logger), logger),
            new JapaneseStockChartFeatureService(
                new JapaneseCandleService(logger, httpService, cache, marketSymbolResolver),
                logger),
            logger,
            new WpfDispatcherTimerAdapter());
    }

    /// <summary>
    /// メイン画面を生成する。
    /// </summary>
    /// <returns>初期化済みのメインウィンドウ。</returns>
    public static MainWindow CreateMainWindow()
    {
        return new MainWindow(CreateMainViewModel());
    }
}
