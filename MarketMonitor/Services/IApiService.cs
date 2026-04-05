using MarketMonitor.Models;

namespace MarketMonitor.Services;

/// <summary>
/// マーケットデータ取得サービスの抽象。
/// </summary>
public interface IApiService
{
    /// <summary>
    /// 為替と株価の現在値を取得する。
    /// </summary>
    /// <param name="symbol">取得する株価シンボル。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>取得したマーケットスナップショット。</returns>
    Task<MarketSnapshot> GetMarketSnapshotAsync(string symbol, CancellationToken cancellationToken);
}
