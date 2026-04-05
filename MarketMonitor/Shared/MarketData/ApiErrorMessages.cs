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
    /// 東証プライム外の入力に対する利用者向けメッセージ。
    /// </summary>
    public const string TokyoPrimeOnlyMessage = "東証プライム銘柄のコードまたは銘柄名を入力してください。";
}