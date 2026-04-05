using System.IO;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// JPX 上場銘柄ファイルから銘柄レコードを読み取る抽象を表す。
/// </summary>
internal interface ITokyoListedCompanyRecordReader
{
    /// <summary>
    /// ストリームから銘柄レコードを読み取る。
    /// </summary>
    IReadOnlyList<TokyoListedCompanyRecord> Read(Stream stream);
}