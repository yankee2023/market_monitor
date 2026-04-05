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
            new() { Date = new DateTime(2026, 1, 1), Open = 100m, High = 120m, Low = 90m, Close = 110m, Volume = 1000000L },
            new() { Date = new DateTime(2026, 3, 20), Open = 110m, High = 125m, Low = 105m, Close = 122m, Volume = 1100000L },
            new() { Date = new DateTime(2026, 4, 10), Open = 122m, High = 130m, Low = 118m, Close = 125m, Volume = 1200000L }
        };
        var service = new JapaneseStockChartFeatureService(new FakeJapaneseCandleService(candles), new FakeLogger());

        // Act
        var result = await service.LoadAsync("7203.T", CandleTimeframe.Daily, CandleDisplayPeriod.OneMonth, 100, CancellationToken.None);

        // Assert
        Assert.True(result.IsJapaneseStock);
        Assert.Equal(2, result.Candlesticks.Count);
        Assert.Equal(6, result.IndicatorDefinitions.Count);
        Assert.Empty(result.OverlayIndicatorSeries);
        Assert.Single(result.IndicatorPanels);
        Assert.Equal("volume", result.IndicatorPanels[0].PanelKey);
        Assert.True(result.CanvasWidth >= 320d);
    }

    /// <summary>
    /// 表示期間外の履歴も使って RSI と MACD を算出できることをテスト。
    /// </summary>
    [Fact]
    public async Task LoadAsync_KeepsIndicatorPanelsVisible_WhenHistoryExistsOutsideVisiblePeriod()
    {
        var candles = Enumerable.Range(0, 40)
            .Select(index => new JapaneseCandleEntry
            {
                Date = new DateTime(2026, 2, 20).AddDays(index),
                Open = 100m + index,
                High = 105m + index,
                Low = 98m + index,
                Close = 101m + (index % 5),
                Volume = 1000000L + (index * 10000L)
            })
            .ToList();

        var service = new JapaneseStockChartFeatureService(new FakeJapaneseCandleService(candles), new FakeLogger());

        var result = await service.LoadAsync("7203.T", CandleTimeframe.Daily, CandleDisplayPeriod.OneMonth, 100, CancellationToken.None);

        Assert.True(result.Candlesticks.Count < candles.Count);
        Assert.Contains(result.IndicatorPanels, item => item.PanelKey == "volume");
        Assert.Contains(result.IndicatorPanels, item => item.PanelKey == "macd");
        Assert.Contains(result.IndicatorPanels, item => item.PanelKey == "rsi");
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
        Assert.Empty(result.OverlayIndicatorSeries);
        Assert.Empty(result.IndicatorPanels);
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