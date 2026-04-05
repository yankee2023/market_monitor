namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// ローソク足描画用の正規化済みデータを表す。
/// </summary>
public sealed class CandlestickRenderItem
{
    /// <summary>
    /// 表示ラベル。
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 横軸ラベルを表示するかどうか。
    /// </summary>
    public bool IsLabelVisible { get; set; }

    /// <summary>
    /// ひげの上端位置。
    /// </summary>
    public double WickTop { get; set; }

    /// <summary>
    /// ひげの高さ。
    /// </summary>
    public double WickHeight { get; set; }

    /// <summary>
    /// 実体の上端位置。
    /// </summary>
    public double BodyTop { get; set; }

    /// <summary>
    /// 実体の高さ。
    /// </summary>
    public double BodyHeight { get; set; }

    /// <summary>
    /// 実体色。
    /// </summary>
    public string BodyColor { get; set; } = string.Empty;

    /// <summary>
    /// ひげ色。
    /// </summary>
    public string WickColor { get; set; } = string.Empty;

    /// <summary>
    /// ツールチップ表示用の日付。
    /// </summary>
    public string DateText { get; set; } = string.Empty;

    /// <summary>
    /// ツールチップ表示用の始値。
    /// </summary>
    public string OpenText { get; set; } = string.Empty;

    /// <summary>
    /// ツールチップ表示用の終値。
    /// </summary>
    public string CloseText { get; set; } = string.Empty;

    /// <summary>
    /// ツールチップ表示用の高値。
    /// </summary>
    public string HighText { get; set; } = string.Empty;

    /// <summary>
    /// ツールチップ表示用の安値。
    /// </summary>
    public string LowText { get; set; } = string.Empty;
}