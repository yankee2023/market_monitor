namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// 分析ラインの種別を表す。
/// </summary>
public enum ChartAnalysisLineType
{
    /// <summary>
    /// トレンドライン。
    /// </summary>
    TrendLine,

    /// <summary>
    /// 支持線。
    /// </summary>
    SupportLine,

    /// <summary>
    /// 抵抗線。
    /// </summary>
    ResistanceLine
}