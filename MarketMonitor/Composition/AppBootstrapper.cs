using System.IO;
using MarketMonitor.Features.Dashboard.ViewModels;
using MarketMonitor.Features.JapaneseStockChart.Services;
using MarketMonitor.Features.MarketSnapshot.Services;
using MarketMonitor.Features.PriceHistory.Services;
using MarketMonitor.Features.SectorComparison.Services;
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
        var tokyoListedSymbolResolver = new TokyoListedSymbolResolver(
            httpService,
            new JpxExcelCompanyRecordReader(),
            CreateMarketSegmentPolicy(logger));
        var marketSymbolResolver = new MarketSymbolResolver(tokyoListedSymbolResolver, logger);
        var marketSnapshotService = new MarketSnapshotService(logger, httpService, cache, marketSymbolResolver);

        return new MainViewModel(
            marketSnapshotService,
            new PriceHistoryFeatureService(new SqlitePriceHistoryRepository(logger), logger),
            new JapaneseStockChartFeatureService(
                new JapaneseCandleService(logger, httpService, cache, marketSymbolResolver),
                logger),
            new SectorComparisonFeatureService(marketSymbolResolver, marketSnapshotService, logger),
            logger,
            new WindowsDesktopNotificationService());
    }

    /// <summary>
    /// メイン画面を生成する。
    /// </summary>
    /// <returns>初期化済みのメインウィンドウ。</returns>
    public static MainWindow CreateMainWindow()
    {
        return new MainWindow(CreateMainViewModel());
    }

    private static ITokyoMarketSegmentPolicy CreateMarketSegmentPolicy(SerilogAppLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var settingsPath = Path.Combine(AppContext.BaseDirectory, "market-settings.json");

        try
        {
            var settingsProvider = new JsonTokyoMarketSegmentSettingsProvider(settingsPath);
            var supportedSegments = settingsProvider.LoadSupportedSegments();
            logger.Info($"市場区分設定を読み込みました。Segments={string.Join(",", supportedSegments)}");
            return new ConfigurableTokyoMarketSegmentPolicy(supportedSegments);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            logger.LogError(ex, $"市場区分設定の読込に失敗したため既定設定を使用します。Path={settingsPath}");
            return new TokyoMainMarketSegmentPolicy();
        }
    }
}
