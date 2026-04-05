using System.IO;
using System.Text.Json;

namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// JSON 設定ファイルから市場区分設定を読み込む。
/// </summary>
public sealed class JsonTokyoMarketSegmentSettingsProvider : ITokyoMarketSegmentSettingsProvider
{
    private const string SupportedSegmentsPropertyName = "supportedSegments";

    private readonly string _settingsFilePath;

    /// <summary>
    /// 設定プロバイダーを初期化する。
    /// </summary>
    public JsonTokyoMarketSegmentSettingsProvider(string settingsFilePath)
    {
        if (string.IsNullOrWhiteSpace(settingsFilePath))
        {
            throw new ArgumentException("設定ファイルパスは必須です。", nameof(settingsFilePath));
        }

        _settingsFilePath = settingsFilePath;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<TokyoMarketSegment> LoadSupportedSegments()
    {
        if (!File.Exists(_settingsFilePath))
        {
            throw new FileNotFoundException("市場区分設定ファイルが見つかりません。", _settingsFilePath);
        }

        using var stream = File.OpenRead(_settingsFilePath);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty(SupportedSegmentsPropertyName, out var supportedSegmentsElement)
            || supportedSegmentsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("supportedSegments 配列が定義されていません。");
        }

        var supportedSegments = new HashSet<TokyoMarketSegment>();
        foreach (var element in supportedSegmentsElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException("supportedSegments は文字列配列で指定してください。");
            }

            var value = element.GetString();
            if (!TokyoMarketSegmentParser.TryParseValue(value, out var marketSegment))
            {
                throw new InvalidDataException($"未対応の市場区分が設定されています: {value}");
            }

            supportedSegments.Add(marketSegment);
        }

        if (supportedSegments.Count == 0)
        {
            throw new InvalidDataException("supportedSegments には少なくとも 1 つの市場区分が必要です。");
        }

        return supportedSegments.ToArray();
    }
}