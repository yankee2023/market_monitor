using Serilog;

namespace MarketMonitor.Services;

/// <summary>
/// Serilogを利用したログ実装。
/// </summary>
public sealed class SerilogAppLogger : IAppLogger
{
    /// <inheritdoc />
    public void Info(string message)
    {
        Log.Information(message);
    }

    /// <inheritdoc />
    public void Error(Exception exception, string message)
    {
        Log.Error(exception, message);
    }
}
