using System.Globalization;
using System.Text;
using Serilog;

namespace MarketMonitor.Composition;

/// <summary>
/// アプリケーション全体のログ出力設定を管理する。
/// </summary>
internal static class AppLoggingConfigurator
{
    /// <summary>
    /// Serilogの初期設定を行う。
    /// </summary>
    public static void Configure()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                "logs/app-.log",
                rollingInterval: RollingInterval.Day,
                formatProvider: CultureInfo.InvariantCulture,
                encoding: Encoding.UTF8)
            .CreateLogger();
    }
}