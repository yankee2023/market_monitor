using System.IO;
using System.Text;
using ExcelDataReader;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// JPX の Excel ファイルから東証プライム銘柄レコードを読み取る。
/// </summary>
internal sealed class JpxExcelCompanyRecordReader : ITokyoPrimeCompanyRecordReader
{
    /// <inheritdoc />
    public IReadOnlyList<TokyoPrimeCompanyRecord> Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var reader = ExcelReaderFactory.CreateBinaryReader(stream);
        var records = new List<TokyoPrimeCompanyRecord>();
        var isHeaderRow = true;

        while (reader.Read())
        {
            if (isHeaderRow)
            {
                isHeaderRow = false;
                continue;
            }

            records.Add(new TokyoPrimeCompanyRecord(
                reader.GetValue(1)?.ToString(),
                reader.GetValue(2)?.ToString(),
                reader.GetValue(3)?.ToString()));
        }

        return records;
    }
}