using MarketMonitor.Features.PriceHistory.Models;
using MarketSnapshotModel = MarketMonitor.Features.MarketSnapshot.Models.MarketSnapshot;

namespace MarketMonitor.Features.PriceHistory.Services;

/// <summary>
/// 価格履歴ストレージの抽象。
/// </summary>
public interface IPriceHistoryRepository
{
    /// <summary>
    /// スナップショットを履歴へ保存する。
    /// </summary>
    Task SaveAsync(MarketSnapshotModel snapshot, CancellationToken cancellationToken);

    /// <summary>
    /// 指定シンボルの最新履歴を取得する。
    /// </summary>
    Task<IReadOnlyList<PriceHistoryEntry>> GetRecentAsync(string symbol, int limit, CancellationToken cancellationToken);
}