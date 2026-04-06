using System.IO;
using Microsoft.Data.Sqlite;
using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;
using MarketMonitor.Shared.Logging;

namespace MarketMonitorTest;

/// <summary>
/// SqliteChartAnalysisLineRepository の振る舞いを検証するテストクラス。
/// </summary>
public sealed class SqliteChartAnalysisLineRepositoryTest : IDisposable
{
    private readonly string _databasePath;

    public SqliteChartAnalysisLineRepositoryTest()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"marketmonitor-analysis-lines-{Guid.NewGuid():N}.db");
    }

    [Fact]
    public async Task SaveAsync_AndGetAsync_PersistsLinesByChartContext()
    {
        var repository = new SqliteChartAnalysisLineRepository(new FakeLogger(), _databasePath);
        var line = new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.SupportLine, 0.10d, 0.20d, 0.80d, 0.70d);

        await repository.SaveAsync("7203.T", CandleTimeframe.Daily, CandleDisplayPeriod.OneMonth, [line], CancellationToken.None);
        var result = await repository.GetAsync("7203.T", CandleTimeframe.Daily, CandleDisplayPeriod.OneMonth, CancellationToken.None);

        var saved = Assert.Single(result);
        Assert.Equal(line.Id, saved.Id);
        Assert.Equal(ChartAnalysisLineType.SupportLine, saved.LineType);
    }

    [Fact]
    public async Task SaveAsync_ReplacesOnlyTargetContext()
    {
        var repository = new SqliteChartAnalysisLineRepository(new FakeLogger(), _databasePath);
        await repository.SaveAsync(
            "7203.T",
            CandleTimeframe.Daily,
            CandleDisplayPeriod.OneMonth,
            [new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.TrendLine, 0.10d, 0.20d, 0.80d, 0.70d)],
            CancellationToken.None);
        await repository.SaveAsync(
            "7203.T",
            CandleTimeframe.Weekly,
            CandleDisplayPeriod.OneMonth,
            [new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.ResistanceLine, 0.30d, 0.30d, 0.90d, 0.30d)],
            CancellationToken.None);

        await repository.SaveAsync(
            "7203.T",
            CandleTimeframe.Daily,
            CandleDisplayPeriod.OneMonth,
            [new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.SupportLine, 0.20d, 0.20d, 0.70d, 0.60d)],
            CancellationToken.None);

        var daily = await repository.GetAsync("7203.T", CandleTimeframe.Daily, CandleDisplayPeriod.OneMonth, CancellationToken.None);
        var weekly = await repository.GetAsync("7203.T", CandleTimeframe.Weekly, CandleDisplayPeriod.OneMonth, CancellationToken.None);

        Assert.Single(daily);
        Assert.Equal(ChartAnalysisLineType.SupportLine, daily[0].LineType);
        Assert.Single(weekly);
        Assert.Equal(ChartAnalysisLineType.ResistanceLine, weekly[0].LineType);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    File.Delete(_databasePath);
                    break;
                }
                catch (IOException) when (attempt < 2)
                {
                    Thread.Sleep(50);
                    SqliteConnection.ClearAllPools();
                }
            }
        }

        GC.SuppressFinalize(this);
    }

    private sealed class FakeLogger : IAppLogger
    {
        public void Info(string message)
        {
        }

        public void LogError(Exception exception, string message)
        {
        }
    }
}