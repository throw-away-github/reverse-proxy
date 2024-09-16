using System.Threading.Channels;
using CF.AccessProxy.Extensions;

namespace CF.AccessProxy.Services;

public class WorkerProcessor<TKey> where TKey : notnull
{
    private readonly WorkDispatcher<TKey> _workDispatcher = new();

    private readonly Channel<Task> _taskChannel = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    private readonly ILogger<WorkerProcessor<TKey>> _logger;
    private readonly Task _worker;

    public WorkerProcessor(ILogger<WorkerProcessor<TKey>> logger)
    {
        _logger = logger;
        _worker = Task.Factory.StartNew(
                state => ((WorkerProcessor<TKey>)state!).ProcessAsync(),
                this,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .Unwrap();
    }

    public Task StopAsync()
    {
        _taskChannel.Writer.TryComplete();
        return _worker.WaitAsync(CancellationToken.None);
    }

    private async Task ProcessAsync()
    {
        await foreach (var task in _taskChannel.Reader.ReadAllAsync())
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                _logger.UncaughtExceptionWhileProcessingTask(ex);
            }
        }
    }

    public bool IsEnqueued(TKey key)
    {
        return _workDispatcher.ContainsKey(key);
    }

    public void Enqueue<T>(TKey key, T state, Func<TKey, T, Task> taskFunc, Action<T> cleanupAction)
    {
        var item = new WorkItem<T>(key, state, taskFunc, _logger, cleanupAction);
        var result = _workDispatcher.ScheduleAsync(key, item, async static (_, item) => 
        {
            try
            {
                await item.TaskFunc(item.Key, item.Arg);
            }
            catch (Exception e)
            {
                item.Logger.ErrorProcessingTask(e, item.Key.ToString());
            }

            Cleanup(item);
        });

        if (!result.IsNewTask)
        {
            Cleanup(item);
        }

        _taskChannel.Writer.TryWrite(result.Task);
        return;

        static void Cleanup(WorkItem<T> item)
        {
            try
            {
                item.CleanupAction(item.Arg);
            }
            catch (Exception e)
            {
                item.Logger.CouldNotCleanupState(e, item.Key.ToString());
            }
        }
    }

    private record WorkItem<T>(TKey Key, T Arg, Func<TKey, T, Task> TaskFunc, ILogger Logger, Action<T> CleanupAction);
}