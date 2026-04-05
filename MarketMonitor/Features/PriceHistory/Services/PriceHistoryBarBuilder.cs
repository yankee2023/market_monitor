using System.Globalization;
using MarketMonitor.Features.PriceHistory.Models;

namespace MarketMonitor.Features.PriceHistory.Services;

/// <summary>
/// 価格履歴バーの描画用データを生成する。
/// </summary>
internal sealed class PriceHistoryBarBuilder
{
    /// <summary>
    /// 表示用バー一覧を生成する。
    /// </summary>
    public static IReadOnlyList<PriceHistoryBar> Build(IReadOnlyList<PriceHistoryEntry> historyEntries)
    {
        if (historyEntries.Count == 0)
        {
            return Array.Empty<PriceHistoryBar>();
        }

        var minPrice = historyEntries.Min(x => x.StockPrice);
        var maxPrice = historyEntries.Max(x => x.StockPrice);
        var range = maxPrice - minPrice;

        return historyEntries.Select(entry =>
        {
            var height = range == 0m
                ? 80d
                : 30d + (double)((entry.StockPrice - minPrice) / range * 110m);

            return new PriceHistoryBar
            {
                Label = entry.RecordedAt.LocalDateTime.ToString("MM/dd\nHH:mm", CultureInfo.CurrentCulture),
                ValueText = entry.StockPrice.ToString("N2", CultureInfo.CurrentCulture),
                Height = height
            };
        }).ToList();
    }
}