using System.Globalization;
using MarketMonitor.Features.Dashboard.Models;
using MarketMonitor.Features.Dashboard.ViewModels;
using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;
using MarketMonitor.Features.MarketSnapshot.Services;
using MarketMonitor.Features.PriceHistory.Models;
using MarketMonitor.Features.PriceHistory.Services;
using MarketMonitor.Features.SectorComparison.Models;
using MarketMonitor.Features.SectorComparison.Services;
using MarketMonitor.Shared.Infrastructure;
using MarketMonitor.Shared.Logging;
using MarketSnapshotModel = MarketMonitor.Features.MarketSnapshot.Models.MarketSnapshot;

namespace MarketMonitorTest;

/// <summary>
/// MainViewModel の画面状態管理を検証するテストクラス。
/// </summary>
public class MainViewModelTest
{
    /// <summary>
    /// 初期化時に取得した値が表示用プロパティへ反映されることをテスト。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_ReflectsSnapshotValues()
    {
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
            WickColor = "#00AA00",
            VolumeText = "1,200,000"
        };
        var marketSnapshotService = new FakeMarketSnapshotService(snapshot);
        var priceHistoryFeatureService = new FakePriceHistoryFeatureService(new PriceHistoryViewData([historyItem], [historyBar]));
        var japaneseStockChartFeatureService = new FakeJapaneseStockChartFeatureService(
            new JapaneseStockChartViewData(
                true,
                [candle],
                CreateIndicatorDefinitions(),
                CreateOverlaySeries(),
                Array.Empty<ChartAnalysisLine>(),
                CreateIndicatorPanels(),
                205m,
                220m,
                320d));
        var viewModel = new MainViewModel(
            marketSnapshotService,
            priceHistoryFeatureService,
            japaneseStockChartFeatureService,
            new FakeSectorComparisonFeatureService(),
            new FakeLogger(),
            new FakeDesktopNotificationService(),
            chartAnalysisLineRepository: new FakeChartAnalysisLineRepository());

        await viewModel.InitializeAsync();

        Assert.Equal("210.50", viewModel.StockPriceDisplay);
        Assert.Equal(
            "株価取得: " + snapshot.StockUpdatedAt.LocalDateTime.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture),
            viewModel.StockUpdatedAtDisplay);
        Assert.Equal("銘柄: トヨタ自動車 (7203.T)", viewModel.CompanyDisplay);
        Assert.Equal("205.00", viewModel.JapaneseCandlestickMinPriceLabel);
        Assert.Equal("212.50", viewModel.JapaneseCandlestickMidPriceLabel);
        Assert.Equal("220.00", viewModel.JapaneseCandlestickMaxPriceLabel);
        Assert.Equal(320d, viewModel.JapaneseCandlestickCanvasWidth);
        Assert.StartsWith("表示完了:", viewModel.StatusMessage);
        Assert.Single(viewModel.PriceHistoryItems);
        Assert.Single(viewModel.StockPriceChartBars);
        Assert.Single(viewModel.JapaneseCandlesticks);
        Assert.Equal(6, viewModel.JapaneseChartIndicatorOptions.Count);
        Assert.Equal(3, viewModel.VisibleJapaneseOverlayIndicators.Count);
        Assert.Equal(3, viewModel.VisibleJapaneseIndicatorPanels.Count);
        Assert.Equal("業種: 輸送用機器", viewModel.SectorDisplay);
        Assert.Equal("市場: プライム", viewModel.MarketSegmentDisplay);
        Assert.NotEmpty(viewModel.SectorComparisonItems);
        Assert.Equal("プライム", viewModel.SectorComparisonItems[0].MarketSegmentDisplay);
    }

    /// <summary>
    /// 現在値取得失敗時に失敗メッセージが設定されることをテスト。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SetsFailureMessage_WhenSnapshotLoadThrows()
    {
        var viewModel = new MainViewModel(
            new ThrowingMarketSnapshotService(),
            new FakePriceHistoryFeatureService(),
            new FakeJapaneseStockChartFeatureService(),
            new FakeSectorComparisonFeatureService(),
            new FakeLogger(),
            new FakeDesktopNotificationService(),
            chartAnalysisLineRepository: new FakeChartAnalysisLineRepository());

        await viewModel.InitializeAsync();

        Assert.StartsWith("表示失敗:", viewModel.StatusMessage);
    }

    /// <summary>
    /// 日足切替コマンド実行時にチャート再読込が走ることをテスト。
    /// </summary>
    [Fact]
    public async Task ShowDailyCandlesCommand_ReloadsChart()
    {
        var chartService = new FakeJapaneseStockChartFeatureService();
        var viewModel = CreateViewModel(japaneseStockChartFeatureService: chartService);
        await viewModel.InitializeAsync();
        chartService.PrepareNextCall();

        viewModel.ShowDailyCandlesCommand.Execute(null);
        await chartService.WaitForNextCallAsync();

        Assert.Equal(CandleTimeframe.Daily, chartService.LastTimeframe);
        Assert.Equal("7203.T", chartService.LastSymbol);
        Assert.True(viewModel.IsDailyCandleSelected);
    }

    /// <summary>
    /// 表示期間切替コマンド実行時にチャート再読込が走ることをテスト。
    /// </summary>
    [Fact]
    public async Task ShowThreeMonthsCandlesCommand_ReloadsChart()
    {
        var chartService = new FakeJapaneseStockChartFeatureService();
        var viewModel = CreateViewModel(japaneseStockChartFeatureService: chartService);
        await viewModel.InitializeAsync();
        chartService.PrepareNextCall();

        viewModel.ShowThreeMonthsCandlesCommand.Execute(null);
        await chartService.WaitForNextCallAsync();

        Assert.Equal(CandleDisplayPeriod.ThreeMonths, chartService.LastDisplayPeriod);
        Assert.True(viewModel.IsThreeMonthsSelected);
    }

    /// <summary>
    /// 更新時に履歴が保存され、履歴表示コレクションが構築されることをテスト。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LoadsPriceHistoryCollections()
    {
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

        await viewModel.InitializeAsync();

        Assert.NotEmpty(viewModel.PriceHistoryItems);
        Assert.NotEmpty(viewModel.StockPriceChartBars);
    }

    /// <summary>
    /// 初期状態で日本株コードが設定されることをテスト。
    /// </summary>
    [Fact]
    public void Constructor_SetsTokyoListedSymbol_AsDefaultInput()
    {
        var viewModel = CreateViewModel();

        Assert.Equal("7203", viewModel.Symbol);
        Assert.False(viewModel.IsSidebarCollapsed);
        Assert.Equal("詳細情報を隠す", viewModel.SidebarToggleText);
    }

    /// <summary>
    /// 補助ペイン開閉コマンドで折りたたみ状態が切り替わることをテスト。
    /// </summary>
    [Fact]
    public void ToggleSidebarCommand_TogglesCollapsedState()
    {
        var viewModel = CreateViewModel();

        viewModel.ToggleSidebarCommand.Execute(null);

        Assert.True(viewModel.IsSidebarCollapsed);
        Assert.False(viewModel.IsSidebarVisible);
        Assert.Equal("詳細情報を表示", viewModel.SidebarToggleText);

        viewModel.ToggleSidebarCommand.Execute(null);

        Assert.False(viewModel.IsSidebarCollapsed);
        Assert.True(viewModel.IsSidebarVisible);
        Assert.Equal("詳細情報を隠す", viewModel.SidebarToggleText);
    }

    /// <summary>
    /// 個別指標の切替で表示中シリーズが更新されることをテスト。
    /// </summary>
    [Fact]
    public async Task ChartIndicatorOption_CanBeToggledIndividually()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        var ma75 = Assert.Single(viewModel.JapaneseChartIndicatorOptions, item => item.IndicatorKey == "ma75");
        ma75.IsSelected = false;

        Assert.False(ma75.IsSelected);
        Assert.Equal(2, viewModel.VisibleJapaneseOverlayIndicators.Count);
        Assert.DoesNotContain(viewModel.VisibleJapaneseOverlayIndicators, item => item.IndicatorKey == "ma75");
    }

    /// <summary>
    /// MACD トグルが MACD パネル全体を制御することをテスト。
    /// </summary>
    [Fact]
    public async Task MacdIndicatorOption_HidesMacdAndSignalTogether()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        var macd = Assert.Single(viewModel.JapaneseChartIndicatorOptions, item => item.IndicatorKey == "macd");
        macd.IsSelected = false;

        Assert.DoesNotContain(viewModel.VisibleJapaneseIndicatorPanels, item => item.PanelKey == "macd");
        Assert.Equal(2, viewModel.VisibleJapaneseIndicatorPanels.Count);
    }

    /// <summary>
    /// 分析ライン描画モードで 2 回クリックすると分析ラインが追加されることをテスト。
    /// </summary>
    [Fact]
    public async Task RegisterJapaneseChartClick_AddsAnalysisLine_AfterSecondPoint()
    {
        var repository = new FakeChartAnalysisLineRepository();
        var viewModel = CreateViewModel(chartAnalysisLineRepository: repository);
        await viewModel.InitializeAsync();

        viewModel.StartManualAnalysisLineDrawing(ChartAnalysisLineType.SupportLine);
        viewModel.RegisterJapaneseChartClick(32d, 52d);
        Assert.True(viewModel.HasPendingJapaneseAnalysisPoint);
        viewModel.RegisterJapaneseChartClick(192d, 156d);

        Assert.False(viewModel.IsAnalysisLineDrawingEnabled);
        Assert.False(viewModel.HasPendingJapaneseAnalysisPoint);
        Assert.True(viewModel.HasJapaneseAnalysisLines);
        var line = Assert.Single(viewModel.JapaneseAnalysisLines);
        Assert.Equal("分析ライン 1 本を表示中です。選択中: 支持線。ドラッグで位置調整できます。", viewModel.AnalysisLineStatusText);
        Assert.Equal(32d, line.X1, 3);
        Assert.Equal(52d, line.Y1, 3);
        Assert.Equal(192d, line.X2, 3);
        Assert.Equal(156d, line.Y2, 3);
        Assert.Equal("8 4", line.StrokeDashArray);
        var persisted = await repository.GetAsync("7203.T", CandleTimeframe.Daily, CandleDisplayPeriod.OneMonth, CancellationToken.None);
        Assert.Single(persisted);
        Assert.Equal(ChartAnalysisLineType.SupportLine, persisted[0].LineType);
    }

    /// <summary>
    /// 分析ライン選択後にドラッグすると位置を移動できることをテスト。
    /// </summary>
    [Fact]
    public async Task PointerInteraction_MovesSelectedAnalysisLine()
    {
        var viewModel = CreateViewModel(chartAnalysisLineRepository: new FakeChartAnalysisLineRepository());
        await viewModel.InitializeAsync();
        viewModel.StartManualAnalysisLineDrawing(ChartAnalysisLineType.TrendLine);
        viewModel.RegisterJapaneseChartClick(32d, 52d);
        viewModel.RegisterJapaneseChartClick(192d, 156d);

        var beginHandled = viewModel.BeginJapaneseChartPointerInteraction(100d, 104d);
        var updateHandled = viewModel.UpdateJapaneseChartPointerInteraction(132d, 130d);
        var completeHandled = viewModel.CompleteJapaneseChartPointerInteraction(132d, 130d);

        Assert.True(beginHandled);
        Assert.True(updateHandled);
        Assert.True(completeHandled);
        var moved = Assert.Single(viewModel.JapaneseAnalysisLines);
        Assert.Equal(64d, moved.X1, 3);
        Assert.Equal(78d, moved.Y1, 3);
        Assert.Equal(224d, moved.X2, 3);
        Assert.Equal(182d, moved.Y2, 3);
        Assert.True(viewModel.HasSelectedJapaneseAnalysisLine);
    }

    /// <summary>
    /// 初期化時に保存済みの分析ラインが読み込まれることをテスト。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LoadsPersistedAnalysisLines()
    {
        var repository = new FakeChartAnalysisLineRepository();
        await repository.SaveAsync(
            "7203.T",
            CandleTimeframe.Daily,
            CandleDisplayPeriod.OneMonth,
            [new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.ResistanceLine, 0.10d, 0.20d, 0.90d, 0.20d)],
            CancellationToken.None);
        var viewModel = CreateViewModel(chartAnalysisLineRepository: repository);

        await viewModel.InitializeAsync();

        var line = Assert.Single(viewModel.JapaneseAnalysisLines);
        Assert.Equal("10 4 2 4", line.StrokeDashArray);
        Assert.True(viewModel.HasJapaneseAnalysisLines);
    }

    /// <summary>
    /// 保存済みラインがない場合は自動生成ラインが採用されることをテスト。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_UsesSuggestedAnalysisLines_WhenNoPersistedLinesExist()
    {
        var suggestedLines = new[]
        {
            new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.TrendLine, 0d, 0.20d, 1d, 0.45d),
            new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.SupportLine, 0d, 0.80d, 1d, 0.80d),
            new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.ResistanceLine, 0d, 0.18d, 1d, 0.18d)
        };
        var repository = new FakeChartAnalysisLineRepository();
        var viewModel = CreateViewModel(
            japaneseStockChartFeatureService: new FakeJapaneseStockChartFeatureService(CreateChartViewData(suggestedLines)),
            chartAnalysisLineRepository: repository);

        await viewModel.InitializeAsync();

        Assert.Equal(3, viewModel.JapaneseAnalysisLines.Count);
        var persisted = await repository.GetAsync("7203.T", CandleTimeframe.Daily, CandleDisplayPeriod.OneMonth, CancellationToken.None);
        Assert.Equal(3, persisted.Count);
    }

    /// <summary>
    /// 自動生成候補選択で既存ラインを残したまま選択した線だけ追加できることをテスト。
    /// </summary>
    [Fact]
    public async Task AppendSelectedAutoAnalysisLines_AppendsSelectedLines_WithoutRemovingExistingLine()
    {
        var suggestedLines = new[]
        {
            new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.TrendLine, 0d, 0.20d, 1d, 0.40d),
            new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.SupportLine, 0d, 0.75d, 1d, 0.75d),
            new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.ResistanceLine, 0d, 0.18d, 1d, 0.18d)
        };
        var repository = new FakeChartAnalysisLineRepository();
        await repository.SaveAsync(
            "7203.T",
            CandleTimeframe.Daily,
            CandleDisplayPeriod.OneMonth,
            [new ChartAnalysisLine(Guid.NewGuid(), ChartAnalysisLineType.TrendLine, 0.10d, 0.20d, 0.80d, 0.45d)],
            CancellationToken.None);
        var viewModel = CreateViewModel(
            japaneseStockChartFeatureService: new FakeJapaneseStockChartFeatureService(CreateChartViewData(suggestedLines)),
            chartAnalysisLineRepository: repository);

        await viewModel.InitializeAsync();
        var selectedIds = suggestedLines
            .Where(item => item.LineType != ChartAnalysisLineType.TrendLine)
            .Select(item => item.Id)
            .ToArray();

        viewModel.AppendSelectedAutoAnalysisLines(selectedIds);

        Assert.Equal(3, viewModel.JapaneseAnalysisLines.Count);
        Assert.Equal("分析ラインを 2 本追加しました。", viewModel.StatusMessage);
        var persisted = await repository.GetAsync("7203.T", CandleTimeframe.Daily, CandleDisplayPeriod.OneMonth, CancellationToken.None);
        Assert.Equal(3, persisted.Count);
        Assert.Equal(2, persisted.Count(item => item.LineType != ChartAnalysisLineType.TrendLine));
    }

    /// <summary>
    /// 銘柄切替時に既存の分析ラインがクリアされることをテスト。
    /// </summary>
    [Fact]
    public async Task ApplySymbolCommand_ClearsAnalysisLines_WhenSymbolChanges()
    {
        var snapshotService = new SequencedMarketSnapshotService(
            new MarketSnapshotModel
            {
                Symbol = "7203.T",
                CompanyName = "トヨタ自動車",
                StockPrice = 200m,
                StockUpdatedAt = DateTimeOffset.Now
            },
            new MarketSnapshotModel
            {
                Symbol = "6758.T",
                CompanyName = "ソニーグループ",
                StockPrice = 300m,
                StockUpdatedAt = DateTimeOffset.Now.AddMinutes(1)
            });
        var viewModel = CreateViewModel(marketSnapshotService: snapshotService);
        await viewModel.InitializeAsync();
        viewModel.StartManualAnalysisLineDrawing(ChartAnalysisLineType.TrendLine);
        viewModel.RegisterJapaneseChartClick(32d, 80d);
        viewModel.RegisterJapaneseChartClick(210d, 160d);

        viewModel.Symbol = "6758";
        viewModel.ApplySymbolCommand.Execute(null);
        await snapshotService.WaitForCallCountAsync(2);

        Assert.False(viewModel.HasJapaneseAnalysisLines);
        Assert.Empty(viewModel.JapaneseAnalysisLines);
    }

    private static MainViewModel CreateViewModel(
        IMarketSnapshotService? marketSnapshotService = null,
        IPriceHistoryFeatureService? priceHistoryFeatureService = null,
        IJapaneseStockChartFeatureService? japaneseStockChartFeatureService = null,
        IAppLogger? logger = null,
        IChartAnalysisLineRepository? chartAnalysisLineRepository = null,
        IChartAnalysisLineService? chartAnalysisLineService = null)
    {
        return new MainViewModel(
            marketSnapshotService ?? new FakeMarketSnapshotService(),
            priceHistoryFeatureService ?? new FakePriceHistoryFeatureService(),
            japaneseStockChartFeatureService ?? new FakeJapaneseStockChartFeatureService(),
            new FakeSectorComparisonFeatureService(),
            logger ?? new FakeLogger(),
            new FakeDesktopNotificationService(),
            chartAnalysisLineRepository: chartAnalysisLineRepository ?? new FakeChartAnalysisLineRepository(),
            chartAnalysisLineService: chartAnalysisLineService);
    }

    private static IReadOnlyList<ChartIndicatorDefinition> CreateIndicatorDefinitions()
    {
        return
        [
            new ChartIndicatorDefinition("ma5", "MA5", ChartIndicatorPlacement.OverlayPriceChart, "#F59E0B", true, 10),
            new ChartIndicatorDefinition("ma25", "MA25", ChartIndicatorPlacement.OverlayPriceChart, "#10B981", true, 20),
            new ChartIndicatorDefinition("ma75", "MA75", ChartIndicatorPlacement.OverlayPriceChart, "#0EA5E9", true, 30),
            new ChartIndicatorDefinition("volume", "出来高", ChartIndicatorPlacement.SecondaryPanel, "#64748B", true, 40),
            new ChartIndicatorDefinition("macd", "MACD", ChartIndicatorPlacement.SecondaryPanel, "#B91C1C", true, 50),
            new ChartIndicatorDefinition("rsi", "RSI", ChartIndicatorPlacement.SecondaryPanel, "#7C3AED", true, 60)
        ];
    }

    private static IReadOnlyList<ChartIndicatorRenderSeries> CreateOverlaySeries()
    {
        return
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
                StrokeColor = "#0EA5E9",
                StrokeThickness = 2.7d,
                StrokeDashArray = string.Empty
            }
        ];
    }

    private static JapaneseStockChartViewData CreateChartViewData(IReadOnlyList<ChartAnalysisLine>? suggestedAnalysisLines = null)
    {
        return new JapaneseStockChartViewData(
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
                    WickColor = "#00AA00",
                    VolumeText = "1,200,000"
                }
            ],
            CreateIndicatorDefinitions(),
            CreateOverlaySeries(),
            suggestedAnalysisLines ?? Array.Empty<ChartAnalysisLine>(),
            CreateIndicatorPanels(),
            190m,
            210m,
            320d);
    }

    private static IReadOnlyList<IndicatorPanelRenderData> CreateIndicatorPanels()
    {
        return
        [
            new IndicatorPanelRenderData(
                "volume",
                "出来高",
                40,
                "N0",
                null,
                Array.Empty<ChartIndicatorRenderSeries>(),
                [
                    new ChartIndicatorBarItem { Left = 11d, Top = 32d, Width = 12d, Height = 64d, FillColor = "#D6282899" }
                ],
                Array.Empty<IndicatorReferenceLine>(),
                0m,
                1200000m),
            new IndicatorPanelRenderData(
                "macd",
                "MACD",
                50,
                "N2",
                48d,
                [
                    new ChartIndicatorRenderSeries
                    {
                        IndicatorKey = "macd",
                        IndicatorDisplayName = "MACD",
                        LegendLabel = "MACD",
                        Placement = ChartIndicatorPlacement.SecondaryPanel,
                        DisplayOrder = 50,
                        Points = "17,40 51,32",
                        StrokeColor = "#B91C1C",
                        StrokeThickness = 2.0d
                    },
                    new ChartIndicatorRenderSeries
                    {
                        IndicatorKey = "macd",
                        IndicatorDisplayName = "シグナル",
                        LegendLabel = "シグナル",
                        Placement = ChartIndicatorPlacement.SecondaryPanel,
                        DisplayOrder = 51,
                        Points = "17,42 51,34",
                        StrokeColor = "#1D4ED8",
                        StrokeThickness = 1.6d,
                        StrokeDashArray = "4 3"
                    }
                ],
                Array.Empty<ChartIndicatorBarItem>(),
                Array.Empty<IndicatorReferenceLine>(),
                -12m,
                18m),
            new IndicatorPanelRenderData(
                "rsi",
                "RSI",
                60,
                "N2",
                null,
                [
                    new ChartIndicatorRenderSeries
                    {
                        IndicatorKey = "rsi",
                        IndicatorDisplayName = "RSI",
                        LegendLabel = "RSI",
                        Placement = ChartIndicatorPlacement.SecondaryPanel,
                        DisplayOrder = 60,
                        Points = "17,48 51,44",
                        StrokeColor = "#7C3AED",
                        StrokeThickness = 1.8d
                    }
                ],
                Array.Empty<ChartIndicatorBarItem>(),
                [
                    new IndicatorReferenceLine { Label = "70", Top = 28.8d, StrokeColor = "#DC2626", StrokeDashArray = "4 3" },
                    new IndicatorReferenceLine { Label = "30", Top = 67.2d, StrokeColor = "#2563EB", StrokeDashArray = "4 3" }
                ],
                0m,
                100m)
        ];
    }

    private sealed class FakeDesktopNotificationService : IDesktopNotificationService
    {
        public void ShowNotification(string title, string message)
        {
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

    private sealed class SequencedMarketSnapshotService : IMarketSnapshotService
    {
        private readonly MarketSnapshotModel[] _snapshots;
        private int _callCount;

        public SequencedMarketSnapshotService(params MarketSnapshotModel[] snapshots)
        {
            _snapshots = snapshots;
        }

        public Task<MarketSnapshotModel> GetMarketSnapshotAsync(string symbol, CancellationToken cancellationToken)
        {
            var index = Math.Min(_callCount, _snapshots.Length - 1);
            _callCount++;
            return Task.FromResult(_snapshots[index]);
        }

        public async Task WaitForCallCountAsync(int expectedCount)
        {
            var start = DateTime.UtcNow;
            while (_callCount < expectedCount)
            {
                if (DateTime.UtcNow - start > TimeSpan.FromSeconds(1))
                {
                    throw new TimeoutException($"スナップショット取得回数が {expectedCount} 回に到達しませんでした。");
                }

                await Task.Yield();
            }
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
            _viewData = viewData ?? CreateChartViewData();
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

    private sealed class FakeChartAnalysisLineRepository : IChartAnalysisLineRepository
    {
        private readonly Dictionary<string, List<ChartAnalysisLine>> _store = new(StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<ChartAnalysisLine>> GetAsync(
            string symbol,
            CandleTimeframe timeframe,
            CandleDisplayPeriod displayPeriod,
            CancellationToken cancellationToken)
        {
            var key = CreateKey(symbol, timeframe, displayPeriod);
            return Task.FromResult<IReadOnlyList<ChartAnalysisLine>>(
                _store.TryGetValue(key, out var lines)
                    ? lines.Select(Clone).ToArray()
                    : Array.Empty<ChartAnalysisLine>());
        }

        public Task SaveAsync(
            string symbol,
            CandleTimeframe timeframe,
            CandleDisplayPeriod displayPeriod,
            IReadOnlyList<ChartAnalysisLine> lines,
            CancellationToken cancellationToken)
        {
            var key = CreateKey(symbol, timeframe, displayPeriod);
            _store[key] = lines.Select(Clone).ToList();
            return Task.CompletedTask;
        }

        private static string CreateKey(string symbol, CandleTimeframe timeframe, CandleDisplayPeriod displayPeriod)
        {
            return $"{symbol.ToUpperInvariant()}::{timeframe}::{displayPeriod}";
        }

        private static ChartAnalysisLine Clone(ChartAnalysisLine line)
        {
            return new ChartAnalysisLine(line.Id, line.LineType, line.StartXRatio, line.StartYRatio, line.EndXRatio, line.EndYRatio);
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

    private sealed class FakeSectorComparisonFeatureService : ISectorComparisonFeatureService
    {
        public Task<SectorComparisonViewData> LoadAsync(string symbol, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SectorComparisonViewData(
                "輸送用機器",
                "プライム",
                [
                    new SectorComparisonPeerItem
                    {
                        Symbol = "7267.T",
                        CompanyName = "本田技研工業",
                        StockPrice = 1580m,
                        StockPriceDisplay = "1,580.00",
                        MarketSegmentDisplay = "プライム"
                    }
                ]));
        }
    }
}