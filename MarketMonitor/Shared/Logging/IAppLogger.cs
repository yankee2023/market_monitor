namespace MarketMonitor.Shared.Logging;

/// <summary>
/// アプリケーション内のログ出力を抽象化する。
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// 情報ログを出力する。
    /// </summary>
    /// <param name="message">出力内容。</param>
    void Info(string message);

    /// <summary>
    /// 例外付きエラーログを出力する。
    /// </summary>
    /// <param name="exception">発生した例外。</param>
    /// <param name="message">補足メッセージ。</param>
    void LogError(Exception exception, string message);
}