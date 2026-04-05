namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// チャート指標の表示定義を表す。
/// </summary>
public sealed class ChartIndicatorDefinition
{
    /// <summary>
    /// 表示定義を初期化する。
    /// </summary>
    public ChartIndicatorDefinition(
        string indicatorKey,
        string displayName,
        ChartIndicatorPlacement placement,
        string accentColor,
        bool isEnabledByDefault,
        int displayOrder)
    {
        IndicatorKey = indicatorKey;
        DisplayName = displayName;
        Placement = placement;
        AccentColor = accentColor;
        IsEnabledByDefault = isEnabledByDefault;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// 指標を一意に識別するキー。
    /// </summary>
    public string IndicatorKey { get; }

    /// <summary>
    /// 画面表示名。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 表示位置。
    /// </summary>
    public ChartIndicatorPlacement Placement { get; }

    /// <summary>
    /// 強調色。
    /// </summary>
    public string AccentColor { get; }

    /// <summary>
    /// 初期表示状態。
    /// </summary>
    public bool IsEnabledByDefault { get; }

    /// <summary>
    /// 表示順。
    /// </summary>
    public int DisplayOrder { get; }
}