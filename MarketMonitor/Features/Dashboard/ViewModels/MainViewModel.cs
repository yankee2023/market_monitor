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

    private readonly IMarketSnapshotService _marketSnapshotService;
    private readonly IPriceHistoryFeatureService _priceHistoryFeatureService;
    private readonly IJapaneseStockChartFeatureService _japaneseStockChartFeatureService;
    private readonly ISectorComparisonFeatureService _sectorComparisonFeatureService;
    private readonly IChartIndicatorSelectionService _chartIndicatorSelectionService;
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
    private CandleTimeframe _selectedCandleTimeframe = CandleTimeframe.Daily;
    private CandleDisplayPeriod _selectedCandleDisplayPeriod = CandleDisplayPeriod.OneMonth;
    private string _currentSnapshotSymbol = "7203.T";

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
        IChartIndicatorSelectionService? chartIndicatorSelectionService = null)
    {
        _marketSnapshotService = marketSnapshotService ?? throw new ArgumentNullException(nameof(marketSnapshotService));
        _priceHistoryFeatureService = priceHistoryFeatureService ?? throw new ArgumentNullException(nameof(priceHistoryFeatureService));
        _japaneseStockChartFeatureService = japaneseStockChartFeatureService ?? throw new ArgumentNullException(nameof(japaneseStockChartFeatureService));
        _sectorComparisonFeatureService = sectorComparisonFeatureService ?? throw new ArgumentNullException(nameof(sectorComparisonFeatureService));
        _chartIndicatorSelectionService = chartIndicatorSelectionService ?? new ChartIndicatorSelectionService();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _desktopNotificationService = desktopNotificationService ?? throw new ArgumentNullException(nameof(desktopNotificationService));

        PriceHistoryItems = new ObservableCollection<PriceHistoryEntry>();
        StockPriceChartBars = new ObservableCollection<PriceHistoryBar>();
        JapaneseCandlesticks = new ObservableCollection<CandlestickRenderItem>();
        JapaneseChartIndicatorOptions = new ObservableCollection<ChartIndicatorToggleItem>();
        VisibleJapaneseOverlayIndicators = new ObservableCollection<ChartIndicatorRenderSeries>();
        VisibleJapaneseIndicatorPanels = new ObservableCollection<IndicatorPanelRenderData>();
        SectorComparisonItems = new ObservableCollection<SectorComparisonPeerItem>();

        ApplySymbolCommand = new AsyncRelayCommand(LoadAnalysisAsync);
        ToggleSidebarCommand = new RelayCommand(ToggleSidebar);
        ShowDailyCandlesCommand = new AsyncRelayCommand(() => ChangeCandlesAsync(CandleTimeframe.Daily));
        ShowWeeklyCandlesCommand = new AsyncRelayCommand(() => ChangeCandlesAsync(CandleTimeframe.Weekly));
        ShowOneMonthCandlesCommand = new AsyncRelayCommand(() => ChangeCandlePeriodAsync(CandleDisplayPeriod.OneMonth));
        ShowThreeMonthsCandlesCommand = new AsyncRelayCommand(() => ChangeCandlePeriodAsync(CandleDisplayPeriod.ThreeMonths));
        ShowSixMonthsCandlesCommand = new AsyncRelayCommand(() => ChangeCandlePeriodAsync(CandleDisplayPeriod.SixMonths));
        ShowOneYearCandlesCommand = new AsyncRelayCommand(() => ChangeCandlePeriodAsync(CandleDisplayPeriod.OneYear));
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
    public string SidebarToggleText => IsSidebarCollapsed ? "補助ペインを開く" : "補助ペインを閉じる";

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
    /// 初期表示時の処理。
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.Info("初期表示時の分析読込を開始します。");
        await LoadAnalysisAsync();
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
        var snapshot = await _marketSnapshotService.GetMarketSnapshotAsync(Symbol, CancellationToken.None);
        ApplySnapshot(snapshot);
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