using System.Reflection;
using MarketMonitor.Services;

namespace MarketMonitorTest;

public class ApiServiceTest
{
    /// <summary>
    /// 4桁の銘柄コード入力時に .T が補完されることをテスト。
    /// 期待値: 7203.T
    /// </summary>
    [Fact]
    public void NormalizeSymbolInput_AppendsTokyoSuffix_ForFourDigitCode()
    {
        // Arrange
        var method = GetNormalizeSymbolInputMethod();

        // Act
        var result = (string)method.Invoke(null, new object[] { "7203" })!;

        // Assert
        Assert.Equal("7203.T", result);
    }

    /// <summary>
    /// 銘柄名入力時に対応シンボルへ変換されることをテスト。
    /// 期待値: 9984.T
    /// </summary>
    [Fact]
    public void NormalizeSymbolInput_ResolvesAlias_ForJapaneseName()
    {
        // Arrange
        var method = GetNormalizeSymbolInputMethod();

        // Act
        var result = (string)method.Invoke(null, new object[] { "ソフトバンク" })!;

        // Assert
        Assert.Equal("9984.T", result);
    }

    /// <summary>
    /// 空入力時に既定値IBMへフォールバックすることをテスト。
    /// 期待値: IBM
    /// </summary>
    [Fact]
    public void NormalizeSymbolInput_FallsBackToIbm_ForEmptyInput()
    {
        // Arrange
        var method = GetNormalizeSymbolInputMethod();

        // Act
        var result = (string)method.Invoke(null, new object[] { " " })!;

        // Assert
        Assert.Equal("IBM", result);
    }

    /// <summary>
    /// 東証シンボルをStooq用シンボルへ変換できることをテスト。
    /// 期待値: 9984.jp
    /// </summary>
    [Fact]
    public void ConvertTokyoSymbolToStooqSymbol_ConvertsSuffix()
    {
        // Arrange
        var method = typeof(ApiService).GetMethod(
            "ConvertTokyoSymbolToStooqSymbol",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = (string)method!.Invoke(null, new object[] { "9984.T" })!;

        // Assert
        Assert.Equal("9984.jp", result);
    }

    /// <summary>
    /// StooqのCSVから終値を取得できることをテスト。
    /// 期待値: true および 9473.0
    /// </summary>
    [Fact]
    public void TryParseStooqClosePrice_ReturnsTrue_ForValidCsv()
    {
        // Arrange
        var method = typeof(ApiService).GetMethod(
            "TryParseStooqClosePrice",
            BindingFlags.NonPublic | BindingFlags.Static);
        var args = new object[]
        {
            "Symbol,Date,Time,Open,High,Low,Close,Volume\n9984.jp,2026-04-04,15:00:00,9400,9500,9380,9473,12000000",
            0m
        };

        // Act
        var success = (bool)method!.Invoke(null, args)!;
        var parsed = (decimal)args[1];

        // Assert
        Assert.True(success);
        Assert.Equal(9473m, parsed);
    }

    /// <summary>
    /// StooqのCSV終値がN/Dのとき失敗扱いになることをテスト。
    /// 期待値: false
    /// </summary>
    [Fact]
    public void TryParseStooqClosePrice_ReturnsFalse_ForNdClose()
    {
        // Arrange
        var method = typeof(ApiService).GetMethod(
            "TryParseStooqClosePrice",
            BindingFlags.NonPublic | BindingFlags.Static);
        var args = new object[]
        {
            "Symbol,Date,Time,Open,High,Low,Close,Volume\n9984.jp,2026-04-04,15:00:00,9400,9500,9380,N/D,12000000",
            0m
        };

        // Act
        var success = (bool)method!.Invoke(null, args)!;

        // Assert
        Assert.False(success);
    }

    /// <summary>
    /// StooqのヘッダーなしCSVから終値を取得できることをテスト。
    /// 期待値: true および 4711
    /// </summary>
    [Fact]
    public void TryParseStooqClosePrice_ReturnsTrue_ForHeaderlessCsv()
    {
        // Arrange
        var method = typeof(ApiService).GetMethod(
            "TryParseStooqClosePrice",
            BindingFlags.NonPublic | BindingFlags.Static);
        var args = new object[]
        {
            "P,20260402,080000,4799,4876,4671,4711,34583700,",
            0m
        };

        // Act
        var success = (bool)method!.Invoke(null, args)!;
        var parsed = (decimal)args[1];

        // Assert
        Assert.True(success);
        Assert.Equal(4711m, parsed);
    }

    private static MethodInfo GetNormalizeSymbolInputMethod()
    {
        var method = typeof(ApiService).GetMethod(
            "NormalizeSymbolInput",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        return method!;
    }
}
