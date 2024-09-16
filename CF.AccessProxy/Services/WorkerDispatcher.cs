using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace CF.AccessProxy.Services;

internal readonly record struct ScheduledTaskResult(Task Task, bool IsNewTask);

internal sealed class WorkDispatcher<TKey> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Task> _workers = new();

    public bool ContainsKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _workers.ContainsKey(key);
    }

    public ScheduledTaskResult ScheduleAsync<TState>(TKey key, TState state, Func<TKey, TState, Task> taskFactory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ScheduledTaskCompletionSource<TState>? tcs = null;

        while (true)
        {
            if (_workers.TryGetValue(key, out var task))
            {
                return new ScheduledTaskResult(task, false);
            }

            // This is the task that we'll return to all waiters. We'll complete it when the factory is complete
            tcs ??= new ScheduledTaskCompletionSource<TState>(key, state, taskFactory, _workers);
            if (tcs.TrySchedule(out task))
            {
                return new ScheduledTaskResult(task, true);
            }
        }
    }

    private class ScheduledTaskCompletionSource<TState>(
        TKey key, TState state, Func<TKey, TState, Task> taskFactory, ConcurrentDictionary<TKey, Task> workers)
        : TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
    {
        public bool TrySchedule([NotNullWhen(true)] out Task? task)
        {
            if (workers.TryAdd(key, Task))
            {
                task = StartAndAwaitTask();
                return true;
            }

            task = null;
            return false;
        }

        private async Task StartAndAwaitTask()
        {
            try
            {
                await taskFactory(key, state);
                TrySetResult();
                await Task;
            }
            catch (Exception ex)
            {
                // Make sure all waiters see the exception
                TrySetException(ex);
                throw;
            }
            finally
            {
                // We remove the entry if the factory failed so it's not a permanent failure
                // and future gets can retry (this could be a pluggable policy)
                workers.TryRemove(key, out _);
            }
        }
    }
}

