using System.IO;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 東証上場銘柄名からシンボルを解決する。
/// </summary>
public sealed class TokyoListedSymbolResolver : ITokyoListedSymbolResolver
{
    private const string JpxListedCompaniesUrl = "https://www.jpx.co.jp/markets/statistics-equities/misc/tvdivq0000001vg2-att/data_j.xls";
    private static readonly SemaphoreSlim LookupLock = new(1, 1);

    private static ResolverLookup? _lookupCache;
    private static DateTimeOffset _loadedAt;

    private readonly IRateLimitedHttpService _httpService;
    private readonly ITokyoListedCompanyRecordReader _recordReader;
    private readonly ITokyoMarketSegmentPolicy _marketSegmentPolicy;
    private readonly Func<CancellationToken, Task<ResolverLookup>>? _lookupLoader;

    /// <summary>
    /// 解決サービスを初期化する。
    /// </summary>
    public TokyoListedSymbolResolver(IRateLimitedHttpService httpService)
        : this(httpService, new JpxExcelCompanyRecordReader(), new TokyoMainMarketSegmentPolicy())
    {
    }

    /// <summary>
    /// テストまたは差し替え向けの依存を指定して初期化する。
    /// </summary>
    internal TokyoListedSymbolResolver(
        IRateLimitedHttpService httpService,
        ITokyoListedCompanyRecordReader recordReader,
        ITokyoMarketSegmentPolicy marketSegmentPolicy)
    {
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
        _recordReader = recordReader ?? throw new ArgumentNullException(nameof(recordReader));
        _marketSegmentPolicy = marketSegmentPolicy ?? throw new ArgumentNullException(nameof(marketSegmentPolicy));
    }

    /// <summary>
    /// テスト用の銘柄一覧ローダーを指定して初期化する。
    /// </summary>
    internal TokyoListedSymbolResolver(Func<CancellationToken, Task<IReadOnlyDictionary<string, string>>> symbolsLoader)
    {
        _httpService = new RateLimitedHttpService();
        _recordReader = new JpxExcelCompanyRecordReader();
        _marketSegmentPolicy = new TokyoMainMarketSegmentPolicy();
        ArgumentNullException.ThrowIfNull(symbolsLoader);
        _lookupLoader = async cancellationToken => BuildLookup(await symbolsLoader(cancellationToken));
    }

    internal static void ResetCache()
    {
        _lookupCache = null;
        _loadedAt = default;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<string?> ResolveSectorNameAsync(string input, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var lookup = await GetLookupAsync(cancellationToken);
        return FindSectorName(input, lookup.SymbolsByName, lookup.SectorNamesBySymbol);
    }

    /// <inheritdoc />
    public async Task<TokyoMarketSegment> ResolveMarketSegmentAsync(string input, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var lookup = await GetLookupAsync(cancellationToken);
        return FindMarketSegment(input, lookup.SymbolsByName, lookup.MarketSegmentsBySymbol);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TokyoListedSectorPeer>> GetSectorPeersAsync(string input, int maxCount, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var lookup = await GetLookupAsync(cancellationToken);
        var sectorName = FindSectorName(input, lookup.SymbolsByName, lookup.SectorNamesBySymbol);
        if (string.IsNullOrWhiteSpace(sectorName))
        {
            return Array.Empty<TokyoListedSectorPeer>();
        }

        var normalizedSymbol = TryNormalizeSymbol(input.Trim(), out var symbol)
            ? symbol
            : FindSymbol(input, lookup.SymbolsByName) ?? string.Empty;

        return lookup.PeersBySector.TryGetValue(sectorName, out var peers)
            ? peers
                .Where(peer => !string.Equals(peer.Symbol, normalizedSymbol, StringComparison.OrdinalIgnoreCase))
                .Take(maxCount <= 0 ? 3 : maxCount)
                .ToList()
            : Array.Empty<TokyoListedSectorPeer>();
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

    internal static Dictionary<string, string> BuildSymbolsByName(IEnumerable<TokyoListedCompanyRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return BuildLookup(records, new TokyoMainMarketSegmentPolicy()).SymbolsByName;
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

    internal static string? FindSectorName(
        string input,
        IReadOnlyDictionary<string, string> symbolsByName,
        IReadOnlyDictionary<string, string> sectorNamesBySymbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentNullException.ThrowIfNull(symbolsByName);
        ArgumentNullException.ThrowIfNull(sectorNamesBySymbol);

        var trimmed = input.Trim();
        if (TryNormalizeSymbol(trimmed, out var symbol)
            && sectorNamesBySymbol.TryGetValue(symbol, out var sectorName))
        {
            return sectorName;
        }

        var resolvedSymbol = FindSymbol(trimmed, symbolsByName);
        if (!string.IsNullOrWhiteSpace(resolvedSymbol)
            && sectorNamesBySymbol.TryGetValue(resolvedSymbol, out sectorName))
        {
            return sectorName;
        }

        return null;
    }

    private ResolverLookup BuildLookup(IEnumerable<TokyoListedCompanyRecord> records)
    {
        return BuildLookup(records, _marketSegmentPolicy);
    }

    private static ResolverLookup BuildLookup(
        IEnumerable<TokyoListedCompanyRecord> records,
        ITokyoMarketSegmentPolicy marketSegmentPolicy)
    {
        var symbolsByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var companyNamesBySymbol = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sectorNamesBySymbol = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var marketSegmentsBySymbol = new Dictionary<string, TokyoMarketSegment>(StringComparer.OrdinalIgnoreCase);
        var peersBySector = new Dictionary<string, List<TokyoListedSectorPeer>>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Code)
                || string.IsNullOrWhiteSpace(record.CompanyName)
                || !marketSegmentPolicy.Includes(record.MarketSegment))
            {
                continue;
            }

            AddLookup(
                symbolsByName,
                companyNamesBySymbol,
                sectorNamesBySymbol,
                marketSegmentsBySymbol,
                peersBySector,
                record.Code.Trim(),
                record.CompanyName.Trim(),
                record.SectorName?.Trim() ?? string.Empty,
                record.MarketSegment);
        }

        return new ResolverLookup(symbolsByName, companyNamesBySymbol, sectorNamesBySymbol, marketSegmentsBySymbol, peersBySector);
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

        return new ResolverLookup(
            new Dictionary<string, string>(symbolsByName, StringComparer.OrdinalIgnoreCase),
            namesBySymbol,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, TokyoMarketSegment>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, List<TokyoListedSectorPeer>>(StringComparer.OrdinalIgnoreCase));
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
        Dictionary<string, string> sectorNamesBySymbol,
        Dictionary<string, TokyoMarketSegment> marketSegmentsBySymbol,
        Dictionary<string, List<TokyoListedSectorPeer>> peersBySector,
        string code,
        string companyName,
        string sectorName,
        TokyoMarketSegment marketSegment)
    {
        var symbol = code.EndsWith(".T", StringComparison.OrdinalIgnoreCase) ? code.ToUpperInvariant() : $"{code}.T";
        companyNamesBySymbol.TryAdd(symbol, companyName);
        marketSegmentsBySymbol.TryAdd(symbol, marketSegment);

        if (!string.IsNullOrWhiteSpace(sectorName))
        {
            sectorNamesBySymbol.TryAdd(symbol, sectorName);
            if (!peersBySector.TryGetValue(sectorName, out var peers))
            {
                peers = [];
                peersBySector[sectorName] = peers;
            }

            peers.Add(new TokyoListedSectorPeer
            {
                Symbol = symbol,
                CompanyName = companyName,
                SectorName = sectorName,
                MarketSegment = marketSegment
            });
        }

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

    internal static TokyoMarketSegment FindMarketSegment(
        string input,
        IReadOnlyDictionary<string, string> symbolsByName,
        IReadOnlyDictionary<string, TokyoMarketSegment> marketSegmentsBySymbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentNullException.ThrowIfNull(symbolsByName);
        ArgumentNullException.ThrowIfNull(marketSegmentsBySymbol);

        var trimmed = input.Trim();
        if (TryNormalizeSymbol(trimmed, out var symbol)
            && marketSegmentsBySymbol.TryGetValue(symbol, out var marketSegment))
        {
            return marketSegment;
        }

        var resolvedSymbol = FindSymbol(trimmed, symbolsByName);
        return !string.IsNullOrWhiteSpace(resolvedSymbol)
            && marketSegmentsBySymbol.TryGetValue(resolvedSymbol, out marketSegment)
                ? marketSegment
                : TokyoMarketSegment.Unknown;
    }

    private sealed record ResolverLookup(
        Dictionary<string, string> SymbolsByName,
        Dictionary<string, string> CompanyNamesBySymbol,
        Dictionary<string, string> SectorNamesBySymbol,
        Dictionary<string, TokyoMarketSegment> MarketSegmentsBySymbol,
        Dictionary<string, List<TokyoListedSectorPeer>> PeersBySector);
}