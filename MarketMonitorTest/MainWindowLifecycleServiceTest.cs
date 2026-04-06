using MarketMonitor.Composition;
using MarketMonitor.Features.Dashboard.Models;
using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitorTest;

/// <summary>
/// MainWindowLifecycleService の初期化委譲を検証するテストクラス。
/// </summary>
public sealed class MainWindowLifecycleServiceTest
{
    /// <summary>
    /// InitializeAsync が ViewModel の初期化処理を呼び出すことをテスト。
    /// 期待値: InitializeAsync が 1 回呼ばれる。
    /// </summary>
    [Fact]
    public async Task InitializeAsync_DelegatesToViewModel()
    {
        var viewModel = new FakeMainWindowViewModel();

        await MainWindowLifecycleService.InitializeAsync(viewModel);

        Assert.True(viewModel.InitializeCalled);
    }

    private sealed class FakeMainWindowViewModel : IMainWindowViewModel
    {
        public bool IsAnalysisLineDrawingEnabled => false;

        public bool InitializeCalled { get; private set; }

        public Task InitializeAsync()
        {
            InitializeCalled = true;
            return Task.CompletedTask;
        }

        public IReadOnlyList<ChartAnalysisLineTypeOption> GetManualAnalysisLineTypeOptions()
        {
            return Array.Empty<ChartAnalysisLineTypeOption>();
        }

        public void StartManualAnalysisLineDrawing(ChartAnalysisLineType lineType)
        {
        }

        public void CancelManualAnalysisLineDrawing()
        {
        }

        public void RegisterJapaneseChartClick(double chartX, double chartY)
        {
        }

        public bool BeginJapaneseChartPointerInteraction(double chartX, double chartY)
        {
            return false;
        }

        public bool UpdateJapaneseChartPointerInteraction(double chartX, double chartY)
        {
            return false;
        }

        public bool CompleteJapaneseChartPointerInteraction(double chartX, double chartY)
        {
            return false;
        }

        public IReadOnlyList<AutoAnalysisLineCandidate> GetAutoAnalysisLineCandidates()
        {
            return Array.Empty<AutoAnalysisLineCandidate>();
        }

        public void AppendSelectedAutoAnalysisLines(IReadOnlyList<Guid> selectedLineIds)
        {
        }
    }
}