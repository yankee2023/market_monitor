using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// ローソク足から自動分析ライン候補を生成する。
/// </summary>
public interface IAutoChartAnalysisLineService
{
    /// <summary>
    /// 表示対象ローソク足から分析ライン候補を生成する。
    /// </summary>
    IReadOnlyList<ChartAnalysisLine> Generate(IReadOnlyList<JapaneseCandleEntry> candles);
}