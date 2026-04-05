using System.Windows;
using MarketMonitor.Composition;

namespace MarketMonitor
{
    /// <summary>
    /// メイン画面を表示するウィンドウ。
    /// </summary>
    public partial class MainWindow : Window, IMainWindowShell
    {
        private readonly IMainWindowViewModel _viewModel;

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
    }
}