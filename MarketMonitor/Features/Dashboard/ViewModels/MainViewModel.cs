using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Windows.Input;
using MarketMonitor.Composition;
using MarketMonitor.Features.Dashboard.Models;
using MarketMonitor.Features.Dashboard.Services;
using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;
using MarketMonitor.Features.MarketSnapshot.Services;
using MarketMonitor.Features.PriceHistory.Models;
using MarketMonitor.Features.PriceHistory.Services;
using MarketMonitor.Features.SectorComparison.Models;
using MarketMonitor.Features.SectorComparison.Services;
using MarketMonitor.Shared.Infrastructure;
using MarketMonitor.Shared.Logging;
using MarketMonitor.Shared.MarketData;
using MarketSnapshotModel = MarketMonitor.Features.MarketSnapshot.Models.MarketSnapshot;

namespace MarketMonitor.Features.Dashboard.ViewModels;

/// <summary>
/// メイン画面の状態と操作を管理する ViewModel。
/// </summary>
public sealed class MainViewModel : ObservableObject, IDisposable, IMainWindowViewModel
{
    private const int HistoryItemLimit = 20;
    private const int CandleFetchLimit = 320;
    private const double AnalysisLineCanvasHeight = 260d;

    private readonly IMarketSnapshotService _marketSnapshotService;
    private readonly IPriceHistoryFeatureService _priceHistoryFeatureService;
    private readonly IJapaneseStockChartFeatureService _japaneseStockChartFeatureService;
    private readonly ISectorComparisonFeatureService _sectorComparisonFeatureService;
    private readonly IChartIndicatorSelectionService _chartIndicatorSelectionService;
    private readonly IChartAnalysisLineRepository _chartAnalysisLineRepository;
    private readonly IChartAnalysisLineService _chartAnalysisLineService;
    private readonly IAppLogger _logger;
    private readonly IDesktopNotificationService _desktopNotificationService;

    private string _symbol = "7203";
    private decimal _stockPrice;
    private DateTimeOffset _stockUpdatedAt;
    private decimal _candleChartMinPrice;
    private decimal _candleChartMaxPrice;
    private double _japaneseCandlestickCanvasWidth = 320d;
    private readonly List<ChartIndicatorRenderSeries> _allJapaneseOverlayIndicatorSeries = [];
    private readonly List<IndicatorPanelRenderData> _allJapaneseIndicatorPanels = [];
    private readonly List<ChartAnalysisLine> _japaneseAnalysisLineDefinitions = [];
    private readonly List<ChartAnalysisLine> _suggestedJapaneseAnalysisLines = [];
    private readonly string _japaneseCandlestickYAxisTitle = "株価 (円)";
    private readonly string _japaneseCandlestickXAxisTitle = "日付";
    private string _companyName = string.Empty;
    private string _sectorName = "-";
    private string _marketSegmentName = "-";
    private string _statusMessage = "準備完了";
    private string _alertThresholdText = string.Empty;
    private bool _isPriceAlertEnabled;
    private bool _isSidebarCollapsed;
    private bool _hasPriceAlertBaseline;
    private bool _wasAlertAtOrAboveThreshold;
    private bool _isAnalysisLineDrawingEnabled;
    private Guid? _selectedJapaneseAnalysisLineId;
    private bool _isJapaneseAnalysisLineDragging;
    private double _lastJapaneseAnalysisLinePointerXRatio;
    private double _lastJapaneseAnalysisLinePointerYRatio;
    private bool _hasPendingAnalysisLinePoint;
    private double _pendingAnalysisLinePointXRatio;
    private double _pendingAnalysisLinePointYRatio;
    private ChartAnalysisLineType _selectedJapaneseAnalysisLineType = ChartAnalysisLineType.TrendLine;
    private CandleTimeframe _selectedCandleTimeframe = CandleTimeframe.Daily;
    private CandleDisplayPeriod _selectedCandleDisplayPeriod = CandleDisplayPeriod.OneMonth;
    private string _currentSnapshotSymbol = "7203.T";
    private readonly RelayCommand _removeSelectedAnalysisLineCommand;
    private readonly RelayCommand _clearAnalysisLinesCommand;

    /// <summary>
    /// ViewModel を初期化する。
    /// </summary>
    public MainViewModel(
        IMarketSnapshotService marketSnapshotService,
        IPriceHistoryFeatureService priceHistoryFeatureService,
        IJapaneseStockChartFeatureService japaneseStockChartFeatureService,
        ISectorComparisonFeatureService sectorComparisonFeatureService,
        IAppLogger logger,
        IDesktopNotificationService desktopNotificationService,
        IChartIndicatorSelectionService? chartIndicatorSelectionService = null,
        IChartAnalysisLineRepository? chartAnalysisLineRepository = null,
        IChartAnalysisLineService? chartAnalysisLineService = null)
    {
        _marketSnapshotService = marketSnapshotService ?? throw new ArgumentNullException(nameof(marketSnapshotService));
        _priceHistoryFeatureService = priceHistoryFeatureService ?? throw new ArgumentNullException(nameof(priceHistoryFeatureService));
        _japaneseStockChartFeatureService = japaneseStockChartFeatureService ?? throw new ArgumentNullException(nameof(japaneseStockChartFeatureService));
        _sectorComparisonFeatureService = sectorComparisonFeatureService ?? throw new ArgumentNullException(nameof(sectorComparisonFeatureService));
        _chartIndicatorSelectionService = chartIndicatorSelectionService ?? new ChartIndicatorSelectionService();
        _chartAnalysisLineRepository = chartAnalysisLineRepository ?? new SqliteChartAnalysisLineRepository(logger);
        _chartAnalysisLineService = chartAnalysisLineService ?? new ChartAnalysisLineService();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _desktopNotificationService = desktopNotificationService ?? throw new ArgumentNullException(nameof(desktopNotificationService));

        PriceHistoryItems = new ObservableCollection<PriceHistoryEntry>();
        StockPriceChartBars = new ObservableCollection<PriceHistoryBar>();
        JapaneseCandlesticks = new ObservableCollection<CandlestickRenderItem>();
        JapaneseChartIndicatorOptions = new ObservableCollection<ChartIndicatorToggleItem>();
        VisibleJapaneseOverlayIndicators = new ObservableCollection<ChartIndicatorRenderSeries>();
        VisibleJapaneseIndicatorPanels = new ObservableCollection<IndicatorPanelRenderData>();
        JapaneseAnalysisLines = new ObservableCollection<ChartAnalysisLineRenderItem>();
        JapanesePendingAnalysisPoints = new ObservableCollection<ChartAnalysisPointRenderItem>();
        JapaneseAnalysisLineTypeOptions = new ObservableCollection<ChartAnalysisLineTypeOption>(CreateAnalysisLineTypeOptions());
        SectorComparisonItems = new ObservableCollection<SectorComparisonPeerItem>();

        ApplySymbolCommand = new AsyncRelayCommand(LoadAnalysisAsync);
        ToggleSidebarCommand = new RelayCommand(ToggleSidebar);
        ShowDailyCandlesCommand = new AsyncRelayCommand(() => ChangeCandlesAsync(CandleTimeframe.Daily));
        ShowWeeklyCandlesCommand = new AsyncRelayCommand(() => ChangeCandlesAsync(CandleTimeframe.Weekly));
        ShowOneMonthCandlesCommand = new AsyncRelayCommand(() => ChangeCandlePeriodAsync(CandleDisplayPeriod.OneMonth));
        ShowThreeMonthsCandlesCommand = new AsyncRelayCommand(() => ChangeCandlePeriodAsync(CandleDisplayPeriod.ThreeMonths));
        ShowSixMonthsCandlesCommand = new AsyncRelayCommand(() => ChangeCandlePeriodAsync(CandleDisplayPeriod.SixMonths));
        ShowOneYearCandlesCommand = new AsyncRelayCommand(() => ChangeCandlePeriodAsync(CandleDisplayPeriod.OneYear));
        _removeSelectedAnalysisLineCommand = new RelayCommand(RemoveSelectedAnalysisLine, () => _selectedJapaneseAnalysisLineId.HasValue);
        _clearAnalysisLinesCommand = new RelayCommand(ClearAnalysisLines, () => _japaneseAnalysisLineDefinitions.Count > 0 || _hasPendingAnalysisLinePoint);
    }

    /// <summary>
    /// 銘柄シンボル。
    /// </summary>
    public string Symbol
    {
        get => _symbol;
        set => SetProperty(ref _symbol, value);
    }

    /// <summary>
    /// 画面表示用の株価。
    /// </summary>
    public string StockPriceDisplay => _stockPrice <= 0 ? "-" : _stockPrice.ToString("N2", CultureInfo.CurrentCulture);

    /// <summary>
    /// 株価の最終取得時刻表示。
    /// </summary>
    public string StockUpdatedAtDisplay => _stockUpdatedAt == default
        ? "株価取得: 未実行"
        : $"株価取得: {_stockUpdatedAt.LocalDateTime:yyyy/MM/dd HH:mm:ss}";

    /// <summary>
    /// 画面表示用の銘柄名。
    /// </summary>
    public string CompanyDisplay => string.IsNullOrWhiteSpace(_companyName)
        ? $"銘柄: {_currentSnapshotSymbol}"
        : $"銘柄: {_companyName} ({_currentSnapshotSymbol})";

    /// <summary>
    /// 表示用セクター名。
    /// </summary>
    public string SectorDisplay => string.IsNullOrWhiteSpace(_sectorName) || _sectorName == "-"
        ? "業種: -"
        : $"業種: {_sectorName}";

    /// <summary>
    /// 表示用市場区分名。
    /// </summary>
    public string MarketSegmentDisplay => string.IsNullOrWhiteSpace(_marketSegmentName) || _marketSegmentName == "-"
        ? "市場: -"
        : $"市場: {_marketSegmentName}";

    /// <summary>
    /// ローソク足縦軸タイトル。
    /// </summary>
    public string JapaneseCandlestickYAxisTitle => _japaneseCandlestickYAxisTitle;

    /// <summary>
    /// ローソク足横軸タイトル。
    /// </summary>
    public string JapaneseCandlestickXAxisTitle => _japaneseCandlestickXAxisTitle;

    /// <summary>
    /// ローソク足縦軸の最小値ラベル。
    /// </summary>
    public string JapaneseCandlestickMinPriceLabel => FormatAxisLabel(_candleChartMinPrice);

    /// <summary>
    /// ローソク足縦軸の中間値ラベル。
    /// </summary>
    public string JapaneseCandlestickMidPriceLabel => FormatAxisLabel((_candleChartMinPrice + _candleChartMaxPrice) / 2m);

    /// <summary>
    /// ローソク足縦軸の最大値ラベル。
    /// </summary>
    public string JapaneseCandlestickMaxPriceLabel => FormatAxisLabel(_candleChartMaxPrice);

    /// <summary>
    /// 通知閾値入力。
    /// </summary>
    public string AlertThresholdText
    {
        get => _alertThresholdText;
        set => SetProperty(ref _alertThresholdText, value);
    }

    /// <summary>
    /// 通知有効状態。
    /// </summary>
    public bool IsPriceAlertEnabled
    {
        get => _isPriceAlertEnabled;
        set
        {
            if (SetProperty(ref _isPriceAlertEnabled, value) && !value)
            {
                _hasPriceAlertBaseline = false;
            }
        }
    }

    /// <summary>
    /// 補助ペイン折りたたみ状態。
    /// </summary>
    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set
        {
            if (!SetProperty(ref _isSidebarCollapsed, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsSidebarVisible));
            OnPropertyChanged(nameof(SidebarToggleText));
        }
    }

    /// <summary>
    /// 補助ペイン表示状態。
    /// </summary>
    public bool IsSidebarVisible => !IsSidebarCollapsed;

    /// <summary>
    /// 補助ペイン開閉ボタン表示文言。
    /// </summary>
    public string SidebarToggleText => IsSidebarCollapsed ? "詳細情報を表示" : "詳細情報を隠す";

    /// <summary>
    /// 分析ライン描画モード有効状態。
    /// </summary>
    public bool IsAnalysisLineDrawingEnabled
    {
        get => _isAnalysisLineDrawingEnabled;
        private set => SetProperty(ref _isAnalysisLineDrawingEnabled, value);
    }

    /// <summary>
    /// 選択中の分析ライン種別。
    /// </summary>
    public ChartAnalysisLineType SelectedJapaneseAnalysisLineType
    {
        get => _selectedJapaneseAnalysisLineType;
        private set => SetProperty(ref _selectedJapaneseAnalysisLineType, value);
    }

    /// <summary>
    /// 分析ライン操作ボタン表示文言。
    /// </summary>
    public string AnalysisLineActionText => IsAnalysisLineDrawingEnabled ? "手動描画を終了" : "手動で線を描く";

    /// <summary>
    /// 手動描画ガイド文言。
    /// </summary>
    public string AnalysisLineQuickGuideText
    {
        get
        {
            if (!IsAnalysisLineDrawingEnabled)
            {
                return "手順: 1) 手動で線を描く を押す 2) 線種を選ぶ 3) チャートを2回クリック";
            }

            return _hasPendingAnalysisLinePoint
                ? "手順 2/2: 終点をクリックしてください。"
                : "手順 1/2: まず始点をクリックしてください。";
        }
    }

    /// <summary>
    /// 分析ライン操作状況を表示する。
    /// </summary>
    public string AnalysisLineStatusText
    {
        get
        {
            var selectedLineText = _selectedJapaneseAnalysisLineId.HasValue
                ? $"選択中: {GetAnalysisLineTypeDisplayName(GetSelectedAnalysisLine()?.LineType ?? ChartAnalysisLineType.TrendLine)}。"
                : string.Empty;

            if (IsAnalysisLineDrawingEnabled)
            {
                var lineCountPrefix = _japaneseAnalysisLineDefinitions.Count > 0
                    ? $"分析ライン {_japaneseAnalysisLineDefinitions.Count} 本を表示中です。"
                    : string.Empty;
                var pendingLineTypeText = $"追加する線種: {GetAnalysisLineTypeDisplayName(SelectedJapaneseAnalysisLineType)}。";

                return _hasPendingAnalysisLinePoint
                    ? $"{lineCountPrefix}{selectedLineText}{pendingLineTypeText}終点をクリックすると線を追加できます。"
                    : $"{lineCountPrefix}{selectedLineText}{pendingLineTypeText}始点をクリックして手動描画を開始してください。";
            }

            return _japaneseAnalysisLineDefinitions.Count <= 0
                ? (string.IsNullOrWhiteSpace(selectedLineText) ? "分析ラインは未描画です。手動描画ボタンから追加できます。" : selectedLineText)
                : $"分析ライン {_japaneseAnalysisLineDefinitions.Count} 本を表示中です。{selectedLineText}ドラッグで位置調整できます。";
        }
    }

    /// <summary>
    /// ローソク足チャート描画領域の横幅。
    /// </summary>
    public double JapaneseCandlestickCanvasWidth
    {
        get => _japaneseCandlestickCanvasWidth;
        private set => SetProperty(ref _japaneseCandlestickCanvasWidth, value);
    }

    /// <summary>
    /// ステータスメッセージ。
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// 価格履歴一覧。
    /// </summary>
    public ObservableCollection<PriceHistoryEntry> PriceHistoryItems { get; }

    /// <summary>
    /// 株価履歴チャートバー。
    /// </summary>
    public ObservableCollection<PriceHistoryBar> StockPriceChartBars { get; }

    /// <summary>
    /// 日本株ローソク足描画データ。
    /// </summary>
    public ObservableCollection<CandlestickRenderItem> JapaneseCandlesticks { get; }

    /// <summary>
    /// 表示可能なチャート指標切替一覧。
    /// </summary>
    public ObservableCollection<ChartIndicatorToggleItem> JapaneseChartIndicatorOptions { get; }

    /// <summary>
    /// 現在表示中の価格チャート重ね描き指標。
    /// </summary>
    public ObservableCollection<ChartIndicatorRenderSeries> VisibleJapaneseOverlayIndicators { get; }

    /// <summary>
    /// 現在表示中のオーバーレイ指標が存在するかどうか。
    /// </summary>
    public bool HasVisibleJapaneseChartIndicators => VisibleJapaneseOverlayIndicators.Count > 0;

    /// <summary>
    /// 現在表示中の下段指標パネル。
    /// </summary>
    public ObservableCollection<IndicatorPanelRenderData> VisibleJapaneseIndicatorPanels { get; }

    /// <summary>
    /// 現在表示中の分析ライン。
    /// </summary>
    public ObservableCollection<ChartAnalysisLineRenderItem> JapaneseAnalysisLines { get; }

    /// <summary>
    /// 手動描画中の始点マーカー。
    /// </summary>
    public ObservableCollection<ChartAnalysisPointRenderItem> JapanesePendingAnalysisPoints { get; }

    /// <summary>
    /// 分析ライン種別選択肢。
    /// </summary>
    public ObservableCollection<ChartAnalysisLineTypeOption> JapaneseAnalysisLineTypeOptions { get; }

    /// <summary>
    /// 分析ライン表示有無。
    /// </summary>
    public bool HasJapaneseAnalysisLines => JapaneseAnalysisLines.Count > 0;

    /// <summary>
    /// 手動描画の始点が設定済みかどうか。
    /// </summary>
    public bool HasPendingJapaneseAnalysisPoint => JapanesePendingAnalysisPoints.Count > 0;

    /// <summary>
    /// 選択中の分析ラインが存在するかどうか。
    /// </summary>
    public bool HasSelectedJapaneseAnalysisLine => _selectedJapaneseAnalysisLineId.HasValue;

    /// <summary>
    /// 下段指標パネル表示有無。
    /// </summary>
    public bool HasVisibleJapaneseIndicatorPanels => VisibleJapaneseIndicatorPanels.Count > 0;

    /// <summary>
    /// 同業比較一覧。
    /// </summary>
    public ObservableCollection<SectorComparisonPeerItem> SectorComparisonItems { get; }

    /// <summary>
    /// 同業比較表示有無。
    /// </summary>
    public bool HasSectorComparisonItems => SectorComparisonItems.Count > 0;

    /// <summary>
    /// 選択中の足種別。
    /// </summary>
    public CandleTimeframe SelectedCandleTimeframe
    {
        get => _selectedCandleTimeframe;
        private set => SetProperty(ref _selectedCandleTimeframe, value);
    }

    /// <summary>
    /// 日足選択状態。
    /// </summary>
    public bool IsDailyCandleSelected => SelectedCandleTimeframe == CandleTimeframe.Daily;

    /// <summary>
    /// 週足選択状態。
    /// </summary>
    public bool IsWeeklyCandleSelected => SelectedCandleTimeframe == CandleTimeframe.Weekly;

    /// <summary>
    /// 選択中の表示期間。
    /// </summary>
    public CandleDisplayPeriod SelectedCandleDisplayPeriod
    {
        get => _selectedCandleDisplayPeriod;
        private set => SetProperty(ref _selectedCandleDisplayPeriod, value);
    }

    /// <summary>
    /// 1か月選択状態。
    /// </summary>
    public bool IsOneMonthSelected => SelectedCandleDisplayPeriod == CandleDisplayPeriod.OneMonth;

    /// <summary>
    /// 3か月選択状態。
    /// </summary>
    public bool IsThreeMonthsSelected => SelectedCandleDisplayPeriod == CandleDisplayPeriod.ThreeMonths;

    /// <summary>
    /// 6か月選択状態。
    /// </summary>
    public bool IsSixMonthsSelected => SelectedCandleDisplayPeriod == CandleDisplayPeriod.SixMonths;

    /// <summary>
    /// 1年選択状態。
    /// </summary>
    public bool IsOneYearSelected => SelectedCandleDisplayPeriod == CandleDisplayPeriod.OneYear;

    /// <summary>
    /// 銘柄適用コマンド。
    /// </summary>
    public ICommand ApplySymbolCommand { get; }

    /// <summary>
    /// 補助ペイン開閉コマンド。
    /// </summary>
    public ICommand ToggleSidebarCommand { get; }

    /// <summary>
    /// 日足表示コマンド。
    /// </summary>
    public ICommand ShowDailyCandlesCommand { get; }

    /// <summary>
    /// 週足表示コマンド。
    /// </summary>
    public ICommand ShowWeeklyCandlesCommand { get; }

    /// <summary>
    /// 1か月表示コマンド。
    /// </summary>
    public ICommand ShowOneMonthCandlesCommand { get; }

    /// <summary>
    /// 3か月表示コマンド。
    /// </summary>
    public ICommand ShowThreeMonthsCandlesCommand { get; }

    /// <summary>
    /// 6か月表示コマンド。
    /// </summary>
    public ICommand ShowSixMonthsCandlesCommand { get; }

    /// <summary>
    /// 1年表示コマンド。
    /// </summary>
    public ICommand ShowOneYearCandlesCommand { get; }

    /// <summary>
    /// 選択中の分析ラインを削除するコマンド。
    /// </summary>
    public ICommand RemoveSelectedAnalysisLineCommand => _removeSelectedAnalysisLineCommand;

    /// <summary>
    /// 分析ラインを全消去するコマンド。
    /// </summary>
    public ICommand ClearAnalysisLinesCommand => _clearAnalysisLinesCommand;

    /// <summary>
    /// 初期表示時の処理。
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.Info("初期表示時の分析読込を開始します。");
        await LoadAnalysisAsync();
    }

    /// <inheritdoc />
    public IReadOnlyList<ChartAnalysisLineTypeOption> GetManualAnalysisLineTypeOptions()
    {
        return JapaneseAnalysisLineTypeOptions.ToArray();
    }

    /// <inheritdoc />
    public void StartManualAnalysisLineDrawing(ChartAnalysisLineType lineType)
    {
        SelectedJapaneseAnalysisLineType = lineType;
        _hasPendingAnalysisLinePoint = false;
        IsAnalysisLineDrawingEnabled = true;
        OnAnalysisLineStateChanged();
    }

    /// <inheritdoc />
    public void CancelManualAnalysisLineDrawing()
    {
        if (!IsAnalysisLineDrawingEnabled && !_hasPendingAnalysisLinePoint)
        {
            return;
        }

        _hasPendingAnalysisLinePoint = false;
        IsAnalysisLineDrawingEnabled = false;
        OnAnalysisLineStateChanged();
    }

    /// <inheritdoc />
    public void RegisterJapaneseChartClick(double chartX, double chartY)
    {
        if (!IsAnalysisLineDrawingEnabled || JapaneseCandlestickCanvasWidth <= 0d)
        {
            return;
        }

        var normalizedXRatio = NormalizeCoordinate(chartX, JapaneseCandlestickCanvasWidth);
        var normalizedYRatio = NormalizeCoordinate(chartY, AnalysisLineCanvasHeight);

        if (!_hasPendingAnalysisLinePoint)
        {
            _pendingAnalysisLinePointXRatio = normalizedXRatio;
            _pendingAnalysisLinePointYRatio = normalizedYRatio;
            _hasPendingAnalysisLinePoint = true;
            OnAnalysisLineStateChanged();
            return;
        }

        var line = _chartAnalysisLineService.CreateLine(
            SelectedJapaneseAnalysisLineType,
            _pendingAnalysisLinePointXRatio,
            _pendingAnalysisLinePointYRatio,
            normalizedXRatio,
            normalizedYRatio);

        _hasPendingAnalysisLinePoint = false;

        if (line is null)
        {
            StatusMessage = "分析ラインの始点と終点が近すぎるため追加できません。";
            OnAnalysisLineStateChanged();
            return;
        }

        _japaneseAnalysisLineDefinitions.Add(line);
        _selectedJapaneseAnalysisLineId = line.Id;
        IsAnalysisLineDrawingEnabled = false;
        RefreshJapaneseAnalysisLines();
        StatusMessage = $"分析ラインを追加しました。現在 {_japaneseAnalysisLineDefinitions.Count} 本です。ドラッグで位置を調整できます。";
        OnAnalysisLineStateChanged();
        PersistJapaneseAnalysisLinesInBackground(GetSymbolForCandles(), SelectedCandleTimeframe, SelectedCandleDisplayPeriod, _japaneseAnalysisLineDefinitions.ToArray());
    }

    /// <inheritdoc />
    public bool BeginJapaneseChartPointerInteraction(double chartX, double chartY)
    {
        if (IsAnalysisLineDrawingEnabled)
        {
            RegisterJapaneseChartClick(chartX, chartY);
            return true;
        }

        if (!TryNormalizeChartPoint(chartX, chartY, out var normalizedXRatio, out var normalizedYRatio))
        {
            return false;
        }

        var selectedId = _chartAnalysisLineService.FindNearestLineId(
            _japaneseAnalysisLineDefinitions,
            normalizedXRatio,
            normalizedYRatio,
            CalculateHitToleranceRatio());

        _selectedJapaneseAnalysisLineId = selectedId;
        _isJapaneseAnalysisLineDragging = selectedId.HasValue;
        _lastJapaneseAnalysisLinePointerXRatio = normalizedXRatio;
        _lastJapaneseAnalysisLinePointerYRatio = normalizedYRatio;
        RefreshJapaneseAnalysisLines();
        OnAnalysisLineStateChanged();

        return selectedId.HasValue;
    }

    /// <inheritdoc />
    public bool UpdateJapaneseChartPointerInteraction(double chartX, double chartY)
    {
        if (!_isJapaneseAnalysisLineDragging || !_selectedJapaneseAnalysisLineId.HasValue)
        {
            return false;
        }

        if (!TryNormalizeChartPoint(chartX, chartY, out var normalizedXRatio, out var normalizedYRatio))
        {
            return false;
        }

        var deltaXRatio = normalizedXRatio - _lastJapaneseAnalysisLinePointerXRatio;
        var deltaYRatio = normalizedYRatio - _lastJapaneseAnalysisLinePointerYRatio;
        if (Math.Abs(deltaXRatio) < double.Epsilon && Math.Abs(deltaYRatio) < double.Epsilon)
        {
            return false;
        }

        ReplaceAnalysisLine(_selectedJapaneseAnalysisLineId.Value, line => _chartAnalysisLineService.MoveLine(line, deltaXRatio, deltaYRatio));
        _lastJapaneseAnalysisLinePointerXRatio = normalizedXRatio;
        _lastJapaneseAnalysisLinePointerYRatio = normalizedYRatio;
        RefreshJapaneseAnalysisLines();
        return true;
    }

    /// <inheritdoc />
    public bool CompleteJapaneseChartPointerInteraction(double chartX, double chartY)
    {
        var wasDragging = _isJapaneseAnalysisLineDragging;
        _isJapaneseAnalysisLineDragging = false;

        if (!wasDragging)
        {
            return IsAnalysisLineDrawingEnabled;
        }

        PersistJapaneseAnalysisLinesInBackground(GetSymbolForCandles(), SelectedCandleTimeframe, SelectedCandleDisplayPeriod, _japaneseAnalysisLineDefinitions.ToArray());
        StatusMessage = _selectedJapaneseAnalysisLineId.HasValue
            ? "分析ライン位置を更新しました。"
            : StatusMessage;
        OnAnalysisLineStateChanged();
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<AutoAnalysisLineCandidate> GetAutoAnalysisLineCandidates()
    {
        var lines = CreateAutoAnalysisLines();
        return lines
            .Select((line, index) => new AutoAnalysisLineCandidate(
                line.Id,
                $"{ChartAnalysisLineStyleCatalog.GetDisplayName(line.LineType)} #{index + 1}",
                $"始点({line.StartXRatio:P0}, {line.StartYRatio:P0}) / 終点({line.EndXRatio:P0}, {line.EndYRatio:P0})"))
            .ToArray();
    }

    /// <inheritdoc />
    public void AppendSelectedAutoAnalysisLines(IReadOnlyList<Guid> selectedLineIds)
    {
        ArgumentNullException.ThrowIfNull(selectedLineIds);

        var idSet = new HashSet<Guid>(selectedLineIds);
        var targetLines = CreateAutoAnalysisLines()
            .Where(line => idSet.Contains(line.Id))
            .ToArray();

        if (targetLines.Length <= 0)
        {
            StatusMessage = "追加対象の自動分析ラインを選択してください。";
            return;
        }

        IsAnalysisLineDrawingEnabled = false;
        _hasPendingAnalysisLinePoint = false;
        _selectedJapaneseAnalysisLineId = null;
        var appendedCount = 0;
        foreach (var line in targetLines)
        {
            if (_japaneseAnalysisLineDefinitions.Any(existing => AreEquivalentAnalysisLines(existing, line)))
            {
                continue;
            }

            _japaneseAnalysisLineDefinitions.Add(CloneAnalysisLine(line));
            appendedCount++;
        }

        if (appendedCount <= 0)
        {
            StatusMessage = "追加対象の自動分析ラインはありません。";
            OnAnalysisLineStateChanged();
            return;
        }

        RefreshJapaneseAnalysisLines();
        StatusMessage = $"分析ラインを {appendedCount} 本追加しました。";
        OnAnalysisLineStateChanged();
        PersistJapaneseAnalysisLinesInBackground(
            GetSymbolForCandles(),
            SelectedCandleTimeframe,
            SelectedCandleDisplayPeriod,
            _japaneseAnalysisLineDefinitions.ToArray());
    }

    private async Task LoadAnalysisAsync()
    {
        try
        {
            StartAnalysisLoad();
            var snapshot = await LoadSnapshotAsync();
            await RefreshAnalysisWorkspaceAsync(snapshot);
            CompleteAnalysisLoad(snapshot);
        }
        catch (HttpRequestException ex)
        {
            HandleAnalysisFailure(ex);
        }
        catch (InvalidOperationException ex)
        {
            HandleAnalysisFailure(ex);
        }
    }

    private async Task<MarketSnapshotModel> LoadSnapshotAsync()
    {
        var previousSymbol = _currentSnapshotSymbol;
        var snapshot = await _marketSnapshotService.GetMarketSnapshotAsync(Symbol, CancellationToken.None);
        ApplySnapshot(snapshot);
        if (!string.Equals(previousSymbol, snapshot.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            ClearAnalysisLinesInternal();
        }

        EvaluatePriceAlert(snapshot);
        return snapshot;
    }

    private async Task RefreshAnalysisWorkspaceAsync(MarketSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await LoadPriceHistoryAsync(snapshot);
        await RefreshTechnicalAnalysisAsync(snapshot.Symbol);
        await LoadSectorComparisonAsync(snapshot.Symbol);
    }

    private async Task RefreshTechnicalAnalysisAsync(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        await LoadJapaneseStockChartAsync(symbol);
    }

    private async Task ChangeCandlesAsync(CandleTimeframe timeframe)
    {
        SetSelectedCandleTimeframe(timeframe);
        await ReloadJapaneseCandlesAsync();
    }

    private async Task ChangeCandlePeriodAsync(CandleDisplayPeriod period)
    {
        SetSelectedCandleDisplayPeriod(period);
        await ReloadJapaneseCandlesAsync();
    }

    private async Task ReloadJapaneseCandlesAsync()
    {
        var chartViewData = await _japaneseStockChartFeatureService.LoadAsync(
            GetSymbolForCandles(),
            SelectedCandleTimeframe,
            SelectedCandleDisplayPeriod,
            CandleFetchLimit,
            CancellationToken.None);

        ReplaceCollection(JapaneseCandlesticks, chartViewData.Candlesticks);
        await LoadPersistedJapaneseAnalysisLinesAsync(GetSymbolForCandles(), chartViewData.SuggestedAnalysisLines);
        UpdateCandlestickAxis(chartViewData);
    }

    private void ApplySnapshot(MarketSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _currentSnapshotSymbol = snapshot.Symbol;
        _companyName = snapshot.CompanyName;
        _stockPrice = snapshot.StockPrice;
        _stockUpdatedAt = snapshot.StockUpdatedAt;

        OnPropertyChanged(nameof(CompanyDisplay));
        OnPropertyChanged(nameof(SectorDisplay));
        OnPropertyChanged(nameof(MarketSegmentDisplay));
        OnPropertyChanged(nameof(StockPriceDisplay));
        OnPropertyChanged(nameof(StockUpdatedAtDisplay));
    }

    private async Task LoadPriceHistoryAsync(MarketSnapshotModel snapshot)
    {
        var historyViewData = await _priceHistoryFeatureService.RecordAndLoadAsync(snapshot, HistoryItemLimit, CancellationToken.None);
        ReplaceCollection(PriceHistoryItems, historyViewData.Items);
        ReplaceCollection(StockPriceChartBars, historyViewData.Bars);
    }

    private async Task LoadJapaneseStockChartAsync(string symbol)
    {
        var chartViewData = await _japaneseStockChartFeatureService.LoadAsync(
            symbol,
            SelectedCandleTimeframe,
            SelectedCandleDisplayPeriod,
            CandleFetchLimit,
            CancellationToken.None);

        ReplaceCollection(JapaneseCandlesticks, chartViewData.Candlesticks);
        await LoadPersistedJapaneseAnalysisLinesAsync(symbol, chartViewData.SuggestedAnalysisLines);
        UpdateCandlestickAxis(chartViewData);
    }

    private async Task LoadSectorComparisonAsync(string symbol)
    {
        var comparison = await _sectorComparisonFeatureService.LoadAsync(symbol, CancellationToken.None);
        _sectorName = comparison.SectorName;
        _marketSegmentName = comparison.MarketSegmentDisplay;
        ReplaceCollection(SectorComparisonItems, comparison.Peers);
        OnPropertyChanged(nameof(SectorDisplay));
        OnPropertyChanged(nameof(MarketSegmentDisplay));
        OnPropertyChanged(nameof(HasSectorComparisonItems));
    }

    private static string CreateFailureMessage(Exception exception)
    {
        return ApiErrorClassifier.CreateUserMessage(exception);
    }

    private void StartAnalysisLoad()
    {
        _logger.Info($"分析データ読込を開始します。Symbol={Symbol}");
        ResetSidebarAnalysisState();
        StatusMessage = "分析データ取得中...";
    }

    private void CompleteAnalysisLoad(MarketSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        StatusMessage = $"表示完了: {snapshot.Symbol} (履歴 {PriceHistoryItems.Count} 件)";
        _logger.Info($"分析データ読込完了。Symbol={snapshot.Symbol}, Stock={snapshot.StockPrice}");
    }

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void RemoveSelectedAnalysisLine()
    {
        if (!_selectedJapaneseAnalysisLineId.HasValue)
        {
            return;
        }

        _japaneseAnalysisLineDefinitions.RemoveAll(line => line.Id == _selectedJapaneseAnalysisLineId.Value);
        _selectedJapaneseAnalysisLineId = null;
        RefreshJapaneseAnalysisLines();
        StatusMessage = _japaneseAnalysisLineDefinitions.Count <= 0
            ? "選択中の分析ラインを削除しました。表示中の分析ラインはありません。"
            : $"選択中の分析ラインを削除しました。現在 {_japaneseAnalysisLineDefinitions.Count} 本です。";
        OnAnalysisLineStateChanged();
        PersistJapaneseAnalysisLinesInBackground(GetSymbolForCandles(), SelectedCandleTimeframe, SelectedCandleDisplayPeriod, _japaneseAnalysisLineDefinitions.ToArray());
    }

    private void ClearAnalysisLines()
    {
        var symbol = GetSymbolForCandles();
        var timeframe = SelectedCandleTimeframe;
        var displayPeriod = SelectedCandleDisplayPeriod;
        ClearAnalysisLinesInternal();
        StatusMessage = "分析ラインを全消去しました。";
        PersistJapaneseAnalysisLinesInBackground(symbol, timeframe, displayPeriod, Array.Empty<ChartAnalysisLine>());
    }

    private void HandleAnalysisFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        StatusMessage = $"表示失敗: {CreateFailureMessage(exception)}";
        _logger.LogError(exception, $"分析データ読込失敗。Symbol={Symbol}");
    }

    private void ResetSidebarAnalysisState()
    {
        _sectorName = "-";
        _marketSegmentName = "-";
        ReplaceCollection(SectorComparisonItems, Array.Empty<SectorComparisonPeerItem>());
        OnPropertyChanged(nameof(SectorDisplay));
        OnPropertyChanged(nameof(MarketSegmentDisplay));
        OnPropertyChanged(nameof(HasSectorComparisonItems));
    }

    private void SetSelectedCandleTimeframe(CandleTimeframe timeframe)
    {
        SelectedCandleTimeframe = timeframe;
        OnPropertyChanged(nameof(IsDailyCandleSelected));
        OnPropertyChanged(nameof(IsWeeklyCandleSelected));
    }

    private void SetSelectedCandleDisplayPeriod(CandleDisplayPeriod period)
    {
        SelectedCandleDisplayPeriod = period;
        OnPropertyChanged(nameof(IsOneMonthSelected));
        OnPropertyChanged(nameof(IsThreeMonthsSelected));
        OnPropertyChanged(nameof(IsSixMonthsSelected));
        OnPropertyChanged(nameof(IsOneYearSelected));
    }

    private string GetSymbolForCandles()
    {
        return string.IsNullOrWhiteSpace(_currentSnapshotSymbol) ? Symbol : _currentSnapshotSymbol;
    }

    private void UpdateCandlestickAxis(JapaneseStockChartViewData chartViewData)
    {
        ArgumentNullException.ThrowIfNull(chartViewData);

        _candleChartMinPrice = chartViewData.MinPrice;
        _candleChartMaxPrice = chartViewData.MaxPrice;
        JapaneseCandlestickCanvasWidth = chartViewData.CanvasWidth <= 0d ? 320d : chartViewData.CanvasWidth;
        SyncChartIndicatorOptions(chartViewData.IndicatorDefinitions);
        _allJapaneseOverlayIndicatorSeries.Clear();
        _allJapaneseOverlayIndicatorSeries.AddRange(chartViewData.OverlayIndicatorSeries);
        _allJapaneseIndicatorPanels.Clear();
        _allJapaneseIndicatorPanels.AddRange(chartViewData.IndicatorPanels);
        RefreshVisibleJapaneseChartIndicators();
        RefreshJapaneseAnalysisLines();
        RefreshPendingAnalysisPoint();

        OnPropertyChanged(nameof(JapaneseCandlestickMinPriceLabel));
        OnPropertyChanged(nameof(JapaneseCandlestickMidPriceLabel));
        OnPropertyChanged(nameof(JapaneseCandlestickMaxPriceLabel));
    }

    private void SyncChartIndicatorOptions(IReadOnlyList<ChartIndicatorDefinition> indicatorDefinitions)
    {
        ArgumentNullException.ThrowIfNull(indicatorDefinitions);

        DetachChartIndicatorOptionHandlers();
        ReplaceCollection(
            JapaneseChartIndicatorOptions,
            _chartIndicatorSelectionService.CreateToggleItems(
                JapaneseChartIndicatorOptions.ToList(),
                indicatorDefinitions));

        foreach (var toggleItem in JapaneseChartIndicatorOptions)
        {
            toggleItem.PropertyChanged += OnChartIndicatorToggleItemPropertyChanged;
        }
    }

    private void OnChartIndicatorToggleItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChartIndicatorToggleItem.IsSelected))
        {
            RefreshVisibleJapaneseChartIndicators();
        }
    }

    private void RefreshVisibleJapaneseChartIndicators()
    {
        var selection = _chartIndicatorSelectionService.CreateSelection(
            JapaneseChartIndicatorOptions.ToList(),
            _allJapaneseOverlayIndicatorSeries,
            _allJapaneseIndicatorPanels,
            VisibleJapaneseIndicatorPanels.ToList());

        ReplaceCollection(VisibleJapaneseOverlayIndicators, selection.VisibleOverlaySeries);
        ReplaceCollection(VisibleJapaneseIndicatorPanels, selection.VisibleIndicatorPanels);
        OnPropertyChanged(nameof(HasVisibleJapaneseChartIndicators));
        OnPropertyChanged(nameof(HasVisibleJapaneseIndicatorPanels));
    }

    private void EvaluatePriceAlert(MarketSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!IsPriceAlertEnabled
            || !decimal.TryParse(AlertThresholdText, NumberStyles.Float, CultureInfo.CurrentCulture, out var threshold)
            || threshold <= 0m)
        {
            _hasPriceAlertBaseline = false;
            return;
        }

        var isAtOrAboveThreshold = snapshot.StockPrice >= threshold;
        if (!_hasPriceAlertBaseline)
        {
            _hasPriceAlertBaseline = true;
            _wasAlertAtOrAboveThreshold = isAtOrAboveThreshold;
            return;
        }

        if (isAtOrAboveThreshold == _wasAlertAtOrAboveThreshold)
        {
            return;
        }

        _wasAlertAtOrAboveThreshold = isAtOrAboveThreshold;
        var direction = isAtOrAboveThreshold ? "上回りました" : "下回りました";
        var title = $"株価通知: {snapshot.Symbol}";
        var message = $"{snapshot.CompanyName} が通知価格 {threshold.ToString("N2", CultureInfo.CurrentCulture)} 円を{direction}。現在値: {snapshot.StockPrice.ToString("N2", CultureInfo.CurrentCulture)} 円";
        _desktopNotificationService.ShowNotification(title, message);
        _logger.Info($"PriceAlertTriggered: Symbol={snapshot.Symbol}, Threshold={threshold}, Price={snapshot.StockPrice}, Direction={direction}");
    }

    private void DetachChartIndicatorOptionHandlers()
    {
        foreach (var item in JapaneseChartIndicatorOptions)
        {
            item.PropertyChanged -= OnChartIndicatorToggleItemPropertyChanged;
        }
    }

    private static string FormatAxisLabel(decimal value)
    {
        return value <= 0m ? "-" : value.ToString("N2", CultureInfo.CurrentCulture);
    }

    private void RefreshJapaneseAnalysisLines()
    {
        ReplaceCollection(
            JapaneseAnalysisLines,
            _chartAnalysisLineService.CreateRenderItems(
                _japaneseAnalysisLineDefinitions,
                JapaneseCandlestickCanvasWidth,
                AnalysisLineCanvasHeight,
                _selectedJapaneseAnalysisLineId));
        OnPropertyChanged(nameof(HasJapaneseAnalysisLines));
    }

    private void RefreshPendingAnalysisPoint()
    {
        if (!_hasPendingAnalysisLinePoint)
        {
            ReplaceCollection(JapanesePendingAnalysisPoints, Array.Empty<ChartAnalysisPointRenderItem>());
            OnPropertyChanged(nameof(HasPendingJapaneseAnalysisPoint));
            return;
        }

        ReplaceCollection(
            JapanesePendingAnalysisPoints,
            [
                new ChartAnalysisPointRenderItem
                {
                    X = _pendingAnalysisLinePointXRatio * JapaneseCandlestickCanvasWidth,
                    Y = _pendingAnalysisLinePointYRatio * AnalysisLineCanvasHeight
                }
            ]);
        OnPropertyChanged(nameof(HasPendingJapaneseAnalysisPoint));
    }

    private void ClearAnalysisLinesInternal()
    {
        _japaneseAnalysisLineDefinitions.Clear();
        _selectedJapaneseAnalysisLineId = null;
        _isJapaneseAnalysisLineDragging = false;
        _hasPendingAnalysisLinePoint = false;
        RefreshJapaneseAnalysisLines();
        OnAnalysisLineStateChanged();
    }

    private void OnAnalysisLineStateChanged()
    {
        OnPropertyChanged(nameof(IsAnalysisLineDrawingEnabled));
        OnPropertyChanged(nameof(AnalysisLineActionText));
        OnPropertyChanged(nameof(AnalysisLineQuickGuideText));
        OnPropertyChanged(nameof(AnalysisLineStatusText));
        OnPropertyChanged(nameof(HasSelectedJapaneseAnalysisLine));
        RefreshPendingAnalysisPoint();
        _removeSelectedAnalysisLineCommand.RaiseCanExecuteChanged();
        _clearAnalysisLinesCommand.RaiseCanExecuteChanged();
    }

    private async Task LoadPersistedJapaneseAnalysisLinesAsync(
        string symbol,
        IReadOnlyList<ChartAnalysisLine> suggestedLines)
    {
        _suggestedJapaneseAnalysisLines.Clear();
        _suggestedJapaneseAnalysisLines.AddRange(suggestedLines.Select(CloneAnalysisLine));

        var persistedLines = await _chartAnalysisLineRepository.GetAsync(
            symbol,
            SelectedCandleTimeframe,
            SelectedCandleDisplayPeriod,
            CancellationToken.None);

        _japaneseAnalysisLineDefinitions.Clear();
        if (persistedLines.Count > 0)
        {
            _japaneseAnalysisLineDefinitions.AddRange(persistedLines);
        }
        else if (suggestedLines.Count > 0)
        {
            _japaneseAnalysisLineDefinitions.AddRange(suggestedLines);
            PersistJapaneseAnalysisLinesInBackground(symbol, SelectedCandleTimeframe, SelectedCandleDisplayPeriod, suggestedLines);
        }

        if (_selectedJapaneseAnalysisLineId.HasValue
            && !_japaneseAnalysisLineDefinitions.Exists(line => line.Id == _selectedJapaneseAnalysisLineId.Value))
        {
            _selectedJapaneseAnalysisLineId = null;
        }

    }

    private void PersistJapaneseAnalysisLinesInBackground(
        string symbol,
        CandleTimeframe timeframe,
        CandleDisplayPeriod displayPeriod,
        IReadOnlyList<ChartAnalysisLine> lines)
    {
        PersistJapaneseAnalysisLinesAsync(symbol, timeframe, displayPeriod, lines);
    }

    private async void PersistJapaneseAnalysisLinesAsync(
        string symbol,
        CandleTimeframe timeframe,
        CandleDisplayPeriod displayPeriod,
        IReadOnlyList<ChartAnalysisLine> lines)
    {
        try
        {
            await _chartAnalysisLineRepository.SaveAsync(symbol, timeframe, displayPeriod, lines, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"分析ライン保存に失敗しました。Symbol={symbol}, Timeframe={timeframe}, Period={displayPeriod}");
        }
    }

    private bool TryNormalizeChartPoint(double chartX, double chartY, out double normalizedXRatio, out double normalizedYRatio)
    {
        normalizedXRatio = NormalizeCoordinate(chartX, JapaneseCandlestickCanvasWidth);
        normalizedYRatio = NormalizeCoordinate(chartY, AnalysisLineCanvasHeight);
        return JapaneseCandlestickCanvasWidth > 0d;
    }

    private double CalculateHitToleranceRatio()
    {
        return Math.Max(10d / Math.Max(JapaneseCandlestickCanvasWidth, 1d), 10d / AnalysisLineCanvasHeight);
    }

    private void ReplaceAnalysisLine(Guid lineId, Func<ChartAnalysisLine, ChartAnalysisLine> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);

        for (var index = 0; index < _japaneseAnalysisLineDefinitions.Count; index++)
        {
            if (_japaneseAnalysisLineDefinitions[index].Id != lineId)
            {
                continue;
            }

            _japaneseAnalysisLineDefinitions[index] = updater(_japaneseAnalysisLineDefinitions[index]);
            return;
        }
    }

    private ChartAnalysisLine? GetSelectedAnalysisLine()
    {
        return _selectedJapaneseAnalysisLineId.HasValue
            ? _japaneseAnalysisLineDefinitions.FirstOrDefault(line => line.Id == _selectedJapaneseAnalysisLineId.Value)
            : null;
    }

    private static IReadOnlyList<ChartAnalysisLineTypeOption> CreateAnalysisLineTypeOptions()
    {
        return
        [
            CreateAnalysisLineTypeOption(ChartAnalysisLineType.TrendLine),
            CreateAnalysisLineTypeOption(ChartAnalysisLineType.SupportLine),
            CreateAnalysisLineTypeOption(ChartAnalysisLineType.ResistanceLine)
        ];
    }

    private static ChartAnalysisLineTypeOption CreateAnalysisLineTypeOption(ChartAnalysisLineType lineType)
    {
        return new ChartAnalysisLineTypeOption(
            lineType,
            ChartAnalysisLineStyleCatalog.GetDisplayName(lineType),
            ChartAnalysisLineStyleCatalog.GetDescription(lineType),
            ChartAnalysisLineStyleCatalog.GetStrokeColor(lineType),
            ChartAnalysisLineStyleCatalog.GetStrokeDashArray(lineType));
    }

    private static string GetAnalysisLineTypeDisplayName(ChartAnalysisLineType lineType)
    {
        return ChartAnalysisLineStyleCatalog.GetDisplayName(lineType);
    }

    private static double NormalizeCoordinate(double value, double maximum)
    {
        if (maximum <= 0d || double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0d;
        }

        return Math.Clamp(value / maximum, 0d, 1d);
    }

    private static ChartAnalysisLine CloneAnalysisLine(ChartAnalysisLine line)
    {
        return new ChartAnalysisLine(line.Id, line.LineType, line.StartXRatio, line.StartYRatio, line.EndXRatio, line.EndYRatio);
    }

    private ChartAnalysisLine[] CreateAutoAnalysisLines()
    {
        return _suggestedJapaneseAnalysisLines
            .Select(CloneAnalysisLine)
            .ToArray();
    }

    private static bool AreEquivalentAnalysisLines(ChartAnalysisLine left, ChartAnalysisLine right)
    {
        return left.LineType == right.LineType
            && Math.Abs(left.StartXRatio - right.StartXRatio) < 0.0001d
            && Math.Abs(left.StartYRatio - right.StartYRatio) < 0.0001d
            && Math.Abs(left.EndXRatio - right.EndXRatio) < 0.0001d
            && Math.Abs(left.EndYRatio - right.EndYRatio) < 0.0001d;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(items);

        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    /// <summary>
    /// リソースを解放する。
    /// </summary>
    public void Dispose()
    {
        DetachChartIndicatorOptionHandlers();
        if (_desktopNotificationService is IDisposable disposableNotificationService)
        {
            disposableNotificationService.Dispose();
        }

        _logger.Info("MainViewModelを破棄しました。");
    }
}