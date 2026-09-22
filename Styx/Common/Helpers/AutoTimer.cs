using System;
using System.Diagnostics;

namespace Styx.Common.Helpers
{
    public class AutoTimer : IDisposable
    {
        private readonly string _codePart;

        public Stopwatch Watch { get; private set; }

        public AutoTimer(string codePart)
        {
            Watch = Stopwatch.StartNew();
            _codePart = codePart;
        }

        public AutoTimer(string codePartFormat, params object[] args)
            : this(string.Format(codePartFormat, args))
        {
        }

        public void Dispose()
        {
            Watch.Stop();
            string elapsed;
            if (Watch.ElapsedMilliseconds > 60000L)
                elapsed = Watch.Elapsed.TotalMinutes.ToString("N3") + " minutes";
            else if (Watch.ElapsedMilliseconds > 1000L)
                elapsed = Watch.Elapsed.TotalSeconds.ToString("N3") + " seconds";
            else
            {
                if (Watch.ElapsedMilliseconds <= 5L)
                    return;
                elapsed = Watch.ElapsedMilliseconds + " ms";
            }

            Logging.WriteDiagnostic("{0} took {1}", _codePart, elapsed);
        }
    }
}
