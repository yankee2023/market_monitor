using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitorTest;

/// <summary>
/// 指標パネルの既定表示スケールを検証するテストを表す。
/// </summary>
public sealed class IndicatorPanelRenderDataTest
{
    [Theory]
    [InlineData("volume", 1.15d)]
    [InlineData("macd", 1.0d)]
    [InlineData("rsi", 0.9d)]
    public void Constructor_SetsIndicatorSpecificDefaultScale(string panelKey, double expectedScale)
    {
        var panel = new IndicatorPanelRenderData(
            panelKey,
            "test",
            10,
            "N2",
            null,
            Array.Empty<ChartIndicatorRenderSeries>(),
            Array.Empty<ChartIndicatorBarItem>(),
            Array.Empty<IndicatorReferenceLine>(),
            0m,
            100m);

        Assert.Equal(expectedScale, panel.PanelScale);
    }
}