namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// テクニカル分析対象として東証プライム、スタンダード、グロースを扱う市場区分ポリシー。
/// </summary>
public sealed class TokyoMainMarketSegmentPolicy : ITokyoMarketSegmentPolicy
{
    private static readonly HashSet<TokyoMarketSegment> SupportedSegments =
    [
        TokyoMarketSegment.Prime,
        TokyoMarketSegment.Standard,
        TokyoMarketSegment.Growth
    ];

    /// <inheritdoc />
    public bool Includes(TokyoMarketSegment marketSegment)
    {
        return SupportedSegments.Contains(marketSegment);
    }
}