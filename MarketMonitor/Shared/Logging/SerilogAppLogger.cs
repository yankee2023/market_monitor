using Serilog;

namespace MarketMonitor.Shared.Logging;

/// <summary>
/// Serilog を利用したロガー実装。
/// </summary>
public sealed class SerilogAppLogger : IAppLogger
{
    /// <inheritdoc />
    public void Info(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Log.Information(message);
    }

    /// <inheritdoc />
    public void LogError(Exception exception, string message)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (string.IsNullOrWhiteSpace(message))
        {
            Log.Error(exception, "アプリケーションエラーが発生しました。");
            return;
        }

        Log.Error(exception, message);
    }
}