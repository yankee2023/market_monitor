using System.Windows;
using MarketMonitor.Services;
using MarketMonitor.ViewModels;

namespace MarketMonitor
{
    /// <summary>
    /// メイン画面を表示するウィンドウ。
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel(new ApiService(), new SerilogAppLogger());
            DataContext = _viewModel;

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }

        private void OnClosed(object? sender, System.EventArgs e)
        {
            _viewModel.Dispose();
        }
    }
}