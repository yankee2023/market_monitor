using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MarketMonitor.Shared.MarketData;
using Xunit;

namespace MarketMonitorTest;

public sealed class TokyoListedSymbolResolverTest
{
    public TokyoListedSymbolResolverTest()
    {
        TokyoListedSymbolResolver.ResetCache();
    }

    [Fact]
    public async Task ResolveAsync_ExactJapaneseName_ReturnsSymbol()
    {
        var resolver = new TokyoListedSymbolResolver(_ => Task.FromResult<IReadOnlyDictionary<string, string>>(
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
        var resolver = new TokyoListedSymbolResolver(_ => Task.FromResult<IReadOnlyDictionary<string, string>>(
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
        var resolver = new TokyoListedSymbolResolver(_ => Task.FromResult<IReadOnlyDictionary<string, string>>(
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
        var resolver = new TokyoListedSymbolResolver(_ => Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["トヨタ自動車"] = "7203.T",
                ["トヨタ"] = "7203.T"
            }));

        var result = await resolver.ResolveCompanyNameAsync("7203", CancellationToken.None);

        Assert.Equal("トヨタ", result);
    }

    [Fact]
    public async Task ResolveMarketSegmentAsync_CodeInput_ReturnsMarketSegment()
    {
        var httpService = new FakeHttpService();
        var recordReader = new FakeRecordReader(
        [
            new TokyoListedCompanyRecord("8058", "三菱商事", "プライム（内国株式）", "卸売業")
        ]);
        var resolver = new TokyoListedSymbolResolver(httpService, recordReader, new TokyoMainMarketSegmentPolicy());

        var result = await resolver.ResolveMarketSegmentAsync("8058", CancellationToken.None);

        Assert.Equal(TokyoMarketSegment.Prime, result);
    }

    [Fact]
    public void CreateLookupKeys_ReturnsTrimmedAndNormalizedKeys()
    {
        var keys = TokyoListedSymbolResolver.CreateLookupKeys(" 株式会社 三菱・商事 ").ToArray();

        Assert.Contains("株式会社 三菱・商事", keys);
        Assert.Contains("三菱商事", keys);
    }

    [Fact]
    public void BuildSymbolsByName_IncludesPrimeStandardGrowth_AndExcludesOthers()
    {
        var result = TokyoListedSymbolResolver.BuildSymbolsByName(
        [
            new TokyoListedCompanyRecord("8058", "三菱商事", "プライム（内国株式）", "卸売業"),
            new TokyoListedCompanyRecord("278A", "テスト成長", "グロース（内国株式）", "情報・通信業"),
            new TokyoListedCompanyRecord("9999", "対象スタンダード", "スタンダード（内国株式）", "卸売業"),
            new TokyoListedCompanyRecord("1306", "ETF対象外", "ETF・ETN", "-"),
            new TokyoListedCompanyRecord(null, "欠損", "プライム（内国株式）", "卸売業")
        ]);

        Assert.Equal("8058.T", result["三菱商事"]);
        Assert.Equal("278A.T", result["テスト成長"]);
        Assert.Equal("9999.T", result["対象スタンダード"]);
        Assert.DoesNotContain("ETF対象外", result.Keys);
    }

    [Fact]
    public async Task ResolveAsync_DownloadsAndCachesSymbols_WhenLoaderIsNotInjected()
    {
        var httpService = new FakeHttpService();
        var recordReader = new FakeRecordReader(
        [
            new TokyoListedCompanyRecord("8058", "三菱商事", "プライム（内国株式）", "卸売業")
        ]);
        var resolver = new TokyoListedSymbolResolver(httpService, recordReader, new TokyoMainMarketSegmentPolicy());

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

    private sealed class FakeRecordReader : ITokyoListedCompanyRecordReader
    {
        private readonly IReadOnlyList<TokyoListedCompanyRecord> _records;

        public FakeRecordReader(IReadOnlyList<TokyoListedCompanyRecord> records)
        {
            _records = records;
        }

        public int ReadCalls { get; private set; }

        public IReadOnlyList<TokyoListedCompanyRecord> Read(Stream stream)
        {
            ReadCalls++;
            return _records;
        }
    }
}