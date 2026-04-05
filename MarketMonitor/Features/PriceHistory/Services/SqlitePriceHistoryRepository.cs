using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using MarketMonitor.Features.PriceHistory.Models;
using MarketMonitor.Shared.Logging;
using MarketSnapshotModel = MarketMonitor.Features.MarketSnapshot.Models.MarketSnapshot;

namespace MarketMonitor.Features.PriceHistory.Services;

/// <summary>
/// SQLite へ価格履歴を保存するリポジトリ実装。
/// </summary>
public sealed class SqlitePriceHistoryRepository : IPriceHistoryRepository
{
    private readonly string _connectionString;
    private readonly IAppLogger _logger;

    /// <summary>
    /// SQLite 履歴リポジトリを初期化する。
    /// </summary>
    public SqlitePriceHistoryRepository(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "market_history.db");
        _connectionString = CreateConnectionString(databasePath);
    }

    /// <summary>
    /// テストまたは特定 DB パス指定で SQLite 履歴リポジトリを初期化する。
    /// </summary>
    /// <param name="logger">アプリケーションロガー。</param>
    /// <param name="databasePath">SQLite ファイルパス。</param>
    public SqlitePriceHistoryRepository(IAppLogger logger, string databasePath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = CreateConnectionString(fullPath);
    }

    /// <inheritdoc />
    public async Task SaveAsync(MarketSnapshotModel snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await EnsureTableAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO price_history(symbol, stock_price, recorded_at) VALUES($symbol, $stockPrice, $recordedAt);";

        command.Parameters.AddWithValue("$symbol", snapshot.Symbol);
        command.Parameters.AddWithValue("$stockPrice", snapshot.StockPrice);
        command.Parameters.AddWithValue("$recordedAt", snapshot.StockUpdatedAt.ToString("O", CultureInfo.InvariantCulture));

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.Info($"履歴を保存しました。Symbol={snapshot.Symbol}, Price={snapshot.StockPrice}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceHistoryEntry>> GetRecentAsync(string symbol, int limit, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken);

        var normalizedSymbol = NormalizeSymbol(symbol);
        var safeLimit = NormalizeLimit(limit);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT id, symbol, stock_price, recorded_at FROM price_history WHERE symbol = $symbol ORDER BY recorded_at DESC LIMIT $limit;";
        command.Parameters.AddWithValue("$symbol", normalizedSymbol);
        command.Parameters.AddWithValue("$limit", safeLimit);

        var result = new List<PriceHistoryEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var recordedAtText = reader.GetString(3);
            result.Add(new PriceHistoryEntry
            {
                Id = reader.GetInt64(0),
                Symbol = reader.GetString(1),
                StockPrice = Convert.ToDecimal(reader.GetDouble(2), CultureInfo.InvariantCulture),
                RecordedAt = DateTimeOffset.Parse(recordedAtText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            });
        }

        _logger.Info($"履歴を読み込みました。Symbol={normalizedSymbol}, Count={result.Count}");
        return result;
    }

    private static string NormalizeSymbol(string symbol)
    {
        return string.IsNullOrWhiteSpace(symbol)
            ? "7203.T"
            : symbol.Trim().ToUpperInvariant();
    }

    private static int NormalizeLimit(int limit)
    {
        return limit <= 0 ? 20 : limit;
    }

    private static string CreateConnectionString(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        return $"Data Source={databasePath}";
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, cancellationToken))
        {
            await CreateCurrentSchemaAsync(connection, cancellationToken);
            return;
        }

        if (await RequiresMigrationAsync(connection, cancellationToken))
        {
            await MigrateSchemaAsync(connection, cancellationToken);
        }
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'price_history' LIMIT 1;";
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    private static async Task<bool> RequiresMigrationAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(price_history);";

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(1));
        }

        return columns.Contains("exchange_rate")
            || !columns.SetEquals(["id", "symbol", "stock_price", "recorded_at"]);
    }

    private static async Task CreateCurrentSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS price_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                symbol TEXT NOT NULL,
                stock_price REAL NOT NULL,
                recorded_at TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MigrateSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        _logger.Info("price_history テーブルを現行スキーマへ移行します。");

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            BEGIN TRANSACTION;
            CREATE TABLE price_history_new (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                symbol TEXT NOT NULL,
                stock_price REAL NOT NULL,
                recorded_at TEXT NOT NULL
            );
            INSERT INTO price_history_new(id, symbol, stock_price, recorded_at)
            SELECT id, symbol, stock_price, recorded_at
            FROM price_history;
            DROP TABLE price_history;
            ALTER TABLE price_history_new RENAME TO price_history;
            COMMIT;
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}