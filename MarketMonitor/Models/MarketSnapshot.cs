namespace MarketMonitor.Models;

/// <summary>
/// 取得したマーケットデータを保持するモデル。
/// </summary>
public sealed class MarketSnapshot
{
    /// <summary>
    /// 為替レート（USD/JPY）。
    /// </summary>
    public decimal ExchangeRate { get; init; }

    /// <summary>
    /// 株価。
    /// </summary>
    public decimal StockPrice { get; init; }

    /// <summary>
    /// 取得対象シンボル。
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// 為替データの更新時刻。
    /// </summary>
    public DateTimeOffset ExchangeUpdatedAt { get; init; }

    /// <summary>
    /// 株価データの更新時刻。
    /// </summary>
    public DateTimeOffset StockUpdatedAt { get; init; }
}
