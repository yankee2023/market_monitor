using System.IO;
using MarketMonitor.Composition;
using Serilog;

namespace MarketMonitorTest;

/// <summary>
/// AppLoggingConfigurator のログ初期化を検証するテストクラス。
/// </summary>
public sealed class AppLoggingConfiguratorTest : IDisposable
{
    private readonly string _originalDirectory;
    private readonly string _testDirectory;

    public AppLoggingConfiguratorTest()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"marketmonitor-logtest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        Directory.SetCurrentDirectory(_testDirectory);
    }

    /// <summary>
    /// Configure 実行後にログファイル出力先が作成されることをテスト。
    /// 期待値: logs 配下にファイルが生成される。
    /// </summary>
    [Fact]
    public void Configure_CreatesLogFileOutput()
    {
        // Act
        AppLoggingConfigurator.Configure();
        Log.Information("log test message");
        Log.CloseAndFlush();

        // Assert
        var logDirectory = Path.Combine(_testDirectory, "logs");
        Assert.True(Directory.Exists(logDirectory));
        Assert.NotEmpty(Directory.GetFiles(logDirectory, "app-*.log"));
    }

    public void Dispose()
    {
        Log.CloseAndFlush();
        Directory.SetCurrentDirectory(_originalDirectory);

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }

        GC.SuppressFinalize(this);
    }
}