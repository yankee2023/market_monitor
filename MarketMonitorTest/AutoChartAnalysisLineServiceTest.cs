using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;

namespace MarketMonitorTest;

/// <summary>
/// AutoChartAnalysisLineService の自動線生成を検証する。
/// </summary>
public sealed class AutoChartAnalysisLineServiceTest
{
    [Fact]
    public void Generate_CreatesTrendSupportResistanceLines_WhenEnoughCandlesExist()
    {
        var service = new AutoChartAnalysisLineService();
        var candles = Enumerable.Range(0, 24)
            .Select(index => new JapaneseCandleEntry
            {
                Date = new DateTime(2026, 1, 1).AddDays(index),
                Open = 100m + index,
                High = 108m + index + ((index % 6 == 0) ? 4m : 0m),
                Low = 96m + index - ((index % 5 == 0) ? 4m : 0m),
                Close = 102m + index,
                Volume = 1000000L + (index * 5000L)
            })
            .ToList();

        var lines = service.Generate(candles);

        Assert.Equal(3, lines.Count);
        Assert.Contains(lines, item => item.LineType == ChartAnalysisLineType.TrendLine);
        Assert.Contains(lines, item => item.LineType == ChartAnalysisLineType.SupportLine);
        Assert.Contains(lines, item => item.LineType == ChartAnalysisLineType.ResistanceLine);
        Assert.All(lines, item => Assert.InRange(item.StartYRatio, 0d, 1d));
        Assert.All(lines, item => Assert.InRange(item.EndYRatio, 0d, 1d));
    }

    [Fact]
    public void Generate_ReturnsEmpty_WhenCandlesAreInsufficient()
    {
        var service = new AutoChartAnalysisLineService();
        var candles = Enumerable.Range(0, 6)
            .Select(index => new JapaneseCandleEntry
            {
                Date = new DateTime(2026, 1, 1).AddDays(index),
                Open = 100m,
                High = 102m,
                Low = 98m,
                Close = 101m,
                Volume = 1000000L
            })
            .ToList();

        var lines = service.Generate(candles);

        Assert.Empty(lines);
    }
}