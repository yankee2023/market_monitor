using MarketMonitor.Features.SectorComparison.Models;

namespace MarketMonitor.Features.SectorComparison.Services;

/// <summary>
/// セクター比較機能の入口を表す。
/// </summary>
public interface ISectorComparisonFeatureService
{
    /// <summary>
    /// 同業比較表示データを読み込む。
    /// </summary>
    Task<SectorComparisonViewData> LoadAsync(string symbol, CancellationToken cancellationToken);
}