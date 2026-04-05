namespace MarketMonitor.Features.PriceHistory.Models;

/// <summary>
/// 保存済み価格履歴の1件分を表す。
/// </summary>
public sealed class PriceHistoryEntry
{
    /// <summary>
    /// レコード ID。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 銘柄シンボル。
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 保存時点の株価。
    /// </summary>
    public decimal StockPrice { get; set; }

    /// <summary>
    /// 記録時刻。
    /// </summary>
    public DateTimeOffset RecordedAt { get; set; }
}