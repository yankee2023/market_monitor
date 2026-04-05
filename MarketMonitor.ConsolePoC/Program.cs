using System.Globalization;
using Serilog;

namespace MarketMonitor.ConsolePoC;

/// <summary>
/// MarketMonitor の PoC コンソールアプリケーションのエントリーポイント。
/// </summary>
internal static class Program
{
    /// <summary>
    /// メインエントリーポイント。日本株データを取得してログに出力します。
    /// </summary>
    /// <param name="args">コマンドライン引数。</param>
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        Log.Information("MarketMonitor PoC - 日本株データ取得テスト開始");

        using var client = new HttpClient();
        var apiService = new ApiService(client);

        try
        {
            await apiService.GetStockPrice("7203");
            await apiService.GetStockPrice("9984.T");

            Log.Information("テスト完了");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "テスト中にエラーが発生しました");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}