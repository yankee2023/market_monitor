using System.Globalization;
using System.Net.Http;
using MarketMonitor.Features.MarketSnapshot.Services;
using MarketMonitor.Features.SectorComparison.Models;
using MarketMonitor.Shared.Logging;
using MarketMonitor.Shared.MarketData;

namespace MarketMonitor.Features.SectorComparison.Services;

/// <summary>
/// 東証銘柄の同業比較表示データを提供する。
/// </summary>
public sealed class SectorComparisonFeatureService : ISectorComparisonFeatureService
{
    private const int DefaultPeerCount = 3;

    private readonly MarketSymbolResolver _marketSymbolResolver;
    private readonly IMarketSnapshotService _marketSnapshotService;
    private readonly IAppLogger _logger;

    /// <summary>
    /// サービスを初期化する。
    /// </summary>
    public SectorComparisonFeatureService(
        MarketSymbolResolver marketSymbolResolver,
        IMarketSnapshotService marketSnapshotService,
        IAppLogger logger)
    {
        _marketSymbolResolver = marketSymbolResolver ?? throw new ArgumentNullException(nameof(marketSymbolResolver));
        _marketSnapshotService = marketSnapshotService ?? throw new ArgumentNullException(nameof(marketSnapshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SectorComparisonViewData> LoadAsync(string symbol, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var normalizedSymbol = await _marketSymbolResolver.ResolveAsync(symbol, cancellationToken);
        var sectorName = await _marketSymbolResolver.ResolveSectorNameAsync(normalizedSymbol, cancellationToken) ?? "-";
        var marketSegment = await _marketSymbolResolver.ResolveMarketSegmentAsync(normalizedSymbol, cancellationToken);
        var peers = await _marketSymbolResolver.GetSectorPeersAsync(normalizedSymbol, DefaultPeerCount, cancellationToken);
        var items = new List<SectorComparisonPeerItem>();

        foreach (var peer in peers)
        {
            try
            {
                var snapshot = await _marketSnapshotService.GetMarketSnapshotAsync(peer.Symbol, cancellationToken);
                items.Add(new SectorComparisonPeerItem
                {
                    Symbol = peer.Symbol,
                    CompanyName = snapshot.CompanyName,
                    StockPrice = snapshot.StockPrice,
                    StockPriceDisplay = snapshot.StockPrice.ToString("N2", CultureInfo.CurrentCulture),
                    MarketSegmentDisplay = TokyoMarketSegmentParser.ToDisplayName(peer.MarketSegment)
                });
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
            {
                _logger.LogError(ex, $"SectorComparisonPeerLoadFailed: Symbol={peer.Symbol}");
            }
        }

        return new SectorComparisonViewData(
            sectorName,
            TokyoMarketSegmentParser.ToDisplayName(marketSegment),
            items);
    }
}