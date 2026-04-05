namespace MarketMonitor.Features.PriceHistory.Models;

/// <summary>
/// 価格履歴機能の表示用データをまとめる。
/// </summary>
public sealed class PriceHistoryViewData
{
    /// <summary>
    /// 表示データを初期化する。
    /// </summary>
    public PriceHistoryViewData(IReadOnlyList<PriceHistoryEntry> items, IReadOnlyList<PriceHistoryBar> bars)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        Bars = bars ?? throw new ArgumentNullException(nameof(bars));
    }

    /// <summary>
    /// 履歴一覧データ。
    /// </summary>
    public IReadOnlyList<PriceHistoryEntry> Items { get; }

    /// <summary>
    /// 履歴バー描画データ。
    /// </summary>
    public IReadOnlyList<PriceHistoryBar> Bars { get; }
}