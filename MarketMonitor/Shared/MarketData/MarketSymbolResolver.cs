using MarketMonitor.Shared.Logging;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 入力シンボルを内部利用向けの正規化済みシンボルへ解決する。
/// </summary>
public sealed class MarketSymbolResolver
{
    private const string DefaultTokyoListedSymbol = "7203.T";
    private static readonly Dictionary<string, string> SymbolAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ソフトバンク"] = "9984.T",
            ["ソフトバンクグループ"] = "9984.T",
            ["トヨタ"] = "7203.T",
            ["三菱重工"] = "7011.T",
            ["三菱UFJ"] = "8306.T"
        };

    private readonly ITokyoListedSymbolResolver _tokyoListedSymbolResolver;
    private readonly IAppLogger? _logger;

    /// <summary>
    /// シンボル解決サービスを初期化する。
    /// </summary>
    public MarketSymbolResolver(ITokyoListedSymbolResolver tokyoListedSymbolResolver, IAppLogger? logger)
    {
        _tokyoListedSymbolResolver = tokyoListedSymbolResolver ?? throw new ArgumentNullException(nameof(tokyoListedSymbolResolver));
        _logger = logger;
    }

    /// <summary>
    /// 入力を正規化し、必要に応じて東証銘柄名をシンボルへ解決する。
    /// </summary>
    public async Task<string> ResolveAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSymbolInput(symbol);
        if (normalized.EndsWith(".T", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        var resolvedTokyoListedSymbol = await _tokyoListedSymbolResolver.ResolveAsync(normalized, cancellationToken);
        if (!string.IsNullOrWhiteSpace(resolvedTokyoListedSymbol))
        {
            _logger?.Info($"TokyoListedSymbolResolved: Input={symbol}, Resolved={resolvedTokyoListedSymbol}");
            return resolvedTokyoListedSymbol;
        }

        throw new InvalidOperationException(ApiErrorMessages.TokyoListedOnlyMessage);
    }

    /// <summary>
    /// 入力から銘柄名を解決する。
    /// </summary>
    public async Task<string?> ResolveCompanyNameAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSymbolInput(symbol);
        return await _tokyoListedSymbolResolver.ResolveCompanyNameAsync(normalized, cancellationToken);
    }

    /// <summary>
    /// セクター名を解決する。
    /// </summary>
    public async Task<string?> ResolveSectorNameAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSymbolInput(symbol);
        return await _tokyoListedSymbolResolver.ResolveSectorNameAsync(normalized, cancellationToken);
    }

    /// <summary>
    /// 市場区分を解決する。
    /// </summary>
    public async Task<TokyoMarketSegment> ResolveMarketSegmentAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSymbolInput(symbol);
        return await _tokyoListedSymbolResolver.ResolveMarketSegmentAsync(normalized, cancellationToken);
    }

    /// <summary>
    /// 同一セクター比較対象を取得する。
    /// </summary>
    public async Task<IReadOnlyList<TokyoListedSectorPeer>> GetSectorPeersAsync(string symbol, int maxCount, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSymbolInput(symbol);
        return await _tokyoListedSymbolResolver.GetSectorPeersAsync(normalized, maxCount, cancellationToken);
    }

    internal static string NormalizeSymbolInput(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return DefaultTokyoListedSymbol;
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

        if (trimmed.Length == 4 && trimmed.All(char.IsDigit))
        {
            return $"{trimmed}.T";
        }

        return trimmed;
    }
}