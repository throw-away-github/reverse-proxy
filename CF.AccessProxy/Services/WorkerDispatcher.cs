using System.Collections.Concurrent;

namespace CF.AccessProxy.Services;

internal sealed class WorkDispatcher<TKey> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Task> _workers = new();

    public bool ContainsKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _workers.ContainsKey(key);
    }

    public async Task ScheduleAsync<TState>(TKey key, TState state, Func<TKey, TState, Task> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);

        while (true)
        {
            if (_workers.TryGetValue(key, out var task))
            {
                await task;
                return;
            }

            // This is the task that we'll return to all waiters. We'll complete it when the factory is complete
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            if (_workers.TryAdd(key, tcs.Task))
            {
                try
                {
                    await valueFactory(key, state);
                    tcs.TrySetResult();
                    await tcs.Task;
                    return;
                }
                catch (Exception ex)
                {
                    // Make sure all waiters see the exception
                    tcs.SetException(ex);

                    throw;
                }
                finally
                {
                    // We remove the entry if the factory failed so it's not a permanent failure
                    // and future gets can retry (this could be a pluggable policy)
                    _workers.TryRemove(key, out _);
                }
            }
        }
    }
}

