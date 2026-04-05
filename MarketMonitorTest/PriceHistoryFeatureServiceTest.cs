using MarketMonitor.Features.MarketSnapshot.Models;
using MarketMonitor.Features.PriceHistory.Models;
using MarketMonitor.Features.PriceHistory.Services;
using MarketMonitor.Shared.Logging;

namespace MarketMonitorTest;

/// <summary>
/// PriceHistoryFeatureService の振る舞いを検証するテストクラス。
/// </summary>
public class PriceHistoryFeatureServiceTest
{
    /// <summary>
    /// 履歴を時刻昇順へ並び替えてバーを生成することをテスト。
    /// 期待値: Items が昇順で Bars 件数も一致する。
    /// </summary>
    [Fact]
    public async Task RecordAndLoadAsync_OrdersHistory_AndBuildsBars()
    {
        // Arrange
        var repository = new FakePriceHistoryRepository(
        [
            new PriceHistoryEntry { Id = 2, Symbol = "7203.T", StockPrice = 120m, RecordedAt = new DateTimeOffset(2026, 4, 5, 10, 5, 0, TimeSpan.Zero) },
            new PriceHistoryEntry { Id = 1, Symbol = "7203.T", StockPrice = 100m, RecordedAt = new DateTimeOffset(2026, 4, 5, 10, 0, 0, TimeSpan.Zero) }
        ]);
        var service = new PriceHistoryFeatureService(repository, new FakeLogger());
        var snapshot = new MarketSnapshot { Symbol = "7203.T", StockPrice = 120m, StockUpdatedAt = DateTimeOffset.UtcNow };

        // Act
        var result = await service.RecordAndLoadAsync(snapshot, 20, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items[0].RecordedAt <= result.Items[1].RecordedAt);
        Assert.Equal(2, result.Bars.Count);
        Assert.True(repository.SaveCalled);
    }

    private sealed class FakePriceHistoryRepository : IPriceHistoryRepository
    {
        private readonly IReadOnlyList<PriceHistoryEntry> _history;

        public FakePriceHistoryRepository(IReadOnlyList<PriceHistoryEntry> history)
        {
            _history = history;
        }

        public bool SaveCalled { get; private set; }

        public Task SaveAsync(MarketSnapshot snapshot, CancellationToken cancellationToken)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PriceHistoryEntry>> GetRecentAsync(string symbol, int limit, CancellationToken cancellationToken)
        {
            return Task.FromResult(_history);
        }
    }

    private sealed class FakeLogger : IAppLogger
    {
        public void Info(string message)
        {
        }

        public void LogError(Exception exception, string message)
        {
        }
    }
}