using System.IO;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// レート制限を考慮した HTTP 取得処理の抽象を表す。
/// </summary>
public interface IRateLimitedHttpService
{
    /// <summary>
    /// 文字列レスポンスを取得する。
    /// </summary>
    Task<string> GetStringAsync(string requestUri, string sourceName, CancellationToken cancellationToken);

    /// <summary>
    /// シーク可能なストリームレスポンスを取得する。
    /// </summary>
    Task<Stream> GetStreamAsync(string requestUri, string sourceName, CancellationToken cancellationToken);
}