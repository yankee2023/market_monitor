using System.IO;
using Microsoft.Data.Sqlite;
using MarketMonitor.Features.MarketSnapshot.Models;
using MarketMonitor.Features.PriceHistory.Services;
using MarketMonitor.Shared.Logging;

namespace MarketMonitorTest;

/// <summary>
/// SqlitePriceHistoryRepository の振る舞いを検証するテストクラス。
/// </summary>
public class SqlitePriceHistoryRepositoryTest : IDisposable
{
    private readonly string _databasePath;

    public SqlitePriceHistoryRepositoryTest()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"marketmonitor-test-{Guid.NewGuid():N}.db");
    }

    /// <summary>
    /// 保存後に最新履歴を取得できることをテスト。
    /// 期待値: 保存した 1 件が返る。
    /// </summary>
    [Fact]
    public async Task SaveAsync_AndGetRecentAsync_PersistsHistory()
    {
        // Arrange
        var repository = new SqlitePriceHistoryRepository(new FakeLogger(), _databasePath);
        var snapshot = new MarketSnapshot
        {
            Symbol = "7203.T",
            StockPrice = 1234.5m,
            StockUpdatedAt = new DateTimeOffset(2026, 4, 5, 9, 0, 0, TimeSpan.Zero)
        };

        // Act
        await repository.SaveAsync(snapshot, CancellationToken.None);
        var result = await repository.GetRecentAsync("7203.T", 20, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(1234.5m, result[0].StockPrice);
        Assert.Equal("7203.T", result[0].Symbol);
    }

    /// <summary>
    /// 旧スキーマに exchange_rate 列があっても新スキーマへ移行できることをテスト。
    /// 期待値: 読込と追加保存が成功する。
    /// </summary>
    [Fact]
    public async Task SaveAsync_MigratesLegacySchema_WhenExchangeRateColumnExists()
    {
        // Arrange
        await CreateLegacyDatabaseAsync();
        var repository = new SqlitePriceHistoryRepository(new FakeLogger(), _databasePath);

        // Act
        var before = await repository.GetRecentAsync("7203.T", 20, CancellationToken.None);
        await repository.SaveAsync(
            new MarketSnapshot
            {
                Symbol = "7203.T",
                StockPrice = 1600m,
                StockUpdatedAt = new DateTimeOffset(2026, 4, 5, 10, 0, 0, TimeSpan.Zero)
            },
            CancellationToken.None);
        var after = await repository.GetRecentAsync("7203.T", 20, CancellationToken.None);

        // Assert
        Assert.Single(before);
        Assert.Equal(2, after.Count);
        Assert.Contains(after, entry => entry.StockPrice == 1500m);
        Assert.Contains(after, entry => entry.StockPrice == 1600m);
    }

    private async Task CreateLegacyDatabaseAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE price_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                symbol TEXT NOT NULL,
                exchange_rate REAL NOT NULL,
                stock_price REAL NOT NULL,
                recorded_at TEXT NOT NULL
            );
            INSERT INTO price_history(symbol, exchange_rate, stock_price, recorded_at)
            VALUES('7203.T', 150.0, 1500.0, '2026-04-05T09:00:00.0000000+00:00');
            """;
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    File.Delete(_databasePath);
                    break;
                }
                catch (IOException) when (attempt < 2)
                {
                    Thread.Sleep(50);
                    SqliteConnection.ClearAllPools();
                }
            }
        }

        GC.SuppressFinalize(this);
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