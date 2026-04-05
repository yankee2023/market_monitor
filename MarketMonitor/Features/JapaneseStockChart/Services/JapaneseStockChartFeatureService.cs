using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Shared.Logging;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// 日本株チャート機能をまとめて提供する。
/// </summary>
public sealed class JapaneseStockChartFeatureService : IJapaneseStockChartFeatureService
{
    private readonly IJapaneseCandleService _japaneseCandleService;
    private readonly IAppLogger _logger;

    /// <summary>
    /// サービスを初期化する。
    /// </summary>
    public JapaneseStockChartFeatureService(IJapaneseCandleService japaneseCandleService, IAppLogger logger)
    {
        _japaneseCandleService = japaneseCandleService ?? throw new ArgumentNullException(nameof(japaneseCandleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<JapaneseStockChartViewData> LoadAsync(
        string symbol,
        CandleTimeframe timeframe,
        CandleDisplayPeriod displayPeriod,
        int fetchLimit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (!IsTokyoSymbol(symbol))
        {
            _logger.Info("CandlesReloadSkipped: Reason=NonJapaneseStock");
            return new JapaneseStockChartViewData(
                false,
                Array.Empty<CandlestickRenderItem>(),
                Array.Empty<ChartIndicatorDefinition>(),
                Array.Empty<ChartIndicatorRenderSeries>(),
                Array.Empty<IndicatorPanelRenderData>(),
                0m,
                0m,
                320d);
        }

        _logger.Info($"CandlesReloadStarted: Symbol={symbol}, Timeframe={timeframe}, Period={displayPeriod}");
        var candles = await _japaneseCandleService.GetJapaneseCandlesAsync(symbol, timeframe, fetchLimit, cancellationToken);
        if (candles.Count == 0)
        {
            var emptyRendered = CandlestickRenderService.Build(Array.Empty<JapaneseCandleEntry>());
            _logger.Info($"CandlesReloadCompleted: Symbol={symbol}, Timeframe={timeframe}, Period={displayPeriod}, SourceCount=0, RenderCount=0");
            return new JapaneseStockChartViewData(
                true,
                Array.Empty<CandlestickRenderItem>(),
                emptyRendered.IndicatorDefinitions,
                emptyRendered.OverlayIndicatorSeries,
                emptyRendered.IndicatorPanels,
                0m,
                0m,
                320d);
        }

        var filtered = FilterCandlesBySelectedPeriod(candles, displayPeriod);
        var visibleStartDate = filtered.Count == 0 ? (DateTime?)null : filtered[0].Date;
        var rendered = CandlestickRenderService.Build(candles, visibleStartDate);
        _logger.Info($"CandlesReloadCompleted: Symbol={symbol}, Timeframe={timeframe}, Period={displayPeriod}, SourceCount={candles.Count}, FilteredCount={filtered.Count}, RenderCount={rendered.Candlesticks.Count}");
        var minPrice = filtered.Count == 0 ? 0m : filtered.Min(item => item.Low);
        var maxPrice = filtered.Count == 0 ? 0m : filtered.Max(item => item.High);
        return new JapaneseStockChartViewData(
            true,
            rendered.Candlesticks,
            rendered.IndicatorDefinitions,
            rendered.OverlayIndicatorSeries,
            rendered.IndicatorPanels,
            minPrice,
            maxPrice,
            rendered.CanvasWidth);
    }

    private static IReadOnlyList<JapaneseCandleEntry> FilterCandlesBySelectedPeriod(
        IReadOnlyList<JapaneseCandleEntry> candles,
        CandleDisplayPeriod displayPeriod)
    {
        if (candles.Count == 0)
        {
            return candles;
        }

        var latest = candles.Max(x => x.Date);
        var months = displayPeriod switch
        {
            CandleDisplayPeriod.OneMonth => 1,
            CandleDisplayPeriod.ThreeMonths => 3,
            CandleDisplayPeriod.SixMonths => 6,
            CandleDisplayPeriod.OneYear => 12,
            _ => 1
        };

        var threshold = latest.AddMonths(-months);
        return candles.Where(x => x.Date >= threshold).OrderBy(x => x.Date).ToList();
    }

    private static bool IsTokyoSymbol(string symbol)
    {
        return symbol.EndsWith(".T", StringComparison.OrdinalIgnoreCase);
    }
}