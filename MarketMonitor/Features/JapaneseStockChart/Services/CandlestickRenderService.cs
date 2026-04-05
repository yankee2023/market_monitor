using System.Globalization;
using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// ローソク足描画用データを生成する。
/// </summary>
internal sealed class CandlestickRenderService
{
    private const double ChartHeight = 200d;
    private const double SecondaryChartHeight = 96d;
    private const double CandleSlotWidth = 34d;
    private const double CandleCenterX = 17d;
    private const double VolumeBarWidth = 16d;

    private static readonly IReadOnlyList<ChartIndicatorDefinition> SupportedIndicatorDefinitions =
    [
        new ChartIndicatorDefinition("ma5", "MA5", ChartIndicatorPlacement.OverlayPriceChart, "#F59E0B", true, 10),
        new ChartIndicatorDefinition("ma25", "MA25", ChartIndicatorPlacement.OverlayPriceChart, "#10B981", true, 20),
        new ChartIndicatorDefinition("ma75", "MA75", ChartIndicatorPlacement.OverlayPriceChart, "#334155", true, 30),
        new ChartIndicatorDefinition("volume", "出来高", ChartIndicatorPlacement.SecondaryPanel, "#64748B", true, 40),
        new ChartIndicatorDefinition("macd", "MACD", ChartIndicatorPlacement.SecondaryPanel, "#B91C1C", true, 50),
        new ChartIndicatorDefinition("rsi", "RSI", ChartIndicatorPlacement.SecondaryPanel, "#7C3AED", true, 60)
    ];

    /// <summary>
    /// 描画用ローソク足一覧を生成する。
    /// </summary>
    public static CandlestickChartRenderData Build(
        IReadOnlyList<JapaneseCandleEntry> candles,
        DateTime? visibleStartDate = null)
    {
        if (candles.Count == 0)
        {
            return new CandlestickChartRenderData(
                Array.Empty<CandlestickRenderItem>(),
                SupportedIndicatorDefinitions,
                Array.Empty<ChartIndicatorRenderSeries>(),
                Array.Empty<IndicatorPanelRenderData>(),
                320d);
        }

            var visibleStartIndex = FindVisibleStartIndex(candles, visibleStartDate);
            var visibleCandles = candles.Skip(visibleStartIndex).ToList();
            var minPrice = visibleCandles.Min(x => x.Low);
            var maxPrice = visibleCandles.Max(x => x.High);
        var range = maxPrice - minPrice;
            var labelStride = CalculateLabelStride(visibleCandles.Count);

            var candlesticks = visibleCandles.Select((candle, index) =>
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
                LowText = candle.Low.ToString("N2", CultureInfo.CurrentCulture),
                VolumeText = candle.Volume <= 0L ? "-" : candle.Volume.ToString("N0", CultureInfo.CurrentCulture)
            };
        }).ToList();

        var overlayIndicatorSeries = new List<ChartIndicatorRenderSeries>
        {
            BuildMovingAverageSeries(candles, visibleStartIndex, minPrice, range, SupportedIndicatorDefinitions[0], 5, 1.8d, string.Empty),
            BuildMovingAverageSeries(candles, visibleStartIndex, minPrice, range, SupportedIndicatorDefinitions[1], 25, 2.1d, string.Empty),
            BuildMovingAverageSeries(candles, visibleStartIndex, minPrice, range, SupportedIndicatorDefinitions[2], 75, 2.7d, "8 4")
        };

        var indicatorPanels = new List<IndicatorPanelRenderData>();
        var volumePanel = BuildVolumePanel(visibleCandles);
        if (volumePanel is not null)
        {
            indicatorPanels.Add(volumePanel);
        }

        var macdPanel = BuildMacdPanel(candles, visibleStartIndex);
        if (macdPanel is not null)
        {
            indicatorPanels.Add(macdPanel);
        }

        var rsiPanel = BuildRsiPanel(candles, visibleStartIndex);
        if (rsiPanel is not null)
        {
            indicatorPanels.Add(rsiPanel);
        }

        return new CandlestickChartRenderData(
            candlesticks,
            SupportedIndicatorDefinitions,
            overlayIndicatorSeries.Where(line => !string.IsNullOrWhiteSpace(line.Points)).ToList(),
            indicatorPanels.OrderBy(item => item.DisplayOrder).ToList(),
            Math.Max(320d, visibleCandles.Count * CandleSlotWidth));
    }

    private static ChartIndicatorRenderSeries BuildMovingAverageSeries(
        IReadOnlyList<JapaneseCandleEntry> candles,
        int visibleStartIndex,
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
            if (index < visibleStartIndex)
            {
                continue;
            }

            var window = candles.Skip(index - period + 1).Take(period);
            var average = window.Average(item => item.Close);
            var x = (index - visibleStartIndex) * CandleSlotWidth + CandleCenterX;
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

    private static IndicatorPanelRenderData? BuildVolumePanel(IReadOnlyList<JapaneseCandleEntry> candles)
    {
        var maxVolume = candles.Max(item => item.Volume);
        if (maxVolume <= 0L)
        {
            return null;
        }

        var bars = candles.Select((candle, index) =>
        {
            var height = NormalizeBarHeight(candle.Volume, maxVolume, SecondaryChartHeight);
            return new ChartIndicatorBarItem
            {
                Left = index * CandleSlotWidth + ((CandleSlotWidth - VolumeBarWidth) / 2d),
                Top = SecondaryChartHeight - height,
                Width = VolumeBarWidth,
                Height = height,
                FillColor = candle.Close >= candle.Open ? "#C81E1E" : "#1D4ED8"
            };
        }).ToList();

        return new IndicatorPanelRenderData(
            "volume",
            "出来高",
            SupportedIndicatorDefinitions[3].DisplayOrder,
            "N0",
            null,
            Array.Empty<ChartIndicatorRenderSeries>(),
            bars,
            Array.Empty<IndicatorReferenceLine>(),
            0m,
            maxVolume,
            BuildVolumeHoverItems(candles));
    }

    private static IndicatorPanelRenderData? BuildMacdPanel(IReadOnlyList<JapaneseCandleEntry> candles, int visibleStartIndex)
    {
        if (candles.Count < 26)
        {
            return null;
        }

        var closes = candles.Select(candle => candle.Close).ToArray();
        var ema12 = CalculateExponentialMovingAverage(closes, 12);
        var ema26 = CalculateExponentialMovingAverage(closes, 26);
        var macdValues = new decimal?[closes.Length];

        for (var index = 0; index < closes.Length; index++)
        {
            if (ema12[index].HasValue && ema26[index].HasValue)
            {
                macdValues[index] = ema12[index]!.Value - ema26[index]!.Value;
            }
        }

        var signalValues = CalculateExponentialMovingAverage(macdValues, 9);
        var visibleMacdValues = macdValues.Skip(visibleStartIndex).Where(value => value.HasValue).Select(value => value!.Value).ToList();
        var visibleSignalValues = signalValues.Skip(visibleStartIndex).Where(value => value.HasValue).Select(value => value!.Value).ToList();
        var allVisibleValues = visibleMacdValues.Concat(visibleSignalValues).ToList();
        if (allVisibleValues.Count == 0)
        {
            return null;
        }

        var minValue = Math.Min(allVisibleValues.Min(), 0m);
        var maxValue = Math.Max(allVisibleValues.Max(), 0m);
        var range = maxValue - minValue;

        var lineSeries = new List<ChartIndicatorRenderSeries>
        {
            BuildSecondaryLineSeries("macd", "MACD", "MACD", SupportedIndicatorDefinitions[4].DisplayOrder, "#B91C1C", macdValues, visibleStartIndex, minValue, range, 2.8d, string.Empty),
            BuildSecondaryLineSeries("macd", "シグナル", "シグナル", SupportedIndicatorDefinitions[4].DisplayOrder + 1, "#1D4ED8", signalValues, visibleStartIndex, minValue, range, 2.2d, "4 3")
        };

        return new IndicatorPanelRenderData(
            "macd",
            "MACD",
            SupportedIndicatorDefinitions[4].DisplayOrder,
            "N2",
            NormalizeY(0m, minValue, range, SecondaryChartHeight),
            lineSeries.Where(item => !string.IsNullOrWhiteSpace(item.Points)).ToList(),
            Array.Empty<ChartIndicatorBarItem>(),
            Array.Empty<IndicatorReferenceLine>(),
            minValue,
            maxValue,
            BuildMacdHoverItems(candles, visibleStartIndex, macdValues, signalValues));
    }

    private static IndicatorPanelRenderData? BuildRsiPanel(IReadOnlyList<JapaneseCandleEntry> candles, int visibleStartIndex)
    {
        var rsiValues = BuildRsiValues(candles, 14);
        if (!rsiValues.Skip(visibleStartIndex).Any(item => item.HasValue))
        {
            return null;
        }

        var lineSeries = new[]
        {
            BuildSecondaryLineSeries("rsi", "RSI", "RSI", SupportedIndicatorDefinitions[5].DisplayOrder, SupportedIndicatorDefinitions[5].AccentColor, rsiValues, visibleStartIndex, 0m, 100m, 2.6d, string.Empty)
        };

        var referenceLines = new[]
        {
            new IndicatorReferenceLine
            {
                Label = "70",
                Top = NormalizeY(70m, 0m, 100m, SecondaryChartHeight),
                StrokeColor = "#DC2626",
                StrokeDashArray = "4 3"
            },
            new IndicatorReferenceLine
            {
                Label = "30",
                Top = NormalizeY(30m, 0m, 100m, SecondaryChartHeight),
                StrokeColor = "#2563EB",
                StrokeDashArray = "4 3"
            }
        };

        return new IndicatorPanelRenderData(
            "rsi",
            "RSI",
            SupportedIndicatorDefinitions[5].DisplayOrder,
            "N2",
            null,
            lineSeries.Where(item => !string.IsNullOrWhiteSpace(item.Points)).ToList(),
            Array.Empty<ChartIndicatorBarItem>(),
            referenceLines,
            0m,
            100m,
            BuildRsiHoverItems(candles, visibleStartIndex, rsiValues));
    }

    private static List<IndicatorPanelHoverItem> BuildVolumeHoverItems(IReadOnlyList<JapaneseCandleEntry> candles)
    {
        return candles.Select((candle, index) => new IndicatorPanelHoverItem
        {
            Left = index * CandleSlotWidth,
            Width = CandleSlotWidth,
            TooltipText = $"日付: {candle.Date:yyyy/MM/dd}\n出来高: {candle.Volume.ToString("N0", CultureInfo.CurrentCulture)}"
        }).ToList();
    }

    private static List<IndicatorPanelHoverItem> BuildMacdHoverItems(
        IReadOnlyList<JapaneseCandleEntry> candles,
        int visibleStartIndex,
        decimal?[] macdValues,
        decimal?[] signalValues)
    {
        var items = new List<IndicatorPanelHoverItem>();
        for (var index = visibleStartIndex; index < candles.Count; index++)
        {
            items.Add(new IndicatorPanelHoverItem
            {
                Left = (index - visibleStartIndex) * CandleSlotWidth,
                Width = CandleSlotWidth,
                TooltipText = string.Join("\n",
                [
                    $"日付: {candles[index].Date:yyyy/MM/dd}",
                    $"MACD: {FormatIndicatorValue(macdValues[index])}",
                    $"シグナル: {FormatIndicatorValue(signalValues[index])}"
                ])
            });
        }

        return items;
    }

    private static List<IndicatorPanelHoverItem> BuildRsiHoverItems(
        IReadOnlyList<JapaneseCandleEntry> candles,
        int visibleStartIndex,
        decimal?[] rsiValues)
    {
        var items = new List<IndicatorPanelHoverItem>();
        for (var index = visibleStartIndex; index < candles.Count; index++)
        {
            items.Add(new IndicatorPanelHoverItem
            {
                Left = (index - visibleStartIndex) * CandleSlotWidth,
                Width = CandleSlotWidth,
                TooltipText = string.Join("\n",
                [
                    $"日付: {candles[index].Date:yyyy/MM/dd}",
                    $"RSI: {FormatIndicatorValue(rsiValues[index])}"
                ])
            });
        }

        return items;
    }

    private static ChartIndicatorRenderSeries BuildSecondaryLineSeries(
        string indicatorKey,
        string indicatorDisplayName,
        string legendLabel,
        int displayOrder,
        string strokeColor,
        decimal?[] values,
        int visibleStartIndex,
        decimal minValue,
        decimal range,
        double strokeThickness,
        string strokeDashArray)
    {
        var pointTexts = new List<string>();

        for (var index = 0; index < values.Length; index++)
        {
            if (index < visibleStartIndex || !values[index].HasValue)
            {
                continue;
            }

            var x = (index - visibleStartIndex) * CandleSlotWidth + CandleCenterX;
            var y = NormalizeY(values[index]!.Value, minValue, range, SecondaryChartHeight);
            pointTexts.Add($"{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)}");
        }

        return new ChartIndicatorRenderSeries
        {
            IndicatorKey = indicatorKey,
            IndicatorDisplayName = indicatorDisplayName,
            LegendLabel = legendLabel,
            Placement = ChartIndicatorPlacement.SecondaryPanel,
            DisplayOrder = displayOrder,
            Points = string.Join(" ", pointTexts),
            StrokeColor = strokeColor,
            StrokeThickness = strokeThickness,
            StrokeDashArray = strokeDashArray
        };
    }

    private static decimal?[] CalculateExponentialMovingAverage(decimal[] values, int period)
    {
        var result = new decimal?[values.Length];
        if (values.Length < period)
        {
            return result;
        }

        var multiplier = 2m / (period + 1m);
        decimal? previous = null;

        for (var index = 0; index < values.Length; index++)
        {
            if (index < period - 1)
            {
                continue;
            }

            if (previous is null)
            {
                previous = values.Skip(index - period + 1).Take(period).Average();
            }
            else
            {
                previous = ((values[index] - previous.Value) * multiplier) + previous.Value;
            }

            result[index] = previous;
        }

        return result;
    }

    private static decimal?[] CalculateExponentialMovingAverage(decimal?[] values, int period)
    {
        var result = new decimal?[values.Length];
        var validValues = new List<decimal>();
        decimal? previous = null;
        var multiplier = 2m / (period + 1m);

        for (var index = 0; index < values.Length; index++)
        {
            if (!values[index].HasValue)
            {
                continue;
            }

            validValues.Add(values[index]!.Value);
            if (validValues.Count < period)
            {
                continue;
            }

            if (previous is null)
            {
                previous = validValues.TakeLast(period).Average();
            }
            else
            {
                previous = ((values[index]!.Value - previous.Value) * multiplier) + previous.Value;
            }

            result[index] = previous;
        }

        return result;
    }

    private static decimal?[] BuildRsiValues(IReadOnlyList<JapaneseCandleEntry> candles, int period)
    {
        var result = new decimal?[candles.Count];
        if (candles.Count <= period)
        {
            return result;
        }

        decimal gainSum = 0m;
        decimal lossSum = 0m;

        for (var index = 1; index <= period; index++)
        {
            var delta = candles[index].Close - candles[index - 1].Close;
            if (delta >= 0m)
            {
                gainSum += delta;
            }
            else
            {
                lossSum += Math.Abs(delta);
            }
        }

        var averageGain = gainSum / period;
        var averageLoss = lossSum / period;
        result[period] = CalculateRsi(averageGain, averageLoss);

        for (var index = period + 1; index < candles.Count; index++)
        {
            var delta = candles[index].Close - candles[index - 1].Close;
            var gain = delta > 0m ? delta : 0m;
            var loss = delta < 0m ? Math.Abs(delta) : 0m;
            averageGain = ((averageGain * (period - 1)) + gain) / period;
            averageLoss = ((averageLoss * (period - 1)) + loss) / period;
            result[index] = CalculateRsi(averageGain, averageLoss);
        }

        return result;
    }

    private static decimal CalculateRsi(decimal averageGain, decimal averageLoss)
    {
        if (averageGain == 0m && averageLoss == 0m)
        {
            return 50m;
        }

        if (averageLoss == 0m)
        {
            return 100m;
        }

        var relativeStrength = averageGain / averageLoss;
        return 100m - (100m / (1m + relativeStrength));
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

    private static int FindVisibleStartIndex(IReadOnlyList<JapaneseCandleEntry> candles, DateTime? visibleStartDate)
    {
        if (!visibleStartDate.HasValue)
        {
            return 0;
        }

        for (var index = 0; index < candles.Count; index++)
        {
            if (candles[index].Date >= visibleStartDate.Value)
            {
                return index;
            }
        }

        return Math.Max(0, candles.Count - 1);
    }

    private static double NormalizeBarHeight(long value, long maxValue, double chartHeight)
    {
        if (maxValue <= 0L || value <= 0L)
        {
            return 0d;
        }

        return (double)value / maxValue * chartHeight;
    }

    private static string FormatIndicatorValue(decimal? value)
    {
        return value.HasValue ? value.Value.ToString("N2", CultureInfo.CurrentCulture) : "-";
    }
}