using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Media;

namespace Styx.Common
{
    public class StyxLog
    {
        public static bool IsLogThreadRunning
        {
            get { return _logThread != null && _logThread.IsAlive; }
        }

        public static void StartLogThread()
        {
            if (IsLogThreadRunning)
            {
                return;
            }
            lock (_threadStateLock)
            {
                if (!IsLogThreadRunning)
                {
                    _logThread = new Thread(LogThreadProc);
                    _logThread.Name = "Logging Thread";
                    _logThread.SetApartmentState(ApartmentState.STA);
                    _logThread.IsBackground = true;
                    _stopSignal.Reset();
                    _logThread.Start();
                }
            }
        }

        public static void StopLogThread()
        {
            if (!IsLogThreadRunning)
            {
                return;
            }
            lock (_threadStateLock)
            {
                if (IsLogThreadRunning)
                {
                    if (Thread.CurrentThread == _logThread)
                    {
                        throw new InvalidOperationException("Cannot stop the log thread from within the log thread");
                    }
                    _stopSignal.Set();
                    _logThread.Join();
                }
            }
        }

        private static void ProcessLogQueues(bool raiseEvents)
        {
            try
            {
                StyxLog[] instances;
                lock (_instancesLock)
                {
                    instances = _instances.ToArray();
                }
                foreach (StyxLog log in instances)
                {
                    log.ProcessQueue(raiseEvents);
                }
            }
            catch (Exception)
            {
            }
        }

        private static void LogThreadProc()
        {
            do
            {
                ProcessLogQueues(true);
            }
            while (!_stopSignal.WaitOne(500));
            ProcessLogQueues(false);
        }

        public static IEnumerable<StyxLog> GetLogs()
        {
            lock (_instancesLock)
            {
                return _instances.ToArray();
            }
        }

        public StyxLog(TimestampType type = TimestampType.Default)
        {
            LogFileLevel = LogLevel.Diagnostic;
            LoggingLevel = LogLevel.Diagnostic;
            StartTime = DateTime.Now;
            _stopwatch = Stopwatch.StartNew();
            TimestampType = type != TimestampType.Default ? type : TimestampType.DateTimeUtc;
            lock (_instancesLock)
            {
                _instances.Add(this);
            }
        }

        public DateTime StartTime { get; private set; }

        public TimestampType TimestampType { get; private set; }

        public bool FileLogging
        {
            get
            {
                return _fileLoggingEnabled;
            }
            set
            {
                if (value && string.IsNullOrWhiteSpace(LogFilePath))
                {
                    throw new InvalidOperationException("You must set a log file path before enabling file logging");
                }
                _fileLoggingEnabled = value;
            }
        }

        public string LogFilePath { get; private set; }

        public LogLevel LoggingLevel { get; set; }

        public LogLevel LogFileLevel { get; set; }

        public event Logging.LogMessageDelegate OnLogMessage;

        private void ProcessQueue(bool raiseEvent)
        {
            if (_messageQueue.Count <= 0)
            {
                return;
            }
            Logging.LogMessageDelegate onLogMessage = OnLogMessage;
            List<Logging.LogMessage> messages = new List<Logging.LogMessage>();
            if (FileLogging)
            {
                lock (_fileLock)
                {
                    using (StreamWriter streamWriter = OpenLogFileForWrite())
                    {
                        Logging.LogMessage message;
                        while (_messageQueue.TryDequeue(out message))
                        {
                            if (message.Level <= LogFileLevel)
                            {
                                streamWriter.WriteLine(message.ToString());
                            }
                            messages.Add(message);
                        }
                        streamWriter.Flush();
                    }
                }
            }
            else
            {
                Logging.LogMessage message;
                while (_messageQueue.TryDequeue(out message))
                {
                    messages.Add(message);
                }
            }
            if (messages.Count <= 0)
            {
                return;
            }
            if (onLogMessage != null && raiseEvent)
            {
                onLogMessage(messages.AsReadOnly());
            }
        }

        public void ChangeLogFilePath(string newPath)
        {
            if (LogFilePath == newPath)
            {
                return;
            }
            if (newPath == null)
            {
                throw new ArgumentNullException("newPath");
            }
            if (File.Exists(newPath))
            {
                throw new ArgumentException("Argument must be a path to a file that doesn't already exist", "newPath");
            }
            lock (_fileLock)
            {
                string directoryName = Path.GetDirectoryName(newPath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                if (!string.IsNullOrWhiteSpace(LogFilePath) && File.Exists(LogFilePath))
                {
                    File.Move(LogFilePath, newPath);
                }
                LogFilePath = newPath;
            }
        }

        private StreamWriter OpenLogFileForWrite()
        {
            string logFilePath = LogFilePath;
            StreamWriter streamWriter;
            try
            {
                streamWriter = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read));
            }
            catch (IOException)
            {
                throw new IOException("The process cannot access the log file");
            }
            return streamWriter;
        }

        public void Write(LogLevel level, Color color, string message)
        {
            if (level <= LogLevel.None)
            {
                return;
            }
            _messageQueue.Enqueue(CreateLogMessage(level, color, message));
        }

        public void Write(LogLevel level, Color color, string format, params object[] args)
        {
            Write(level, color, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void Write(LogLevel level, string message)
        {
            Write(level, Colors.WhiteSmoke, message);
        }

        public void Write(LogLevel level, string format, params object[] args)
        {
            Write(level, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void WriteToFileSync(LogLevel level, string message)
        {
            Logging.LogMessage logMessage = CreateLogMessage(level, Colors.WhiteSmoke, message);
            lock (_fileLock)
            {
                using (StreamWriter streamWriter = OpenLogFileForWrite())
                {
                    streamWriter.WriteLine(logMessage);
                    streamWriter.Flush();
                }
            }
        }

        public void WriteToFileSync(LogLevel level, string format, params object[] args)
        {
            WriteToFileSync(level, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void WriteQuiet(Color color, string message)
        {
            Write(LogLevel.Quiet, color, message);
        }

        public void WriteQuiet(Color color, string format, params object[] args)
        {
            Write(LogLevel.Quiet, color, format, args);
        }

        public void WriteQuiet(string message)
        {
            WriteQuiet(Colors.Gray, message);
        }

        public void WriteQuiet(string format, params object[] args)
        {
            WriteQuiet(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void Write(Color color, string message)
        {
            Write(LogLevel.Normal, color, message);
        }

        public void Write(Color color, string format, params object[] args)
        {
            Write(color, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void Write(string message)
        {
            Write(Colors.WhiteSmoke, message);
        }

        public void Write(string format, params object[] args)
        {
            Write(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void WriteVerbose(Color color, string message)
        {
            Write(LogLevel.Verbose, color, message);
        }

        public void WriteVerbose(Color color, string format, params object[] args)
        {
            WriteVerbose(color, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void WriteVerbose(string message)
        {
            WriteVerbose(Colors.DarkOrange, message);
        }

        public void WriteVerbose(string format, params object[] args)
        {
            WriteVerbose(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void WriteDiagnostic(Color color, string message)
        {
            Write(LogLevel.Diagnostic, color, message);
        }

        public void WriteDiagnostic(Color color, string format, params object[] args)
        {
            WriteDiagnostic(color, string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void WriteDiagnostic(string message)
        {
            WriteDiagnostic(Colors.Orange, message);
        }

        public void WriteDiagnostic(string format, params object[] args)
        {
            WriteDiagnostic(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void WriteException(LogLevel logLevel, Color color, Exception ex)
        {
            Write(logLevel, color, ex.ToString());
        }

        public void WriteException(LogLevel logLevel, Exception ex)
        {
            WriteException(logLevel, Colors.Red, ex);
        }

        public void WriteException(Color color, Exception ex)
        {
            WriteException(LogLevel.Diagnostic, color, ex);
        }

        public void WriteException(Exception ex)
        {
            WriteException(LogLevel.Diagnostic, Colors.Red, ex);
        }

        private Logging.LogMessage CreateLogMessage(LogLevel level, Color color, string message)
        {
            return new Logging.LogMessage(level, color, TimestampType, DateTime.Now, _stopwatch.Elapsed, message);
        }

        private static Thread _logThread;

        private static readonly object _threadStateLock = new object();

        private static readonly AutoResetEvent _stopSignal = new AutoResetEvent(false);

        private static readonly object _instancesLock = new object();

        private static readonly List<StyxLog> _instances = new List<StyxLog>();

        private readonly object _fileLock = new object();

        private readonly ConcurrentQueue<Logging.LogMessage> _messageQueue = new ConcurrentQueue<Logging.LogMessage>();

        private bool _fileLoggingEnabled;

        private readonly Stopwatch _stopwatch;
    }
}
