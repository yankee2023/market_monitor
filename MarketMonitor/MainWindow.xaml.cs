using System.Windows;
using System.Windows.Input;
using MarketMonitor.Composition;
using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor
{
    /// <summary>
    /// メイン画面を表示するウィンドウ。
    /// </summary>
    public partial class MainWindow : Window, IMainWindowShell
    {
        private readonly IMainWindowViewModel _viewModel;
        private bool _isChartPointerCaptured;

        /// <summary>
        /// ViewModelを受け取って画面を初期化する。
        /// </summary>
        /// <param name="viewModel">画面にバインドするViewModel。</param>
        internal MainWindow(IMainWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            WindowStartupPlacementService.Apply(this, SystemParameters.WorkArea);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await MainWindowLifecycleService.InitializeAsync(_viewModel);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (_viewModel is IDisposable disposable)
            {
                SourceInitialized -= OnSourceInitialized;
                disposable.Dispose();
            }
        }

        private void OnChartPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!TryGetChartPosition(e, out var chartX, out var chartY))
            {
                return;
            }

            if (!_viewModel.BeginJapaneseChartPointerInteraction(chartX, chartY))
            {
                return;
            }

            MainCandlestickPlotViewport.CaptureMouse();
            _isChartPointerCaptured = true;
            e.Handled = true;
        }

        private void OnChartPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isChartPointerCaptured || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (!TryGetChartPosition(e, out var chartX, out var chartY))
            {
                return;
            }

            if (_viewModel.UpdateJapaneseChartPointerInteraction(chartX, chartY))
            {
                e.Handled = true;
            }
        }

        private void OnChartPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isChartPointerCaptured)
            {
                return;
            }

            if (!TryGetChartPosition(e, out var chartX, out var chartY))
            {
                chartX = 0d;
                chartY = 0d;
            }

            if (_viewModel.CompleteJapaneseChartPointerInteraction(chartX, chartY))
            {
                e.Handled = true;
            }

            MainCandlestickPlotViewport.ReleaseMouseCapture();
            _isChartPointerCaptured = false;
        }

        private void OnAppendAutoAnalysisLinesClick(object sender, RoutedEventArgs e)
        {
            var candidates = _viewModel.GetAutoAnalysisLineCandidates();
            if (candidates.Count <= 0)
            {
                System.Windows.MessageBox.Show(
                    this,
                    "追加できる自動分析ラインがありません。",
                    "分析ライン追加",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            var dialog = new AutoAnalysisLineSelectionWindow(candidates)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            _viewModel.AppendSelectedAutoAnalysisLines(dialog.GetSelectedLineIds());
        }

        private void OnToggleManualAnalysisLineDrawingClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsAnalysisLineDrawingEnabled)
            {
                _viewModel.CancelManualAnalysisLineDrawing();
                return;
            }

            var options = _viewModel.GetManualAnalysisLineTypeOptions();
            if (options.Count <= 0)
            {
                System.Windows.MessageBox.Show(
                    this,
                    "手動描画に利用できる分析ライン種別がありません。",
                    "分析ライン描画",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            var dialog = new ManualAnalysisLineTypeSelectionWindow(options)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var selectedLineType = dialog.GetSelectedLineType();
            if (!selectedLineType.HasValue)
            {
                return;
            }

            _viewModel.StartManualAnalysisLineDrawing(selectedLineType.Value);
        }

        private bool TryGetChartPosition(System.Windows.Input.MouseEventArgs e, out double chartX, out double chartY)
        {
            chartX = 0d;
            chartY = 0d;

            if (MainCandlestickPlotViewport.ActualHeight <= 0d)
            {
                return false;
            }

            var position = e.GetPosition(MainCandlestickPlotViewport);
            chartX = MainChartScrollViewer.HorizontalOffset + position.X;
            chartY = position.Y * (MainCandlestickScaledGrid.Height / MainCandlestickPlotViewport.ActualHeight);
            return true;
        }
    }
}