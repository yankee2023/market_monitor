using System.Windows;
using Serilog;

namespace MarketMonitor
{
    /// <summary>
    /// アプリケーション全体の初期化を管理するクラス。
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 起動時にロガーを初期化する。
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("MarketMonitor WPFを起動しました。");
            base.OnStartup(e);
        }

        /// <summary>
        /// 終了時にロガーを破棄する。
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("MarketMonitor WPFを終了します。");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }

}
