using System.Globalization;
using System.Text.Json;
using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Shared.Logging;
using MarketMonitor.Shared.MarketData;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// 日本株ローソク足データ取得を提供する。
/// </summary>
public sealed class JapaneseCandleService : IJapaneseCandleService
{
    private static readonly string[] ResponseSplitSeparators = ["\r\n", "\n"];

    private readonly IRateLimitedHttpService _httpService;
    private readonly MarketDataCache _cache;
    private readonly MarketSymbolResolver _symbolResolver;
    private readonly IAppLogger? _logger;

    /// <summary>
    /// サービスを初期化する。
    /// </summary>
    public JapaneseCandleService(
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
    public async Task<IReadOnlyList<JapaneseCandleEntry>> GetJapaneseCandlesAsync(
        string symbol,
        CandleTimeframe timeframe,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalizedSymbol = await _symbolResolver.ResolveAsync(symbol, cancellationToken);
        if (!normalizedSymbol.EndsWith(".T", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.Info($"CandlesSkipped: Symbol={normalizedSymbol}, Reason=NonJapaneseStock");
            return Array.Empty<JapaneseCandleEntry>();
        }

        _logger?.Info($"CandlesRequestStarted: Symbol={normalizedSymbol}, Timeframe={timeframe}, Limit={limit}");

        if (_cache.TryGetCandles(normalizedSymbol, timeframe, out var cachedCandles))
        {
            var cachedResult = LimitCandles(cachedCandles, limit);
            _logger?.Info($"CandlesRequestUsedCache: Symbol={normalizedSymbol}, Timeframe={timeframe}, ReturnedCount={cachedResult.Count}");
            return cachedResult;
        }

        try
        {
            var yahooCandles = await GetJapaneseCandlesFromYahooFinanceAsync(normalizedSymbol, timeframe, cancellationToken);
            if (yahooCandles.Count > 0)
            {
                _cache.SetCandles(normalizedSymbol, timeframe, yahooCandles);
                var yahooResult = LimitCandles(yahooCandles, limit);
                _logger?.Info($"CandlesRequestSucceeded: Symbol={normalizedSymbol}, Timeframe={timeframe}, Source=YahooFinance, ParsedCount={yahooCandles.Count}, ReturnedCount={yahooResult.Count}");
                return yahooResult;
            }
        }
        catch (InvalidOperationException ex) when (ApiErrorClassifier.IsRateLimitException(ex))
        {
            _logger?.Info($"CandlesPrimarySourceRateLimited: Symbol={normalizedSymbol}, Timeframe={timeframe}, Source=YahooFinance, Fallback=Stooq, Message={ex.Message}");
        }

        var stooqSymbol = MarketDataSymbolConverter.ToStooqSymbol(normalizedSymbol);
        var interval = timeframe == CandleTimeframe.Weekly ? "w" : "d";
        var requestUri = $"https://stooq.com/q/d/l/?s={Uri.EscapeDataString(stooqSymbol)}&i={interval}";

        _logger?.Info($"CandlesFallbackStarted: Symbol={normalizedSymbol}, Source=Stooq, StooqSymbol={stooqSymbol}, Timeframe={timeframe}, Interval={interval}, Limit={limit}");

        var csv = await _httpService.GetStringAsync(requestUri, "Stooq", cancellationToken);
        if (IsStooqAccessMessage(csv))
        {
            var exception = new InvalidOperationException("Stooq historical endpoint returned access message.");
            _logger?.LogError(
                exception,
                $"CandlesFallbackFailed: Symbol={normalizedSymbol}, Source=Stooq, StooqSymbol={stooqSymbol}, Timeframe={timeframe}, Reason=StooqAccessMessage, ResponsePreview={CreateResponsePreview(csv)}");
            return Array.Empty<JapaneseCandleEntry>();
        }

        var candles = ParseStooqHistoricalCandles(csv);
        if (candles.Count == 0)
        {
            var exception = new InvalidOperationException("No candlestick rows could be parsed from Stooq historical response.");
            _logger?.LogError(
                exception,
                $"CandlesFallbackFailed: Symbol={normalizedSymbol}, Source=Stooq, StooqSymbol={stooqSymbol}, Timeframe={timeframe}, Reason=NoParsableCandles, ResponsePreview={CreateResponsePreview(csv)}");
            return Array.Empty<JapaneseCandleEntry>();
        }

        var result = LimitCandles(candles, limit);
        _cache.SetCandles(normalizedSymbol, timeframe, candles);
        _logger?.Info($"CandlesRequestSucceeded: Symbol={normalizedSymbol}, Timeframe={timeframe}, Source=StooqFallback, ParsedCount={candles.Count}, ReturnedCount={result.Count}");
        return result;
    }

    private async Task<IReadOnlyList<JapaneseCandleEntry>> GetJapaneseCandlesFromYahooFinanceAsync(
        string tokyoSymbol,
        CandleTimeframe timeframe,
        CancellationToken cancellationToken)
    {
        var interval = timeframe == CandleTimeframe.Weekly ? "1wk" : "1d";
        var requestUri = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(tokyoSymbol)}?range=2y&interval={interval}&includePrePost=false&events=div%2Csplits";
        _logger?.Info($"CandlesPrimarySourceStarted: Symbol={tokyoSymbol}, Source=YahooFinance, Timeframe={timeframe}, Interval={interval}");

        var json = await _httpService.GetStringAsync(requestUri, "Yahoo Finance", cancellationToken);
        if (TryParseYahooFinanceCandles(json, out var candles, out var errorMessage))
        {
            _logger?.Info($"CandlesPrimarySourceSucceeded: Symbol={tokyoSymbol}, Source=YahooFinance, Timeframe={timeframe}, ParsedCount={candles.Count}");
            return candles;
        }

        var exception = new InvalidOperationException(errorMessage);
        _logger?.LogError(exception, $"CandlesPrimarySourceFailed: Symbol={tokyoSymbol}, Source=YahooFinance, Timeframe={timeframe}, Reason={errorMessage}");
        return Array.Empty<JapaneseCandleEntry>();
    }

    private static List<JapaneseCandleEntry> LimitCandles(IReadOnlyList<JapaneseCandleEntry> candles, int limit)
    {
        var safeLimit = limit <= 0 ? 30 : limit;
        return candles
            .OrderByDescending(x => x.Date)
            .Take(safeLimit)
            .OrderBy(x => x.Date)
            .ToList();
    }

    private static bool TryParseYahooFinanceCandles(
        string json,
        out IReadOnlyList<JapaneseCandleEntry> candles,
        out string errorMessage)
    {
        candles = Array.Empty<JapaneseCandleEntry>();
        errorMessage = string.Empty;

        using var document = JsonDocument.Parse(json);
        if (!TryGetYahooFinanceChartResult(document.RootElement, out var result, out errorMessage))
        {
            return false;
        }

        if (!result.TryGetProperty("timestamp", out var timestampArray)
            || timestampArray.ValueKind != JsonValueKind.Array)
        {
            errorMessage = "Yahoo Financeのローソク足データにtimestamp配列が見つかりませんでした。";
            return false;
        }

        if (!result.TryGetProperty("indicators", out var indicators)
            || !indicators.TryGetProperty("quote", out var quotes)
            || quotes.ValueKind != JsonValueKind.Array
            || quotes.GetArrayLength() == 0)
        {
            errorMessage = "Yahoo Financeのローソク足データにquote配列が見つかりませんでした。";
            return false;
        }

        var quote = quotes[0];
        if (!quote.TryGetProperty("open", out var openArray)
            || !quote.TryGetProperty("high", out var highArray)
            || !quote.TryGetProperty("low", out var lowArray)
            || !quote.TryGetProperty("close", out var closeArray))
        {
            errorMessage = "Yahoo Financeのローソク足データにOHLC配列が揃っていません。";
            return false;
        }

        var volumeArray = quote.TryGetProperty("volume", out var fetchedVolumeArray)
            ? fetchedVolumeArray
            : default;

        var count = new[]
        {
            timestampArray.GetArrayLength(),
            openArray.GetArrayLength(),
            highArray.GetArrayLength(),
            lowArray.GetArrayLength(),
            closeArray.GetArrayLength()
        }.Min();

        var items = new List<JapaneseCandleEntry>();
        for (var index = 0; index < count; index++)
        {
            if (!TryGetUnixDate(timestampArray[index], out var date)
                || !TryGetDecimal(openArray[index], out var open)
                || !TryGetDecimal(highArray[index], out var high)
                || !TryGetDecimal(lowArray[index], out var low)
                || !TryGetDecimal(closeArray[index], out var close))
            {
                continue;
            }

            items.Add(new JapaneseCandleEntry
            {
                Date = date,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = TryGetInt64(volumeArray, index, out var volume) ? volume : 0L
            });
        }

        if (items.Count == 0)
        {
            errorMessage = "Yahoo Financeのローソク足データから有効なOHLCを抽出できませんでした。";
            return false;
        }

        candles = items;
        return true;
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

    private static bool TryGetUnixDate(JsonElement element, out DateTime date)
    {
        date = default;
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt64(out var unixSeconds))
        {
            return false;
        }

        date = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).Date;
        return true;
    }

    private static bool IsStooqAccessMessage(string response)
    {
        return response.Contains("Write to www@stooq.com if you want to use our data", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateResponsePreview(string response)
    {
        var normalized = response.Replace("\r", " ").Replace("\n", " ").Trim();
        return normalized.Length <= 180 ? normalized : normalized[..180];
    }

    private static IReadOnlyList<JapaneseCandleEntry> ParseStooqHistoricalCandles(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<JapaneseCandleEntry>();
        }

        var lines = csv.Split(ResponseSplitSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length <= 1)
        {
            return Array.Empty<JapaneseCandleEntry>();
        }

        var result = new List<JapaneseCandleEntry>();
        foreach (var row in lines.Skip(1))
        {
            var parts = row.Split(',');
            if (parts.Length < 5)
            {
                continue;
            }

            if (!TryParseHistoricalOhlc(parts, out var date, out var open, out var high, out var low, out var close))
            {
                continue;
            }

            result.Add(new JapaneseCandleEntry
            {
                Date = date,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = TryParseVolume(parts, out var volume) ? volume : 0L
            });
        }

        return result;
    }

    private static bool TryParseHistoricalOhlc(
        string[] parts,
        out DateTime date,
        out decimal open,
        out decimal high,
        out decimal low,
        out decimal close)
    {
        date = default;
        open = 0m;
        high = 0m;
        low = 0m;
        close = 0m;

        if (parts.Length >= 5
            && DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            && TryParseDecimal(parts[1], out open)
            && TryParseDecimal(parts[2], out high)
            && TryParseDecimal(parts[3], out low)
            && TryParseDecimal(parts[4], out close))
        {
            return true;
        }

        if (parts.Length >= 6
            && DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            && TryParseDecimal(parts[2], out open)
            && TryParseDecimal(parts[3], out high)
            && TryParseDecimal(parts[4], out low)
            && TryParseDecimal(parts[5], out close))
        {
            return true;
        }

        return false;
    }

    private static bool TryParseVolume(string[] parts, out long volume)
    {
        volume = 0L;

        var volumeIndex = parts.Length >= 7 ? 6 : parts.Length >= 6 ? 5 : -1;
        if (volumeIndex < 0)
        {
            return false;
        }

        return long.TryParse(parts[volumeIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out volume);
    }

    private static bool TryGetInt64(JsonElement arrayElement, int index, out long value)
    {
        value = 0L;
        if (arrayElement.ValueKind != JsonValueKind.Array || index >= arrayElement.GetArrayLength())
        {
            return false;
        }

        var element = arrayElement[index];
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out value),
            JsonValueKind.String => long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value),
            _ => false
        };
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        value = 0m;
        var trimmed = text.Trim();
        if (trimmed.Equals("N/D", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool IsRateLimitException(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.Message.StartsWith(ApiErrorMessages.RateLimitMessage, StringComparison.Ordinal);
    }
}