using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;
using System.Globalization;

namespace MarketMonitorTest;

/// <summary>
/// CandlestickRenderService の描画用データ生成を検証するテストクラス。
/// </summary>
public sealed class CandlestickRenderServiceTest
{
    /// <summary>
    /// ツールチップ表示用の OHLC と日付が設定されることをテスト。
    /// 期待値: 日付と各価格文字列が埋まる。
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
                Close = 110m
            }
        ]);

        Assert.Single(result.Candlesticks);
        Assert.Equal(new DateTime(2026, 4, 1).ToString("yyyy/MM/dd", CultureInfo.CurrentCulture), result.Candlesticks[0].DateText);
        Assert.Equal(100m.ToString("N2", CultureInfo.CurrentCulture), result.Candlesticks[0].OpenText);
        Assert.Equal(110m.ToString("N2", CultureInfo.CurrentCulture), result.Candlesticks[0].CloseText);
        Assert.Equal(120m.ToString("N2", CultureInfo.CurrentCulture), result.Candlesticks[0].HighText);
        Assert.Equal(95m.ToString("N2", CultureInfo.CurrentCulture), result.Candlesticks[0].LowText);
    }

    /// <summary>
    /// 横軸ラベルが件数に応じて間引かれることをテスト。
    /// 期待値: 全件は表示されないが最後のラベルは表示される。
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
                Close = 102m + index
            })
            .ToList();

        var result = CandlestickRenderService.Build(candles);

        Assert.Equal(30, result.Candlesticks.Count);
        Assert.Contains(result.Candlesticks, item => !item.IsLabelVisible);
        Assert.True(result.Candlesticks[^1].IsLabelVisible);
    }

    /// <summary>
    /// 十分な本数がある場合にチャート指標定義と描画線が生成されることをテスト。
    /// 期待値: MA5、MA25、MA75 の定義と描画データが含まれる。
    /// </summary>
    [Fact]
    public void Build_CreatesChartIndicatorSeries_WhenEnoughCandlesExist()
    {
        var candles = Enumerable.Range(0, 90)
            .Select(index => new JapaneseCandleEntry
            {
                Date = new DateTime(2026, 1, 1).AddDays(index),
                Open = 100m + index,
                High = 103m + index,
                Low = 98m + index,
                Close = 101m + index
            })
            .ToList();

        var result = CandlestickRenderService.Build(candles);

        Assert.Equal(3, result.IndicatorDefinitions.Count);
        Assert.Equal(3, result.IndicatorSeries.Count);
        Assert.Contains(result.IndicatorDefinitions, item => item.IndicatorKey == "ma5" && item.DisplayName == "MA5");
        Assert.Contains(result.IndicatorDefinitions, item => item.IndicatorKey == "ma25" && item.DisplayName == "MA25");
        Assert.Contains(result.IndicatorDefinitions, item => item.IndicatorKey == "ma75" && item.DisplayName == "MA75");
        Assert.Contains(result.IndicatorSeries, line => line.IndicatorKey == "ma5" && line.LegendLabel == "MA5" && !string.IsNullOrWhiteSpace(line.Points));
        Assert.Contains(result.IndicatorSeries, line => line.IndicatorKey == "ma25" && line.LegendLabel == "MA25" && !string.IsNullOrWhiteSpace(line.Points));
        Assert.Contains(result.IndicatorSeries, line => line.IndicatorKey == "ma75" && line.LegendLabel == "MA75" && line.StrokeDashArray == "8 4");
        Assert.True(result.CanvasWidth >= 320d);
    }
}