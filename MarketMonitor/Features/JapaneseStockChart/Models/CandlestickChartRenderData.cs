namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// ローソク足チャート全体の描画データを表す。
/// </summary>
public sealed class CandlestickChartRenderData
{
    /// <summary>
    /// 描画データを初期化する。
    /// </summary>
    public CandlestickChartRenderData(
        IReadOnlyList<CandlestickRenderItem> candlesticks,
        IReadOnlyList<ChartIndicatorDefinition> indicatorDefinitions,
        IReadOnlyList<ChartIndicatorRenderSeries> indicatorSeries,
        double canvasWidth)
    {
        Candlesticks = candlesticks ?? throw new ArgumentNullException(nameof(candlesticks));
        IndicatorDefinitions = indicatorDefinitions ?? throw new ArgumentNullException(nameof(indicatorDefinitions));
        IndicatorSeries = indicatorSeries ?? throw new ArgumentNullException(nameof(indicatorSeries));
        CanvasWidth = canvasWidth;
    }

    /// <summary>
    /// ローソク足描画データ。
    /// </summary>
    public IReadOnlyList<CandlestickRenderItem> Candlesticks { get; }

    /// <summary>
    /// 利用可能な指標定義。
    /// </summary>
    public IReadOnlyList<ChartIndicatorDefinition> IndicatorDefinitions { get; }

    /// <summary>
    /// 指標描画データ。
    /// </summary>
    public IReadOnlyList<ChartIndicatorRenderSeries> IndicatorSeries { get; }

    /// <summary>
    /// チャート描画領域の横幅。
    /// </summary>
    public double CanvasWidth { get; }
}