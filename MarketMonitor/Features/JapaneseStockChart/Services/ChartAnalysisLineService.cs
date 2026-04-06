using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// 分析ラインの正規化と描画座標変換を提供する。
/// </summary>
public sealed class ChartAnalysisLineService : IChartAnalysisLineService
{
    private const double MinimumLineLengthRatioSquared = 0.0004d;

    /// <inheritdoc />
    public ChartAnalysisLine? CreateLine(
        ChartAnalysisLineType lineType,
        double startXRatio,
        double startYRatio,
        double endXRatio,
        double endYRatio)
    {
        var normalizedStartX = ClampRatio(startXRatio);
        var normalizedStartY = ClampRatio(startYRatio);
        var normalizedEndX = ClampRatio(endXRatio);
        var normalizedEndY = ClampRatio(endYRatio);
        var deltaX = normalizedEndX - normalizedStartX;
        var deltaY = normalizedEndY - normalizedStartY;

        if ((deltaX * deltaX) + (deltaY * deltaY) < MinimumLineLengthRatioSquared)
        {
            return null;
        }

        return new ChartAnalysisLine(Guid.NewGuid(), lineType, normalizedStartX, normalizedStartY, normalizedEndX, normalizedEndY);
    }

    /// <inheritdoc />
    public IReadOnlyList<ChartAnalysisLineRenderItem> CreateRenderItems(
        IReadOnlyList<ChartAnalysisLine> lines,
        double canvasWidth,
        double canvasHeight,
        Guid? selectedLineId = null)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (canvasWidth <= 0d || canvasHeight <= 0d)
        {
            return Array.Empty<ChartAnalysisLineRenderItem>();
        }

        return lines
            .Select(line => new ChartAnalysisLineRenderItem
            {
                Id = line.Id,
                X1 = ClampRatio(line.StartXRatio) * canvasWidth,
                Y1 = ClampRatio(line.StartYRatio) * canvasHeight,
                X2 = ClampRatio(line.EndXRatio) * canvasWidth,
                Y2 = ClampRatio(line.EndYRatio) * canvasHeight,
                StrokeColor = ChartAnalysisLineStyleCatalog.GetStrokeColor(line.LineType),
                StrokeThickness = selectedLineId == line.Id ? 3.2d : 2.2d,
                StrokeDashArray = ChartAnalysisLineStyleCatalog.GetStrokeDashArray(line.LineType),
                IsSelected = selectedLineId == line.Id
            })
            .ToArray();
    }

    /// <inheritdoc />
    public Guid? FindNearestLineId(
        IReadOnlyList<ChartAnalysisLine> lines,
        double xRatio,
        double yRatio,
        double hitToleranceRatio)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var normalizedXRatio = ClampRatio(xRatio);
        var normalizedYRatio = ClampRatio(yRatio);
        var safeTolerance = Math.Max(0d, hitToleranceRatio);
        double? bestDistance = null;
        Guid? bestId = null;

        foreach (var line in lines)
        {
            var distance = CalculateDistanceToSegment(
                normalizedXRatio,
                normalizedYRatio,
                line.StartXRatio,
                line.StartYRatio,
                line.EndXRatio,
                line.EndYRatio);

            if (distance > safeTolerance)
            {
                continue;
            }

            if (bestDistance is null || distance < bestDistance)
            {
                bestDistance = distance;
                bestId = line.Id;
            }
        }

        return bestId;
    }

    /// <inheritdoc />
    public ChartAnalysisLine MoveLine(ChartAnalysisLine line, double deltaXRatio, double deltaYRatio)
    {
        ArgumentNullException.ThrowIfNull(line);

        var clampedDeltaX = Math.Clamp(deltaXRatio, -Math.Min(line.StartXRatio, line.EndXRatio), 1d - Math.Max(line.StartXRatio, line.EndXRatio));
        var clampedDeltaY = Math.Clamp(deltaYRatio, -Math.Min(line.StartYRatio, line.EndYRatio), 1d - Math.Max(line.StartYRatio, line.EndYRatio));

        return new ChartAnalysisLine(
            line.Id,
            line.LineType,
            ClampRatio(line.StartXRatio + clampedDeltaX),
            ClampRatio(line.StartYRatio + clampedDeltaY),
            ClampRatio(line.EndXRatio + clampedDeltaX),
            ClampRatio(line.EndYRatio + clampedDeltaY));
    }

    private static double CalculateDistanceToSegment(
        double pointX,
        double pointY,
        double startX,
        double startY,
        double endX,
        double endY)
    {
        var deltaX = endX - startX;
        var deltaY = endY - startY;
        var lengthSquared = (deltaX * deltaX) + (deltaY * deltaY);

        if (lengthSquared <= double.Epsilon)
        {
            return Math.Sqrt(((pointX - startX) * (pointX - startX)) + ((pointY - startY) * (pointY - startY)));
        }

        var projection = ((pointX - startX) * deltaX + (pointY - startY) * deltaY) / lengthSquared;
        var clampedProjection = Math.Clamp(projection, 0d, 1d);
        var nearestX = startX + (clampedProjection * deltaX);
        var nearestY = startY + (clampedProjection * deltaY);
        var distanceX = pointX - nearestX;
        var distanceY = pointY - nearestY;
        return Math.Sqrt((distanceX * distanceX) + (distanceY * distanceY));
    }

    private static double ClampRatio(double ratio)
    {
        if (double.IsNaN(ratio) || double.IsInfinity(ratio))
        {
            return 0d;
        }

        return Math.Clamp(ratio, 0d, 1d);
    }
}