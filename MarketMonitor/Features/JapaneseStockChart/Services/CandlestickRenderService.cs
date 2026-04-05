using System.Globalization;
using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// ローソク足描画用データを生成する。
/// </summary>
internal sealed class CandlestickRenderService
{
    private const double ChartHeight = 200d;
    private const double CandleSlotWidth = 34d;
    private const double CandleCenterX = 17d;

    private static readonly IReadOnlyList<ChartIndicatorDefinition> SupportedIndicatorDefinitions =
    [
        new ChartIndicatorDefinition("ma5", "MA5", ChartIndicatorPlacement.OverlayPriceChart, "#F59E0B", true, 10),
        new ChartIndicatorDefinition("ma25", "MA25", ChartIndicatorPlacement.OverlayPriceChart, "#10B981", true, 20),
        new ChartIndicatorDefinition("ma75", "MA75", ChartIndicatorPlacement.OverlayPriceChart, "#334155", true, 30)
    ];

    /// <summary>
    /// 描画用ローソク足一覧を生成する。
    /// </summary>
    public static CandlestickChartRenderData Build(IReadOnlyList<JapaneseCandleEntry> candles)
    {
        if (candles.Count == 0)
        {
            return new CandlestickChartRenderData(
                Array.Empty<CandlestickRenderItem>(),
                SupportedIndicatorDefinitions,
                Array.Empty<ChartIndicatorRenderSeries>(),
                320d);
        }

        var minPrice = candles.Min(x => x.Low);
        var maxPrice = candles.Max(x => x.High);
        var range = maxPrice - minPrice;
        var labelStride = CalculateLabelStride(candles.Count);

        var candlesticks = candles.Select((candle, index) =>
        {
            var high = NormalizeY(candle.High, minPrice, range, ChartHeight);
            var low = NormalizeY(candle.Low, minPrice, range, ChartHeight);
            var open = NormalizeY(candle.Open, minPrice, range, ChartHeight);
            var close = NormalizeY(candle.Close, minPrice, range, ChartHeight);

            var bodyTop = Math.Min(open, close);
            var bodyHeight = Math.Max(2d, Math.Abs(open - close));
            var wickTop = Math.Min(high, low);
            var wickHeight = Math.Max(1d, Math.Abs(low - high));
            var candleColor = candle.Close >= candle.Open ? "#D62828" : "#1D4ED8";

            return new CandlestickRenderItem
            {
                Label = candle.Date.ToString("M/d", CultureInfo.CurrentCulture),
                IsLabelVisible = index % labelStride == 0 || index == candles.Count - 1,
                WickTop = wickTop,
                WickHeight = wickHeight,
                BodyTop = bodyTop,
                BodyHeight = bodyHeight,
                BodyColor = candleColor,
                WickColor = candleColor,
                DateText = candle.Date.ToString("yyyy/MM/dd", CultureInfo.CurrentCulture),
                OpenText = candle.Open.ToString("N2", CultureInfo.CurrentCulture),
                CloseText = candle.Close.ToString("N2", CultureInfo.CurrentCulture),
                HighText = candle.High.ToString("N2", CultureInfo.CurrentCulture),
                LowText = candle.Low.ToString("N2", CultureInfo.CurrentCulture)
            };
        }).ToList();

        var indicatorSeries = new List<ChartIndicatorRenderSeries>
        {
            BuildMovingAverageSeries(candles, minPrice, range, SupportedIndicatorDefinitions[0], 5, 1.8d, string.Empty),
            BuildMovingAverageSeries(candles, minPrice, range, SupportedIndicatorDefinitions[1], 25, 2.1d, string.Empty),
            BuildMovingAverageSeries(candles, minPrice, range, SupportedIndicatorDefinitions[2], 75, 2.7d, "8 4")
        };

        return new CandlestickChartRenderData(
            candlesticks,
            SupportedIndicatorDefinitions,
            indicatorSeries.Where(line => !string.IsNullOrWhiteSpace(line.Points)).ToList(),
            Math.Max(320d, candles.Count * CandleSlotWidth));
    }

    private static ChartIndicatorRenderSeries BuildMovingAverageSeries(
        IReadOnlyList<JapaneseCandleEntry> candles,
        decimal minPrice,
        decimal range,
        ChartIndicatorDefinition definition,
        int period,
        double strokeThickness,
        string strokeDashArray)
    {
        var pointTexts = new List<string>();

        for (var index = period - 1; index < candles.Count; index++)
        {
            var window = candles.Skip(index - period + 1).Take(period);
            var average = window.Average(item => item.Close);
            var x = index * CandleSlotWidth + CandleCenterX;
            var y = NormalizeY(average, minPrice, range, ChartHeight);
            pointTexts.Add($"{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)}");
        }

        return new ChartIndicatorRenderSeries
        {
            IndicatorKey = definition.IndicatorKey,
            IndicatorDisplayName = definition.DisplayName,
            LegendLabel = definition.DisplayName,
            Placement = definition.Placement,
            DisplayOrder = definition.DisplayOrder,
            Points = string.Join(" ", pointTexts),
            StrokeColor = definition.AccentColor,
            StrokeThickness = strokeThickness,
            StrokeDashArray = strokeDashArray
        };
    }

    private static int CalculateLabelStride(int count)
    {
        if (count <= 12)
        {
            return 1;
        }

        if (count <= 24)
        {
            return 2;
        }

        if (count <= 48)
        {
            return 4;
        }

        if (count <= 72)
        {
            return 6;
        }

        return 8;
    }

    private static double NormalizeY(decimal price, decimal minPrice, decimal range, double chartHeight)
    {
        if (range <= 0m)
        {
            return chartHeight / 2d;
        }

        var ratio = (price - minPrice) / range;
        return chartHeight - (double)ratio * chartHeight;
    }
}