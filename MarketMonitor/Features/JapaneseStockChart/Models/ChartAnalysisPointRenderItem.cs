namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// 手動描画中の始点を示す描画座標。
/// </summary>
public sealed class ChartAnalysisPointRenderItem
{
    /// <summary>
    /// X 座標。
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y 座標。
    /// </summary>
    public double Y { get; init; }
}
