using System;
using System.Threading;

namespace CopilotBuddy
{
    internal static class MutexHelper
    {
        public static Mutex CreateMutexForProcess(int processId, out bool wasCreated)
        {
            return new Mutex(true, @"Local\" + (Environment.MachineName.GetHashCode() ^ processId.GetHashCode() ^ TimeZone.CurrentTimeZone.StandardName.GetHashCode() ^ "HerroBrizzard".GetHashCode() + 25), out wasCreated);
        }
    }
}
