using System.Windows.Input;
using System.Windows.Threading;
using MarketMonitor.Infrastructure;
using MarketMonitor.Models;
using MarketMonitor.Services;

namespace MarketMonitor.ViewModels;

/// <summary>
/// メイン画面の状態と操作を管理するViewModel。
/// </summary>
public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly IApiService _apiService;
    private readonly IAppLogger _logger;
    private readonly DispatcherTimer _timer;

    private string _symbol = "IBM";
    private int _autoUpdateIntervalSeconds = 60;
    private bool _isAutoUpdateEnabled;
    private decimal _exchangeRate;
    private decimal _stockPrice;
    private DateTimeOffset _exchangeUpdatedAt;
    private DateTimeOffset _stockUpdatedAt;
    private string _statusMessage = "準備完了";
    private bool _isJapaneseStock;

    public MainViewModel(IApiService apiService, IAppLogger logger)
    {
        _apiService = apiService;
        _logger = logger;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ToggleAutoUpdateCommand = new RelayCommand(ToggleAutoUpdate);

        _timer = new DispatcherTimer();
        _timer.Tick += OnTimerTick;
        UpdateTimerInterval();
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
            var normalized = value < 10 ? 10 : value;
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
        private set => SetProperty(ref _isAutoUpdateEnabled, value);
    }

    /// <summary>
    /// 画面表示用の為替レート。
    /// </summary>
    public string ExchangeRateDisplay => _exchangeRate <= 0 ? "-" : _exchangeRate.ToString("N4");

    /// <summary>
    /// 画面表示用の株価。
    /// </summary>
    public string StockPriceDisplay => _stockPrice <= 0 ? "-" : _stockPrice.ToString("N2");

    /// <summary>
    /// 為替の最終更新時刻表示。
    /// </summary>
    public string ExchangeUpdatedAtDisplay => _exchangeUpdatedAt == default
        ? "為替更新: 未取得"
        : $"為替更新: {_exchangeUpdatedAt.LocalDateTime:yyyy/MM/dd HH:mm:ss}";

    /// <summary>
    /// 株価の最終更新時刻表示。
    /// </summary>
    public string StockUpdatedAtDisplay => _stockUpdatedAt == default
        ? "株価更新: 未取得"
        : $"株価更新: {_stockUpdatedAt.LocalDateTime:yyyy/MM/dd HH:mm:ss}";

    /// <summary>
    /// ステータスメッセージ。
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// 日本株表示かどうか。
    /// </summary>
    public bool IsJapaneseStock
    {
        get => _isJapaneseStock;
        private set => SetProperty(ref _isJapaneseStock, value);
    }

    /// <summary>
    /// 手動更新コマンド。
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 自動更新切替コマンド。
    /// </summary>
    public ICommand ToggleAutoUpdateCommand { get; }

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
            _logger.Info($"手動/自動更新を開始します。Symbol={Symbol}, Interval={AutoUpdateIntervalSeconds}s");
            StatusMessage = "データ取得中...";
            var snapshot = await _apiService.GetMarketSnapshotAsync(Symbol, CancellationToken.None);
            ApplySnapshot(snapshot);
            StatusMessage = $"更新完了: {snapshot.Symbol}";
            _logger.Info($"更新完了。Symbol={snapshot.Symbol}, Exchange={snapshot.ExchangeRate}, Stock={snapshot.StockPrice}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"更新失敗: {ex.Message}";
            _logger.Error(ex, $"更新失敗。Symbol={Symbol}");
        }
    }

    private void ApplySnapshot(MarketSnapshot snapshot)
    {
        _exchangeRate = snapshot.ExchangeRate;
        _stockPrice = snapshot.StockPrice;
        _exchangeUpdatedAt = snapshot.ExchangeUpdatedAt;
        _stockUpdatedAt = snapshot.StockUpdatedAt;
        IsJapaneseStock = snapshot.Symbol.EndsWith(".T", StringComparison.OrdinalIgnoreCase);

        OnPropertyChanged(nameof(ExchangeRateDisplay));
        OnPropertyChanged(nameof(StockPriceDisplay));
        OnPropertyChanged(nameof(ExchangeUpdatedAtDisplay));
        OnPropertyChanged(nameof(StockUpdatedAtDisplay));
    }

    private void ToggleAutoUpdate()
    {
        if (IsAutoUpdateEnabled)
        {
            _timer.Stop();
            IsAutoUpdateEnabled = false;
            StatusMessage = "自動更新を停止しました。";
            _logger.Info("自動更新を停止しました。");
            return;
        }

        UpdateTimerInterval();
        _timer.Start();
        IsAutoUpdateEnabled = true;
        StatusMessage = "自動更新を開始しました。";
        _logger.Info($"自動更新を開始しました。Interval={AutoUpdateIntervalSeconds}s");
    }

    private void UpdateTimerInterval()
    {
        _timer.Interval = TimeSpan.FromSeconds(AutoUpdateIntervalSeconds);
        _logger.Info($"自動更新間隔を設定しました。Interval={AutoUpdateIntervalSeconds}s");
    }

    /// <summary>
    /// リソースを解放する。
    /// </summary>
    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _logger.Info("MainViewModelを破棄しました。");
    }
}
