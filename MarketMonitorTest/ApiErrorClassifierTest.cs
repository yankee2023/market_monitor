using System.Net;
using System.Net.Http;
using MarketMonitor.Shared.MarketData;

namespace MarketMonitorTest;

/// <summary>
/// API 例外分類の共通処理を検証するテストを表す。
/// </summary>
public sealed class ApiErrorClassifierTest
{
    [Fact]
    public void CreateUserMessage_ReturnsRateLimitMessage_ForTooManyRequests()
    {
        var message = ApiErrorClassifier.CreateUserMessage(new HttpRequestException("429", null, HttpStatusCode.TooManyRequests));

        Assert.Equal(ApiErrorMessages.RateLimitMessage, message);
    }

    [Fact]
    public void IsRateLimitException_ReturnsTrue_ForSharedRateLimitMessage()
    {
        var exception = new InvalidOperationException(ApiErrorMessages.RateLimitMessage + " extra");

        Assert.True(ApiErrorClassifier.IsRateLimitException(exception));
    }
}