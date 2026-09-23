using Styx.Common;

namespace Styx.Pathing
{
    public static class NavigatorDebug
    {
        public static bool Enabled { get; set; } = true;

        public static bool Verbose { get; set; } = false;

        public static bool Movement { get; set; } = true;

        public static bool ApiCalls { get; set; } = true;

        public static bool PathGen { get; set; } = true;

        public static bool Stuck { get; set; } = true;

        public static LogLevel Level { get; set; } = LogLevel.Diagnostic;

        internal static void Log(string message)
        {
            if (Enabled)
                Logging.Write(Level, $"[Nav] {message}");
        }

        internal static void LogVerbose(string message)
        {
            if (Enabled && Verbose)
                Logging.Write(Level, $"[Nav:Verbose] {message}");
        }

        internal static void LogMovement(string message)
        {
            if (Enabled && Movement)
                Logging.Write(Level, $"[Nav:Move] {message}");
        }

        internal static void LogApi(string message)
        {
            if (Enabled && ApiCalls)
                Logging.Write(Level, $"[Nav:API] {message}");
        }

        internal static void LogPath(string message)
        {
            if (Enabled && PathGen)
                Logging.Write(Level, $"[Nav:Path] {message}");
        }

        internal static void LogStuck(string message)
        {
            if (Enabled && Stuck)
                Logging.Write(Level, $"[Nav:Stuck] {message}");
        }

        internal static void LogError(string message)
        {
            Logging.Write(LogLevel.Quiet, $"[Nav:ERROR] {message}");
        }

        internal static void LogException(System.Exception ex)
        {
            Logging.WriteException(ex);
        }
    }
}
