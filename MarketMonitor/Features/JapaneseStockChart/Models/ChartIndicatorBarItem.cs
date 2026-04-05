namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// 指標バー描画項目を表す。
/// </summary>
public sealed class ChartIndicatorBarItem
{
    /// <summary>
    /// 左端位置。
    /// </summary>
    public double Left { get; set; }

    /// <summary>
    /// 上端位置。
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// 幅。
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 高さ。
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 塗り色。
    /// </summary>
    public string FillColor { get; set; } = string.Empty;
}