using MarketMonitor.Features.PriceHistory.Models;
using MarketSnapshotModel = MarketMonitor.Features.MarketSnapshot.Models.MarketSnapshot;

namespace MarketMonitor.Features.PriceHistory.Services;

/// <summary>
/// 価格履歴機能の入口を表す。
/// </summary>
public interface IPriceHistoryFeatureService
{
    /// <summary>
    /// スナップショットを保存し、表示用履歴データを組み立てる。
    /// </summary>
    Task<PriceHistoryViewData> RecordAndLoadAsync(MarketSnapshotModel snapshot, int limit, CancellationToken cancellationToken);
}