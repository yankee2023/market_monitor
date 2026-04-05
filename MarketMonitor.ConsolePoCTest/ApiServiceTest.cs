using Xunit;

namespace MarketMonitor.ConsolePoC.Tests;

public class ApiServiceTests
{
    /// <summary>
    /// 日本株のシンボル（.Tで終わる）に対してtrueを返すことをテスト。
    /// 期待値: true
    /// </summary>
    [Fact]
    public void IsJapaneseStock_ReturnsTrue_ForJapaneseSymbol()
    {
        // Arrange
        var apiService = new ApiService(null, "dummy"); // HttpClientはテストで不要

        // Act
        var result = apiService.GetType().GetMethod("IsJapaneseStock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { "9984.T" });

        // Assert
        Assert.True((bool)result);
    }

    /// <summary>
    /// 米国株のシンボルに対してfalseを返すことをテスト。
    /// 期待値: false
    /// </summary>
    [Fact]
    public void IsJapaneseStock_ReturnsFalse_ForUSSymbol()
    {
        // Arrange
        var apiService = new ApiService(null, "dummy");

        // Act
        var result = apiService.GetType().GetMethod("IsJapaneseStock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { "IBM" });

        // Assert
        Assert.False((bool)result);
    }
}
