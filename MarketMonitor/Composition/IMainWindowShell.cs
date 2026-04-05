namespace MarketMonitor.Composition;

/// <summary>
/// アプリケーションのメインウィンドウとして扱う最小契約を表す。
/// </summary>
internal interface IMainWindowShell
{
    /// <summary>
    /// バインド対象のデータコンテキスト。
    /// </summary>
    object? DataContext { get; }

    /// <summary>
    /// ウィンドウを表示する。
    /// </summary>
    void Show();
}