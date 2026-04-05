using System.IO;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// JPX 上場銘柄ファイルから銘柄レコードを読み取る抽象を表す。
/// </summary>
internal interface ITokyoPrimeCompanyRecordReader
{
    /// <summary>
    /// ストリームから銘柄レコードを読み取る。
    /// </summary>
    /// <param name="stream">読み取り対象のストリーム。</param>
    /// <returns>抽出した銘柄レコード一覧。</returns>
    IReadOnlyList<TokyoPrimeCompanyRecord> Read(Stream stream);
}