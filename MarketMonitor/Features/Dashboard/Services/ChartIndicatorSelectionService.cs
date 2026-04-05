using MarketMonitor.Features.Dashboard.Models;
using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.Dashboard.Services;

/// <summary>
/// チャート指標の選択状態を計算するサービスを表す。
/// </summary>
public sealed class ChartIndicatorSelectionService : IChartIndicatorSelectionService
{
    /// <inheritdoc />
    public IReadOnlyList<ChartIndicatorToggleItem> CreateToggleItems(
        IReadOnlyList<ChartIndicatorToggleItem> existingItems,
        IReadOnlyList<ChartIndicatorDefinition> indicatorDefinitions)
    {
        ArgumentNullException.ThrowIfNull(existingItems);
        ArgumentNullException.ThrowIfNull(indicatorDefinitions);

        var previousSelections = existingItems.ToDictionary(
            item => item.IndicatorKey,
            item => item.IsSelected,
            StringComparer.Ordinal);

        return indicatorDefinitions
            .OrderBy(item => item.DisplayOrder)
            .Select(definition => new ChartIndicatorToggleItem(
                definition.IndicatorKey,
                definition.DisplayName,
                definition.AccentColor,
                definition.Placement,
                definition.DisplayOrder,
                previousSelections.TryGetValue(definition.IndicatorKey, out var previous)
                    ? previous
                    : definition.IsEnabledByDefault))
            .ToList();
    }

    /// <inheritdoc />
    public ChartIndicatorSelectionResult CreateSelection(
        IReadOnlyList<ChartIndicatorToggleItem> toggleItems,
        IReadOnlyList<ChartIndicatorRenderSeries> overlaySeries,
        IReadOnlyList<IndicatorPanelRenderData> indicatorPanels,
        IReadOnlyList<IndicatorPanelRenderData> currentVisiblePanels)
    {
        ArgumentNullException.ThrowIfNull(toggleItems);
        ArgumentNullException.ThrowIfNull(overlaySeries);
        ArgumentNullException.ThrowIfNull(indicatorPanels);
        ArgumentNullException.ThrowIfNull(currentVisiblePanels);

        var selectedIndicatorKeys = toggleItems
            .Where(item => item.IsSelected)
            .Select(item => item.IndicatorKey)
            .ToHashSet(StringComparer.Ordinal);

        var visibleOverlaySeries = overlaySeries
            .Where(item => selectedIndicatorKeys.Contains(item.IndicatorKey))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.LegendLabel, StringComparer.CurrentCulture)
            .ToList();

        var currentPanelStateByKey = currentVisiblePanels.ToDictionary(
            item => item.PanelKey,
            item => (item.IsExpanded, item.PanelScale),
            StringComparer.Ordinal);

        var visibleIndicatorPanels = indicatorPanels
            .Where(item => selectedIndicatorKeys.Contains(item.PanelKey))
            .OrderBy(item => item.DisplayOrder)
            .ToList();

        foreach (var panel in visibleIndicatorPanels)
        {
            if (currentPanelStateByKey.TryGetValue(panel.PanelKey, out var state))
            {
                panel.IsExpanded = state.IsExpanded;
                panel.PanelScale = state.PanelScale;
            }
        }

        return new ChartIndicatorSelectionResult(visibleOverlaySeries, visibleIndicatorPanels);
    }
}