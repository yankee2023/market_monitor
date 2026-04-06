namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// ローソク足チャート上に重ねる分析ライン定義を表す。
/// </summary>
public sealed class ChartAnalysisLine
{
    /// <summary>
    /// 分析ラインを初期化する。
    /// </summary>
    /// <param name="id">分析ライン識別子。</param>
    /// <param name="lineType">分析ライン種別。</param>
    /// <param name="startXRatio">始点 X 座標の正規化比率。</param>
    /// <param name="startYRatio">始点 Y 座標の正規化比率。</param>
    /// <param name="endXRatio">終点 X 座標の正規化比率。</param>
    /// <param name="endYRatio">終点 Y 座標の正規化比率。</param>
    public ChartAnalysisLine(
        Guid id,
        ChartAnalysisLineType lineType,
        double startXRatio,
        double startYRatio,
        double endXRatio,
        double endYRatio)
    {
        Id = id;
        LineType = lineType;
        StartXRatio = startXRatio;
        StartYRatio = startYRatio;
        EndXRatio = endXRatio;
        EndYRatio = endYRatio;
    }

    /// <summary>
    /// 分析ライン識別子。
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 分析ライン種別。
    /// </summary>
    public ChartAnalysisLineType LineType { get; }

    /// <summary>
    /// 始点 X 座標の正規化比率。
    /// </summary>
    public double StartXRatio { get; }

    /// <summary>
    /// 始点 Y 座標の正規化比率。
    /// </summary>
    public double StartYRatio { get; }

    /// <summary>
    /// 終点 X 座標の正規化比率。
    /// </summary>
    public double EndXRatio { get; }

    /// <summary>
    /// 終点 Y 座標の正規化比率。
    /// </summary>
    public double EndYRatio { get; }
}