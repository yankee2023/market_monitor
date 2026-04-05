using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Windows.Input;
using MarketMonitor.Features.Dashboard.Models;
using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;
using MarketMonitor.Features.MarketSnapshot.Services;
using MarketMonitor.Features.PriceHistory.Models;
using MarketMonitor.Features.PriceHistory.Services;
using MarketMonitor.Composition;
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
    private const int MinimumAutoUpdateIntervalSeconds = 10;
    private const int HistoryItemLimit = 20;
    private const int CandleFetchLimit = 320;

    private readonly IMarketSnapshotService _marketSnapshotService;
    private readonly IPriceHistoryFeatureService _priceHistoryFeatureService;
    private readonly IJapaneseStockChartFeatureService _japaneseStockChartFeatureService;
    private readonly IAppLogger _logger;
    private readonly IUiDispatcherTimer _timer;

    private string _symbol = "7203";
    private int _autoUpdateIntervalSeconds = 60;
    private bool _isAutoUpdateEnabled;
    private decimal _stockPrice;
    private DateTimeOffset _stockUpdatedAt;
    private decimal _candleChartMinPrice;
    private decimal _candleChartMaxPrice;
    private double _japaneseCandlestickCanvasWidth = 320d;
    private readonly List<ChartIndicatorRenderSeries> _allJapaneseChartIndicatorSeries = [];
    private readonly string _japaneseCandlestickYAxisTitle = "株価 (円)";
    private readonly string _japaneseCandlestickXAxisTitle = "日付";
    private string _companyName = string.Empty;
    private string _statusMessage = "準備完了";
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
        IAppLogger logger,
        IUiDispatcherTimer timer)
    {
        _marketSnapshotService = marketSnapshotService ?? throw new ArgumentNullException(nameof(marketSnapshotService));
        _priceHistoryFeatureService = priceHistoryFeatureService ?? throw new ArgumentNullException(nameof(priceHistoryFeatureService));
        _japaneseStockChartFeatureService = japaneseStockChartFeatureService ?? throw new ArgumentNullException(nameof(japaneseStockChartFeatureService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timer = timer ?? throw new ArgumentNullException(nameof(timer));

        _timer.Tick += OnTimerTick;
        UpdateTimerInterval();

        PriceHistoryItems = new ObservableCollection<PriceHistoryEntry>();
        StockPriceChartBars = new ObservableCollection<PriceHistoryBar>();
        JapaneseCandlesticks = new ObservableCollection<CandlestickRenderItem>();
        JapaneseChartIndicatorOptions = new ObservableCollection<ChartIndicatorToggleItem>();
        VisibleJapaneseChartIndicators = new ObservableCollection<ChartIndicatorRenderSeries>();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ToggleAutoUpdateCommand = new RelayCommand(ToggleAutoUpdate);
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
    /// 自動更新間隔（秒）。
    /// </summary>
    public int AutoUpdateIntervalSeconds
    {
        get => _autoUpdateIntervalSeconds;
        set
        {
            var normalized = value < MinimumAutoUpdateIntervalSeconds ? MinimumAutoUpdateIntervalSeconds : value;
            if (SetProperty(ref _autoUpdateIntervalSeconds, normalized))
            {
                UpdateTimerInterval();
            }
        }
    }

    /// <summary>
    /// 自動更新の有効/無効。
    /// </summary>
    public bool IsAutoUpdateEnabled
    {
        get => _isAutoUpdateEnabled;
        private set
        {
            if (SetProperty(ref _isAutoUpdateEnabled, value))
            {
                OnPropertyChanged(nameof(AutoUpdateStateDisplay));
            }
        }
    }

    /// <summary>
    /// 自動更新状態の表示文字列。
    /// </summary>
    public string AutoUpdateStateDisplay => IsAutoUpdateEnabled ? "自動更新: オン" : "自動更新: オフ";

    /// <summary>
    /// 画面表示用の株価。
    /// </summary>
    public string StockPriceDisplay => _stockPrice <= 0 ? "-" : _stockPrice.ToString("N2", CultureInfo.CurrentCulture);

    /// <summary>
    /// 株価の最終更新時刻表示。
    /// </summary>
    public string StockUpdatedAtDisplay => _stockUpdatedAt == default
        ? "株価更新: 未取得"
        : $"株価更新: {_stockUpdatedAt.LocalDateTime:yyyy/MM/dd HH:mm:ss}";

    /// <summary>
    /// 画面表示用の銘柄名。
    /// </summary>
    public string CompanyDisplay => string.IsNullOrWhiteSpace(_companyName)
        ? $"銘柄: {_currentSnapshotSymbol}"
        : $"銘柄: {_companyName} ({_currentSnapshotSymbol})";

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
    /// 現在表示中のチャート指標描画データ。
    /// </summary>
    public ObservableCollection<ChartIndicatorRenderSeries> VisibleJapaneseChartIndicators { get; }

    /// <summary>
    /// 現在表示中のオーバーレイ指標が存在するかどうか。
    /// </summary>
    public bool HasVisibleJapaneseChartIndicators => VisibleJapaneseChartIndicators.Count > 0;

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
    /// 手動更新コマンド。
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 自動更新切替コマンド。
    /// </summary>
    public ICommand ToggleAutoUpdateCommand { get; }

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
        _logger.Info("初期表示時のデータ取得を開始します。");
        await RefreshAsync();
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            StartRefresh();
            var snapshot = await _marketSnapshotService.GetMarketSnapshotAsync(Symbol, CancellationToken.None);
            ApplySnapshot(snapshot);
            await LoadPriceHistoryAsync(snapshot);
            await LoadJapaneseStockChartAsync(snapshot.Symbol);
            CompleteRefresh(snapshot);
        }
        catch (HttpRequestException ex)
        {
            HandleRefreshFailure(ex);
        }
        catch (InvalidOperationException ex)
        {
            HandleRefreshFailure(ex);
        }
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

    private void ToggleAutoUpdate()
    {
        if (IsAutoUpdateEnabled)
        {
            SetAutoUpdate(false);
            return;
        }

        UpdateTimerInterval();
        SetAutoUpdate(true);
    }

    private void UpdateTimerInterval()
    {
        _timer.Interval = TimeSpan.FromSeconds(AutoUpdateIntervalSeconds);
        _logger.Info($"自動更新間隔を設定しました。Interval={AutoUpdateIntervalSeconds}s");
    }

    private static string CreateFailureMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests })
        {
            return ApiErrorMessages.RateLimitMessage;
        }

        if (exception is InvalidOperationException invalidOperationException
            && invalidOperationException.Message.StartsWith(ApiErrorMessages.RateLimitMessage, StringComparison.Ordinal))
        {
            return ApiErrorMessages.RateLimitMessage;
        }

        return exception.Message;
    }

    private void StartRefresh()
    {
        _logger.Info($"手動/自動更新を開始します。Symbol={Symbol}, Interval={AutoUpdateIntervalSeconds}s");
        StatusMessage = "データ取得中...";
    }

    private void CompleteRefresh(MarketSnapshotModel snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        StatusMessage = $"更新完了: {snapshot.Symbol} (履歴 {PriceHistoryItems.Count} 件)";
        _logger.Info($"更新完了。Symbol={snapshot.Symbol}, Stock={snapshot.StockPrice}");
    }

    private void HandleRefreshFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        StatusMessage = $"更新失敗: {CreateFailureMessage(exception)}";
        _logger.LogError(exception, $"更新失敗。Symbol={Symbol}");
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
        _allJapaneseChartIndicatorSeries.Clear();
        _allJapaneseChartIndicatorSeries.AddRange(chartViewData.IndicatorSeries);
        RefreshVisibleJapaneseChartIndicators();

        OnPropertyChanged(nameof(JapaneseCandlestickMinPriceLabel));
        OnPropertyChanged(nameof(JapaneseCandlestickMidPriceLabel));
        OnPropertyChanged(nameof(JapaneseCandlestickMaxPriceLabel));
    }

    private void SyncChartIndicatorOptions(IReadOnlyList<ChartIndicatorDefinition> indicatorDefinitions)
    {
        ArgumentNullException.ThrowIfNull(indicatorDefinitions);

        var previousSelections = JapaneseChartIndicatorOptions.ToDictionary(
            item => item.IndicatorKey,
            item => item.IsSelected,
            StringComparer.Ordinal);

        DetachChartIndicatorOptionHandlers();
        JapaneseChartIndicatorOptions.Clear();

        foreach (var definition in indicatorDefinitions.OrderBy(item => item.DisplayOrder))
        {
            var isSelected = previousSelections.TryGetValue(definition.IndicatorKey, out var previous)
                ? previous
                : definition.IsEnabledByDefault;

            var toggleItem = new ChartIndicatorToggleItem(
                definition.IndicatorKey,
                definition.DisplayName,
                definition.AccentColor,
                definition.Placement,
                definition.DisplayOrder,
                isSelected);

            toggleItem.PropertyChanged += OnChartIndicatorToggleItemPropertyChanged;
            JapaneseChartIndicatorOptions.Add(toggleItem);
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
        var selectedIndicatorKeys = JapaneseChartIndicatorOptions
            .Where(item => item.IsSelected)
            .Select(item => item.IndicatorKey)
            .ToHashSet(StringComparer.Ordinal);

        var visibleSeries = _allJapaneseChartIndicatorSeries
            .Where(item => item.Placement == ChartIndicatorPlacement.OverlayPriceChart)
            .Where(item => selectedIndicatorKeys.Contains(item.IndicatorKey))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.LegendLabel, StringComparer.CurrentCulture)
            .ToList();

        ReplaceCollection(VisibleJapaneseChartIndicators, visibleSeries);
        OnPropertyChanged(nameof(HasVisibleJapaneseChartIndicators));
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

    private void SetAutoUpdate(bool isEnabled)
    {
        if (isEnabled)
        {
            _timer.Start();
            IsAutoUpdateEnabled = true;
            StatusMessage = "自動更新を開始しました。";
            _logger.Info($"自動更新を開始しました。Interval={AutoUpdateIntervalSeconds}s");
            return;
        }

        _timer.Stop();
        IsAutoUpdateEnabled = false;
        StatusMessage = "自動更新を停止しました。";
        _logger.Info("自動更新を停止しました。");
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
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        DetachChartIndicatorOptionHandlers();
        _logger.Info("MainViewModelを破棄しました。");
    }
}