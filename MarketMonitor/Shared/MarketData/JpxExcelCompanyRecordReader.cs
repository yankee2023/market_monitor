using System.IO;
using System.Text;
using ExcelDataReader;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// JPX の Excel ファイルから東証上場銘柄レコードを読み取る。
/// </summary>
internal sealed class JpxExcelCompanyRecordReader : ITokyoListedCompanyRecordReader
{
    /// <inheritdoc />
    public IReadOnlyList<TokyoListedCompanyRecord> Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var reader = ExcelReaderFactory.CreateBinaryReader(stream);
        var records = new List<TokyoListedCompanyRecord>();
        var isHeaderRow = true;

        while (reader.Read())
        {
            if (isHeaderRow)
            {
                isHeaderRow = false;
                continue;
            }

            records.Add(new TokyoListedCompanyRecord(
                reader.GetValue(1)?.ToString(),
                reader.GetValue(2)?.ToString(),
                reader.GetValue(3)?.ToString(),
                reader.GetValue(5)?.ToString()));
        }

        return records;
    }
}