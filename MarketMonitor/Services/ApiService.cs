using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using MarketMonitor.Models;

namespace MarketMonitor.Services;

/// <summary>
/// Alpha Vantage APIを利用してマーケットデータを取得するサービス。
/// </summary>
public sealed class ApiService : IApiService
{
    private static readonly HttpClient SharedClient = new();
    private static readonly IReadOnlyDictionary<string, string> SymbolAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ソフトバンク"] = "9984.T",
            ["ソフトバンクグループ"] = "9984.T",
            ["トヨタ"] = "7203.T",
            ["三菱重工"] = "7011.T",
            ["三菱UFJ"] = "8306.T",
            ["IBM"] = "IBM"
        };

    private readonly string _apiKey;

    public ApiService()
    {
        _apiKey = Environment.GetEnvironmentVariable("ALPHA_VANTAGE_API_KEY") ?? "demo";
    }

    /// <inheritdoc />
    public async Task<MarketSnapshot> GetMarketSnapshotAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalizedSymbol = NormalizeSymbolInput(symbol);

        var exchangeTask = GetExchangeRateAsync(cancellationToken);
        var stockTask = GetStockPriceAsync(normalizedSymbol, cancellationToken);

        await Task.WhenAll(exchangeTask, stockTask);

        return new MarketSnapshot
        {
            Symbol = normalizedSymbol,
            ExchangeRate = exchangeTask.Result,
            StockPrice = stockTask.Result,
            ExchangeUpdatedAt = DateTimeOffset.Now,
            StockUpdatedAt = DateTimeOffset.Now
        };
    }

    private static string NormalizeSymbolInput(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return "IBM";
        }

        var trimmed = symbol.Trim();
        if (SymbolAliases.TryGetValue(trimmed, out var alias))
        {
            return alias;
        }

        if (trimmed.EndsWith(".T", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.ToUpperInvariant();
        }

        // 4桁コード入力は東証シンボルとして .T を補完する。
        if (trimmed.Length == 4 && trimmed.All(char.IsDigit))
        {
            return $"{trimmed}.T";
        }

        return trimmed.ToUpperInvariant();
    }

    private async Task<decimal> GetExchangeRateAsync(CancellationToken cancellationToken)
    {
        var requestUri =
            $"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency=USD&to_currency=JPY&apikey={_apiKey}";

        var json = await SharedClient.GetStringAsync(requestUri, cancellationToken);
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("Realtime Currency Exchange Rate", out var rateNode))
        {
            throw new InvalidOperationException("為替データの形式が不正です。");
        }

        if (!rateNode.TryGetProperty("5. Exchange Rate", out var valueNode))
        {
            throw new InvalidOperationException("為替レートが見つかりませんでした。");
        }

        var valueText = valueNode.GetString();
        if (string.IsNullOrWhiteSpace(valueText))
        {
            throw new InvalidOperationException("為替レートが空です。");
        }

        if (!decimal.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidOperationException("為替レートの数値変換に失敗しました。");
        }

        return value;
    }

    private async Task<decimal> GetStockPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        var requestUri =
            $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={Uri.EscapeDataString(symbol)}&apikey={_apiKey}";

        var json = await SharedClient.GetStringAsync(requestUri, cancellationToken);
        if (TryParseAlphaVantageStockPrice(json, out var alphaVantagePrice, out var alphaVantageError))
        {
            return alphaVantagePrice;
        }

        // Alpha Vantageで日本株が取得できない場合はStooqで補完する。
        if (symbol.EndsWith(".T", StringComparison.OrdinalIgnoreCase))
        {
            return await GetStockPriceFromStooqAsync(symbol, cancellationToken);
        }

        throw new InvalidOperationException($"株価取得に失敗しました。{alphaVantageError}");
    }

    private static bool TryParseAlphaVantageStockPrice(string json, out decimal price, out string errorMessage)
    {
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.TryGetProperty("Global Quote", out var quoteNode)
            && quoteNode.TryGetProperty("05. price", out var valueNode))
        {
            var valueText = valueNode.GetString();
            if (!string.IsNullOrWhiteSpace(valueText)
                && decimal.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                price = parsed;
                errorMessage = string.Empty;
                return true;
            }

            price = 0m;
            errorMessage = "Alpha Vantageの株価データの数値変換に失敗しました。";
            return false;
        }

        if (TryExtractAlphaVantageError(document.RootElement, out var avError))
        {
            price = 0m;
            errorMessage = $"Alpha Vantage応答エラー: {avError}";
            return false;
        }

        price = 0m;
        errorMessage = "Alpha Vantageの株価データ形式が不正です。";
        return false;
    }

    private static bool TryExtractAlphaVantageError(JsonElement root, out string error)
    {
        foreach (var key in new[] { "Error Message", "Note", "Information" })
        {
            if (root.TryGetProperty(key, out var node))
            {
                var message = node.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    error = message;
                    return true;
                }
            }
        }

        error = string.Empty;
        return false;
    }

    private async Task<decimal> GetStockPriceFromStooqAsync(string tokyoSymbol, CancellationToken cancellationToken)
    {
        var stooqSymbol = ConvertTokyoSymbolToStooqSymbol(tokyoSymbol);
        var requestUri = $"https://stooq.com/q/l/?s={Uri.EscapeDataString(stooqSymbol)}&i=d";

        var csv = await SharedClient.GetStringAsync(requestUri, cancellationToken);
        if (!TryParseStooqClosePrice(csv, out var closePrice))
        {
            throw new InvalidOperationException($"株価取得に失敗しました。Alpha VantageとStooqの両方で {tokyoSymbol} を取得できませんでした。");
        }

        return closePrice;
    }

    private static string ConvertTokyoSymbolToStooqSymbol(string symbol)
    {
        if (!symbol.EndsWith(".T", StringComparison.OrdinalIgnoreCase))
        {
            return symbol.ToLowerInvariant();
        }

        return $"{symbol[..^2].ToLowerInvariant()}.jp";
    }

    private static bool TryParseStooqClosePrice(string csv, out decimal closePrice)
    {
        closePrice = 0m;
        if (string.IsNullOrWhiteSpace(csv))
        {
            return false;
        }

        var lines = csv
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length < 7)
            {
                continue;
            }

            var closeText = parts[6].Trim();
            if (closeText.Equals("N/D", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (decimal.TryParse(closeText, NumberStyles.Float, CultureInfo.InvariantCulture, out closePrice))
            {
                return true;
            }
        }

        return false;
    }
}
