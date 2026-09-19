using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Media;

namespace Styx.Common
{
    public static class Logging
    {
        public static DateTime StartTime
        {
            get { return _styxLog.StartTime; }
        }

        static Logging()
        {
            _styxLog.ChangeLogFilePath(DefaultLogFilePath);
            _styxLog.FileLogging = true;
        }

        public static LogLevel LoggingLevel
        {
            get { return _styxLog.LoggingLevel; }
            set { _styxLog.LoggingLevel = value; }
        }

        public static LogLevel LogFileLevel
        {
            get { return _styxLog.LogFileLevel; }
            set { _styxLog.LogFileLevel = value; }
        }

        public static string DefaultLogFilePath
        {
            get
            {
                return Path.Combine(Utilities.AssemblyDirectory, string.Format("Logs\\{5} {0:0000}-{1:00}-{2:00} {3:00}.{4:00}.txt", new object[]
                {
                    StartTime.Year,
                    StartTime.Month,
                    StartTime.Day,
                    StartTime.Hour,
                    StartTime.Minute,
                    Process.GetCurrentProcess().Id
                }));
            }
        }

        public static string LogFilePath
        {
            get { return _styxLog.LogFilePath; }
            set { _styxLog.ChangeLogFilePath(value); }
        }

        public static event LogMessageDelegate OnLogMessage
        {
            add { _styxLog.OnLogMessage += value; }
            remove { _styxLog.OnLogMessage -= value; }
        }

        public static void Write(LogLevel level, Color color, string message)
        {
            if (!string.IsNullOrWhiteSpace(message) && _lastMessage != message)
            {
                _lastMessage = message;
                _styxLog.Write(level, color, message);
            }
        }

        public static void Write(LogLevel level, Color color, string format, params object[] args)
        {
            Write(level, color, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public static void Write(LogLevel level, string message)
        {
            Write(level, Colors.WhiteSmoke, message);
        }

        public static void Write(LogLevel level, string format, params object[] args)
        {
            Write(level, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public static void WriteQuiet(Color color, string message)
        {
            Write(LogLevel.Quiet, color, message);
        }

        public static void WriteQuiet(Color color, string format, params object[] args)
        {
            Write(LogLevel.Quiet, color, format, args);
        }

        public static void WriteQuiet(string message)
        {
            WriteQuiet(Colors.Gray, message);
        }

        public static void WriteQuiet(string format, params object[] args)
        {
            WriteQuiet(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public static void Write(Color color, string message)
        {
            Write(LogLevel.Normal, color, message);
        }

        public static void Write(Color color, string format, params object[] args)
        {
            Write(color, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public static void Write(string message)
        {
            Write(Colors.WhiteSmoke, message);
        }

        public static void Write(string format, params object[] args)
        {
            Write(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public static void WriteVerbose(Color color, string message)
        {
            Write(LogLevel.Verbose, color, message);
        }

        public static void WriteVerbose(Color color, string format, params object[] args)
        {
            WriteVerbose(color, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public static void WriteVerbose(string message)
        {
            WriteVerbose(Colors.DarkOrange, message);
        }

        public static void WriteVerbose(string format, params object[] args)
        {
            WriteVerbose(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public static void WriteDiagnostic(Color color, string message)
        {
            Write(LogLevel.Diagnostic, color, message);
        }

        public static void WriteDiagnostic(Color color, string format, params object[] args)
        {
            WriteDiagnostic(color, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public static void WriteDiagnostic(string message)
        {
            WriteDiagnostic(Colors.Orange, message);
        }

        public static void WriteDiagnostic(string format, params object[] args)
        {
            WriteDiagnostic(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public static void WriteException(LogLevel logLevel, Color color, Exception ex)
        {
            Write(logLevel, color, ex.ToString());
        }

        public static void WriteException(LogLevel logLevel, Exception ex)
        {
            WriteException(logLevel, Colors.Red, ex);
        }

        public static void WriteException(Color color, Exception ex)
        {
            WriteException(LogLevel.Diagnostic, color, ex);
        }

        public static void WriteException(Exception ex)
        {
            WriteException(LogLevel.Diagnostic, Colors.Red, ex);
        }

        public static void WriteToFileSync(LogLevel level, string message)
        {
            _styxLog.WriteToFileSync(level, message);
        }

        public static void WriteToFileSync(LogLevel level, string format, params object[] args)
        {
            WriteToFileSync(level, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        private static readonly StyxLog _styxLog = new StyxLog(TimestampType.Default);

        private static string _lastMessage = string.Empty;

        public delegate void LogMessageDelegate(ReadOnlyCollection<LogMessage> messages);

        public class LogMessage
        {
            internal LogMessage(LogLevel level, Color color, TimestampType timestampType, DateTime timestamp, TimeSpan timerTimestamp, string message)
            {
                Level = level;
                Color = color;
                TimestampType = timestampType;
                Timestamp = timestamp;
                TimerTimestamp = timerTimestamp;
                Message = message;
            }

            public LogLevel Level { get; set; }

            public Color Color { get; set; }

            public TimestampType TimestampType { get; set; }

            public DateTime Timestamp { get; set; }

            public TimeSpan TimerTimestamp { get; set; }

            public string Message { get; set; }

            public override string ToString()
            {
                string text;
                switch (TimestampType)
                {
                    case TimestampType.DateTimeLocal:
                        text = Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
                        break;
                    case TimestampType.Timer:
                        text = TimerTimestamp.TotalSeconds.ToString("F3");
                        break;
                    default:
                        text = Timestamp.ToUniversalTime().ToString("HH:mm:ss.fff");
                        break;
                }
                return string.Format("[{0} {1}] {2}", text, Level.ToString()[0], Message);
            }
        }

        private class LoggingTraceListener : TraceListener
        {
            public LoggingTraceListener(string type)
            {
                Name = "LoggingTraceListener_" + type;
                _type = type;
            }

            public bool Enabled { get; set; }

            public override void Write(string message)
            {
                WriteLine(message);
            }

            public override void WriteLine(string message)
            {
                if (Enabled)
                {
                    WriteDiagnostic(Colors.DarkOrange, string.Format("[{0}-{2}] {1}", _type, message, _spamCounter++));
                }
            }

            private readonly string _type;

            private static ulong _spamCounter;
        }
    }
}
