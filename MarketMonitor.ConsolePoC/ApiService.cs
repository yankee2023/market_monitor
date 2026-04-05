using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Serilog;

namespace MarketMonitor.ConsolePoC;

/// <summary>
/// 日本株の現在値取得を検証するためのサービスクラス。
/// </summary>
public sealed class ApiService
{
    private readonly HttpClient _client;

    /// <summary>
    /// ApiService のコンストラクタ。
    /// </summary>
    /// <param name="client">HTTP クライアント。</param>
    public ApiService(HttpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// 指定された日本株の株価を取得してログに出力します。
    /// </summary>
    /// <param name="symbol">株価を取得する銘柄シンボルまたは 4 桁コード。</param>
    public async Task GetStockPrice(string symbol)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        if (!IsJapaneseStock(normalizedSymbol))
        {
            throw new InvalidOperationException("東証銘柄コードまたは .T 付きシンボルを指定してください。");
        }

        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(normalizedSymbol)}?range=5d&interval=1d&includePrePost=false&events=div%2Csplits";
        await FetchAndLogData(url, $"株価 ({normalizedSymbol})", response => ParseStockPrice(response, normalizedSymbol));
    }

    /// <summary>
    /// API からデータを取得し、パース関数で処理してログに出力します。
    /// </summary>
    private async Task FetchAndLogData(string url, string dataType, Func<string, bool> parseFunc)
    {
        try
        {
            var response = await _client.GetStringAsync(url);
            if (!parseFunc(response))
            {
                var responseSnippet = response.Length < 500 ? response : response[..500];
                Log.Error("{DataType}の取得に失敗しました。レスポンス: {Response}", dataType, responseSnippet);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{DataType}取得中にエラーが発生しました", dataType);
        }
    }

    /// <summary>
    /// 株価レスポンスをパースしてログに出力します。
    /// </summary>
    private static bool ParseStockPrice(string response, string symbol)
    {
        using var json = JsonDocument.Parse(response);
        var root = json.RootElement;

        if (!root.TryGetProperty("chart", out var chart)
            || !chart.TryGetProperty("result", out var resultArray)
            || resultArray.ValueKind != JsonValueKind.Array
            || resultArray.GetArrayLength() == 0)
        {
            return false;
        }

        var result = resultArray[0];
        if (result.TryGetProperty("meta", out var meta)
            && meta.TryGetProperty("regularMarketPrice", out var regularMarketPrice)
            && TryGetDecimal(regularMarketPrice, out var price))
        {
            Log.Information("株価 ({Symbol}): 最新値 = {Price} 円", symbol, price);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 入力を東証シンボルへ正規化します。
    /// </summary>
    private static string NormalizeSymbol(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var trimmed = symbol.Trim();
        if (trimmed.Length == 4 && trimmed.All(char.IsDigit))
        {
            return $"{trimmed}.T";
        }

        if (trimmed.EndsWith(".T", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.ToUpperInvariant();
        }

        return trimmed;
    }

    /// <summary>
    /// シンボルが日本株かどうかを判定します。
    /// </summary>
    private static bool IsJapaneseStock(string symbol)
    {
        return symbol.EndsWith(".T", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetDecimal(JsonElement element, out decimal value)
    {
        value = 0m;

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value),
            _ => false
        };
    }
}