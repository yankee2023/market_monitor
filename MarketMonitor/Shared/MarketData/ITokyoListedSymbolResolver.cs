namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 東証上場銘柄のシンボル解決抽象を表す。
/// </summary>
public interface ITokyoListedSymbolResolver
{
    /// <summary>
    /// 入力文字列から東証銘柄シンボルを解決する。
    /// </summary>
    Task<string?> ResolveAsync(string input, CancellationToken cancellationToken);

    /// <summary>
    /// 入力文字列またはシンボルから銘柄名を解決する。
    /// </summary>
    Task<string?> ResolveCompanyNameAsync(string input, CancellationToken cancellationToken);

    /// <summary>
    /// 入力文字列またはシンボルからセクター名を解決する。
    /// </summary>
    Task<string?> ResolveSectorNameAsync(string input, CancellationToken cancellationToken);

    /// <summary>
    /// 入力文字列またはシンボルから市場区分を解決する。
    /// </summary>
    Task<TokyoMarketSegment> ResolveMarketSegmentAsync(string input, CancellationToken cancellationToken);

    /// <summary>
    /// 同一セクターの比較対象銘柄を取得する。
    /// </summary>
    Task<IReadOnlyList<TokyoListedSectorPeer>> GetSectorPeersAsync(string input, int maxCount, CancellationToken cancellationToken);
}