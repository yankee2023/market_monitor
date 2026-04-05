namespace MarketMonitor.Features.SectorComparison.Models;

/// <summary>
/// セクター比較表示用データを表す。
/// </summary>
public sealed class SectorComparisonViewData
{
    /// <summary>
    /// 表示データを初期化する。
    /// </summary>
    public SectorComparisonViewData(string sectorName, string marketSegmentDisplay, IReadOnlyList<SectorComparisonPeerItem> peers)
    {
        SectorName = sectorName;
        MarketSegmentDisplay = marketSegmentDisplay;
        Peers = peers ?? throw new ArgumentNullException(nameof(peers));
    }

    /// <summary>
    /// セクター名。
    /// </summary>
    public string SectorName { get; }

    /// <summary>
    /// 表示用市場区分名。
    /// </summary>
    public string MarketSegmentDisplay { get; }

    /// <summary>
    /// 比較対象一覧。
    /// </summary>
    public IReadOnlyList<SectorComparisonPeerItem> Peers { get; }
}