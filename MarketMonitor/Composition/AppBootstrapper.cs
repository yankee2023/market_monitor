using System.IO;
using Microsoft.Extensions.DependencyInjection;
using MarketMonitor.Features.Dashboard.Services;
using MarketMonitor.Features.Dashboard.ViewModels;
using MarketMonitor.Features.JapaneseStockChart.Services;
using MarketMonitor.Features.MarketSnapshot.Services;
using MarketMonitor.Features.PriceHistory.Services;
using MarketMonitor.Features.SectorComparison.Services;
using MarketMonitor.Shared.Infrastructure;
using MarketMonitor.Shared.Logging;
using MarketMonitor.Shared.MarketData;

namespace MarketMonitor.Composition;

/// <summary>
/// アプリケーション起動時のオブジェクト構成を組み立てる。
/// </summary>
internal static class AppBootstrapper
{
    private static readonly Lazy<ServiceProvider> RootServiceProvider = new(CreateServiceProvider);

    /// <summary>
    /// メイン画面用の ViewModel を生成する。
    /// </summary>
    /// <returns>初期化済みの ViewModel。</returns>
    internal static MainViewModel CreateMainViewModel()
    {
        return RootServiceProvider.Value.GetRequiredService<MainViewModel>();
    }

    /// <summary>
    /// メイン画面を生成する。
    /// </summary>
    /// <returns>初期化済みのメインウィンドウ。</returns>
    public static MainWindow CreateMainWindow()
    {
        return RootServiceProvider.Value.GetRequiredService<MainWindow>();
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = false
        });
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAppLogger, SerilogAppLogger>();
        services.AddSingleton<IRateLimitedHttpService, RateLimitedHttpService>();
        services.AddSingleton<MarketDataCache>();
        services.AddSingleton<ITokyoListedCompanyRecordReader, JpxExcelCompanyRecordReader>();
        services.AddSingleton<ITokyoMarketSegmentPolicy>(serviceProvider =>
            CreateMarketSegmentPolicy(serviceProvider.GetRequiredService<IAppLogger>()));
        services.AddSingleton<ITokyoListedSymbolResolver, TokyoListedSymbolResolver>();
        services.AddSingleton<MarketSymbolResolver>();
        services.AddSingleton<IMarketSnapshotService, MarketSnapshotService>();
        services.AddSingleton<IPriceHistoryRepository, SqlitePriceHistoryRepository>();
        services.AddSingleton<IPriceHistoryFeatureService, PriceHistoryFeatureService>();
        services.AddSingleton<IJapaneseCandleService, JapaneseCandleService>();
        services.AddSingleton<IJapaneseStockChartFeatureService, JapaneseStockChartFeatureService>();
        services.AddSingleton<ISectorComparisonFeatureService, SectorComparisonFeatureService>();
        services.AddSingleton<IChartIndicatorSelectionService, ChartIndicatorSelectionService>();
        services.AddTransient<IDesktopNotificationService, WindowsDesktopNotificationService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<IMainWindowViewModel>(serviceProvider => serviceProvider.GetRequiredService<MainViewModel>());
        services.AddTransient<MainWindow>(serviceProvider =>
            new MainWindow(serviceProvider.GetRequiredService<IMainWindowViewModel>()));
    }

    private static ITokyoMarketSegmentPolicy CreateMarketSegmentPolicy(IAppLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var settingsPath = Path.Combine(AppContext.BaseDirectory, "market-settings.json");

        try
        {
            var settingsProvider = new JsonTokyoMarketSegmentSettingsProvider(settingsPath);
            var supportedSegments = settingsProvider.LoadSupportedSegments();
            logger.Info($"市場区分設定を読み込みました。Segments={string.Join(",", supportedSegments)}");
            return new ConfigurableTokyoMarketSegmentPolicy(supportedSegments);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            logger.LogError(ex, $"市場区分設定の読込に失敗したため既定設定を使用します。Path={settingsPath}");
            return new TokyoMainMarketSegmentPolicy();
        }
    }
}
