using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Shared.Logging;

namespace MarketMonitor.Features.JapaneseStockChart.Services;

/// <summary>
/// 分析ラインを SQLite へ保存するリポジトリ実装。
/// </summary>
public sealed class SqliteChartAnalysisLineRepository : IChartAnalysisLineRepository
{
    private readonly string _connectionString;
    private readonly IAppLogger _logger;

    /// <summary>
    /// SQLite 分析ラインリポジトリを初期化する。
    /// </summary>
    public SqliteChartAnalysisLineRepository(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "analysis_lines.db");
        _connectionString = $"Data Source={databasePath}";
    }

    /// <summary>
    /// テストまたは特定 DB パス指定で SQLite 分析ラインリポジトリを初期化する。
    /// </summary>
    public SqliteChartAnalysisLineRepository(IAppLogger logger, string databasePath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = $"Data Source={fullPath}";
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChartAnalysisLine>> GetAsync(
        string symbol,
        CandleTimeframe timeframe,
        CandleDisplayPeriod displayPeriod,
        CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken);

        var normalizedSymbol = NormalizeSymbol(symbol);
        var result = new List<ChartAnalysisLine>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT line_id, line_type, start_x_ratio, start_y_ratio, end_x_ratio, end_y_ratio
            FROM analysis_lines
            WHERE symbol = $symbol AND timeframe = $timeframe AND display_period = $displayPeriod
            ORDER BY sort_order;
            """;
        command.Parameters.AddWithValue("$symbol", normalizedSymbol);
        command.Parameters.AddWithValue("$timeframe", (int)timeframe);
        command.Parameters.AddWithValue("$displayPeriod", (int)displayPeriod);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ChartAnalysisLine(
                Guid.Parse(reader.GetString(0)),
                (ChartAnalysisLineType)reader.GetInt32(1),
                reader.GetDouble(2),
                reader.GetDouble(3),
                reader.GetDouble(4),
                reader.GetDouble(5)));
        }

        _logger.Info($"分析ラインを読み込みました。Symbol={normalizedSymbol}, Timeframe={timeframe}, Period={displayPeriod}, Count={result.Count}");
        return result;
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        string symbol,
        CandleTimeframe timeframe,
        CandleDisplayPeriod displayPeriod,
        IReadOnlyList<ChartAnalysisLine> lines,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lines);

        await EnsureTableAsync(cancellationToken);

        var normalizedSymbol = NormalizeSymbol(symbol);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText =
                "DELETE FROM analysis_lines WHERE symbol = $symbol AND timeframe = $timeframe AND display_period = $displayPeriod;";
            deleteCommand.Parameters.AddWithValue("$symbol", normalizedSymbol);
            deleteCommand.Parameters.AddWithValue("$timeframe", (int)timeframe);
            deleteCommand.Parameters.AddWithValue("$displayPeriod", (int)displayPeriod);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];

            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText =
                """
                INSERT INTO analysis_lines(
                    symbol,
                    timeframe,
                    display_period,
                    line_id,
                    line_type,
                    start_x_ratio,
                    start_y_ratio,
                    end_x_ratio,
                    end_y_ratio,
                    sort_order)
                VALUES(
                    $symbol,
                    $timeframe,
                    $displayPeriod,
                    $lineId,
                    $lineType,
                    $startXRatio,
                    $startYRatio,
                    $endXRatio,
                    $endYRatio,
                    $sortOrder);
                """;
            insertCommand.Parameters.AddWithValue("$symbol", normalizedSymbol);
            insertCommand.Parameters.AddWithValue("$timeframe", (int)timeframe);
            insertCommand.Parameters.AddWithValue("$displayPeriod", (int)displayPeriod);
            insertCommand.Parameters.AddWithValue("$lineId", line.Id.ToString("D", CultureInfo.InvariantCulture));
            insertCommand.Parameters.AddWithValue("$lineType", (int)line.LineType);
            insertCommand.Parameters.AddWithValue("$startXRatio", line.StartXRatio);
            insertCommand.Parameters.AddWithValue("$startYRatio", line.StartYRatio);
            insertCommand.Parameters.AddWithValue("$endXRatio", line.EndXRatio);
            insertCommand.Parameters.AddWithValue("$endYRatio", line.EndYRatio);
            insertCommand.Parameters.AddWithValue("$sortOrder", index);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        _logger.Info($"分析ラインを保存しました。Symbol={normalizedSymbol}, Timeframe={timeframe}, Period={displayPeriod}, Count={lines.Count}");
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS analysis_lines (
                symbol TEXT NOT NULL,
                timeframe INTEGER NOT NULL,
                display_period INTEGER NOT NULL,
                line_id TEXT NOT NULL,
                line_type INTEGER NOT NULL,
                start_x_ratio REAL NOT NULL,
                start_y_ratio REAL NOT NULL,
                end_x_ratio REAL NOT NULL,
                end_y_ratio REAL NOT NULL,
                sort_order INTEGER NOT NULL,
                PRIMARY KEY(symbol, timeframe, display_period, line_id)
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string NormalizeSymbol(string symbol)
    {
        return string.IsNullOrWhiteSpace(symbol)
            ? "7203.T"
            : symbol.Trim().ToUpperInvariant();
    }
}