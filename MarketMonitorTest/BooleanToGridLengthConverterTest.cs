using System.Globalization;
using System.Windows;
using MarketMonitor.Shared.Infrastructure;

namespace MarketMonitorTest;

/// <summary>
/// GridLength 変換のレイアウト設定を検証するテストを表す。
/// </summary>
public sealed class BooleanToGridLengthConverterTest
{
    [Fact]
    public void Convert_ReturnsStarLength_WhenConfigured()
    {
        var converter = new BooleanToGridLengthConverter
        {
            TrueWidth = 0,
            FalseWidth = 2,
            FalseUnitType = GridUnitType.Star
        };

        var result = (GridLength)converter.Convert(false, typeof(GridLength), string.Empty, CultureInfo.InvariantCulture);

        Assert.Equal(2d, result.Value);
        Assert.Equal(GridUnitType.Star, result.GridUnitType);
    }
}