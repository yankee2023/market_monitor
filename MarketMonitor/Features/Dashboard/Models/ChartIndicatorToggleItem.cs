using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Shared.Infrastructure;

namespace MarketMonitor.Features.Dashboard.Models;

/// <summary>
/// 画面上で切り替えるチャート指標の状態を表す。
/// </summary>
public sealed class ChartIndicatorToggleItem : ObservableObject
{
    private bool _isSelected;

    /// <summary>
    /// 表示状態を初期化する。
    /// </summary>
    public ChartIndicatorToggleItem(
        string indicatorKey,
        string displayName,
        string accentColor,
        ChartIndicatorPlacement placement,
        int displayOrder,
        bool isSelected)
    {
        IndicatorKey = indicatorKey;
        DisplayName = displayName;
        AccentColor = accentColor;
        Placement = placement;
        DisplayOrder = displayOrder;
        _isSelected = isSelected;
    }

    /// <summary>
    /// 指標キー。
    /// </summary>
    public string IndicatorKey { get; }

    /// <summary>
    /// 表示名。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 強調色。
    /// </summary>
    public string AccentColor { get; }

    /// <summary>
    /// 表示位置。
    /// </summary>
    public ChartIndicatorPlacement Placement { get; }

    /// <summary>
    /// 表示順。
    /// </summary>
    public int DisplayOrder { get; }

    /// <summary>
    /// 選択状態。
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}