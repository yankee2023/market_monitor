using System.Globalization;
using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;

namespace MarketMonitorTest;

/// <summary>
/// CandlestickRenderService の描画用データ生成を検証するテストクラス。
/// </summary>
public sealed class CandlestickRenderServiceTest
{
    /// <summary>
    /// ツールチップ表示用の OHLC と日付と出来高が設定されることをテスト。
    /// </summary>
    [Fact]
    public void Build_SetsTooltipTexts()
    {
        var result = CandlestickRenderService.Build(
        [
            new JapaneseCandleEntry
            {
                Date = new DateTime(2026, 4, 1),
                Open = 100m,
                High = 120m,
                Low = 95m,
                Close = 110m,
                Volume = 1200000L
            }
        ]);

        Assert.Single(result.Candlesticks);
        Assert.Equal(new DateTime(2026, 4, 1).ToString("yyyy/MM/dd", CultureInfo.CurrentCulture), result.Candlesticks[0].DateText);
        Assert.Equal(100m.ToString("N2", CultureInfo.CurrentCulture), result.Candlesticks[0].OpenText);
        Assert.Equal(110m.ToString("N2", CultureInfo.CurrentCulture), result.Candlesticks[0].CloseText);
        Assert.Equal(120m.ToString("N2", CultureInfo.CurrentCulture), result.Candlesticks[0].HighText);
        Assert.Equal(95m.ToString("N2", CultureInfo.CurrentCulture), result.Candlesticks[0].LowText);
        Assert.Equal(1200000L.ToString("N0", CultureInfo.CurrentCulture), result.Candlesticks[0].VolumeText);
    }

    /// <summary>
    /// 横軸ラベルが件数に応じて間引かれることをテスト。
    /// </summary>
    [Fact]
    public void Build_ThinsAxisLabels_WhenManyCandlesExist()
    {
        var candles = Enumerable.Range(0, 30)
            .Select(index => new JapaneseCandleEntry
            {
                Date = new DateTime(2026, 4, 1).AddDays(index),
                Open = 100m + index,
                High = 105m + index,
                Low = 95m + index,
                Close = 102m + index,
                Volume = 1000000L + index * 1000L
            })
            .ToList();

        var result = CandlestickRenderService.Build(candles);

        Assert.Equal(30, result.Candlesticks.Count);
        Assert.Contains(result.Candlesticks, item => !item.IsLabelVisible);
        Assert.True(result.Candlesticks[^1].IsLabelVisible);
    }

    /// <summary>
    /// 十分な本数がある場合にチャート指標定義、重ね描き線、下段パネルが生成されることをテスト。
    /// </summary>
    [Fact]
    public void Build_CreatesIndicatorPanels_WhenEnoughCandlesExist()
    {
        var candles = Enumerable.Range(0, 90)
            .Select(index => new JapaneseCandleEntry
            {
                Date = new DateTime(2026, 1, 1).AddDays(index),
                Open = 100m + index,
                High = 103m + index,
                Low = 98m + index,
                Close = 101m + (index % 6),
                Volume = 1500000L + index * 2500L
            })
            .ToList();

        var result = CandlestickRenderService.Build(candles);

        Assert.Equal(6, result.IndicatorDefinitions.Count);
        Assert.Equal(3, result.OverlayIndicatorSeries.Count);
        Assert.Equal(3, result.IndicatorPanels.Count);
        Assert.Contains(result.IndicatorDefinitions, item => item.IndicatorKey == "volume" && item.DisplayName == "出来高");
        Assert.Contains(result.IndicatorDefinitions, item => item.IndicatorKey == "macd" && item.DisplayName == "MACD");
        Assert.Contains(result.IndicatorDefinitions, item => item.IndicatorKey == "rsi" && item.DisplayName == "RSI");

        var volumePanel = Assert.Single(result.IndicatorPanels, item => item.PanelKey == "volume");
        Assert.Equal(90, volumePanel.BarItems.Count);
        Assert.Equal(90, volumePanel.HoverItems.Count);
        Assert.Empty(volumePanel.LineSeries);
        Assert.Contains("出来高", volumePanel.HoverItems[0].TooltipText);

        var macdPanel = Assert.Single(result.IndicatorPanels, item => item.PanelKey == "macd");
        Assert.Equal(2, macdPanel.LineSeries.Count);
        Assert.Equal(90, macdPanel.HoverItems.Count);
        Assert.All(macdPanel.LineSeries, item => Assert.Equal("macd", item.IndicatorKey));
        Assert.Contains("MACD", macdPanel.HoverItems[^1].TooltipText);

        var rsiPanel = Assert.Single(result.IndicatorPanels, item => item.PanelKey == "rsi");
        Assert.Single(rsiPanel.LineSeries);
        Assert.Equal(90, rsiPanel.HoverItems.Count);
        Assert.Equal(0m, rsiPanel.MinValue);
        Assert.Equal(100m, rsiPanel.MaxValue);
        Assert.Contains("RSI", rsiPanel.HoverItems[^1].TooltipText);
        Assert.True(result.CanvasWidth >= 320d);
    }
}