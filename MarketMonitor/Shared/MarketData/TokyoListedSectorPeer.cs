namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 同一セクター比較用の東証銘柄情報を表す。
/// </summary>
public sealed class TokyoListedSectorPeer
{
    /// <summary>
    /// シンボル。
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// 銘柄名。
    /// </summary>
    public string CompanyName { get; init; } = string.Empty;

    /// <summary>
    /// セクター名。
    /// </summary>
    public string SectorName { get; init; } = string.Empty;

    /// <summary>
    /// 市場区分。
    /// </summary>
    public TokyoMarketSegment MarketSegment { get; init; }
}