namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// 分析ラインの描画用座標を表す。
/// </summary>
public sealed class ChartAnalysisLineRenderItem
{
    /// <summary>
    /// 分析ライン識別子。
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 始点 X 座標。
    /// </summary>
    public double X1 { get; init; }

    /// <summary>
    /// 始点 Y 座標。
    /// </summary>
    public double Y1 { get; init; }

    /// <summary>
    /// 終点 X 座標。
    /// </summary>
    public double X2 { get; init; }

    /// <summary>
    /// 終点 Y 座標。
    /// </summary>
    public double Y2 { get; init; }

    /// <summary>
    /// 線色。
    /// </summary>
    public string StrokeColor { get; init; } = "#7C3AED";

    /// <summary>
    /// 線幅。
    /// </summary>
    public double StrokeThickness { get; init; } = 2.2d;

    /// <summary>
    /// 破線設定。
    /// </summary>
    public string StrokeDashArray { get; init; } = string.Empty;

    /// <summary>
    /// 選択状態。
    /// </summary>
    public bool IsSelected { get; init; }
}