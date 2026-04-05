namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 東証市場区分設定の読込抽象を表す。
/// </summary>
public interface ITokyoMarketSegmentSettingsProvider
{
    /// <summary>
    /// サポート対象の市場区分一覧を読み込む。
    /// </summary>
    IReadOnlyCollection<TokyoMarketSegment> LoadSupportedSegments();
}