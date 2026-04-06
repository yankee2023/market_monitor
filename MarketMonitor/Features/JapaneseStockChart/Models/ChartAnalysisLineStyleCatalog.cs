namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// 分析ライン種別ごとの表示定義を提供する。
/// </summary>
public static class ChartAnalysisLineStyleCatalog
{
    /// <summary>
    /// 種別表示名を返す。
    /// </summary>
    public static string GetDisplayName(ChartAnalysisLineType lineType)
    {
        return lineType switch
        {
            ChartAnalysisLineType.SupportLine => "支持線",
            ChartAnalysisLineType.ResistanceLine => "抵抗線",
            _ => "トレンドライン"
        };
    }

    /// <summary>
    /// 画面説明文を返す。
    /// </summary>
    public static string GetDescription(ChartAnalysisLineType lineType)
    {
        return lineType switch
        {
            ChartAnalysisLineType.SupportLine => "押し目で下げ止まりやすい価格帯の目安です。",
            ChartAnalysisLineType.ResistanceLine => "戻り売りが出やすい上値の目安です。",
            _ => "安値切り上げや高値切り下げの方向感を見る目安です。"
        };
    }

    /// <summary>
    /// 線色を返す。
    /// </summary>
    public static string GetStrokeColor(ChartAnalysisLineType lineType)
    {
        return lineType switch
        {
            ChartAnalysisLineType.SupportLine => "#0F766E",
            ChartAnalysisLineType.ResistanceLine => "#DC2626",
            _ => "#7C3AED"
        };
    }

    /// <summary>
    /// 破線パターンを返す。
    /// </summary>
    public static string GetStrokeDashArray(ChartAnalysisLineType lineType)
    {
        return lineType switch
        {
            ChartAnalysisLineType.SupportLine => "8 4",
            ChartAnalysisLineType.ResistanceLine => "10 4 2 4",
            _ => string.Empty
        };
    }
}