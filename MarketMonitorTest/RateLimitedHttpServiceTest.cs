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

public sealed class RateLimitedHttpServiceTest
{
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

    private static HttpResponseMessage CreateTooManyRequestsResponse()
    {
        var response = new HttpResponseMessage((HttpStatusCode)429)
        {
            Content = new StringContent("rate limited")
        };
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
        return response;
    }

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