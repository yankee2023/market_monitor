using System.Reflection;
using MarketMonitor.Shared.MarketData;

namespace MarketMonitorTest;

/// <summary>
/// MarketSymbolResolver と TokyoListedSymbolResolver の補助ロジックを検証するテストクラス。
/// </summary>
public class MarketSymbolResolverTest
{
    /// <summary>
    /// 4 桁の銘柄コード入力時に .T が補完されることをテスト。
    /// 期待値: 7203.T。
    /// </summary>
    [Fact]
    public void NormalizeSymbolInput_AppendsTokyoSuffix_ForFourDigitCode()
    {
        // Arrange
        var method = typeof(MarketSymbolResolver).GetMethod(
            "NormalizeSymbolInput",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = (string)method!.Invoke(null, ["7203"])!;

        // Assert
        Assert.Equal("7203.T", result);
    }

    /// <summary>
    /// 銘柄名入力時に対応シンボルへ変換されることをテスト。
    /// 期待値: 9984.T。
    /// </summary>
    [Fact]
    public void NormalizeSymbolInput_ResolvesAlias_ForJapaneseName()
    {
        // Arrange
        var method = typeof(MarketSymbolResolver).GetMethod(
            "NormalizeSymbolInput",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = (string)method!.Invoke(null, ["ソフトバンク"])!;

        // Assert
        Assert.Equal("9984.T", result);
    }

    /// <summary>
    /// 会社名の正規化キーから一意に東証銘柄を解決できることをテスト。
    /// 期待値: 8058.T。
    /// </summary>
    [Fact]
    public void FindSymbol_ReturnsSymbol_ForUniquePartialMatch()
    {
        // Arrange
        var method = typeof(TokyoListedSymbolResolver).GetMethod(
            "FindSymbol",
            BindingFlags.NonPublic | BindingFlags.Static);
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["三菱商事"] = "8058.T",
            ["トヨタ自動車"] = "7203.T"
        };

        // Act
        var result = (string?)method!.Invoke(null, ["三菱商", lookup]);

        // Assert
        Assert.Equal("8058.T", result);
    }

    /// <summary>
    /// 株式会社や記号を除去して正規化できることをテスト。
    /// 期待値: 三菱商事。
    /// </summary>
    [Fact]
    public void NormalizeCompanyName_RemovesCorporationAndSymbols()
    {
        // Arrange
        var method = typeof(TokyoListedSymbolResolver).GetMethod(
            "NormalizeCompanyName",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = (string)method!.Invoke(null, ["株式会社 三菱・商事-"])!;

        // Assert
        Assert.Equal("三菱商事", result);
    }

    /// <summary>
    /// 一意に解決できない部分一致は null を返すことをテスト。
    /// 期待値: null。
    /// </summary>
    [Fact]
    public void FindSymbol_ReturnsNull_WhenMultipleCandidatesExist()
    {
        // Arrange
        var method = typeof(TokyoListedSymbolResolver).GetMethod(
            "FindSymbol",
            BindingFlags.NonPublic | BindingFlags.Static);
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["三菱商事"] = "8058.T",
            ["三菱重工業"] = "7011.T"
        };

        // Act
        var result = (string?)method!.Invoke(null, ["三菱", lookup]);

        // Assert
        Assert.Null(result);
    }
}