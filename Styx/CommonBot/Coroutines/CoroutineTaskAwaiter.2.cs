using System;
using System.Runtime.CompilerServices;

namespace Styx.CommonBot.Coroutines
{
    public struct CoroutineTaskAwaiter<T> : INotifyCompletion
    {
        private readonly CoroutineTask<T> _task;

        internal CoroutineTaskAwaiter(CoroutineTask<T> task)
        {
            _task = task;
        }

        public bool IsCompleted { get { return _task.TaskAwaiter.IsCompleted; } }

        public T GetResult()
        {
            return _task.TaskAwaiter.GetResult();
        }

        public void OnCompleted(Action continuation)
        {
            _task.TaskAwaiter.OnCompleted(continuation);
        }
    }
}
