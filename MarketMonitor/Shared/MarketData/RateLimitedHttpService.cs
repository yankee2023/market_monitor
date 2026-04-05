using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// レート制限を考慮した HTTP GET 処理を提供する。
/// </summary>
public sealed class RateLimitedHttpService : IRateLimitedHttpService
{
    private const int MaxTooManyRequestsRetries = 2;
    private static readonly HttpClient SharedClient = new();
    private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36";

    private readonly HttpClient _httpClient;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    /// <summary>
    /// 共通 HTTP クライアントを利用して初期化する。
    /// </summary>
    public RateLimitedHttpService()
        : this(SharedClient, Task.Delay)
    {
    }

    /// <summary>
    /// テストまたは差し替え向けの依存を指定して初期化する。
    /// </summary>
    /// <param name="httpClient">使用する HTTP クライアント。</param>
    /// <param name="delayAsync">待機処理。</param>
    internal RateLimitedHttpService(HttpClient httpClient, Func<TimeSpan, CancellationToken, Task> delayAsync)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _delayAsync = delayAsync ?? throw new ArgumentNullException(nameof(delayAsync));
    }

    /// <summary>
    /// 文字列レスポンスを取得する。
    /// </summary>
    /// <param name="requestUri">取得先 URI。</param>
    /// <param name="sourceName">提供元名。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>レスポンス本文。</returns>
    public async Task<string> GetStringAsync(string requestUri, string sourceName, CancellationToken cancellationToken)
    {
        using var response = await SendWithRateLimitRetryAsync(requestUri, sourceName, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// シーク可能なストリームレスポンスを取得する。
    /// </summary>
    /// <param name="requestUri">取得先 URI。</param>
    /// <param name="sourceName">提供元名。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>先頭位置へ巻き戻したメモリストリーム。</returns>
    public async Task<Stream> GetStreamAsync(string requestUri, string sourceName, CancellationToken cancellationToken)
    {
        using var response = await SendWithRateLimitRetryAsync(requestUri, sourceName, cancellationToken);
        await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var memoryStream = new MemoryStream();
        await sourceStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    private async Task<HttpResponseMessage> SendWithRateLimitRetryAsync(
        string requestUri,
        string sourceName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        for (var attempt = 0; attempt <= MaxTooManyRequestsRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            ConfigureRequestHeaders(request);
            var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                response.EnsureSuccessStatusCode();
            }

            var delay = GetRetryDelay(response, attempt);
            response.Dispose();
            if (attempt == MaxTooManyRequestsRetries)
            {
                throw CreateRateLimitException(sourceName);
            }

            await _delayAsync(delay, cancellationToken);
        }

        throw CreateRateLimitException(sourceName);
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        ArgumentNullException.ThrowIfNull(response);

        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                return delay;
            }
        }

        return TimeSpan.FromSeconds(Math.Min(8, Math.Pow(2, attempt + 1)));
    }

    private static void ConfigureRequestHeaders(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var headers = request.Headers;
        headers.TryAddWithoutValidation("User-Agent", BrowserUserAgent);
        headers.TryAddWithoutValidation("Accept", "application/json,text/plain,text/csv,text/html,*/*");
        headers.TryAddWithoutValidation("Accept-Language", "ja,en-US;q=0.9,en;q=0.8");
        headers.TryAddWithoutValidation("Cache-Control", "no-cache");
        headers.TryAddWithoutValidation("Pragma", "no-cache");

        if (request.RequestUri is null)
        {
            return;
        }

        if (request.RequestUri.Host.Contains("finance.yahoo.com", StringComparison.OrdinalIgnoreCase))
        {
            headers.Referrer = new Uri("https://finance.yahoo.com/");
            headers.TryAddWithoutValidation("Origin", "https://finance.yahoo.com");
            return;
        }

        if (request.RequestUri.Host.Contains("stooq.com", StringComparison.OrdinalIgnoreCase))
        {
            headers.Referrer = new Uri("https://stooq.com/");
        }
    }

    private static InvalidOperationException CreateRateLimitException(string sourceName)
    {
        return new InvalidOperationException($"{ApiErrorMessages.RateLimitMessage} ({sourceName})");
    }
}