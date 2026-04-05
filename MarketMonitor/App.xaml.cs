using System.Windows;
using MarketMonitor.Composition;
using Serilog;

namespace MarketMonitor
{
    /// <summary>
    /// アプリケーション全体の初期化を管理するクラス。
    /// </summary>
    public partial class App : System.Windows.Application, IDisposable
    {
        private MainWindow? _mainWindow;

        /// <summary>
        /// 起動時にロガーとメインウィンドウを初期化する。
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            AppLoggingConfigurator.Configure();

            Log.Information("Tokyo Market Technical WPFを起動しました。");

            base.OnStartup(e);

            _mainWindow = AppLifecycleService.Start(AppBootstrapper.CreateMainWindow);
            MainWindow = _mainWindow;
        }

        /// <summary>
        /// 終了時にロガーを破棄する。
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            Dispose();
            Log.Information("Tokyo Market Technical WPFを終了します。");
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        /// <summary>
        /// 保持中のリソースを破棄する。
        /// </summary>
        public void Dispose()
        {
            AppLifecycleService.Stop(_mainWindow);
            _mainWindow = null;
            GC.SuppressFinalize(this);
        }
    }
}
