namespace MarketMonitor.Features.PriceHistory.Models;

/// <summary>
/// 株価履歴バー描画用の表示データを表す。
/// </summary>
public sealed class PriceHistoryBar
{
    /// <summary>
    /// バー表示ラベル。
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// ツールチップ表示用値。
    /// </summary>
    public string ValueText { get; set; } = string.Empty;

    /// <summary>
    /// バー高さ。
    /// </summary>
    public double Height { get; set; }
}