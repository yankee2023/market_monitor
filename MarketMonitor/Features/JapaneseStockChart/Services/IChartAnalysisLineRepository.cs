using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// 分析ラインの永続化を提供する。
/// </summary>
public interface IChartAnalysisLineRepository
{
    /// <summary>
    /// 指定コンテキストに保存された分析ラインを取得する。
    /// </summary>
    Task<IReadOnlyList<ChartAnalysisLine>> GetAsync(
        string symbol,
        CandleTimeframe timeframe,
        CandleDisplayPeriod displayPeriod,
        CancellationToken cancellationToken);

    /// <summary>
    /// 指定コンテキストの分析ライン一覧を置換保存する。
    /// </summary>
    Task SaveAsync(
        string symbol,
        CandleTimeframe timeframe,
        CandleDisplayPeriod displayPeriod,
        IReadOnlyList<ChartAnalysisLine> lines,
        CancellationToken cancellationToken);
}