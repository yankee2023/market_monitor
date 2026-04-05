using System.IO;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 東証プライム銘柄名からシンボルを解決する。
/// </summary>
public sealed class TokyoPrimeSymbolResolver : ITokyoPrimeSymbolResolver
{
    private const string JpxListedCompaniesUrl = "https://www.jpx.co.jp/markets/statistics-equities/misc/tvdivq0000001vg2-att/data_j.xls";
    private static readonly SemaphoreSlim LookupLock = new(1, 1);

    private static ResolverLookup? _lookupCache;
    private static DateTimeOffset _loadedAt;

    private readonly IRateLimitedHttpService _httpService;
    private readonly ITokyoPrimeCompanyRecordReader _recordReader;
    private readonly Func<CancellationToken, Task<ResolverLookup>>? _lookupLoader;

    /// <summary>
    /// 解決サービスを初期化する。
    /// </summary>
    public TokyoPrimeSymbolResolver(IRateLimitedHttpService httpService)
        : this(httpService, new JpxExcelCompanyRecordReader())
    {
    }

    /// <summary>
    /// テストまたは差し替え向けの依存を指定して初期化する。
    /// </summary>
    internal TokyoPrimeSymbolResolver(IRateLimitedHttpService httpService, ITokyoPrimeCompanyRecordReader recordReader)
    {
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
        _recordReader = recordReader ?? throw new ArgumentNullException(nameof(recordReader));
    }

    /// <summary>
    /// テスト用の銘柄一覧ローダーを指定して初期化する。
    /// </summary>
    internal TokyoPrimeSymbolResolver(Func<CancellationToken, Task<IReadOnlyDictionary<string, string>>> symbolsLoader)
    {
        _httpService = new RateLimitedHttpService();
        _recordReader = new JpxExcelCompanyRecordReader();
        ArgumentNullException.ThrowIfNull(symbolsLoader);
        _lookupLoader = async cancellationToken => BuildLookup(await symbolsLoader(cancellationToken));
    }

    internal static void ResetCache()
    {
        _lookupCache = null;
        _loadedAt = default;
    }

    /// <summary>
    /// 入力文字列から東証プライム銘柄シンボルを解決する。
    /// </summary>
    public async Task<string?> ResolveAsync(string input, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var lookup = await GetLookupAsync(cancellationToken);
        return FindSymbol(input, lookup.SymbolsByName);
    }

    /// <inheritdoc />
    public async Task<string?> ResolveCompanyNameAsync(string input, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var lookup = await GetLookupAsync(cancellationToken);
        return FindCompanyName(input, lookup.SymbolsByName, lookup.CompanyNamesBySymbol);
    }

    internal static string? FindSymbol(string input, IReadOnlyDictionary<string, string> symbolsByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentNullException.ThrowIfNull(symbolsByName);

        var normalizedInput = NormalizeCompanyName(input);
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return null;
        }

        if (symbolsByName.TryGetValue(input.Trim(), out var exactSymbol)
            || symbolsByName.TryGetValue(normalizedInput, out exactSymbol))
        {
            return exactSymbol;
        }

        var matchedSymbols = symbolsByName
            .Where(entry => entry.Key.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        return matchedSymbols.Count == 1 ? matchedSymbols[0] : null;
    }

    internal static string NormalizeCompanyName(string companyName)
    {
        return companyName
            .Replace("株式会社", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("　", string.Empty, StringComparison.Ordinal)
            .Replace("・", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    internal static IEnumerable<string> CreateLookupKeys(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            yield break;
        }

        var trimmed = companyName.Trim();
        var normalized = NormalizeCompanyName(trimmed);
        var withoutCorporation = NormalizeCompanyName(trimmed.Replace("株式会社", string.Empty, StringComparison.Ordinal));

        yield return trimmed;
        yield return normalized;

        if (!string.IsNullOrWhiteSpace(withoutCorporation))
        {
            yield return withoutCorporation;
        }
    }

    internal static Dictionary<string, string> BuildSymbolsByName(IEnumerable<TokyoPrimeCompanyRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return BuildLookup(records).SymbolsByName;
    }

    internal static string? FindCompanyName(
        string input,
        IReadOnlyDictionary<string, string> symbolsByName,
        IReadOnlyDictionary<string, string> companyNamesBySymbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentNullException.ThrowIfNull(symbolsByName);
        ArgumentNullException.ThrowIfNull(companyNamesBySymbol);

        var trimmed = input.Trim();
        if (TryNormalizeSymbol(trimmed, out var symbol)
            && companyNamesBySymbol.TryGetValue(symbol, out var companyName))
        {
            return companyName;
        }

        var resolvedSymbol = FindSymbol(trimmed, symbolsByName);
        if (!string.IsNullOrWhiteSpace(resolvedSymbol)
            && companyNamesBySymbol.TryGetValue(resolvedSymbol, out companyName))
        {
            return companyName;
        }

        return null;
    }

    private static ResolverLookup BuildLookup(IEnumerable<TokyoPrimeCompanyRecord> records)
    {
        var symbolsByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var companyNamesBySymbol = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Code)
                || string.IsNullOrWhiteSpace(record.CompanyName)
                || !string.Equals(record.MarketCategory, "プライム（内国株式）", StringComparison.Ordinal))
            {
                continue;
            }

            AddLookup(symbolsByName, companyNamesBySymbol, record.Code.Trim(), record.CompanyName.Trim());
        }

        return new ResolverLookup(symbolsByName, companyNamesBySymbol);
    }

    private static ResolverLookup BuildLookup(IReadOnlyDictionary<string, string> symbolsByName)
    {
        ArgumentNullException.ThrowIfNull(symbolsByName);

        var namesBySymbol = symbolsByName
            .GroupBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(pair => pair.Key).OrderBy(key => key.Length).ThenBy(key => key, StringComparer.Ordinal).First(),
                StringComparer.OrdinalIgnoreCase);

        return new ResolverLookup(new Dictionary<string, string>(symbolsByName, StringComparer.OrdinalIgnoreCase), namesBySymbol);
    }

    private async Task<ResolverLookup> GetLookupAsync(CancellationToken cancellationToken)
    {
        if (_lookupLoader is not null)
        {
            return await _lookupLoader(cancellationToken);
        }

        if (_lookupCache is not null && DateTimeOffset.UtcNow - _loadedAt < TimeSpan.FromDays(7))
        {
            return _lookupCache;
        }

        await LookupLock.WaitAsync(cancellationToken);
        try
        {
            if (_lookupCache is not null && DateTimeOffset.UtcNow - _loadedAt < TimeSpan.FromDays(7))
            {
                return _lookupCache;
            }

            _lookupCache = await DownloadSymbolsAsync(cancellationToken);
            _loadedAt = DateTimeOffset.UtcNow;
            return _lookupCache;
        }
        finally
        {
            LookupLock.Release();
        }
    }

    private async Task<ResolverLookup> DownloadSymbolsAsync(CancellationToken cancellationToken)
    {
        await using var responseStream = await _httpService.GetStreamAsync(JpxListedCompaniesUrl, "JPX", cancellationToken);
        return BuildLookup(_recordReader.Read(responseStream));
    }

    private static void AddLookup(
        Dictionary<string, string> symbolsByName,
        Dictionary<string, string> companyNamesBySymbol,
        string code,
        string companyName)
    {
        var symbol = code.EndsWith(".T", StringComparison.OrdinalIgnoreCase) ? code.ToUpperInvariant() : $"{code}.T";
        companyNamesBySymbol.TryAdd(symbol, companyName);

        foreach (var key in CreateLookupKeys(companyName))
        {
            symbolsByName.TryAdd(key, symbol);
        }
    }

    private static bool TryNormalizeSymbol(string input, out string symbol)
    {
        symbol = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        if (trimmed.EndsWith(".T", StringComparison.OrdinalIgnoreCase))
        {
            symbol = trimmed.ToUpperInvariant();
            return true;
        }

        if (trimmed.Length == 4 && trimmed.All(char.IsDigit))
        {
            symbol = $"{trimmed}.T";
            return true;
        }

        return false;
    }

    private sealed record ResolverLookup(
        Dictionary<string, string> SymbolsByName,
        Dictionary<string, string> CompanyNamesBySymbol);
}