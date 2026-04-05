namespace MarketMonitor.Composition;

/// <summary>
/// メインウィンドウ初期化時に必要な ViewModel 契約を表す。
/// </summary>
internal interface IMainWindowViewModel
{
    /// <summary>
    /// 初期表示に必要な非同期処理を実行する。
    /// </summary>
    Task InitializeAsync();
}