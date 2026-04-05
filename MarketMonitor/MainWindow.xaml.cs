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

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await MainWindowLifecycleService.InitializeAsync(_viewModel);
        }
    }
}