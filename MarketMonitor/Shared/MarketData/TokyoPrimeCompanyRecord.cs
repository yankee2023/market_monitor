namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// JPX 上場銘柄一覧の 1 行分を表す。
/// </summary>
internal readonly record struct TokyoPrimeCompanyRecord(string? Code, string? CompanyName, string? MarketCategory);