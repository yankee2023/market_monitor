using System.Windows.Input;

namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// 同期処理向けの簡易コマンドを提供する。
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// コマンドを初期化する。
    /// </summary>
    /// <param name="execute">実行処理。</param>
    /// <param name="canExecute">実行可否判定。</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        _execute();
    }

    /// <summary>
    /// 実行可否状態の再評価を通知する。
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}