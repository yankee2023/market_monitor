using MarketMonitor.Shared.Logging;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 入力シンボルを内部利用向けの正規化済みシンボルへ解決する。
/// </summary>
public sealed class MarketSymbolResolver
{
    private const string DefaultTokyoPrimeSymbol = "7203.T";
    private static readonly Dictionary<string, string> SymbolAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ソフトバンク"] = "9984.T",
            ["ソフトバンクグループ"] = "9984.T",
            ["トヨタ"] = "7203.T",
            ["三菱重工"] = "7011.T",
            ["三菱UFJ"] = "8306.T"
        };

    private readonly ITokyoPrimeSymbolResolver _tokyoPrimeSymbolResolver;
    private readonly IAppLogger? _logger;

    /// <summary>
    /// シンボル解決サービスを初期化する。
    /// </summary>
    public MarketSymbolResolver(ITokyoPrimeSymbolResolver tokyoPrimeSymbolResolver, IAppLogger? logger)
    {
        _tokyoPrimeSymbolResolver = tokyoPrimeSymbolResolver ?? throw new ArgumentNullException(nameof(tokyoPrimeSymbolResolver));
        _logger = logger;
    }

    /// <summary>
    /// 入力を正規化し、必要に応じて東証プライム銘柄名をシンボルへ解決する。
    /// </summary>
    public async Task<string> ResolveAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSymbolInput(symbol);
        if (normalized.EndsWith(".T", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        var resolvedTokyoPrimeSymbol = await _tokyoPrimeSymbolResolver.ResolveAsync(normalized, cancellationToken);
        if (!string.IsNullOrWhiteSpace(resolvedTokyoPrimeSymbol))
        {
            _logger?.Info($"TokyoPrimeSymbolResolved: Input={symbol}, Resolved={resolvedTokyoPrimeSymbol}");
            return resolvedTokyoPrimeSymbol;
        }

        throw new InvalidOperationException(ApiErrorMessages.TokyoPrimeOnlyMessage);
    }

    /// <summary>
    /// 入力から銘柄名を解決する。
    /// </summary>
    public async Task<string?> ResolveCompanyNameAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSymbolInput(symbol);
        return await _tokyoPrimeSymbolResolver.ResolveCompanyNameAsync(normalized, cancellationToken);
    }

    internal static string NormalizeSymbolInput(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return DefaultTokyoPrimeSymbol;
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