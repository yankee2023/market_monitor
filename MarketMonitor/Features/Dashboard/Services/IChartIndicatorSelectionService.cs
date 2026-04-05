using MarketMonitor.Features.Dashboard.Models;
using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.Dashboard.Services;

/// <summary>
/// チャート指標の選択状態と表示対象の計算を表す。
/// </summary>
public interface IChartIndicatorSelectionService
{
    /// <summary>
    /// 既存選択状態を維持しながらトグル一覧を生成する。
    /// </summary>
    IReadOnlyList<ChartIndicatorToggleItem> CreateToggleItems(
        IReadOnlyList<ChartIndicatorToggleItem> existingItems,
        IReadOnlyList<ChartIndicatorDefinition> indicatorDefinitions);

    /// <summary>
    /// 選択状態から表示対象の系列と指標パネルを計算する。
    /// </summary>
    ChartIndicatorSelectionResult CreateSelection(
        IReadOnlyList<ChartIndicatorToggleItem> toggleItems,
        IReadOnlyList<ChartIndicatorRenderSeries> overlaySeries,
        IReadOnlyList<IndicatorPanelRenderData> indicatorPanels,
        IReadOnlyList<IndicatorPanelRenderData> currentVisiblePanels);
}