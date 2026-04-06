using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;

namespace MarketMonitorTest;

/// <summary>
/// ChartAnalysisLineService の変換ロジックを検証する。
/// </summary>
public sealed class ChartAnalysisLineServiceTest
{
    [Fact]
    public void CreateLine_ReturnsNull_WhenDistanceIsTooShort()
    {
        var service = new ChartAnalysisLineService();

        var line = service.CreateLine(ChartAnalysisLineType.TrendLine, 0.10d, 0.20d, 0.105d, 0.205d);

        Assert.Null(line);
    }

    [Fact]
    public void CreateRenderItems_MapsNormalizedCoordinatesToCanvas()
    {
        var service = new ChartAnalysisLineService();
        var lines = new[]
        {
            new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.SupportLine, 0.10d, 0.25d, 0.90d, 0.75d)
        };

        var renderItems = service.CreateRenderItems(lines, 400d, 260d);

        var line = Assert.Single(renderItems);
        Assert.Equal(40d, line.X1, 3);
        Assert.Equal(65d, line.Y1, 3);
        Assert.Equal(360d, line.X2, 3);
        Assert.Equal(195d, line.Y2, 3);
        Assert.Equal("8 4", line.StrokeDashArray);
    }

    [Fact]
    public void MoveLine_ClampsMovementWithinCanvas()
    {
        var service = new ChartAnalysisLineService();
        var line = new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.TrendLine, 0.20d, 0.20d, 0.80d, 0.80d);

        var moved = service.MoveLine(line, 0.50d, 0.50d);

        Assert.Equal(0.40d, moved.StartXRatio, 3);
        Assert.Equal(0.40d, moved.StartYRatio, 3);
        Assert.Equal(1.00d, moved.EndXRatio, 3);
        Assert.Equal(1.00d, moved.EndYRatio, 3);
    }

    [Fact]
    public void FindNearestLineId_ReturnsNearestLineWithinTolerance()
    {
        var service = new ChartAnalysisLineService();
        var expected = Guid.NewGuid();
        var lines = new[]
        {
            new ChartAnalysisLine(expected, ChartAnalysisLineType.TrendLine, 0.10d, 0.10d, 0.90d, 0.90d),
            new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.SupportLine, 0.10d, 0.90d, 0.90d, 0.90d)
        };

        var lineId = service.FindNearestLineId(lines, 0.50d, 0.52d, 0.05d);

        Assert.Equal(expected, lineId);
    }
}