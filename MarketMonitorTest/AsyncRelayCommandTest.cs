using System;
using System.Threading.Tasks;
using MarketMonitor.Shared.Infrastructure;
using Xunit;

namespace MarketMonitorTest;

public sealed class AsyncRelayCommandTest
{
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

    [Fact]
    public void CanExecute_WhenPredicateReturnsFalse_ReturnsFalse()
    {
        var command = new AsyncRelayCommand(() => Task.CompletedTask, () => false);

        var result = command.CanExecute(null);

        Assert.False(result);
    }

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