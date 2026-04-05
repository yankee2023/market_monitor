namespace MarketMonitor.Features.SectorComparison.Models;

/// <summary>
/// 同業比較対象の 1 銘柄を表す。
/// </summary>
public sealed class SectorComparisonPeerItem
{
    /// <summary>
    /// シンボル。
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 銘柄名。
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// 現在値。
    /// </summary>
    public decimal StockPrice { get; set; }

    /// <summary>
    /// 表示用価格文字列。
    /// </summary>
    public string StockPriceDisplay { get; set; } = string.Empty;

    /// <summary>
    /// 表示用市場区分文字列。
    /// </summary>
    public string MarketSegmentDisplay { get; set; } = "-";
}