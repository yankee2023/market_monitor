namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// チャート指標の表示位置を表す。
/// </summary>
public enum ChartIndicatorPlacement
{
    /// <summary>
    /// 株価チャート上に重ねて表示する。
    /// </summary>
    OverlayPriceChart = 0,

    /// <summary>
    /// 別パネルに表示する。
    /// </summary>
    SecondaryPanel = 1
}