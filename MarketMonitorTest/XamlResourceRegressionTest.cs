using System.Windows;
using MarketMonitor;
using MarketMonitor.Composition;

namespace MarketMonitorTest;

/// <summary>
/// XAML の実行時パース退行を検出するテストを表す。
/// </summary>
public sealed class XamlResourceRegressionTest
{
    [Fact]
    public void MainWindowStyles_LoadsMergedResourcesWithoutMissingStaticResource()
    {
        RunInSta(() =>
        {
            var dictionary = (ResourceDictionary)Application.LoadComponent(
                new Uri("/TokyoMarketTechnical;component/Resources/MainWindowStyles.xaml", UriKind.Relative));

            Assert.NotNull(dictionary["SidebarSummaryTemplate"]);
            Assert.NotNull(dictionary["SectorComparisonItemTemplate"]);
            Assert.NotNull(dictionary["SidebarCardBorderStyle"]);
        });
    }

    [Fact]
    public void MainWindow_CanBeConstructedWithoutXamlParseException()
    {
        RunInSta(() =>
        {
            var viewModel = AppBootstrapper.CreateMainViewModel();
            var window = new MainWindow(viewModel);

            Assert.NotNull(window);
            Assert.NotNull(window.FindResource("SidebarSummaryTemplate"));
            Assert.Equal(WindowStartupLocation.Manual, window.WindowStartupLocation);

            window.Close();
            viewModel.Dispose();
        });
    }

    [Fact]
    public void CalculateStartupBounds_ConstrainsWindowIntoWorkArea()
    {
        var workArea = new Rect(0, 0, 1200, 800);

        var bounds = MainWindow.CalculateStartupBounds(workArea, 1400, 900);

        Assert.Equal(1200, bounds.Width);
        Assert.Equal(800, bounds.Height);
        Assert.Equal(0, bounds.Left);
        Assert.Equal(0, bounds.Top);
    }

    private static void RunInSta(Action action)
    {
        Exception? capturedException = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                capturedException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(capturedException);
    }
}