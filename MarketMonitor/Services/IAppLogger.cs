namespace MarketMonitor.Services;

/// <summary>
/// アプリケーションログ出力の抽象。
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// 情報ログを出力する。
    /// </summary>
    void Info(string message);

    /// <summary>
    /// エラーログを出力する。
    /// </summary>
    void Error(Exception exception, string message);
}
