namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// 指標パネル上の基準線を表す。
/// </summary>
public sealed class IndicatorReferenceLine
{
    /// <summary>
    /// 表示ラベル。
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 上端位置。
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// 線色。
    /// </summary>
    public string StrokeColor { get; set; } = string.Empty;

    /// <summary>
    /// 破線パターン。
    /// </summary>
    public string StrokeDashArray { get; set; } = string.Empty;
}