using Serilog;

/// <summary>
/// MarketMonitorのPoCコンソールアプリケーションのエントリーポイント。
/// </summary>
class Program
{
    /// <summary>
    /// メインエントリーポイント。APIからデータを取得してログに出力します。
    /// </summary>
    /// <param name="args">コマンドライン引数（未使用）。</param>
    static async Task Main(string[] args)
    {
        // Serilog設定: ログレベルINFOとERRORのみ、ファイル出力
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("MarketMonitor PoC - データ取得テスト開始");

        // APIキー取得（環境変数から、なければデモ）
        string apiKey = Environment.GetEnvironmentVariable("ALPHA_VANTAGE_API_KEY") ?? "demo";

        var client = new HttpClient();
        var apiService = new ApiService(client, apiKey);

        try
        {
            // 為替レート取得 (USD to JPY)
            await apiService.GetExchangeRate();

            // 株価取得 (米国株例: IBM)
            await apiService.GetStockPrice("IBM");

            // 株価取得 (日本株例: ソフトバンク)
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
