using System.Windows.Input;

namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// 非同期処理向けの簡易コマンドを提供する。
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _executeAsync;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    /// <summary>
    /// コマンドを初期化する。
    /// </summary>
    /// <param name="executeAsync">実行処理。</param>
    /// <param name="canExecute">実行可否判定。</param>
    public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke() ?? true);
    }

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await _executeAsync();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// 実行可否状態の再評価を通知する。
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}