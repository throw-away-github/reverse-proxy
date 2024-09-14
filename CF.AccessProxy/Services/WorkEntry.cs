using System.Diagnostics;
using Microsoft.Extensions.ObjectPool;

namespace CF.AccessProxy.Services;

internal interface IWorkEntry
{
    public Task StartTask();
    public void CancelTask();
}

internal static class WorkEntryObjectPool<TState>
{
    private static readonly ObjectPool<StatefulWorkEntry> _pool = new DefaultObjectPool<StatefulWorkEntry>(new StatefulWorkEntryPoolPolicy());

    public static IWorkEntry Get(TState state, Func<TState, Task> taskFunc)
    {
        var entry = _pool.Get();
        entry.SetTask(state, taskFunc);
        return entry;
    }

    private record StatefulWorkEntry : IWorkEntry
    {
        private int _workEntryState = (int)WorkEntryState.Created;
        private TState _taskArg = default!;
        private Func<TState, Task>? _taskFunc;

        public void SetTask(TState state, Func<TState, Task> taskFunc)
        {
            Transition(WorkEntryState.Created, WorkEntryState.Ready);
            _taskArg = state;
            _taskFunc = taskFunc;
        }

        public async Task StartTask()
        {
            Transition(WorkEntryState.Ready, WorkEntryState.Running);
            Debug.Assert(_taskFunc != null);

            try
            {
                await _taskFunc(_taskArg);
            }
            finally
            {
                Transition(WorkEntryState.Running, WorkEntryState.Completed);
                _pool.Return(this);
            }
        }

        private void Transition(WorkEntryState from, WorkEntryState to)
        {
            if (!TryTransition(from, to))
            {
                var currentState = (WorkEntryState)Volatile.Read(ref _workEntryState);
                throw new InvalidOperationException($"Could not transition from {from} to {to} because the current state is {currentState}.");
            }
        }
        
        private bool TryTransition(WorkEntryState from, WorkEntryState to)
        {
            var fromInt = (int)from;
            var toInt = (int)to;
            return Interlocked.CompareExchange(ref _workEntryState, toInt, fromInt) == fromInt;
        }

        public bool TryReset()
        {
            if (!TryTransition(WorkEntryState.Completed, WorkEntryState.Created))
            {
                return false;
            }

            _taskArg = default!;
            _taskFunc = default;
            return true;
        }

        public void CancelTask()
        {
            if (!TryTransition(WorkEntryState.Ready, WorkEntryState.Completed))
            {
                return;
            }
            _taskArg = default!;
            _taskFunc = default;
            _pool.Return(this);
        }
    }

    private enum WorkEntryState
    {
        Created,
        Ready,
        Running,
        Completed
    }

    private class StatefulWorkEntryPoolPolicy : DefaultPooledObjectPolicy<StatefulWorkEntry>
    {
        public override StatefulWorkEntry Create() => new();

        public override bool Return(StatefulWorkEntry obj)
        {
            return obj.TryReset();
        }
    }
}