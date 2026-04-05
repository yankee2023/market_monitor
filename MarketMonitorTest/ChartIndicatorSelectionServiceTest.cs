using MarketMonitor.Features.Dashboard.Models;
using MarketMonitor.Features.Dashboard.Services;
using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitorTest;

/// <summary>
/// チャート指標選択サービスを検証するテストクラス。
/// </summary>
public sealed class ChartIndicatorSelectionServiceTest
{
    /// <summary>
    /// 既存トグル状態が新しい定義生成時にも維持されることを検証する。
    /// </summary>
    [Fact]
    public void CreateToggleItems_PreservesPreviousSelection()
    {
        var service = new ChartIndicatorSelectionService();
        var existingItems = new[]
        {
            new ChartIndicatorToggleItem("ma5", "MA5", "#fff", ChartIndicatorPlacement.OverlayPriceChart, 10, false)
        };
        var definitions = new[]
        {
            new ChartIndicatorDefinition("ma5", "MA5", ChartIndicatorPlacement.OverlayPriceChart, "#fff", true, 10),
            new ChartIndicatorDefinition("ma25", "MA25", ChartIndicatorPlacement.OverlayPriceChart, "#fff", true, 20)
        };

        var result = service.CreateToggleItems(existingItems, definitions);

        Assert.False(result.Single(item => item.IndicatorKey == "ma5").IsSelected);
        Assert.True(result.Single(item => item.IndicatorKey == "ma25").IsSelected);
    }

    /// <summary>
    /// 再描画時に既存パネルの展開状態と高さ倍率が引き継がれることを検証する。
    /// </summary>
    [Fact]
    public void CreateSelection_PreservesPanelPresentationState()
    {
        var service = new ChartIndicatorSelectionService();
        var toggles = new[]
        {
            new ChartIndicatorToggleItem("macd", "MACD", "#fff", ChartIndicatorPlacement.SecondaryPanel, 10, true)
        };
        var nextPanel = new IndicatorPanelRenderData("macd", "MACD", 10, "N2", null, Array.Empty<ChartIndicatorRenderSeries>(), Array.Empty<ChartIndicatorBarItem>(), Array.Empty<IndicatorReferenceLine>(), 0m, 1m);
        var currentPanel = new IndicatorPanelRenderData("macd", "MACD", 10, "N2", null, Array.Empty<ChartIndicatorRenderSeries>(), Array.Empty<ChartIndicatorBarItem>(), Array.Empty<IndicatorReferenceLine>(), 0m, 1m)
        {
            IsExpanded = false,
            PanelScale = 1.7d
        };

        var result = service.CreateSelection(
            toggles,
            Array.Empty<ChartIndicatorRenderSeries>(),
            new[] { nextPanel },
            new[] { currentPanel });

        var visiblePanel = Assert.Single(result.VisibleIndicatorPanels);
        Assert.False(visiblePanel.IsExpanded);
        Assert.Equal(1.7d, visiblePanel.PanelScale);
    }
}