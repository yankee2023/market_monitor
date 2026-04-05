using System.Net.Http;
using System.Text.Json;
using Serilog;

/// <summary>
/// Alpha Vantage APIとの通信を担当するサービスクラス。
/// </summary>
public class ApiService
{
    private readonly HttpClient _client;
    private readonly string _apiKey;

    /// <summary>
    /// ApiServiceのコンストラクタ。
    /// </summary>
    /// <param name="client">HTTPクライアント。</param>
    /// <param name="apiKey">Alpha Vantage APIキー。</param>
    public ApiService(HttpClient client, string apiKey)
    {
        _client = client;
        _apiKey = apiKey;
    }

    /// <summary>
    /// USDからJPYへの為替レートを取得してログに出力します。
    /// </summary>
    public async Task GetExchangeRate()
    {
        string url = $"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency=USD&to_currency=JPY&apikey={_apiKey}";
        await FetchAndLogData(url, "為替レート", ParseExchangeRate);
    }

    /// <summary>
    /// 指定された銘柄の株価を取得してログに出力します。
    /// </summary>
    /// <param name="symbol">株価を取得する銘柄シンボル（例: IBM, 9984.T）。</param>
    public async Task GetStockPrice(string symbol)
    {
        string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={symbol}&interval=5min&apikey={_apiKey}";
        await FetchAndLogData(url, $"株価 ({symbol})", response => ParseStockPrice(response, symbol));
    }

    /// <summary>
    /// APIからデータを取得し、パース関数で処理してログに出力します。
    /// </summary>
    /// <param name="url">APIのURL。</param>
    /// <param name="dataType">データの種類（ログ用）。</param>
    /// <param name="parseFunc">レスポンスをパースする関数。</param>
    private async Task FetchAndLogData(string url, string dataType, Func<string, bool> parseFunc)
    {
        try
        {
            string response = await _client.GetStringAsync(url);
            if (!parseFunc(response))
            {
                string responseSnippet = response.Length < 500 ? response : response.Substring(0, 500);
                Log.Error("{DataType}の取得に失敗しました。レスポンス: {Response}", dataType, responseSnippet);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{DataType}取得中にエラーが発生しました", dataType);
        }
    }

    /// <summary>
    /// 為替レートのレスポンスをパースしてログに出力します。
    /// </summary>
    /// <param name="response">APIレスポンス。</param>
    /// <returns>パース成功の場合true。</returns>
    private bool ParseExchangeRate(string response)
    {
        var json = JsonDocument.Parse(response);
        var root = json.RootElement;

        if (root.TryGetProperty("Realtime Currency Exchange Rate", out var exchangeRate))
        {
            string fromCurrency = exchangeRate.GetProperty("1. From_Currency Code").GetString() ?? "Unknown";
            string toCurrency = exchangeRate.GetProperty("3. To_Currency Code").GetString() ?? "Unknown";
            string rate = exchangeRate.GetProperty("5. Exchange Rate").GetString() ?? "0";

            Log.Information("為替レート: {From} to {To} = {Rate}", fromCurrency, toCurrency, rate);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 株価のレスポンスをパースしてログに出力します。
    /// </summary>
    /// <param name="response">APIレスポンス。</param>
    /// <param name="symbol">銘柄シンボル。</param>
    /// <returns>パース成功の場合true。</returns>
    private bool ParseStockPrice(string response, string symbol)
    {
        var json = JsonDocument.Parse(response);
        var root = json.RootElement;

        if (root.TryGetProperty("Time Series (5min)", out var timeSeries))
        {
            var latestEntry = timeSeries.EnumerateObject().First();
            var data = latestEntry.Value;

            string closePrice = data.GetProperty("4. close").GetString() ?? "0";
            string unit = IsJapaneseStock(symbol) ? "円" : "ドル";
            Log.Information("株価 ({Symbol}): 最新終値 = {Price} {Unit}", symbol, closePrice, unit);
            return true;
        }
        return false;
    }

    /// <summary>
    /// シンボルが日本株かどうかを判定します。
    /// </summary>
    /// <param name="symbol">銘柄シンボル。</param>
    /// <returns>日本株の場合true。</returns>
    private static bool IsJapaneseStock(string symbol)
    {
        return symbol.EndsWith(".T");
    }
}