using System.IO;
using MarketMonitor.Features.JapaneseStockChart.Models;
using MarketMonitor.Features.JapaneseStockChart.Services;
using MarketMonitor.Shared.Logging;
using MarketMonitor.Shared.MarketData;

namespace MarketMonitorTest;

/// <summary>
/// JapaneseCandleService の振る舞いを検証するテストクラス。
/// </summary>
public class JapaneseCandleServiceTest
{
    /// <summary>
    /// Yahoo Finance 応答からローソク足を取得できることをテスト。
    /// 期待値: 2 件返る。
    /// </summary>
    [Fact]
    public async Task GetJapaneseCandlesAsync_ReturnsCandles_FromYahooFinance()
    {
        // Arrange
        var httpService = new FakeHttpService();
        httpService.AddStringResponse("query1.finance.yahoo.com", CreateYahooCandlesJson());
        var service = CreateService(httpService);

        // Act
        var result = await service.GetJapaneseCandlesAsync("7203", CandleTimeframe.Daily, 10, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(100m, result[0].Open);
        Assert.Equal(103m, result[1].Close);
    }

    /// <summary>
    /// Yahoo Finance がレート制限時に Stooq へフォールバックすることをテスト。
    /// 期待値: Stooq の 2 件が返る。
    /// </summary>
    [Fact]
    public async Task GetJapaneseCandlesAsync_FallsBackToStooq_WhenYahooRateLimited()
    {
        // Arrange
        var httpService = new FakeHttpService();
        httpService.AddException("query1.finance.yahoo.com", new InvalidOperationException(ApiErrorMessages.RateLimitMessage));
        httpService.AddStringResponse("stooq.com", "Date,Open,High,Low,Close,Volume\n2026-04-01,100,110,95,108,1000\n2026-04-02,108,112,101,103,1200");
        var service = CreateService(httpService);

        // Act
        var result = await service.GetJapaneseCandlesAsync("7203", CandleTimeframe.Daily, 10, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(108m, result[0].Close);
        Assert.Equal(103m, result[1].Close);
    }

    /// <summary>
    /// キャッシュ済みローソク足がある場合に HTTP を呼ばず返却することをテスト。
    /// 期待値: limit に応じて 1 件返る。
    /// </summary>
    [Fact]
    public async Task GetJapaneseCandlesAsync_UsesCache_WhenAvailable()
    {
        // Arrange
        var cache = new MarketDataCache();
        cache.SetCandles("7203.T", CandleTimeframe.Daily,
        [
            new JapaneseCandleEntry { Date = new DateTime(2026, 4, 1), Open = 100m, High = 110m, Low = 95m, Close = 108m },
            new JapaneseCandleEntry { Date = new DateTime(2026, 4, 2), Open = 108m, High = 112m, Low = 101m, Close = 103m }
        ]);
        var httpService = new FakeHttpService();
        var service = CreateService(httpService, cache);

        // Act
        var result = await service.GetJapaneseCandlesAsync("7203", CandleTimeframe.Daily, 1, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateTime(2026, 4, 2), result[0].Date);
        Assert.Empty(httpService.Requests);
    }

    private static JapaneseCandleService CreateService(FakeHttpService httpService, MarketDataCache? cache = null)
    {
        var resolver = new MarketSymbolResolver(new FakeTokyoPrimeResolver(), new FakeLogger());
        return new JapaneseCandleService(new FakeLogger(), httpService, cache ?? new MarketDataCache(), resolver);
    }

    private static string CreateYahooCandlesJson()
    {
        return
            """
            {
              "chart": {
                "result": [
                  {
                    "timestamp": [1711926000, 1712012400],
                    "indicators": {
                      "quote": [
                        {
                          "open": [100.0, 108.0],
                          "high": [110.0, 112.0],
                          "low": [95.0, 101.0],
                          "close": [108.0, 103.0]
                        }
                      ]
                    }
                  }
                ],
                "error": null
              }
            }
            """;
    }

    private sealed class FakeHttpService : IRateLimitedHttpService
    {
        private readonly Dictionary<string, string> _stringResponses = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Exception> _exceptions = new(StringComparer.OrdinalIgnoreCase);

        public List<string> Requests { get; } = [];

        public void AddStringResponse(string match, string response)
        {
            _stringResponses[match] = response;
        }

        public void AddException(string match, Exception exception)
        {
            _exceptions[match] = exception;
        }

        public Task<string> GetStringAsync(string requestUri, string sourceName, CancellationToken cancellationToken)
        {
            Requests.Add(requestUri);

            foreach (var pair in _exceptions)
            {
                if (requestUri.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    throw pair.Value;
                }
            }

            foreach (var pair in _stringResponses)
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
            return Task.FromResult<string?>(input switch
            {
                "7203" => "7203.T",
                _ => null
            });
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