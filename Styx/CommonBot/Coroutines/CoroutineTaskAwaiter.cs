using System;
using System.Runtime.CompilerServices;

namespace Styx.CommonBot.Coroutines
{
    public struct CoroutineTaskAwaiter : INotifyCompletion
    {
        private readonly CoroutineTask _task;
        internal CoroutineTaskAwaiter(CoroutineTask task) { _task = task; }
        public bool IsCompleted => _task.TaskAwaiter.IsCompleted;
        public void GetResult() => _task.TaskAwaiter.GetResult();
        public void OnCompleted(Action continuation) => _task.TaskAwaiter.OnCompleted(continuation);
    }

}
