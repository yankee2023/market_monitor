using System.Reflection;
using System.IO;
using MarketMonitor.Features.MarketSnapshot.Services;
using MarketMonitor.Shared.Logging;
using MarketMonitor.Shared.MarketData;

namespace MarketMonitorTest;

/// <summary>
/// MarketSnapshotService の補助ロジックを検証するテストクラス。
/// </summary>
public class MarketSnapshotServiceTest
{
    /// <summary>
    /// Yahoo Finance 応答から現在値スナップショットを生成できることをテスト。
    /// 期待値: Symbol と StockPrice が反映される。
    /// </summary>
    [Fact]
    public async Task GetMarketSnapshotAsync_ReturnsSnapshot_FromYahooFinance()
    {
        // Arrange
        var httpService = new FakeHttpService();
        httpService.AddStringResponse("query1.finance.yahoo.com",
            """
            {
                "chart": {
                    "result": [
                        {
                            "meta": {
                                "regularMarketPrice": 3609.0
                            },
                            "indicators": {
                                "quote": [
                                    {
                                        "close": [3580.0, 3609.0]
                                    }
                                ]
                            }
                        }
                    ],
                    "error": null
                }
            }
            """);
        var resolver = new MarketSymbolResolver(new FakeTokyoPrimeResolver(), new FakeLogger());
        var service = new MarketSnapshotService(new FakeLogger(), httpService, new MarketDataCache(), resolver);

        // Act
        var result = await service.GetMarketSnapshotAsync("7203", CancellationToken.None);

        // Assert
        Assert.Equal("7203.T", result.Symbol);
        Assert.Equal("トヨタ自動車", result.CompanyName);
        Assert.Equal(3609.0m, result.StockPrice);
    }

    /// <summary>
    /// Yahoo Finance 取得失敗時に Stooq へフォールバックすることをテスト。
    /// 期待値: Stooq の終値が返る。
    /// </summary>
    [Fact]
    public async Task GetMarketSnapshotAsync_FallsBackToStooq_WhenYahooDoesNotReturnPrice()
    {
        // Arrange
        var httpService = new FakeHttpService();
                httpService.AddStringResponse("query1.finance.yahoo.com", """
                        {
                            "chart": {
                                "result": [],
                                "error": null
                            }
                        }
                        """);
        httpService.AddStringResponse("stooq.com", "Symbol,Date,Time,Open,High,Low,Close,Volume\n7203.jp,2026-04-04,15:00:00,3400,3500,3380,3473,12000000");
        var resolver = new MarketSymbolResolver(new FakeTokyoPrimeResolver(), new FakeLogger());
        var service = new MarketSnapshotService(new FakeLogger(), httpService, new MarketDataCache(), resolver);

        // Act
        var result = await service.GetMarketSnapshotAsync("7203", CancellationToken.None);

        // Assert
        Assert.Equal(3473m, result.StockPrice);
    }

    /// <summary>
    /// Yahoo Finance のチャート JSON から現在値を取得できることをテスト。
    /// 期待値: true および 3609.0。
    /// </summary>
    [Fact]
    public void TryParseYahooFinanceStockPrice_ReturnsTrue_ForValidJson()
    {
        // Arrange
        var method = typeof(MarketSnapshotService).GetMethod(
            "TryParseYahooFinanceStockPrice",
            BindingFlags.NonPublic | BindingFlags.Static);
        var args = new object[]
        {
            """
            {
                "chart": {
                    "result": [
                        {
                            "meta": {
                                "regularMarketPrice": 3609.0
                            },
                            "indicators": {
                                "quote": [
                                    {
                                        "close": [3580.0, 3609.0]
                                    }
                                ]
                            }
                        }
                    ],
                    "error": null
                }
            }
            """,
            0m,
            string.Empty
        };

        // Act
        var success = (bool)method!.Invoke(null, args)!;
        var price = (decimal)args[1];

        // Assert
        Assert.True(success);
        Assert.Equal(3609.0m, price);
    }

    /// <summary>
    /// Stooq の CSV から終値を取得できることをテスト。
    /// 期待値: true および 9473.0。
    /// </summary>
    [Fact]
    public void TryParseStooqClosePrice_ReturnsTrue_ForValidCsv()
    {
        // Arrange
        var method = typeof(MarketSnapshotService).GetMethod(
            "TryParseStooqClosePrice",
            BindingFlags.NonPublic | BindingFlags.Static);
        var args = new object[]
        {
            "Symbol,Date,Time,Open,High,Low,Close,Volume\n9984.jp,2026-04-04,15:00:00,9400,9500,9380,9473,12000000",
            0m
        };

        // Act
        var success = (bool)method!.Invoke(null, args)!;
        var parsed = (decimal)args[1];

        // Assert
        Assert.True(success);
        Assert.Equal(9473m, parsed);
    }

    /// <summary>
    /// Stooq の終値が N/D のとき失敗扱いになることをテスト。
    /// 期待値: false。
    /// </summary>
    [Fact]
    public void TryParseStooqClosePrice_ReturnsFalse_ForNdClose()
    {
        // Arrange
        var method = typeof(MarketSnapshotService).GetMethod(
            "TryParseStooqClosePrice",
            BindingFlags.NonPublic | BindingFlags.Static);
        var args = new object[]
        {
            "Symbol,Date,Time,Open,High,Low,Close,Volume\n9984.jp,2026-04-04,15:00:00,9400,9500,9380,N/D,12000000",
            0m
        };

        // Act
        var success = (bool)method!.Invoke(null, args)!;

        // Assert
        Assert.False(success);
    }

    private sealed class FakeHttpService : IRateLimitedHttpService
    {
        private readonly Dictionary<string, string> _responses = new(StringComparer.OrdinalIgnoreCase);

        public void AddStringResponse(string match, string response)
        {
            _responses[match] = response;
        }

        public Task<string> GetStringAsync(string requestUri, string sourceName, CancellationToken cancellationToken)
        {
            foreach (var pair in _responses)
            {
                if (requestUri.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(pair.Value);
                }
            }

            throw new InvalidOperationException($"未定義の HTTP 応答です: {requestUri}");
        }

        public Task<Stream> GetStreamAsync(string requestUri, string sourceName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeTokyoPrimeResolver : ITokyoPrimeSymbolResolver
    {
        public Task<string?> ResolveAsync(string input, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(input == "7203" ? "7203.T" : null);
        }

        public Task<string?> ResolveCompanyNameAsync(string input, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(input switch
            {
                "7203" => "トヨタ自動車",
                "7203.T" => "トヨタ自動車",
                _ => null
            });
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