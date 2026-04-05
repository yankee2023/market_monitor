using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// 日本株ローソク足取得機能の抽象。
/// </summary>
public interface IJapaneseCandleService
{
    /// <summary>
    /// 日本株のローソク足データを取得する。
    /// </summary>
    Task<IReadOnlyList<JapaneseCandleEntry>> GetJapaneseCandlesAsync(
        string symbol,
        CandleTimeframe timeframe,
        int limit,
        CancellationToken cancellationToken);
}