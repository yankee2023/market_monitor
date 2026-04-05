using MarketSnapshotModel = MarketMonitor.Features.MarketSnapshot.Models.MarketSnapshot;

namespace MarketMonitor.Features.MarketSnapshot.Services;

/// <summary>
/// 現在値取得機能の入口を表す。
/// </summary>
public interface IMarketSnapshotService
{
    /// <summary>
    /// 日本株の現在値を取得する。
    /// </summary>
    /// <param name="symbol">ユーザー入力の銘柄名またはコード。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>最新スナップショット。</returns>
    Task<MarketSnapshotModel> GetMarketSnapshotAsync(string symbol, CancellationToken cancellationToken);
}