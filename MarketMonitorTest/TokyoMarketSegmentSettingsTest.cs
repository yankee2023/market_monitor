using System.IO;
using MarketMonitor.Shared.MarketData;

namespace MarketMonitorTest;

/// <summary>
/// 市場区分設定読込とポリシー適用を検証するテストクラス。
/// </summary>
public sealed class TokyoMarketSegmentSettingsTest
{
    [Fact]
    public void LoadSupportedSegments_ReadsConfiguredSegments()
    {
        var filePath = CreateSettingsFile("""
            {
              "supportedSegments": ["Prime", "Growth"]
            }
            """);
        var provider = new JsonTokyoMarketSegmentSettingsProvider(filePath);

        var result = provider.LoadSupportedSegments();

        Assert.Equal(2, result.Count);
        Assert.Contains(TokyoMarketSegment.Prime, result);
        Assert.Contains(TokyoMarketSegment.Growth, result);
        Assert.DoesNotContain(TokyoMarketSegment.Standard, result);
    }

    [Fact]
    public void LoadSupportedSegments_Throws_WhenUnsupportedValueExists()
    {
        var filePath = CreateSettingsFile("""
            {
              "supportedSegments": ["Prime", "ETF"]
            }
            """);
        var provider = new JsonTokyoMarketSegmentSettingsProvider(filePath);

        Assert.Throws<InvalidDataException>(() => provider.LoadSupportedSegments());
    }

    [Fact]
    public void ConfigurablePolicy_IncludesOnlyConfiguredSegments()
    {
        var policy = new ConfigurableTokyoMarketSegmentPolicy([TokyoMarketSegment.Standard]);

        Assert.True(policy.Includes(TokyoMarketSegment.Standard));
        Assert.False(policy.Includes(TokyoMarketSegment.Prime));
        Assert.False(policy.Includes(TokyoMarketSegment.Growth));
    }

    private static string CreateSettingsFile(string json)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"tokyo-market-settings-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, json);
        return filePath;
    }
}