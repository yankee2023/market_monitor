using System;
using System.Threading.Tasks;
using MarketMonitor.Shared.Infrastructure;
using Xunit;

namespace MarketMonitorTest;

/// <summary>
/// AsyncRelayCommand の実行状態遷移と実行可否判定を検証するテスト。
/// </summary>
public sealed class AsyncRelayCommandTest
{
    /// <summary>
    /// 実行開始から完了までに CanExecuteChanged が発火し、完了後に実行可能へ戻ることを確認する。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RaisesCanExecuteChangedAroundExecution()
    {
        var notifications = 0;
        var completionSource = new TaskCompletionSource();
        var canExecuteRestored = new TaskCompletionSource();
        var command = new AsyncRelayCommand(async () =>
        {
            await Task.Yield();
            completionSource.SetResult();
        });
        command.CanExecuteChanged += (_, _) =>
        {
            notifications++;
            if (command.CanExecute(null))
            {
                canExecuteRestored.TrySetResult();
            }
        };

        command.Execute(null);
        await completionSource.Task;
        await canExecuteRestored.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(notifications >= 1);
        Assert.True(command.CanExecute(null));
    }

    /// <summary>
    /// 実行可否デリゲートが false を返す場合、CanExecute が false になることを確認する。
    /// </summary>
    [Fact]
    public void CanExecute_WhenPredicateReturnsFalse_ReturnsFalse()
    {
        var command = new AsyncRelayCommand(() => Task.CompletedTask, () => false);

        var result = command.CanExecute(null);

        Assert.False(result);
    }

    /// <summary>
    /// Execute 呼び出しが同期的な例外を投げずに処理開始できることを確認する。
    /// </summary>
    [Fact]
    public void Execute_DoesNotThrowSynchronously()
    {
        var completed = false;
        var command = new AsyncRelayCommand(() =>
        {
            completed = true;
            return Task.CompletedTask;
        });

        command.Execute(null);

        Assert.True(completed);
    }
}