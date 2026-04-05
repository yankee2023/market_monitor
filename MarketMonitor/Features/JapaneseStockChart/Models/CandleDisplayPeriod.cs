namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// ローソク足の表示期間を表す。
/// </summary>
public enum CandleDisplayPeriod
{
    /// <summary>
    /// 1か月。
    /// </summary>
    OneMonth,

    /// <summary>
    /// 3か月。
    /// </summary>
    ThreeMonths,

    /// <summary>
    /// 6か月。
    /// </summary>
    SixMonths,

    /// <summary>
    /// 1年。
    /// </summary>
    OneYear
}