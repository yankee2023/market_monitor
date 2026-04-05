using System.Net.Http;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// API 取得系例外の分類と利用者向けメッセージ変換を表す。
/// </summary>
public static class ApiErrorClassifier
{
    /// <summary>
    /// レート制限由来の例外かどうかを判定する。
    /// </summary>
    public static bool IsRateLimitException(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.Message.StartsWith(ApiErrorMessages.RateLimitMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// 利用者向け失敗メッセージを生成する。
    /// </summary>
    public static string CreateUserMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is HttpRequestException { StatusCode: System.Net.HttpStatusCode.TooManyRequests })
        {
            return ApiErrorMessages.RateLimitMessage;
        }

        if (exception is InvalidOperationException invalidOperationException
            && IsRateLimitException(invalidOperationException))
        {
            return ApiErrorMessages.RateLimitMessage;
        }

        return exception.Message;
    }
}