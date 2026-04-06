using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// 分析ラインの生成と描画座標変換を提供する。
/// </summary>
public interface IChartAnalysisLineService
{
    /// <summary>
    /// 正規化座標から分析ラインを生成する。
    /// </summary>
    /// <param name="lineType">分析ライン種別。</param>
    /// <param name="startXRatio">始点 X 座標の正規化比率。</param>
    /// <param name="startYRatio">始点 Y 座標の正規化比率。</param>
    /// <param name="endXRatio">終点 X 座標の正規化比率。</param>
    /// <param name="endYRatio">終点 Y 座標の正規化比率。</param>
    /// <returns>生成できた場合は分析ライン。長さ不足の場合は null。</returns>
    ChartAnalysisLine? CreateLine(
        ChartAnalysisLineType lineType,
        double startXRatio,
        double startYRatio,
        double endXRatio,
        double endYRatio);

    /// <summary>
    /// 分析ラインを現在の描画領域座標へ変換する。
    /// </summary>
    /// <param name="lines">分析ライン一覧。</param>
    /// <param name="canvasWidth">描画領域の横幅。</param>
    /// <param name="canvasHeight">描画領域の高さ。</param>
    /// <returns>描画用座標一覧。</returns>
    IReadOnlyList<ChartAnalysisLineRenderItem> CreateRenderItems(
        IReadOnlyList<ChartAnalysisLine> lines,
        double canvasWidth,
        double canvasHeight,
        Guid? selectedLineId = null);

    /// <summary>
    /// 指定座標に最も近い分析ラインを検索する。
    /// </summary>
    Guid? FindNearestLineId(
        IReadOnlyList<ChartAnalysisLine> lines,
        double xRatio,
        double yRatio,
        double hitToleranceRatio);

    /// <summary>
    /// 分析ラインを平行移動する。
    /// </summary>
    ChartAnalysisLine MoveLine(ChartAnalysisLine line, double deltaXRatio, double deltaYRatio);
}