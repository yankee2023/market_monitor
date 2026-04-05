using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// 日本株チャート機能の入口を表す。
/// </summary>
public interface IJapaneseStockChartFeatureService
{
    /// <summary>
    /// 表示条件に応じた日本株チャート用データを組み立てる。
    /// </summary>
    Task<JapaneseStockChartViewData> LoadAsync(
        string symbol,
        CandleTimeframe timeframe,
        CandleDisplayPeriod displayPeriod,
        int fetchLimit,
        CancellationToken cancellationToken);
}