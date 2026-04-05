using System.Globalization;
using System.Text.Json;
using MarketMonitor.Shared.Logging;
using MarketMonitor.Shared.MarketData;
using MarketSnapshotModel = MarketMonitor.Features.MarketSnapshot.Models.MarketSnapshot;

namespace MarketMonitor.Features.MarketSnapshot.Services;

/// <summary>
/// 日本株現在値取得機能を提供する。
/// </summary>
public sealed class MarketSnapshotService : IMarketSnapshotService
{
    private readonly IRateLimitedHttpService _httpService;
    private readonly MarketDataCache _cache;
    private readonly MarketSymbolResolver _symbolResolver;
    private readonly IAppLogger? _logger;

    /// <summary>
    /// サービスを初期化する。
    /// </summary>
    public MarketSnapshotService(
        IAppLogger? logger,
        IRateLimitedHttpService httpService,
        MarketDataCache cache,
        MarketSymbolResolver symbolResolver)
    {
        _logger = logger;
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _symbolResolver = symbolResolver ?? throw new ArgumentNullException(nameof(symbolResolver));
    }

    /// <inheritdoc />
    public async Task<MarketSnapshotModel> GetMarketSnapshotAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalizedSymbol = await _symbolResolver.ResolveAsync(symbol, cancellationToken);
        var companyName = await _symbolResolver.ResolveCompanyNameAsync(normalizedSymbol, cancellationToken) ?? string.Empty;
        var stockPrice = await GetStockPriceAsync(normalizedSymbol, cancellationToken);

        return new MarketSnapshotModel
        {
            Symbol = normalizedSymbol,
            CompanyName = companyName,
            StockPrice = stockPrice,
            StockUpdatedAt = DateTimeOffset.Now
        };
    }

    private async Task<decimal> GetStockPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        if (_cache.TryGetStockPrice(symbol, out var cachedPrice))
        {
            _logger?.Info($"StockPriceUsedCache: Symbol={symbol}, Price={cachedPrice}");
            return cachedPrice;
        }

        try
        {
            var yahooPrice = await GetStockPriceFromYahooFinanceAsync(symbol, cancellationToken);
            if (yahooPrice > 0m)
            {
                _cache.SetStockPrice(symbol, yahooPrice);
                return yahooPrice;
            }
        }
        catch (InvalidOperationException ex) when (IsRateLimitException(ex) && _cache.TryGetStockPrice(symbol, out cachedPrice))
        {
            _logger?.Info($"StockPriceRateLimitedUsingCache: Symbol={symbol}, Price={cachedPrice}, Message={ex.Message}");
            return cachedPrice;
        }
        catch (InvalidOperationException ex) when (IsRateLimitException(ex))
        {
            _logger?.Info($"StockPricePrimarySourceRateLimited: Symbol={symbol}, Source=YahooFinance, Fallback=Stooq, Message={ex.Message}");
        }

        var stooqPrice = await GetStockPriceFromStooqAsync(symbol, cancellationToken);
        _cache.SetStockPrice(symbol, stooqPrice);
        return stooqPrice;
    }

    private async Task<decimal> GetStockPriceFromYahooFinanceAsync(string tokyoSymbol, CancellationToken cancellationToken)
    {
        var requestUri = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(tokyoSymbol)}?range=5d&interval=1d&includePrePost=false&events=div%2Csplits";
        _logger?.Info($"StockPricePrimarySourceStarted: Symbol={tokyoSymbol}, Source=YahooFinance");

        var json = await _httpService.GetStringAsync(requestUri, "Yahoo Finance", cancellationToken);
        if (TryParseYahooFinanceStockPrice(json, out var price, out var errorMessage))
        {
            _logger?.Info($"StockPricePrimarySourceSucceeded: Symbol={tokyoSymbol}, Source=YahooFinance, Price={price}");
            return price;
        }

        var exception = new InvalidOperationException(errorMessage);
        _logger?.LogError(exception, $"StockPricePrimarySourceFailed: Symbol={tokyoSymbol}, Source=YahooFinance, Reason={errorMessage}");
        return 0m;
    }

    private async Task<decimal> GetStockPriceFromStooqAsync(string tokyoSymbol, CancellationToken cancellationToken)
    {
        var stooqSymbol = MarketDataSymbolConverter.ToStooqSymbol(tokyoSymbol);
        var requestUri = $"https://stooq.com/q/l/?s={Uri.EscapeDataString(stooqSymbol)}&i=d";

        _logger?.Info($"StockPriceFallbackStarted: Symbol={tokyoSymbol}, Source=Stooq, StooqSymbol={stooqSymbol}");

        var csv = await _httpService.GetStringAsync(requestUri, "Stooq", cancellationToken);
        if (!TryParseStooqClosePrice(csv, out var closePrice))
        {
            var exception = new InvalidOperationException($"株価取得に失敗しました。Yahoo Finance、Stooqのいずれでも {tokyoSymbol} を取得できませんでした。");
            _logger?.LogError(exception, $"StockPriceFallbackFailed: Symbol={tokyoSymbol}, Source=Stooq, StooqSymbol={stooqSymbol}, ResponsePreview={CreateResponsePreview(csv)}");
            throw exception;
        }

        _logger?.Info($"StockPriceFallbackSucceeded: Symbol={tokyoSymbol}, Source=Stooq, Price={closePrice}");
        return closePrice;
    }

    private static bool TryParseYahooFinanceStockPrice(string json, out decimal price, out string errorMessage)
    {
        price = 0m;
        errorMessage = string.Empty;

        using var document = JsonDocument.Parse(json);
        if (!TryGetYahooFinanceChartResult(document.RootElement, out var result, out errorMessage))
        {
            return false;
        }

        if (result.TryGetProperty("meta", out var meta)
            && meta.TryGetProperty("regularMarketPrice", out var regularMarketPrice)
            && TryGetDecimal(regularMarketPrice, out price))
        {
            return true;
        }

        if (TryGetLatestYahooFinanceClose(result, out price))
        {
            return true;
        }

        errorMessage = "Yahoo Financeの株価データに終値が見つかりませんでした。";
        return false;
    }

    private static bool TryGetYahooFinanceChartResult(
        JsonElement root,
        out JsonElement result,
        out string errorMessage)
    {
        result = default;
        errorMessage = string.Empty;

        if (!root.TryGetProperty("chart", out var chart))
        {
            errorMessage = "Yahoo Finance応答にchartノードが見つかりませんでした。";
            return false;
        }

        if (chart.TryGetProperty("error", out var errorNode)
            && errorNode.ValueKind != JsonValueKind.Null)
        {
            errorMessage = errorNode.TryGetProperty("description", out var descriptionNode)
                ? descriptionNode.GetString() ?? "Yahoo Finance応答エラー"
                : "Yahoo Finance応答エラー";
            return false;
        }

        if (!chart.TryGetProperty("result", out var resultArray)
            || resultArray.ValueKind != JsonValueKind.Array
            || resultArray.GetArrayLength() == 0)
        {
            errorMessage = "Yahoo Finance応答にresult配列が見つかりませんでした。";
            return false;
        }

        result = resultArray[0];
        return true;
    }

    private static bool TryGetLatestYahooFinanceClose(JsonElement result, out decimal closePrice)
    {
        closePrice = 0m;

        if (!result.TryGetProperty("indicators", out var indicators)
            || !indicators.TryGetProperty("quote", out var quotes)
            || quotes.ValueKind != JsonValueKind.Array
            || quotes.GetArrayLength() == 0)
        {
            return false;
        }

        var quote = quotes[0];
        if (!quote.TryGetProperty("close", out var closeArray)
            || closeArray.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        for (var index = closeArray.GetArrayLength() - 1; index >= 0; index--)
        {
            if (TryGetDecimal(closeArray[index], out closePrice))
            {
                return true;
            }
        }

        return false;
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

    private static bool TryParseStooqClosePrice(string csv, out decimal closePrice)
    {
        closePrice = 0m;
        if (string.IsNullOrWhiteSpace(csv))
        {
            return false;
        }

        var lines = csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            return false;
        }

        var dataLine = lines.Length > 1 ? lines[1] : lines[0];
        var parts = dataLine.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 7)
        {
            return false;
        }

        var closeIndex = lines[0].StartsWith("Symbol", StringComparison.OrdinalIgnoreCase) ? 6 : 6;
        var closeValue = parts[closeIndex];
        if (string.Equals(closeValue, "N/D", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return decimal.TryParse(closeValue, NumberStyles.Float, CultureInfo.InvariantCulture, out closePrice);
    }

    private static string CreateResponsePreview(string response)
    {
        var normalized = response.Replace("\r", " ").Replace("\n", " ").Trim();
        return normalized.Length <= 180 ? normalized : normalized[..180];
    }

    private static bool IsRateLimitException(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.Message.StartsWith(ApiErrorMessages.RateLimitMessage, StringComparison.Ordinal);
    }
}