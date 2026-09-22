using Styx.Common;

namespace Styx.Pathing
{
    /// <summary>
    /// Central debug logging for the navigation system.
    /// Set <see cref="Verbose"/> to true to enable detailed per-pulse logs.
    /// Set <see cref="Movement"/> to true to log movement commands.
    /// Set <see cref="ApiCalls"/> to true to log P/Invoke calls.
    /// </summary>
    public static class NavigatorDebug
    {
        /// <summary>Master switch — when false, all nav logs are silenced.</summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>Log every pulse (path following, waypoint advancement). Very noisy.</summary>
        public static bool Verbose { get; set; } = false;

        /// <summary>Log movement commands (MoveForward, Stop, Facing).</summary>
        public static bool Movement { get; set; } = true;

        /// <summary>Log native DLL API calls (pathfinding, raycast, queries).</summary>
        public static bool ApiCalls { get; set; } = true;

        /// <summary>Log path generation details (waypoints, distances).</summary>
        public static bool PathGen { get; set; } = true;

        /// <summary>Log stuck detection events.</summary>
        public static bool Stuck { get; set; } = true;

        /// <summary>Log level for navigation messages.</summary>
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
