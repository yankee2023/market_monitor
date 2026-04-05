namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// サポート対象とする東証市場区分のポリシーを表す。
/// </summary>
public interface ITokyoMarketSegmentPolicy
{
    /// <summary>
    /// 対象市場区分として扱うかどうかを判定する。
    /// </summary>
    bool Includes(TokyoMarketSegment marketSegment);
}