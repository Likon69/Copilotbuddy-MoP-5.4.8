using System;

namespace Styx.CommonBot
{
    public class ShutdownRequestedEventArgs : EventArgs
    {
        public ShutdownRequestedEventArgs(int exitCode = 0, bool closeWow = false)
        {
            ExitCode = exitCode;
            CloseWow = closeWow;
        }

        public int ExitCode { get; set; }

        public bool CloseWow { get; set; }
    }
}
