using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;
using MarketMonitor.Shared.Logging;

namespace MarketMonitorTest;

/// <summary>
/// JapaneseStockChartFeatureService の振る舞いを検証するテストクラス。
/// </summary>
public class JapaneseStockChartFeatureServiceTest
{
    /// <summary>
    /// 表示期間に応じてローソク足が絞り込まれることをテスト。
    /// 期待値: 1 か月範囲のデータだけが残る。
    /// </summary>
    [Fact]
    public async Task LoadAsync_FiltersCandles_ByDisplayPeriod()
    {
        // Arrange
        var candles = new List<JapaneseCandleEntry>
        {
            new() { Date = new DateTime(2026, 1, 1), Open = 100m, High = 120m, Low = 90m, Close = 110m },
            new() { Date = new DateTime(2026, 3, 20), Open = 110m, High = 125m, Low = 105m, Close = 122m },
            new() { Date = new DateTime(2026, 4, 10), Open = 122m, High = 130m, Low = 118m, Close = 125m }
        };
        var service = new JapaneseStockChartFeatureService(new FakeJapaneseCandleService(candles), new FakeLogger());

        // Act
        var result = await service.LoadAsync("7203.T", CandleTimeframe.Daily, CandleDisplayPeriod.OneMonth, 100, CancellationToken.None);

        // Assert
        Assert.True(result.IsJapaneseStock);
        Assert.Equal(2, result.Candlesticks.Count);
        Assert.Equal(3, result.IndicatorDefinitions.Count);
        Assert.Empty(result.IndicatorSeries);
        Assert.True(result.CanvasWidth >= 320d);
    }

    /// <summary>
    /// 非東証シンボル入力時に空表示を返すことをテスト。
    /// 期待値: IsJapaneseStock が false で空コレクション。
    /// </summary>
    [Fact]
    public async Task LoadAsync_ReturnsEmpty_ForNonTokyoSymbol()
    {
        // Arrange
        var service = new JapaneseStockChartFeatureService(new FakeJapaneseCandleService([]), new FakeLogger());

        // Act
        var result = await service.LoadAsync("IBM", CandleTimeframe.Daily, CandleDisplayPeriod.OneMonth, 100, CancellationToken.None);

        // Assert
        Assert.False(result.IsJapaneseStock);
        Assert.Empty(result.Candlesticks);
        Assert.Empty(result.IndicatorDefinitions);
        Assert.Empty(result.IndicatorSeries);
    }

    private sealed class FakeJapaneseCandleService : IJapaneseCandleService
    {
        private readonly IReadOnlyList<JapaneseCandleEntry> _candles;

        public FakeJapaneseCandleService(IReadOnlyList<JapaneseCandleEntry> candles)
        {
            _candles = candles;
        }

        public Task<IReadOnlyList<JapaneseCandleEntry>> GetJapaneseCandlesAsync(string symbol, CandleTimeframe timeframe, int limit, CancellationToken cancellationToken)
        {
            return Task.FromResult(_candles);
        }
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