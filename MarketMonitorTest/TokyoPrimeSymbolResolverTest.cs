using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MarketMonitor.Shared.MarketData;
using Xunit;

namespace MarketMonitorTest;

public sealed class TokyoPrimeSymbolResolverTest
{
    public TokyoPrimeSymbolResolverTest()
    {
        TokyoPrimeSymbolResolver.ResetCache();
    }

    [Fact]
    public async Task ResolveAsync_ExactJapaneseName_ReturnsSymbol()
    {
        var resolver = new TokyoPrimeSymbolResolver(_ => Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["トヨタ自動車"] = "7203.T",
                ["トヨタ"] = "7203.T"
            }));

        var result = await resolver.ResolveAsync("トヨタ自動車", CancellationToken.None);

        Assert.Equal("7203.T", result);
    }

    [Fact]
    public async Task ResolveAsync_NameWithKabushikiKaishaPrefix_ReturnsSymbol()
    {
        var resolver = new TokyoPrimeSymbolResolver(_ => Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ソニーグループ"] = "6758.T"
            }));

        var result = await resolver.ResolveAsync("株式会社ソニーグループ", CancellationToken.None);

        Assert.Equal("6758.T", result);
    }

    [Fact]
    public async Task ResolveAsync_UnknownName_ReturnsNull()
    {
        var resolver = new TokyoPrimeSymbolResolver(_ => Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["任天堂"] = "7974.T"
            }));

        var result = await resolver.ResolveAsync("存在しない会社", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCompanyNameAsync_CodeInput_ReturnsCompanyName()
    {
        var resolver = new TokyoPrimeSymbolResolver(_ => Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["トヨタ自動車"] = "7203.T",
                ["トヨタ"] = "7203.T"
            }));

        var result = await resolver.ResolveCompanyNameAsync("7203", CancellationToken.None);

        Assert.Equal("トヨタ", result);
    }

    [Fact]
    public void CreateLookupKeys_ReturnsTrimmedAndNormalizedKeys()
    {
        var keys = TokyoPrimeSymbolResolver.CreateLookupKeys(" 株式会社 三菱・商事 ").ToArray();

        Assert.Contains("株式会社 三菱・商事", keys);
        Assert.Contains("三菱商事", keys);
    }

    [Fact]
    public void BuildSymbolsByName_FiltersPrimeRows_AndBuildsAliases()
    {
        var result = TokyoPrimeSymbolResolver.BuildSymbolsByName(
        [
            new TokyoPrimeCompanyRecord("8058", "三菱商事", "プライム（内国株式）"),
            new TokyoPrimeCompanyRecord("9999", "対象外", "スタンダード（内国株式）"),
            new TokyoPrimeCompanyRecord(null, "欠損", "プライム（内国株式）")
        ]);

        Assert.Equal("8058.T", result["三菱商事"]);
        Assert.Equal("8058.T", result["株式会社三菱商事".Replace("株式会社", string.Empty, StringComparison.Ordinal)]);
        Assert.DoesNotContain("対象外", result.Keys);
    }

    [Fact]
    public async Task ResolveAsync_DownloadsAndCachesSymbols_WhenLoaderIsNotInjected()
    {
        var httpService = new FakeHttpService();
        var recordReader = new FakeRecordReader(
        [
            new TokyoPrimeCompanyRecord("8058", "三菱商事", "プライム（内国株式）")
        ]);
        var resolver = new TokyoPrimeSymbolResolver(httpService, recordReader);

        var first = await resolver.ResolveAsync("三菱商事", CancellationToken.None);
        var second = await resolver.ResolveAsync("三菱商事", CancellationToken.None);

        Assert.Equal("8058.T", first);
        Assert.Equal("8058.T", second);
        Assert.Equal(1, httpService.GetStreamCalls);
        Assert.Equal(1, recordReader.ReadCalls);
    }

    private sealed class FakeHttpService : IRateLimitedHttpService
    {
        public int GetStreamCalls { get; private set; }

        public Task<string> GetStringAsync(string requestUri, string sourceName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Stream> GetStreamAsync(string requestUri, string sourceName, CancellationToken cancellationToken)
        {
            GetStreamCalls++;
            return Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));
        }
    }

    private sealed class FakeRecordReader : ITokyoPrimeCompanyRecordReader
    {
        private readonly IReadOnlyList<TokyoPrimeCompanyRecord> _records;

        public FakeRecordReader(IReadOnlyList<TokyoPrimeCompanyRecord> records)
        {
            _records = records;
        }

        public int ReadCalls { get; private set; }

        public IReadOnlyList<TokyoPrimeCompanyRecord> Read(Stream stream)
        {
            ReadCalls++;
            return _records;
        }
    }
}