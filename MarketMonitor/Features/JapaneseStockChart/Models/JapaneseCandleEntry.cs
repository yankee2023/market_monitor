namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// 日本株ローソク足の元データを表す。
/// </summary>
public sealed class JapaneseCandleEntry
{
    /// <summary>
    /// 日付。
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 始値。
    /// </summary>
    public decimal Open { get; set; }

    /// <summary>
    /// 高値。
    /// </summary>
    public decimal High { get; set; }

    /// <summary>
    /// 安値。
    /// </summary>
    public decimal Low { get; set; }

    /// <summary>
    /// 終値。
    /// </summary>
    public decimal Close { get; set; }
}