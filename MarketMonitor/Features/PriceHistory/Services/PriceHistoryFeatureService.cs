using MarketMonitor.Features.PriceHistory.Models;
using MarketMonitor.Shared.Logging;
using MarketSnapshotModel = MarketMonitor.Features.MarketSnapshot.Models.MarketSnapshot;

namespace MarketMonitor.Features.PriceHistory.Services;

/// <summary>
/// 価格履歴機能をまとめて提供する。
/// </summary>
public sealed class PriceHistoryFeatureService : IPriceHistoryFeatureService
{
    private readonly IPriceHistoryRepository _repository;
    private readonly IAppLogger _logger;

    /// <summary>
    /// サービスを初期化する。
    /// </summary>
    public PriceHistoryFeatureService(IPriceHistoryRepository repository, IAppLogger logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PriceHistoryViewData> RecordAndLoadAsync(MarketSnapshotModel snapshot, int limit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await _repository.SaveAsync(snapshot, cancellationToken);
        var history = await _repository.GetRecentAsync(snapshot.Symbol, limit, cancellationToken);
        var orderedHistory = history.OrderBy(x => x.RecordedAt).ToList();
        var bars = PriceHistoryBarBuilder.Build(orderedHistory);

        _logger.Info($"PriceHistoryFeatureCompleted: Symbol={snapshot.Symbol}, Count={orderedHistory.Count}");
        return new PriceHistoryViewData(orderedHistory, bars);
    }
}