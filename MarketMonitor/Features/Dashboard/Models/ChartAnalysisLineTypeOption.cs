using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.Dashboard.Models;

/// <summary>
/// 分析ライン種別選択肢を表す。
/// </summary>
public sealed class ChartAnalysisLineTypeOption
{
    /// <summary>
    /// 種別を初期化する。
    /// </summary>
    /// <param name="lineType">分析ライン種別。</param>
    /// <param name="displayName">画面表示名。</param>
    public ChartAnalysisLineTypeOption(
        ChartAnalysisLineType lineType,
        string displayName,
        string description,
        string strokeColor,
        string strokeDashArray)
    {
        LineType = lineType;
        DisplayName = displayName;
        Description = description;
        StrokeColor = strokeColor;
        StrokeDashArray = strokeDashArray;
    }

    /// <summary>
    /// 分析ライン種別。
    /// </summary>
    public ChartAnalysisLineType LineType { get; }

    /// <summary>
    /// 画面表示名。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 画面説明文。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 線色。
    /// </summary>
    public string StrokeColor { get; }

    /// <summary>
    /// 破線設定。
    /// </summary>
    public string StrokeDashArray { get; }
}