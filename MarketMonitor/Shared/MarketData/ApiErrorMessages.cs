namespace MarketMonitor.Shared.MarketData;

/// <summary>
/// データ取得まわりの共通エラーメッセージを定義する。
/// </summary>
public static class ApiErrorMessages
{
    /// <summary>
    /// レート制限到達時の利用者向けメッセージ。
    /// </summary>
    public const string RateLimitMessage = "データ提供元のアクセス上限に達しました。しばらく待って再試行してください。";

    /// <summary>
    /// 東証対象外の入力に対する利用者向けメッセージ。
    /// </summary>
    public const string TokyoListedOnlyMessage = "東証銘柄のコードまたは銘柄名を入力してください。";
}