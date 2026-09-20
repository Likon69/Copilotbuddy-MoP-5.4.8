using System;
using System.Diagnostics;

namespace Styx.Common.Helpers
{
    public class WaitTimer
    {
        public sealed class WaitTimerEventArgs : EventArgs
        {
            public DateTime TimeFinished { get; set; }

            public TimeSpan WaitTime { get; set; }

            public DateTime TimeStarted { get; set; }
        }

        public static WaitTimer OneSecond
        {
            get { return new WaitTimer(new TimeSpan(0, 0, 1)); }
        }

        public static WaitTimer FiveSeconds
        {
            get { return new WaitTimer(new TimeSpan(0, 0, 5)); }
        }

        public static WaitTimer TenSeconds
        {
            get { return new WaitTimer(new TimeSpan(0, 0, 10)); }
        }

        public static WaitTimer ThirtySeconds
        {
            get { return new WaitTimer(new TimeSpan(0, 0, 30)); }
        }

        public TimeSpan WaitTime { get; set; }

        public DateTime EndTime
        {
            get { return StartTime + WaitTime; }
        }

        public DateTime StartTime
        {
            get { return DateTime.Now - _stopwatch.Elapsed; }
        }

        public WaitTimer(TimeSpan waitTime)
        {
            WaitTime = waitTime;
            _stopwatch = new Stopwatch();
            Stop();
        }

        public void Reset()
        {
            _finishedNotified = false;
            _stopwatch.Restart();
        }

        public void Stop()
        {
            _finishedNotified = true;
            _stopwatch.Stop();
        }

        public void Update()
        {
            bool isFinished = IsFinished;
            if (isFinished && !_finishedNotified)
            {
                WaitTimerFinishedHandler finished = Finished;
                if (finished != null)
                {
                    finished(this, new WaitTimerEventArgs
                    {
                        TimeFinished = DateTime.Now,
                        TimeStarted = StartTime,
                        WaitTime = WaitTime
                    });
                }
            }
            _finishedNotified = isFinished;
        }

        public bool IsFinished
        {
            get { return !_stopwatch.IsRunning || _stopwatch.Elapsed > WaitTime; }
        }

        public TimeSpan TimeLeft
        {
            get { return WaitTime - _stopwatch.Elapsed; }
        }

        public event WaitTimerFinishedHandler Finished;

        private bool _finishedNotified;

        private readonly Stopwatch _stopwatch;
    }
}
