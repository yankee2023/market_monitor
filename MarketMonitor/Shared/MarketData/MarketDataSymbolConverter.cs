namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 市場データ提供元ごとのシンボル形式変換を提供する。
/// </summary>
internal static class MarketDataSymbolConverter
{
    /// <summary>
    /// 東証シンボルを Stooq 形式へ変換する。
    /// </summary>
    /// <param name="symbol">変換対象シンボル。</param>
    /// <returns>Stooq 形式のシンボル。</returns>
    public static string ToStooqSymbol(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (!symbol.EndsWith(".T", StringComparison.OrdinalIgnoreCase))
        {
            return symbol.ToLowerInvariant();
        }

        return $"{symbol[..^2].ToLowerInvariant()}.jp";
    }
}