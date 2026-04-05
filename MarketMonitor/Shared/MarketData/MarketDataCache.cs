using System.Collections.Concurrent;
using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// マーケットデータの短期キャッシュを管理する。
/// </summary>
public sealed class MarketDataCache
{
    private static readonly TimeSpan StockPriceCacheDuration = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan JapaneseCandlesCacheDuration = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, CachedValue<decimal>> _stockPriceCache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, CachedValue<IReadOnlyList<JapaneseCandleEntry>>> _candlesCache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 株価キャッシュを取得する。
    /// </summary>
    public bool TryGetStockPrice(string symbol, out decimal value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (_stockPriceCache.TryGetValue(symbol, out var cache) && cache.ExpiresAt > DateTimeOffset.UtcNow)
        {
            value = cache.Value;
            return true;
        }

        value = 0m;
        return false;
    }

    /// <summary>
    /// 株価キャッシュを保存する。
    /// </summary>
    public void SetStockPrice(string symbol, decimal value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (value <= 0m)
        {
            return;
        }

        _stockPriceCache[symbol] = new CachedValue<decimal>(value, DateTimeOffset.UtcNow.Add(StockPriceCacheDuration));
    }

    /// <summary>
    /// ローソク足キャッシュを取得する。
    /// </summary>
    public bool TryGetCandles(string symbol, CandleTimeframe timeframe, out IReadOnlyList<JapaneseCandleEntry> candles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (_candlesCache.TryGetValue(CreateCandlesCacheKey(symbol, timeframe), out var cache)
            && cache.ExpiresAt > DateTimeOffset.UtcNow)
        {
            candles = cache.Value;
            return true;
        }

        candles = Array.Empty<JapaneseCandleEntry>();
        return false;
    }

    /// <summary>
    /// ローソク足キャッシュを保存する。
    /// </summary>
    public void SetCandles(string symbol, CandleTimeframe timeframe, IReadOnlyList<JapaneseCandleEntry> candles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentNullException.ThrowIfNull(candles);

        _candlesCache[CreateCandlesCacheKey(symbol, timeframe)] =
            new CachedValue<IReadOnlyList<JapaneseCandleEntry>>([.. candles], DateTimeOffset.UtcNow.Add(JapaneseCandlesCacheDuration));
    }

    private static string CreateCandlesCacheKey(string symbol, CandleTimeframe timeframe)
    {
        return $"{symbol}:{timeframe}";
    }

    private sealed record CachedValue<T>(T Value, DateTimeOffset ExpiresAt);
}