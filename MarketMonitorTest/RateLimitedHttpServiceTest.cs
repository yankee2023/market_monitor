using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MarketMonitor.Shared.MarketData;
using Xunit;

namespace MarketMonitorTest;

/// <summary>
/// RateLimitedHttpService の成功系、再試行、ヘッダー付与を検証するテスト。
/// </summary>
public sealed class RateLimitedHttpServiceTest
{
    /// <summary>
    /// HTTP 200 応答時に本文をそのまま返すことを確認する。
    /// </summary>
    [Fact]
    public async Task GetStringAsync_200Response_ReturnsBody()
    {
        using var client = new HttpClient(new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("payload")
            }));

        var service = new RateLimitedHttpService(client, (_, _) => Task.CompletedTask);

        var result = await service.GetStringAsync("https://example.test/value", "TestSource", CancellationToken.None);

        Assert.Equal("payload", result);
    }

    /// <summary>
    /// 429 の後に 200 が返る場合、1 回再試行して本文を返すことを確認する。
    /// </summary>
    [Fact]
    public async Task GetStringAsync_429Then200_RetriesAndReturnsBody()
    {
        using var client = new HttpClient(new SequenceHttpMessageHandler(
            CreateTooManyRequestsResponse(),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("recovered")
            }));

        var delayCalls = 0;
        var service = new RateLimitedHttpService(client, (_, _) =>
        {
            delayCalls++;
            return Task.CompletedTask;
        });

        var result = await service.GetStringAsync("https://example.test/value", "TestSource", CancellationToken.None);

        Assert.Equal("recovered", result);
        Assert.Equal(1, delayCalls);
    }

    /// <summary>
    /// 429 が上限回数を超えて継続した場合、InvalidOperationException を送出することを確認する。
    /// </summary>
    [Fact]
    public async Task GetStringAsync_429Exceeded_ThrowsInvalidOperationException()
    {
        using var client = new HttpClient(new SequenceHttpMessageHandler(
            CreateTooManyRequestsResponse(),
            CreateTooManyRequestsResponse(),
            CreateTooManyRequestsResponse()));

        var service = new RateLimitedHttpService(client, (_, _) => Task.CompletedTask);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetStringAsync("https://example.test/value", "TestSource", CancellationToken.None));

        Assert.Contains("TestSource", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Stream 取得 API が読み取り可能なメモリストリームを返すことを確認する。
    /// </summary>
    [Fact]
    public async Task GetStreamAsync_200Response_ReturnsReadableMemoryStream()
    {
        using var client = new HttpClient(new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream([1, 2, 3]))
            }));

        var service = new RateLimitedHttpService(client, (_, _) => Task.CompletedTask);

        await using var stream = await service.GetStreamAsync("https://example.test/stream", "TestSource", CancellationToken.None);
        using var reader = new BinaryReader(stream);

        var data = reader.ReadBytes(3);

        Assert.Equal([1, 2, 3], data);
    }

    /// <summary>
    /// Yahoo Finance 呼び出し時にブラウザ互換ヘッダーが付与されることを確認する。
    /// </summary>
    [Fact]
    public async Task GetStringAsync_AddsBrowserLikeHeaders()
    {
        var handler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("payload")
            });
        using var client = new HttpClient(handler);

        var service = new RateLimitedHttpService(client, (_, _) => Task.CompletedTask);

        await service.GetStringAsync("https://query1.finance.yahoo.com/v8/finance/chart/7203.T", "Yahoo Finance", CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest!.Headers.Contains("User-Agent"));
        Assert.True(handler.LastRequest.Headers.Contains("Accept-Language"));
        Assert.Equal(new Uri("https://finance.yahoo.com/"), handler.LastRequest.Headers.Referrer);
    }

    /// <summary>
    /// レート制限応答を生成する。
    /// </summary>
    private static HttpResponseMessage CreateTooManyRequestsResponse()
    {
        var response = new HttpResponseMessage((HttpStatusCode)429)
        {
            Content = new StringContent("rate limited")
        };
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
        return response;
    }

    /// <summary>
    /// 事前定義した HTTP 応答列を順番に返すテスト用ハンドラ。
    /// </summary>
    private sealed class SequenceHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response configured.");
            }

            LastRequest = request;

            return Task.FromResult(_responses.Dequeue());
        }
    }
}