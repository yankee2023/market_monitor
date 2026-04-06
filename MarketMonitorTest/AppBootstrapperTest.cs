using MarketMonitor.Composition;
using MarketMonitor.Features.Dashboard.ViewModels;
using Xunit;

namespace MarketMonitorTest;

/// <summary>
/// AppBootstrapper が想定どおりに依存関係を解決できることを検証するテスト。
/// </summary>
public sealed class AppBootstrapperTest
{
    /// <summary>
    /// MainViewModel 生成時に既定シンボルを持つ画面統合 ViewModel が返ることを確認する。
    /// </summary>
    [Fact]
    public void CreateMainViewModel_ReturnsConfiguredMainViewModel()
    {
        var viewModel = AppBootstrapper.CreateMainViewModel();

        Assert.NotNull(viewModel);
        Assert.IsType<MainViewModel>(viewModel);
        Assert.Equal("7203", ((MainViewModel)viewModel).Symbol);
    }

    /// <summary>
    /// STA スレッド上で MainWindow と DataContext が解決できることを確認する。
    /// </summary>
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