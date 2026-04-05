using System.Globalization;

using MarketMonitor.Features.Dashboard.ViewModels;
using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;
using MarketMonitor.Features.MarketSnapshot.Services;
using MarketMonitor.Features.PriceHistory.Models;
using MarketMonitor.Features.PriceHistory.Services;
using MarketMonitor.Shared.Infrastructure;
using MarketMonitor.Shared.Logging;
using MarketSnapshotModel = MarketMonitor.Features.MarketSnapshot.Models.MarketSnapshot;

namespace MarketMonitorTest;

/// <summary>
/// MainViewModelの画面状態管理を検証するテストクラス。
/// </summary>
public class MainViewModelTest
{
    /// <summary>
    /// 初期化時に取得した値が表示用プロパティへ反映されることをテスト。
    /// 期待値: 株価、履歴、ローソク足、状態表示が更新される。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_ReflectsSnapshotValues()
    {
        // Arrange
        var snapshot = new MarketSnapshotModel
        {
            Symbol = "7203.T",
            CompanyName = "トヨタ自動車",
            StockPrice = 210.50m,
            StockUpdatedAt = DateTimeOffset.Now
        };
        var historyItem = new PriceHistoryEntry
        {
            Id = 1,
            Symbol = "7203.T",
            StockPrice = 210.50m,
            RecordedAt = snapshot.StockUpdatedAt
        };
        var historyBar = new PriceHistoryBar
        {
            Label = "10:00",
            ValueText = "210.50",
            Height = 88
        };
        var candle = new CandlestickRenderItem
        {
            Label = "04/01",
            WickTop = 12,
            WickHeight = 40,
            BodyTop = 18,
            BodyHeight = 16,
            BodyColor = "#00AA00",
            WickColor = "#00AA00"
        };
        var indicatorDefinitions = new[]
        {
            new ChartIndicatorDefinition("ma5", "MA5", ChartIndicatorPlacement.OverlayPriceChart, "#F59E0B", true, 10),
            new ChartIndicatorDefinition("ma25", "MA25", ChartIndicatorPlacement.OverlayPriceChart, "#10B981", true, 20),
            new ChartIndicatorDefinition("ma75", "MA75", ChartIndicatorPlacement.OverlayPriceChart, "#334155", true, 30)
        };
        var movingAverageLine = new ChartIndicatorRenderSeries
        {
            IndicatorKey = "ma5",
            IndicatorDisplayName = "MA5",
            LegendLabel = "MA5",
            Placement = ChartIndicatorPlacement.OverlayPriceChart,
            DisplayOrder = 10,
            Points = "17,120 51,100",
            StrokeColor = "#F59E0B",
            StrokeThickness = 1.8d
        };
        var movingAverageLine25 = new ChartIndicatorRenderSeries
        {
            IndicatorKey = "ma25",
            IndicatorDisplayName = "MA25",
            LegendLabel = "MA25",
            Placement = ChartIndicatorPlacement.OverlayPriceChart,
            DisplayOrder = 20,
            Points = "17,130 51,110",
            StrokeColor = "#10B981",
            StrokeThickness = 2.1d
        };
        var movingAverageLine75 = new ChartIndicatorRenderSeries
        {
            IndicatorKey = "ma75",
            IndicatorDisplayName = "MA75",
            LegendLabel = "MA75",
            Placement = ChartIndicatorPlacement.OverlayPriceChart,
            DisplayOrder = 30,
            Points = "17,140 51,130",
            StrokeColor = "#334155",
            StrokeThickness = 2.7d,
            StrokeDashArray = "8 4"
        };
        var marketSnapshotService = new FakeMarketSnapshotService(snapshot);
        var priceHistoryFeatureService = new FakePriceHistoryFeatureService(new PriceHistoryViewData([historyItem], [historyBar]));
        var japaneseStockChartFeatureService = new FakeJapaneseStockChartFeatureService(
            new JapaneseStockChartViewData(true, [candle], indicatorDefinitions, [movingAverageLine, movingAverageLine25, movingAverageLine75], 205m, 220m, 320d));
        var fakeLogger = new FakeLogger();
        var viewModel = new MainViewModel(
            marketSnapshotService,
            priceHistoryFeatureService,
            japaneseStockChartFeatureService,
            fakeLogger,
            new FakeUiDispatcherTimer());

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.Equal("210.50", viewModel.StockPriceDisplay);
        Assert.Equal(
            "株価更新: " + snapshot.StockUpdatedAt.LocalDateTime.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture),
            viewModel.StockUpdatedAtDisplay);
        Assert.Equal("銘柄: トヨタ自動車 (7203.T)", viewModel.CompanyDisplay);
        Assert.Equal("株価 (円)", viewModel.JapaneseCandlestickYAxisTitle);
        Assert.Equal("日付", viewModel.JapaneseCandlestickXAxisTitle);
        Assert.Equal("205.00", viewModel.JapaneseCandlestickMinPriceLabel);
        Assert.Equal("212.50", viewModel.JapaneseCandlestickMidPriceLabel);
        Assert.Equal("220.00", viewModel.JapaneseCandlestickMaxPriceLabel);
        Assert.Equal(320d, viewModel.JapaneseCandlestickCanvasWidth);
        Assert.StartsWith("更新完了:", viewModel.StatusMessage);
        Assert.Single(viewModel.PriceHistoryItems);
        Assert.Single(viewModel.StockPriceChartBars);
        Assert.Single(viewModel.JapaneseCandlesticks);
        Assert.Equal(3, viewModel.JapaneseChartIndicatorOptions.Count);
        Assert.Equal(3, viewModel.VisibleJapaneseChartIndicators.Count);
    }

    /// <summary>
    /// 更新間隔に10秒未満を設定した場合に10秒へ補正されることをテスト。
    /// 期待値: AutoUpdateIntervalSeconds が 10。
    /// </summary>
    [Fact]
    public void AutoUpdateIntervalSeconds_ClampsToTen()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AutoUpdateIntervalSeconds = 1;

        // Assert
        Assert.Equal(10, viewModel.AutoUpdateIntervalSeconds);
    }

    /// <summary>
    /// 自動更新切替コマンド実行で有効/無効が切り替わることをテスト。
    /// 期待値: true から false へ遷移する。
    /// </summary>
    [Fact]
    public void ToggleAutoUpdateCommand_TogglesEnabledState()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.ToggleAutoUpdateCommand.Execute(null);
        var first = viewModel.IsAutoUpdateEnabled;

        viewModel.ToggleAutoUpdateCommand.Execute(null);
        var second = viewModel.IsAutoUpdateEnabled;

        // Assert
        Assert.True(first);
        Assert.False(second);
    }

    /// <summary>
    /// 現在値取得失敗時に失敗メッセージが設定されることをテスト。
    /// 期待値: StatusMessage が "更新失敗:" で始まる。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SetsFailureMessage_WhenSnapshotLoadThrows()
    {
        // Arrange
        var viewModel = new MainViewModel(
            new ThrowingMarketSnapshotService(),
            new FakePriceHistoryFeatureService(),
            new FakeJapaneseStockChartFeatureService(),
            new FakeLogger(),
            new FakeUiDispatcherTimer());

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.StartsWith("更新失敗:", viewModel.StatusMessage);
    }

    /// <summary>
    /// 日足切替コマンド実行時にチャート再読込が走ることをテスト。
    /// 期待値: 2回目の読込が Daily 指定で発生する。
    /// </summary>
    [Fact]
    public async Task ShowDailyCandlesCommand_ReloadsChart()
    {
        // Arrange
        var chartService = new FakeJapaneseStockChartFeatureService();
        var viewModel = CreateViewModel(japaneseStockChartFeatureService: chartService);
        await viewModel.InitializeAsync();
        chartService.PrepareNextCall();

        // Act
        viewModel.ShowDailyCandlesCommand.Execute(null);
        await chartService.WaitForNextCallAsync();

        // Assert
        Assert.Equal(CandleTimeframe.Daily, chartService.LastTimeframe);
        Assert.Equal("7203.T", chartService.LastSymbol);
        Assert.True(viewModel.IsDailyCandleSelected);
    }

    /// <summary>
    /// 表示期間切替コマンド実行時にチャート再読込が走ることをテスト。
    /// 期待値: 2回目の読込が ThreeMonths 指定で発生する。
    /// </summary>
    [Fact]
    public async Task ShowThreeMonthsCandlesCommand_ReloadsChart()
    {
        // Arrange
        var chartService = new FakeJapaneseStockChartFeatureService();
        var viewModel = CreateViewModel(japaneseStockChartFeatureService: chartService);
        await viewModel.InitializeAsync();
        chartService.PrepareNextCall();

        // Act
        viewModel.ShowThreeMonthsCandlesCommand.Execute(null);
        await chartService.WaitForNextCallAsync();

        // Assert
        Assert.Equal(CandleDisplayPeriod.ThreeMonths, chartService.LastDisplayPeriod);
        Assert.True(viewModel.IsThreeMonthsSelected);
    }

    /// <summary>
    /// 更新時に履歴が保存され、履歴表示コレクションが構築されることをテスト。
    /// 期待値: 履歴件数が1件以上になる。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LoadsPriceHistoryCollections()
    {
        // Arrange
        var historyItem = new PriceHistoryEntry
        {
            Id = 1,
            Symbol = "7203.T",
            StockPrice = 210.50m,
            RecordedAt = DateTimeOffset.Now
        };
        var historyBar = new PriceHistoryBar
        {
            Label = "10:00",
            ValueText = "210.50",
            Height = 88
        };
        var viewModel = CreateViewModel(
            priceHistoryFeatureService: new FakePriceHistoryFeatureService(new PriceHistoryViewData([historyItem], [historyBar])));

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.NotEmpty(viewModel.PriceHistoryItems);
        Assert.NotEmpty(viewModel.StockPriceChartBars);
    }

    /// <summary>
    /// 初期状態で日本株コードが設定されることをテスト。
    /// 期待値: Symbol が 7203。
    /// </summary>
    [Fact]
    public void Constructor_SetsTokyoPrimeSymbol_AsDefaultInput()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal("7203", viewModel.Symbol);
    }

    /// <summary>
    /// 個別指標の切替で表示中シリーズが更新されることをテスト。
    /// 期待値: MA75 をオフにすると表示系列数が減る。
    /// </summary>
    [Fact]
    public async Task ChartIndicatorOption_CanBeToggledIndividually()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        var ma75 = Assert.Single(viewModel.JapaneseChartIndicatorOptions, item => item.IndicatorKey == "ma75");
        ma75.IsSelected = false;

        Assert.False(ma75.IsSelected);
        Assert.Equal(2, viewModel.VisibleJapaneseChartIndicators.Count);
        Assert.DoesNotContain(viewModel.VisibleJapaneseChartIndicators, item => item.IndicatorKey == "ma75");
    }

    private static MainViewModel CreateViewModel(
        IMarketSnapshotService? marketSnapshotService = null,
        IPriceHistoryFeatureService? priceHistoryFeatureService = null,
        IJapaneseStockChartFeatureService? japaneseStockChartFeatureService = null,
        IAppLogger? logger = null)
    {
        return new MainViewModel(
            marketSnapshotService ?? new FakeMarketSnapshotService(),
            priceHistoryFeatureService ?? new FakePriceHistoryFeatureService(),
            japaneseStockChartFeatureService ?? new FakeJapaneseStockChartFeatureService(),
            logger ?? new FakeLogger(),
            new FakeUiDispatcherTimer());
    }

    private sealed class FakeUiDispatcherTimer : IUiDispatcherTimer
    {
        public TimeSpan Interval { get; set; }

        public event EventHandler? Tick;

        public bool IsRunning { get; private set; }

        public void Start()
        {
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void RaiseTick()
        {
            Tick?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class FakeMarketSnapshotService : IMarketSnapshotService
    {
        private readonly MarketSnapshotModel _snapshot;

        public FakeMarketSnapshotService(MarketSnapshotModel? snapshot = null)
        {
            _snapshot = snapshot ?? new MarketSnapshotModel
            {
                Symbol = "7203.T",
                CompanyName = "トヨタ自動車",
                StockPrice = 200m,
                StockUpdatedAt = DateTimeOffset.Now
            };
        }

        public Task<MarketSnapshotModel> GetMarketSnapshotAsync(string symbol, CancellationToken cancellationToken)
        {
            return Task.FromResult(new MarketSnapshotModel
            {
                Symbol = _snapshot.Symbol,
                CompanyName = _snapshot.CompanyName,
                StockPrice = _snapshot.StockPrice,
                StockUpdatedAt = _snapshot.StockUpdatedAt
            });
        }
    }

    private sealed class ThrowingMarketSnapshotService : IMarketSnapshotService
    {
        public Task<MarketSnapshotModel> GetMarketSnapshotAsync(string symbol, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("テスト用例外");
        }
    }

    private sealed class FakePriceHistoryFeatureService : IPriceHistoryFeatureService
    {
        private readonly PriceHistoryViewData _viewData;

        public FakePriceHistoryFeatureService(PriceHistoryViewData? viewData = null)
        {
            _viewData = viewData ?? new PriceHistoryViewData(
            [
                new PriceHistoryEntry
                {
                    Id = 1,
                    Symbol = "7203.T",
                    StockPrice = 200m,
                    RecordedAt = DateTimeOffset.Now
                }
            ],
            [
                new PriceHistoryBar
                {
                    Label = "09:00",
                    ValueText = "200.00",
                    Height = 64
                }
            ]);
        }

        public Task<PriceHistoryViewData> RecordAndLoadAsync(MarketSnapshotModel snapshot, int limit, CancellationToken cancellationToken)
        {
            return Task.FromResult(_viewData);
        }
    }

    private sealed class FakeJapaneseStockChartFeatureService : IJapaneseStockChartFeatureService
    {
        private readonly JapaneseStockChartViewData _viewData;
        private TaskCompletionSource? _nextCallCompletionSource;

        public FakeJapaneseStockChartFeatureService(JapaneseStockChartViewData? viewData = null)
        {
            _viewData = viewData ?? new JapaneseStockChartViewData(
                true,
                [
                    new CandlestickRenderItem
                    {
                        Label = "04/01",
                        WickTop = 12,
                        WickHeight = 40,
                        BodyTop = 18,
                        BodyHeight = 16,
                        BodyColor = "#00AA00",
                        WickColor = "#00AA00"
                    }
                ],
                [
                    new ChartIndicatorDefinition("ma5", "MA5", ChartIndicatorPlacement.OverlayPriceChart, "#F59E0B", true, 10),
                    new ChartIndicatorDefinition("ma25", "MA25", ChartIndicatorPlacement.OverlayPriceChart, "#10B981", true, 20),
                    new ChartIndicatorDefinition("ma75", "MA75", ChartIndicatorPlacement.OverlayPriceChart, "#334155", true, 30)
                ],
                [
                    new ChartIndicatorRenderSeries
                    {
                        IndicatorKey = "ma5",
                        IndicatorDisplayName = "MA5",
                        LegendLabel = "MA5",
                        Placement = ChartIndicatorPlacement.OverlayPriceChart,
                        DisplayOrder = 10,
                        Points = "17,120 51,100",
                        StrokeColor = "#F59E0B",
                        StrokeThickness = 1.8d
                    },
                    new ChartIndicatorRenderSeries
                    {
                        IndicatorKey = "ma25",
                        IndicatorDisplayName = "MA25",
                        LegendLabel = "MA25",
                        Placement = ChartIndicatorPlacement.OverlayPriceChart,
                        DisplayOrder = 20,
                        Points = "17,130 51,110",
                        StrokeColor = "#10B981",
                        StrokeThickness = 2.1d
                    },
                    new ChartIndicatorRenderSeries
                    {
                        IndicatorKey = "ma75",
                        IndicatorDisplayName = "MA75",
                        LegendLabel = "MA75",
                        Placement = ChartIndicatorPlacement.OverlayPriceChart,
                        DisplayOrder = 30,
                        Points = "17,140 51,130",
                        StrokeColor = "#334155",
                        StrokeThickness = 2.7d,
                        StrokeDashArray = "8 4"
                    }
                ],
                190m,
                210m,
                320d);
        }

        public string LastSymbol { get; private set; } = string.Empty;

        public CandleTimeframe LastTimeframe { get; private set; } = CandleTimeframe.Daily;

        public CandleDisplayPeriod LastDisplayPeriod { get; private set; } = CandleDisplayPeriod.OneMonth;

        public void PrepareNextCall()
        {
            _nextCallCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task WaitForNextCallAsync()
        {
            if (_nextCallCompletionSource is null)
            {
                throw new InvalidOperationException("PrepareNextCall を先に呼び出してください。");
            }

            return _nextCallCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(1));
        }

        public Task<JapaneseStockChartViewData> LoadAsync(
            string symbol,
            CandleTimeframe timeframe,
            CandleDisplayPeriod displayPeriod,
            int fetchLimit,
            CancellationToken cancellationToken)
        {
            LastSymbol = symbol;
            LastTimeframe = timeframe;
            LastDisplayPeriod = displayPeriod;
            _nextCallCompletionSource?.TrySetResult();
            return Task.FromResult(_viewData);
        }
    }

    private sealed class FakeLogger : IAppLogger
    {
        public void Info(string message)
        {
        }

        public void LogError(Exception exception, string message)
        {
        }
    }
}
