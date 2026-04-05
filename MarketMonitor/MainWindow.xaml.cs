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

        internal static Rect CalculateStartupBounds(Rect workArea, double desiredWidth, double desiredHeight)
        {
            var width = Math.Min(desiredWidth, workArea.Width);
            var height = Math.Min(desiredHeight, workArea.Height);
            var left = workArea.Left + Math.Max(0d, (workArea.Width - width) / 2d);
            var top = workArea.Top + Math.Max(0d, (workArea.Height - height) / 2d);

            return new Rect(left, top, width, height);
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            ApplyStartupBounds(SystemParameters.WorkArea);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await MainWindowLifecycleService.InitializeAsync(_viewModel);
        }

        private void ApplyStartupBounds(Rect workArea)
        {
            var bounds = CalculateStartupBounds(workArea, Width, Height);
            MaxWidth = workArea.Width;
            MaxHeight = workArea.Height;
            Width = bounds.Width;
            Height = bounds.Height;
            Left = bounds.Left;
            Top = bounds.Top;
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