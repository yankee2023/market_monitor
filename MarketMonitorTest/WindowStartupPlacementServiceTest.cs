using System.Windows;
using MarketMonitor.Composition;

namespace MarketMonitorTest;

/// <summary>
/// 起動位置調整サービスを検証するテストを表す。
/// </summary>
public sealed class WindowStartupPlacementServiceTest
{
    [Fact]
    public void CalculateStartupBounds_CentersWindow_WhenItFitsWorkArea()
    {
        var bounds = WindowStartupPlacementService.CalculateStartupBounds(new Rect(100, 50, 1600, 900), 1200, 700);

        Assert.Equal(300, bounds.Left);
        Assert.Equal(150, bounds.Top);
        Assert.Equal(1200, bounds.Width);
        Assert.Equal(700, bounds.Height);
    }
}