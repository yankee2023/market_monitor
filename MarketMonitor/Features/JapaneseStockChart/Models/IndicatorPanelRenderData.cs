using System.Globalization;
using MarketMonitor.Shared.Infrastructure;

namespace MarketMonitor.Features.JapaneseStockChart.Models;

/// <summary>
/// 下段指標パネルの描画データを表す。
/// </summary>
public sealed class IndicatorPanelRenderData : ObservableObject
{
    private bool _isExpanded = true;
    private double _panelScale;

    /// <summary>
    /// 描画データを初期化する。
    /// </summary>
    public IndicatorPanelRenderData(
        string panelKey,
        string panelTitle,
        int displayOrder,
        string axisLabelFormat,
        double? zeroLineTop,
        IReadOnlyList<ChartIndicatorRenderSeries> lineSeries,
        IReadOnlyList<ChartIndicatorBarItem> barItems,
        IReadOnlyList<IndicatorReferenceLine> referenceLines,
        decimal minValue,
        decimal maxValue,
        IReadOnlyList<IndicatorPanelHoverItem>? hoverItems = null)
    {
        PanelKey = panelKey ?? throw new ArgumentNullException(nameof(panelKey));
        PanelTitle = panelTitle ?? throw new ArgumentNullException(nameof(panelTitle));
        AxisLabelFormat = string.IsNullOrWhiteSpace(axisLabelFormat) ? "N2" : axisLabelFormat;
        DisplayOrder = displayOrder;
        ZeroLineTop = zeroLineTop;
        LineSeries = lineSeries ?? throw new ArgumentNullException(nameof(lineSeries));
        BarItems = barItems ?? throw new ArgumentNullException(nameof(barItems));
        ReferenceLines = referenceLines ?? throw new ArgumentNullException(nameof(referenceLines));
        MinValue = minValue;
        MaxValue = maxValue;
        HoverItems = hoverItems ?? Array.Empty<IndicatorPanelHoverItem>();
        _panelScale = panelKey switch
        {
            "volume" => 1.15d,
            "macd" => 1.0d,
            "rsi" => 0.9d,
            _ => 1.0d
        };
    }

    /// <summary>
    /// パネル識別キー。
    /// </summary>
    public string PanelKey { get; }

    /// <summary>
    /// パネルタイトル。
    /// </summary>
    public string PanelTitle { get; }

    /// <summary>
    /// 折りたたみ状態。
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>
    /// 表示順。
    /// </summary>
    public int DisplayOrder { get; }

    /// <summary>
    /// パネル縦方向スケール。
    /// </summary>
    public double PanelScale
    {
        get => _panelScale;
        set
        {
            var normalized = Math.Clamp(value, 0.8d, 2.4d);
            if (SetProperty(ref _panelScale, normalized))
            {
                OnPropertyChanged(nameof(PlotHeight));
            }
        }
    }

    /// <summary>
    /// 軸ラベル書式。
    /// </summary>
    public string AxisLabelFormat { get; }

    /// <summary>
    /// ゼロライン位置。
    /// </summary>
    public double? ZeroLineTop { get; }

    /// <summary>
    /// 線描画系列。
    /// </summary>
    public IReadOnlyList<ChartIndicatorRenderSeries> LineSeries { get; }

    /// <summary>
    /// バー描画項目。
    /// </summary>
    public IReadOnlyList<ChartIndicatorBarItem> BarItems { get; }

    /// <summary>
    /// 基準線。
    /// </summary>
    public IReadOnlyList<IndicatorReferenceLine> ReferenceLines { get; }

    /// <summary>
    /// ホバー領域。
    /// </summary>
    public IReadOnlyList<IndicatorPanelHoverItem> HoverItems { get; }

    /// <summary>
    /// 最小値。
    /// </summary>
    public decimal MinValue { get; }

    /// <summary>
    /// 最大値。
    /// </summary>
    public decimal MaxValue { get; }

    /// <summary>
    /// 中間値。
    /// </summary>
    public decimal MidValue => (MinValue + MaxValue) / 2m;

    /// <summary>
    /// 線系列の有無。
    /// </summary>
    public bool HasLineSeries => LineSeries.Count > 0;

    /// <summary>
    /// バー系列の有無。
    /// </summary>
    public bool HasBarItems => BarItems.Count > 0;

    /// <summary>
    /// 基準線の有無。
    /// </summary>
    public bool HasReferenceLines => ReferenceLines.Count > 0;

    /// <summary>
    /// ホバー領域の有無。
    /// </summary>
    public bool HasHoverItems => HoverItems.Count > 0;

    /// <summary>
    /// ゼロライン表示有無。
    /// </summary>
    public bool HasZeroLine => ZeroLineTop.HasValue;

    /// <summary>
    /// 描画領域高さ。
    /// </summary>
    public double PlotHeight => 96d * PanelScale;

    /// <summary>
    /// 最小値ラベル。
    /// </summary>
    public string MinLabel => MinValue.ToString(AxisLabelFormat, CultureInfo.CurrentCulture);

    /// <summary>
    /// 中間値ラベル。
    /// </summary>
    public string MidLabel => MidValue.ToString(AxisLabelFormat, CultureInfo.CurrentCulture);

    /// <summary>
    /// 最大値ラベル。
    /// </summary>
    public string MaxLabel => MaxValue.ToString(AxisLabelFormat, CultureInfo.CurrentCulture);
}