namespace MarketMonitor.Composition;

/// <summary>
/// メインウィンドウ表示直後の初期化処理を担当する。
/// </summary>
internal static class MainWindowLifecycleService
{
    /// <summary>
    /// ViewModel の初期化処理を実行する。
    /// </summary>
    /// <param name="viewModel">初期化対象の ViewModel。</param>
    internal static Task InitializeAsync(IMainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        return viewModel.InitializeAsync();
    }
}