using System;

namespace Tripper.Navigation
{
    public class PathFindProgressEventArgs : EventArgs
    {
        internal PathFindProgressEventArgs(TimeSpan runTime)
        {
            RunTime = runTime;
        }

        public DateTime PathFindStart { get { return DateTime.Now - RunTime; } }
        public TimeSpan RunTime { get; private set; }
        public bool Cancel { get; set; }
    }
}
