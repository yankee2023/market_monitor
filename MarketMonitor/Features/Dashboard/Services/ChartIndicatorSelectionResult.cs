using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.Dashboard.Services;

/// <summary>
/// 指標選択計算結果を表す。
/// </summary>
public sealed class ChartIndicatorSelectionResult
{
    /// <summary>
    /// 計算結果を初期化する。
    /// </summary>
    public ChartIndicatorSelectionResult(
        IReadOnlyList<ChartIndicatorRenderSeries> visibleOverlaySeries,
        IReadOnlyList<IndicatorPanelRenderData> visibleIndicatorPanels)
    {
        VisibleOverlaySeries = visibleOverlaySeries ?? throw new ArgumentNullException(nameof(visibleOverlaySeries));
        VisibleIndicatorPanels = visibleIndicatorPanels ?? throw new ArgumentNullException(nameof(visibleIndicatorPanels));
    }

    /// <summary>
    /// 表示中オーバーレイ系列。
    /// </summary>
    public IReadOnlyList<ChartIndicatorRenderSeries> VisibleOverlaySeries { get; }

    /// <summary>
    /// 表示中指標パネル。
    /// </summary>
    public IReadOnlyList<IndicatorPanelRenderData> VisibleIndicatorPanels { get; }
}