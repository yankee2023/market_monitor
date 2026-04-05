using MarketMonitor.Composition;
using MarketMonitor.Features.Dashboard.ViewModels;
using Xunit;

namespace MarketMonitorTest;

public sealed class AppBootstrapperTest
{
    [Fact]
    public void CreateMainViewModel_ReturnsConfiguredMainViewModel()
    {
        var viewModel = AppBootstrapper.CreateMainViewModel();

        Assert.NotNull(viewModel);
        Assert.IsType<MainViewModel>(viewModel);
        Assert.Equal("7203", ((MainViewModel)viewModel).Symbol);
    }

    [Fact]
    public void CreateMainWindow_ResolvesWindowAndViewModelFromContainer()
    {
        Exception? capturedException = null;

        var thread = new Thread(() =>
        {
            try
            {
                var window = AppBootstrapper.CreateMainWindow();

                Assert.NotNull(window);
                Assert.IsType<MainViewModel>(window.DataContext);

                window.Close();
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