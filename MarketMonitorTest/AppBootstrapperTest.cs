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
}