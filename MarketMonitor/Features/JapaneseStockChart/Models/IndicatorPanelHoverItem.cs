namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// 下段指標パネル上のホバー領域を表す。
/// </summary>
public sealed class IndicatorPanelHoverItem
{
    /// <summary>
    /// 左端位置。
    /// </summary>
    public double Left { get; set; }

    /// <summary>
    /// 幅。
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// ツールチップ表示文字列。
    /// </summary>
    public string TooltipText { get; set; } = string.Empty;
}