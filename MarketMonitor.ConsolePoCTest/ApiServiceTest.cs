using MarketMonitor.ConsolePoC;

namespace MarketMonitor.ConsolePoC.Tests;

/// <summary>
/// Console PoC 用 ApiService の補助ロジックを検証するテストクラス。
/// </summary>
public class ApiServiceTest
{
    /// <summary>
    /// 4 桁コードに .T が補完されることをテスト。
    /// 期待値: 7203.T。
    /// </summary>
    [Fact]
    public void NormalizeSymbol_AppendsTokyoSuffix_ForFourDigitCode()
    {
        // Act
        var result = GetNormalizeSymbolMethod().Invoke(null, ["7203"]);

        // Assert
        Assert.Equal("7203.T", Assert.IsType<string>(result));
    }

    /// <summary>
    /// 日本株のシンボルに対して true を返すことをテスト。
    /// 期待値: true。
    /// </summary>
    [Fact]
    public void IsJapaneseStock_ReturnsTrue_ForJapaneseSymbol()
    {
        // Act
        var result = GetIsJapaneseStockMethod().Invoke(null, ["9984.T"]);

        // Assert
        Assert.True(Assert.IsType<bool>(result));
    }

    /// <summary>
    /// .T が付いていない入力に対して false を返すことをテスト。
    /// 期待値: false。
    /// </summary>
    [Fact]
    public void IsJapaneseStock_ReturnsFalse_ForCodeWithoutSuffix()
    {
        // Act
        var result = GetIsJapaneseStockMethod().Invoke(null, ["7203"]);

        // Assert
        Assert.False(Assert.IsType<bool>(result));
    }

    private static System.Reflection.MethodInfo GetNormalizeSymbolMethod()
    {
        var method = typeof(ApiService).GetMethod(
            "NormalizeSymbol",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
        return method!;
    }

    private static System.Reflection.MethodInfo GetIsJapaneseStockMethod()
    {
        var method = typeof(ApiService).GetMethod(
            "IsJapaneseStock",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
        return method!;
    }
}