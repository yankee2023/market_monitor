using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// ローソク足からトレンドライン、支持線、抵抗線を自動生成する。
/// </summary>
public sealed class AutoChartAnalysisLineService : IAutoChartAnalysisLineService
{
    private const int MinimumCandleCount = 12;
    private const int PivotWindow = 2;
    private const decimal HorizontalToleranceRate = 0.025m;

    /// <inheritdoc />
    public IReadOnlyList<ChartAnalysisLine> Generate(IReadOnlyList<JapaneseCandleEntry> candles)
    {
        ArgumentNullException.ThrowIfNull(candles);

        if (candles.Count < MinimumCandleCount)
        {
            return Array.Empty<ChartAnalysisLine>();
        }

        var orderedCandles = candles.OrderBy(item => item.Date).ToList();
        var minPrice = orderedCandles.Min(item => item.Low);
        var maxPrice = orderedCandles.Max(item => item.High);
        var priceRange = maxPrice - minPrice;
        if (priceRange <= 0m)
        {
            return Array.Empty<ChartAnalysisLine>();
        }

        var lowPivots = FindPivotPoints(orderedCandles, useLows: true);
        var highPivots = FindPivotPoints(orderedCandles, useLows: false);
        var trendLine = BuildTrendLine(orderedCandles, lowPivots, highPivots, minPrice, maxPrice, priceRange);
        var supportLine = BuildHorizontalLine(
            orderedCandles,
            lowPivots,
            minPrice,
            maxPrice,
            priceRange,
            ChartAnalysisLineType.SupportLine,
            upperBoundRatio: 0.55m,
            fallbackPriceSelector: source => source.Min(item => item.Low));
        var resistanceLine = BuildHorizontalLine(
            orderedCandles,
            highPivots,
            minPrice,
            maxPrice,
            priceRange,
            ChartAnalysisLineType.ResistanceLine,
            lowerBoundRatio: 0.45m,
            fallbackPriceSelector: source => source.Max(item => item.High));

        return new[] { trendLine, supportLine, resistanceLine }
            .Where(line => line is not null)
            .Cast<ChartAnalysisLine>()
            .ToArray();
    }

    private static List<PivotPoint> FindPivotPoints(List<JapaneseCandleEntry> candles, bool useLows)
    {
        var pivots = new List<PivotPoint>();

        for (var index = PivotWindow; index < candles.Count - PivotWindow; index++)
        {
            var price = useLows ? candles[index].Low : candles[index].High;
            var isPivot = true;

            for (var offset = 1; offset <= PivotWindow; offset++)
            {
                var previousPrice = useLows ? candles[index - offset].Low : candles[index - offset].High;
                var nextPrice = useLows ? candles[index + offset].Low : candles[index + offset].High;

                if (useLows)
                {
                    if (price > previousPrice || price > nextPrice)
                    {
                        isPivot = false;
                        break;
                    }
                }
                else if (price < previousPrice || price < nextPrice)
                {
                    isPivot = false;
                    break;
                }
            }

            if (isPivot)
            {
                pivots.Add(new PivotPoint(index, price));
            }
        }

        return pivots;
    }

    private static ChartAnalysisLine BuildTrendLine(
        List<JapaneseCandleEntry> candles,
        List<PivotPoint> lowPivots,
        List<PivotPoint> highPivots,
        decimal minPrice,
        decimal maxPrice,
        decimal priceRange)
    {
        var slope = CalculateCloseSlope(candles);
        var isUpTrend = slope >= 0m;
        var pivotPair = isUpTrend
            ? FindTrendPivotPair(lowPivots, expectHigherSecondPoint: true)
            : FindTrendPivotPair(highPivots, expectHigherSecondPoint: false);

        decimal startPrice;
        decimal endPrice;
        double startXRatio;
        double endXRatio;

        if (pivotPair is not null)
        {
            startXRatio = ConvertIndexToRatio(pivotPair.Value.Start.Index, candles.Count);
            endXRatio = ConvertIndexToRatio(pivotPair.Value.End.Index, candles.Count);
            startPrice = pivotPair.Value.Start.Price;
            endPrice = pivotPair.Value.End.Price;
        }
        else
        {
            startXRatio = 0d;
            endXRatio = 1d;
            startPrice = candles[0].Close;
            endPrice = candles[^1].Close;
        }

        var startYRatio = ConvertPriceToRatio(startPrice, minPrice, maxPrice, priceRange);
        var endYRatio = ConvertPriceToRatio(endPrice, minPrice, maxPrice, priceRange);
        var deltaXRatio = Math.Max(endXRatio - startXRatio, 0.001d);
        var trendSlope = (endYRatio - startYRatio) / deltaXRatio;
        var extendedStartYRatio = ClampRatio(startYRatio - (trendSlope * startXRatio));
        var extendedEndYRatio = ClampRatio(startYRatio + (trendSlope * (1d - startXRatio)));

        return new ChartAnalysisLine(
            Guid.NewGuid(),
            ChartAnalysisLineType.TrendLine,
            0d,
            extendedStartYRatio,
            1d,
            extendedEndYRatio);
    }

    private static ChartAnalysisLine BuildHorizontalLine(
        List<JapaneseCandleEntry> candles,
        List<PivotPoint> pivots,
        decimal minPrice,
        decimal maxPrice,
        decimal priceRange,
        ChartAnalysisLineType lineType,
        decimal? lowerBoundRatio = null,
        decimal? upperBoundRatio = null,
        Func<List<JapaneseCandleEntry>, decimal>? fallbackPriceSelector = null)
    {
        var lowerBound = lowerBoundRatio.HasValue ? minPrice + (priceRange * lowerBoundRatio.Value) : decimal.MinValue;
        var upperBound = upperBoundRatio.HasValue ? minPrice + (priceRange * upperBoundRatio.Value) : decimal.MaxValue;
        var filteredPivots = pivots
            .Where(pivot => pivot.Price >= lowerBound && pivot.Price <= upperBound)
            .ToList();

        decimal selectedPrice;
        if (TryFindHorizontalCluster(filteredPivots, priceRange, out var clusteredPrice))
        {
            selectedPrice = clusteredPrice;
        }
        else
        {
            selectedPrice = fallbackPriceSelector?.Invoke(candles)
                ?? (lineType == ChartAnalysisLineType.SupportLine
                    ? candles.Min(item => item.Low)
                    : candles.Max(item => item.High));
        }

        var yRatio = ConvertPriceToRatio(selectedPrice, minPrice, maxPrice, priceRange);
        return new ChartAnalysisLine(Guid.NewGuid(), lineType, 0d, yRatio, 1d, yRatio);
    }

    private static bool TryFindHorizontalCluster(
        List<PivotPoint> pivots,
        decimal priceRange,
        out decimal clusteredPrice)
    {
        clusteredPrice = 0m;
        if (pivots.Count == 0)
        {
            return false;
        }

        var tolerance = Math.Max(priceRange * HorizontalToleranceRate, 0.01m);
        List<PivotPoint>? bestCluster = null;
        var bestScore = int.MinValue;

        foreach (var anchor in pivots.OrderByDescending(item => item.Index).Take(8))
        {
            var cluster = pivots
                .Where(item => Math.Abs(item.Price - anchor.Price) <= tolerance)
                .OrderBy(item => item.Index)
                .ToList();

            if (cluster.Count < 2)
            {
                continue;
            }

            var span = cluster[^1].Index - cluster[0].Index;
            var score = (cluster.Count * 1000) + span;
            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestCluster = cluster;
        }

        if (bestCluster is null)
        {
            return false;
        }

        clusteredPrice = bestCluster.Average(item => item.Price);
        return true;
    }

    private static PivotPair? FindTrendPivotPair(List<PivotPoint> pivots, bool expectHigherSecondPoint)
    {
        if (pivots.Count < 2)
        {
            return null;
        }

        PivotPair? bestPair = null;
        var bestScore = decimal.MinValue;

        for (var startIndex = 0; startIndex < pivots.Count - 1; startIndex++)
        {
            for (var endIndex = startIndex + 1; endIndex < pivots.Count; endIndex++)
            {
                var start = pivots[startIndex];
                var end = pivots[endIndex];
                var isValid = expectHigherSecondPoint
                    ? end.Price >= start.Price
                    : end.Price <= start.Price;
                if (!isValid)
                {
                    continue;
                }

                var span = end.Index - start.Index;
                if (span < 4)
                {
                    continue;
                }

                var priceScore = expectHigherSecondPoint
                    ? end.Price - start.Price
                    : start.Price - end.Price;
                var totalScore = priceScore + span;
                if (totalScore <= bestScore)
                {
                    continue;
                }

                bestScore = totalScore;
                bestPair = new PivotPair(start, end);
            }
        }

        return bestPair;
    }

    private static decimal CalculateCloseSlope(List<JapaneseCandleEntry> candles)
    {
        if (candles.Count <= 1)
        {
            return 0m;
        }

        decimal xAverage = (candles.Count - 1) / 2m;
        var yAverage = candles.Average(item => item.Close);
        decimal numerator = 0m;
        decimal denominator = 0m;

        for (var index = 0; index < candles.Count; index++)
        {
            var x = index - xAverage;
            var y = candles[index].Close - yAverage;
            numerator += x * y;
            denominator += x * x;
        }

        return denominator <= 0m ? 0m : numerator / denominator;
    }

    private static double ConvertIndexToRatio(int index, int count)
    {
        if (count <= 1)
        {
            return 0d;
        }

        return ClampRatio((double)index / (count - 1));
    }

    private static double ConvertPriceToRatio(decimal price, decimal minPrice, decimal maxPrice, decimal priceRange)
    {
        if (priceRange <= 0m || maxPrice <= minPrice)
        {
            return 0.5d;
        }

        var ratio = (double)((maxPrice - price) / priceRange);
        return ClampRatio(ratio);
    }

    private static double ClampRatio(double ratio)
    {
        if (double.IsNaN(ratio) || double.IsInfinity(ratio))
        {
            return 0d;
        }

        return Math.Clamp(ratio, 0d, 1d);
    }

    private readonly record struct PivotPoint(int Index, decimal Price);

    private readonly record struct PivotPair(PivotPoint Start, PivotPoint End);
}