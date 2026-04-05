namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 設定値に基づいて対象市場区分を切り替えるポリシーを表す。
/// </summary>
public sealed class ConfigurableTokyoMarketSegmentPolicy : ITokyoMarketSegmentPolicy
{
    private readonly HashSet<TokyoMarketSegment> _supportedSegments;

    /// <summary>
    /// ポリシーを初期化する。
    /// </summary>
    public ConfigurableTokyoMarketSegmentPolicy(IEnumerable<TokyoMarketSegment> supportedSegments)
    {
        ArgumentNullException.ThrowIfNull(supportedSegments);

        _supportedSegments = supportedSegments
            .Where(segment => segment != TokyoMarketSegment.Unknown)
            .ToHashSet();

        if (_supportedSegments.Count == 0)
        {
            throw new ArgumentException("少なくとも 1 つの市場区分を指定してください。", nameof(supportedSegments));
        }
    }

    /// <inheritdoc />
    public bool Includes(TokyoMarketSegment marketSegment)
    {
        return _supportedSegments.Contains(marketSegment);
    }
}