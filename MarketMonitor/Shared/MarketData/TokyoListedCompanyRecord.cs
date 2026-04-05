namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// JPX 上場銘柄一覧の 1 行分を表す。
/// </summary>
internal readonly record struct TokyoListedCompanyRecord(
    string? Code,
    string? CompanyName,
    string? MarketCategory,
    string? SectorName)
{
    /// <summary>
    /// 市場区分。
    /// </summary>
    public TokyoMarketSegment MarketSegment => TokyoMarketSegmentParser.Parse(MarketCategory);
}