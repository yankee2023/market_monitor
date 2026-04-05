namespace MarketMonitor.Features.MarketSnapshot.Models;

/// <summary>
/// 画面表示用の日本株最新スナップショットを表す。
/// </summary>
public sealed class MarketSnapshot
{
    /// <summary>
    /// 正規化済みシンボル。
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 銘柄名。
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// 株価。
    /// </summary>
    public decimal StockPrice { get; set; }

    /// <summary>
    /// 株価の更新時刻。
    /// </summary>
    public DateTimeOffset StockUpdatedAt { get; set; }
}