namespace MarketMonitor.Composition;

/// <summary>
/// アプリケーション起動時と終了時のメインウィンドウ制御をまとめる。
/// </summary>
internal static class AppLifecycleService
{
    /// <summary>
    /// メインウィンドウを生成して表示する。
    /// </summary>
    /// <typeparam name="TWindow">生成するウィンドウ型。</typeparam>
    /// <param name="createMainWindow">ウィンドウ生成処理。</param>
    /// <returns>表示済みのメインウィンドウ。</returns>
    internal static TWindow Start<TWindow>(Func<TWindow> createMainWindow)
        where TWindow : class, IMainWindowShell
    {
        ArgumentNullException.ThrowIfNull(createMainWindow);

        var mainWindow = createMainWindow();
        mainWindow.Show();
        return mainWindow;
    }

    /// <summary>
    /// メインウィンドウに関連するリソースを解放する。
    /// </summary>
    /// <param name="mainWindow">終了対象のメインウィンドウ。</param>
    internal static void Stop(IMainWindowShell? mainWindow)
    {
        if (mainWindow?.DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}