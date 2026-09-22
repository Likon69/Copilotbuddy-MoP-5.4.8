using System;
using System.Diagnostics;
using System.Threading;

namespace Styx.Common
{
    public class PerformanceTimer : IDisposable
    {
        public PerformanceTimer(string debugText, FinishedMeasuringCallback callback = null, int logMsLimit = 0)
        {
            this.finishedMeasuringCallback = callback;
            this.stopwatch = new Stopwatch();
            this.debugText = debugText;
            this.logMsLimit = logMsLimit;
        }

        public void Start()
        {
            this.stopwatch.Start();
        }

        [Conditional("TIMERS")]
        private void StartTimer()
        {
            this.Start();
        }

        [Conditional("TIMERS")]
        private void StopTimer()
        {
            this.StopAndPrint();
        }

        public void DontPrint()
        {
            this.dontPrint = true;
        }

        public long ElapsedMilliseconds
        {
            get
            {
                return this.stopwatch.ElapsedMilliseconds;
            }
        }

        public void StopAndPrint()
        {
            this.stopwatch.Stop();
            if (this.dontPrint)
            {
                return;
            }
            if (this.ElapsedMilliseconds > (long)this.logMsLimit)
            {
                Trace.Write(string.Format("[{0}] {1}", this.stopwatch.Elapsed, this.debugText));
            }
            if (this.finishedMeasuringCallback != null)
            {
                // Honorbuddy wrote `finishedMeasuringCallback.BeginInvoke(ElapsedMilliseconds, null, null)`.
                // Delegate BeginInvoke/EndInvoke throw PlatformNotSupportedException on .NET Core and
                // later, so they cannot be used here. With a null callback and null state the original
                // was a pure fire-and-forget invocation on a thread-pool thread, and nothing ever
                // consumed the IAsyncResult -- which is exactly what this reproduces.
                long elapsedMilliseconds = this.ElapsedMilliseconds;
                FinishedMeasuringCallback callback = this.finishedMeasuringCallback;
                ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    callback(elapsedMilliseconds);
                });
            }
        }

        public void Dispose()
        {
        }

        private readonly Stopwatch stopwatch;

        private readonly string debugText;

        private bool dontPrint;

        private readonly FinishedMeasuringCallback finishedMeasuringCallback;

        private int logMsLimit;
    }
}
