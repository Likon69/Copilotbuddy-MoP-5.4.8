using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Buddy.Coroutines
{
    public sealed class Coroutine : IDisposable
    {
        #region Fields

        private readonly YieldAwaiter _awaiter = new YieldAwaiter();
        private Task<object> _task;
        private Action _continuation;
        private bool _multipleContinuations;

        [ThreadStatic]
        private static Coroutine _current;

        #endregion

        #region Constructors

        public Coroutine(Func<Task> taskProducer) : this(WrapTaskProducer(taskProducer))
        {
        }

        public Coroutine(Func<Task<object>> taskProducer)
        {
            if (taskProducer == null)
                throw new ArgumentNullException("taskProducer");

            _continuation = delegate
            {
                Task<object> task;
                try
                {
                    task = taskProducer();
                }
                catch (Exception ex)
                {
                    throw SetFault(new CoroutineUnhandledException("Exception was thrown by root coroutine task producer", ex));
                }

                if (task == null)
                    throw SetFault(new CoroutineBehaviorException("The root coroutine task producer returned null"));

                _task = task;
            };
            Status = CoroutineStatus.Runnable;
        }

        #endregion

        #region Properties

        public static Coroutine Current { get { return _current; } }
        public object Result { get; private set; }
        public CoroutineStatus Status { get; private set; }
        public bool IsDisposed { get; private set; }
        public CoroutineException FaultingException { get; private set; }
        public int Ticks { get; private set; }

        public bool IsFinished
        {
            get
            {
                return Status == CoroutineStatus.RanToCompletion || Status == CoroutineStatus.Faulted || Status == CoroutineStatus.Stopped;
            }
        }

        #endregion

        #region Methods

        private static Func<Task<object>> WrapTaskProducer(Func<Task> taskProducer)
        {
            return async () =>
            {
                await taskProducer();
                return null;
            };
        }

        public void Resume()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (IsFinished)
                throw new InvalidOperationException("This coroutine has finished execution and cannot be resumed.");
            if (_current == this)
                throw new InvalidOperationException("A coroutine cannot resume itself");

            ResumeCore(false);
        }

        private void ResumeCore(bool disposing)
        {
            SynchronizationContext context = SynchronizationContext.Current;
            Coroutine previous = _current;
            try
            {
                _current = this;
                SynchronizationContext.SetSynchronizationContext(null);
                Action continuation = _continuation;
                _continuation = null;
                continuation();
                if (!disposing)
                    Ticks++;
                CheckTaskStatus(disposing);
            }
            finally
            {
                _current = previous;
                SynchronizationContext.SetSynchronizationContext(context);
            }
        }

        private Exception SetFault(CoroutineException exception)
        {
            Status = CoroutineStatus.Faulted;
            FaultingException = exception;
            return exception;
        }

        private void CheckTaskStatus(bool disposing)
        {
            switch (_task.Status)
            {
                case TaskStatus.WaitingForActivation:
                    if (disposing)
                        throw SetFault(new CoroutineBehaviorException("The coroutine could not successfully be disposed of. This is usually an indication that the CoroutineStoppedException was caught."));
                    if (_continuation == null)
                        throw SetFault(new CoroutineBehaviorException("No continuation was queued and coroutine didn't finish. This is usually an indication that an external task was awaited, which is not supported by coroutines."));
                    if (_multipleContinuations)
                        throw SetFault(new CoroutineBehaviorException("Multiple continuations were encountered in a single coroutine tick. This is usually an indication that a coroutine task was created but not awaited."));
                    break;
                case TaskStatus.RanToCompletion:
                    if (_continuation != null)
                        throw SetFault(new CoroutineBehaviorException("The coroutine finished with a continuation queued. This is usually an indication that a task was created but not awaited."));
                    Status = CoroutineStatus.RanToCompletion;
                    Result = _task.Result;
                    break;
                case TaskStatus.Canceled:
                    try
                    {
                        _task.Wait(0);
                        throw SetFault(new CoroutineBehaviorException("Coroutine was canceled without any exceptions"));
                    }
                    catch (AggregateException ex)
                    {
                        throw SetFault(new CoroutineUnhandledException("Exception was thrown by coroutine", ex.InnerExceptions.FirstOrDefault()));
                    }
                case TaskStatus.Faulted:
                    Exception exception = _task.Exception.InnerExceptions.FirstOrDefault();
                    if (!(exception is CoroutineStoppedException))
                        throw SetFault(new CoroutineUnhandledException("Exception was thrown by coroutine", exception));
                    Status = CoroutineStatus.Stopped;
                    break;
                default:
                    throw SetFault(new CoroutineBehaviorException("Unexpected task status " + _task.Status));
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            if (Current == this)
            {
                IsDisposed = true;
                throw CreateStoppedException();
            }
            if (Status == CoroutineStatus.Runnable)
            {
                if (_task != null)
                {
                    _awaiter.Canceled = true;
                    ResumeCore(true);
                }
                else
                    _continuation = null;
            }
            IsDisposed = true;
        }

        public override string ToString()
        {
            switch (Status)
            {
                case CoroutineStatus.Runnable:
                    return "Runnable";
                case CoroutineStatus.RanToCompletion:
                    return Result != null ? "Ran to completion. Result: " + Result : "Ran to completion";
                case CoroutineStatus.Stopped:
                    return "Stopped";
                case CoroutineStatus.Faulted:
                    return FaultingException != null ? "Faulted with exception " + FaultingException.InnerException.GetType() : "Faulted";
                default:
                    return "Invalid state";
            }
        }

        internal static Exception CreateStoppedException()
        {
            return new CoroutineStoppedException("Coroutine was stopped");
        }

        private static void EnsureCurrent()
        {
            if (_current == null)
                throw new InvalidOperationException("This function can only be used from within a coroutine");
        }

        private static async Task YieldCore()
        {
            await Current._awaiter;
        }

        public static Task Yield()
        {
            EnsureCurrent();
            return YieldCore();
        }

        private static async Task SleepCore(TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                while (true)
                    await Current._awaiter;
            }
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                await Current._awaiter;
            } while (timer.Elapsed < timeout);
        }

        public static Task Sleep(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException("timeout");
            EnsureCurrent();
            return SleepCore(timeout);
        }

        public static Task Sleep(int milliseconds)
        {
            if (milliseconds < 0 && milliseconds != -1)
                throw new ArgumentOutOfRangeException("milliseconds");
            return Sleep(new TimeSpan(0, 0, 0, 0, milliseconds));
        }

        private static async Task<bool> WaitCore(TimeSpan timeout, Func<bool> condition)
        {
            if (timeout == TimeSpan.Zero)
                return condition();
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                while (!condition())
                    await Current._awaiter;
                return true;
            }
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                if (condition())
                    return true;
                await Current._awaiter;
            } while (timer.Elapsed < timeout);
            return false;
        }

        public static Task<bool> Wait(TimeSpan maxWaitTimeout, Func<bool> condition)
        {
            if (maxWaitTimeout < TimeSpan.Zero && maxWaitTimeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException("maxWaitTimeout", "Cannot wait for a negative timespan");
            if (condition == null)
                throw new ArgumentNullException("condition");
            EnsureCurrent();
            return WaitCore(maxWaitTimeout, condition);
        }

        public static Task<bool> Wait(int maxWaitMs, Func<bool> condition)
        {
            if (maxWaitMs < 0 && maxWaitMs != -1)
                throw new ArgumentOutOfRangeException("maxWaitMs", "Cannot wait for a negative amount of time");
            return Wait(new TimeSpan(0, 0, 0, 0, maxWaitMs), condition);
        }

        private static async Task<bool> WaitExternalTaskCore(Task task, TimeSpan timeout)
        {
            if (timeout == TimeSpan.Zero)
                return task.Wait(0);
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                while (!task.Wait(0))
                    await Current._awaiter;
                return true;
            }
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                if (task.Wait(0))
                    return true;
                await Current._awaiter;
            } while (timer.Elapsed < timeout);
            return false;
        }

        public static Task ExternalTask(Task externalTask)
        {
            return ExternalTask(externalTask, Timeout.InfiniteTimeSpan);
        }

        public static Task<bool> ExternalTask(Task externalTask, TimeSpan timeout)
        {
            if (externalTask == null)
                throw new ArgumentNullException("externalTask");
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException("timeout");
            EnsureCurrent();
            return WaitExternalTaskCore(externalTask, timeout);
        }

        public static Task<bool> ExternalTask(Task externalTask, int millisecondsTimeout)
        {
            if (millisecondsTimeout != -1 && millisecondsTimeout < 0)
                throw new ArgumentOutOfRangeException("millisecondsTimeout", "Timeout cannot be negative");
            return ExternalTask(externalTask, new TimeSpan(0, 0, 0, 0, millisecondsTimeout));
        }

        private static async Task<ExternalTaskWaitResult<T>> WaitExternalTaskCore<T>(Task<T> task, TimeSpan timeout)
        {
            if (timeout == TimeSpan.Zero)
                return task.Wait(0) ? ExternalTaskWaitResult<T>.FromResult(task.Result) : ExternalTaskWaitResult<T>.TimedOut;
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                while (!task.Wait(0))
                    await Current._awaiter;
                return ExternalTaskWaitResult<T>.FromResult(task.Result);
            }
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                if (task.Wait(0))
                    return ExternalTaskWaitResult<T>.FromResult(task.Result);
                await Current._awaiter;
            } while (timer.Elapsed < timeout);
            return ExternalTaskWaitResult<T>.TimedOut;
        }

        private static async Task<T> GetExternalTaskResult<T>(Task<ExternalTaskWaitResult<T>> task)
        {
            ExternalTaskWaitResult<T> result = await task;
            return result.Result;
        }

        public static Task<T> ExternalTask<T>(Task<T> externalTask)
        {
            Task<ExternalTaskWaitResult<T>> task = ExternalTask(externalTask, Timeout.InfiniteTimeSpan);
            return GetExternalTaskResult(task);
        }

        public static Task<ExternalTaskWaitResult<T>> ExternalTask<T>(Task<T> externalTask, TimeSpan timeout)
        {
            if (externalTask == null)
                throw new ArgumentNullException("externalTask");
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException("timeout", "Timeout cannot be negative");
            EnsureCurrent();
            return WaitExternalTaskCore(externalTask, timeout);
        }

        public static Task<ExternalTaskWaitResult<T>> ExternalTask<T>(Task<T> externalTask, int millisecondsTimeout)
        {
            if (millisecondsTimeout < 0 && millisecondsTimeout != -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeout", "Timeout cannot be negative");
            return ExternalTask(externalTask, new TimeSpan(0, 0, 0, 0, millisecondsTimeout));
        }

        #endregion

        #region Nested Types

        private class YieldAwaiter : INotifyCompletion
        {
            public bool Canceled { get; set; }
            public bool IsCompleted { get { return false; } }

            public YieldAwaiter GetAwaiter()
            {
                return this;
            }

            public void GetResult()
            {
                if (Canceled)
                    throw CreateStoppedException();
            }

            void INotifyCompletion.OnCompleted(Action continuation)
            {
                if (Current._continuation != null)
                    Current._multipleContinuations = true;
                Current._continuation = continuation;
            }
        }

        #endregion
    }
}
