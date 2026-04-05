namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// チャート指標の描画シリーズを表す。
/// </summary>
public sealed class ChartIndicatorRenderSeries
{
    /// <summary>
    /// 指標キー。
    /// </summary>
    public string IndicatorKey { get; set; } = string.Empty;

    /// <summary>
    /// 指標表示名。
    /// </summary>
    public string IndicatorDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 凡例表示名。
    /// </summary>
    public string LegendLabel { get; set; } = string.Empty;

    /// <summary>
    /// 表示位置。
    /// </summary>
    public ChartIndicatorPlacement Placement { get; set; }

    /// <summary>
    /// 表示順。
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Polyline 用のポイント文字列。
    /// </summary>
    public string Points { get; set; } = string.Empty;

    /// <summary>
    /// 線色。
    /// </summary>
    public string StrokeColor { get; set; } = string.Empty;

    /// <summary>
    /// 線の太さ。
    /// </summary>
    public double StrokeThickness { get; set; }

    /// <summary>
    /// 破線パターン。
    /// </summary>
    public string StrokeDashArray { get; set; } = string.Empty;
}