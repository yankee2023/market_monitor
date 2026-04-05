namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 東証プライム銘柄のシンボル解決抽象を表す。
/// </summary>
public interface ITokyoPrimeSymbolResolver
{
    /// <summary>
    /// 入力文字列から東証プライム銘柄シンボルを解決する。
    /// </summary>
    Task<string?> ResolveAsync(string input, CancellationToken cancellationToken);

    /// <summary>
    /// 入力文字列またはシンボルから東証プライム銘柄名を解決する。
    /// </summary>
    Task<string?> ResolveCompanyNameAsync(string input, CancellationToken cancellationToken);
}