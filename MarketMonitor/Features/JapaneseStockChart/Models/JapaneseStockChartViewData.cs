namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// 日本株チャート機能の表示用データをまとめる。
/// </summary>
public sealed class JapaneseStockChartViewData
{
    /// <summary>
    /// 表示データを初期化する。
    /// </summary>
    public JapaneseStockChartViewData(
        bool isJapaneseStock,
        IReadOnlyList<CandlestickRenderItem> candlesticks,
        IReadOnlyList<ChartIndicatorDefinition> indicatorDefinitions,
        IReadOnlyList<ChartIndicatorRenderSeries> overlayIndicatorSeries,
        IReadOnlyList<ChartAnalysisLine> suggestedAnalysisLines,
        IReadOnlyList<IndicatorPanelRenderData> indicatorPanels,
        decimal minPrice,
        decimal maxPrice,
        double canvasWidth)
    {
        IsJapaneseStock = isJapaneseStock;
        Candlesticks = candlesticks ?? throw new ArgumentNullException(nameof(candlesticks));
        IndicatorDefinitions = indicatorDefinitions ?? throw new ArgumentNullException(nameof(indicatorDefinitions));
        OverlayIndicatorSeries = overlayIndicatorSeries ?? throw new ArgumentNullException(nameof(overlayIndicatorSeries));
        SuggestedAnalysisLines = suggestedAnalysisLines ?? throw new ArgumentNullException(nameof(suggestedAnalysisLines));
        IndicatorPanels = indicatorPanels ?? throw new ArgumentNullException(nameof(indicatorPanels));
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        CanvasWidth = canvasWidth;
    }

    /// <summary>
    /// 日本株表示かどうか。
    /// </summary>
    public bool IsJapaneseStock { get; }

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
    public IReadOnlyList<ChartIndicatorRenderSeries> OverlayIndicatorSeries { get; }

    /// <summary>
    /// 自動生成した分析ライン候補。
    /// </summary>
    public IReadOnlyList<ChartAnalysisLine> SuggestedAnalysisLines { get; }

    /// <summary>
    /// 下段パネル描画データ。
    /// </summary>
    public IReadOnlyList<IndicatorPanelRenderData> IndicatorPanels { get; }

    /// <summary>
    /// 表示対象レンジの最小株価。
    /// </summary>
    public decimal MinPrice { get; }

    /// <summary>
    /// 表示対象レンジの最大株価。
    /// </summary>
    public decimal MaxPrice { get; }

    /// <summary>
    /// チャート描画領域の横幅。
    /// </summary>
    public double CanvasWidth { get; }
}