namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// 東証の市場区分を表す。
/// </summary>
public enum TokyoMarketSegment
{
    /// <summary>
    /// 判定不能。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// プライム市場。
    /// </summary>
    Prime,

    /// <summary>
    /// スタンダード市場。
    /// </summary>
    Standard,

    /// <summary>
    /// グロース市場。
    /// </summary>
    Growth
}

/// <summary>
/// JPX の市場区分表記を内部列挙へ変換する補助を表す。
/// </summary>
public static class TokyoMarketSegmentParser
{
    /// <summary>
    /// JPX の市場区分文字列を列挙へ変換する。
    /// </summary>
    public static TokyoMarketSegment Parse(string? marketCategory)
    {
        if (string.IsNullOrWhiteSpace(marketCategory))
        {
            return TokyoMarketSegment.Unknown;
        }

        if (marketCategory.Contains("プライム", StringComparison.Ordinal))
        {
            return TokyoMarketSegment.Prime;
        }

        if (marketCategory.Contains("スタンダード", StringComparison.Ordinal))
        {
            return TokyoMarketSegment.Standard;
        }

        if (marketCategory.Contains("グロース", StringComparison.Ordinal))
        {
            return TokyoMarketSegment.Growth;
        }

        return TokyoMarketSegment.Unknown;
    }

    /// <summary>
    /// 設定値または JPX 表記から市場区分を判定する。
    /// </summary>
    public static bool TryParseValue(string? value, out TokyoMarketSegment marketSegment)
    {
        marketSegment = TokyoMarketSegment.Unknown;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (Enum.TryParse(value.Trim(), ignoreCase: true, out TokyoMarketSegment parsed)
            && parsed != TokyoMarketSegment.Unknown)
        {
            marketSegment = parsed;
            return true;
        }

        parsed = Parse(value);
        if (parsed == TokyoMarketSegment.Unknown)
        {
            return false;
        }

        marketSegment = parsed;
        return true;
    }

    /// <summary>
    /// 画面表示用の市場区分名へ変換する。
    /// </summary>
    public static string ToDisplayName(TokyoMarketSegment marketSegment)
    {
        return marketSegment switch
        {
            TokyoMarketSegment.Prime => "プライム",
            TokyoMarketSegment.Standard => "スタンダード",
            TokyoMarketSegment.Growth => "グロース",
            _ => "-"
        };
    }
}