using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Styx.Common
{
    public static class Utilities
    {
        public static string AssemblyDirectory
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static DateTime ConvertFromUnixTimestamp(ulong timestamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return dateTime.AddSeconds(timestamp);
        }

        public static int ConvertToUnixTimestamp(DateTime date)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (int)(date - dateTime).TotalSeconds;
        }

        [Obsolete("Use StyxWoW.WoWClient.PerformanceCounterToDateTime instead")]
        public static DateTime PerformanceCounterToDateTime(ulong performanceCounter)
        {
            return DateTime.Now.Subtract(TimeSpan.FromMilliseconds((double)Environment.TickCount)).AddMilliseconds(performanceCounter);
        }

        public static bool IsWindowsXp
        {
            get
            {
                return Environment.OSVersion.Version.Major == 5;
            }
        }

        [Conditional("DEBUG")]
        public static void PrintStackTrace(string reason = "Debug")
        {
            Logging.Write("Stack trace for {0}:{1}{2}", new object[]
            {
                reason,
                Environment.NewLine,
                Utilities.GetStackTrace(2, 10)
            });
        }

        public static string GetStackTrace(int startFrame = 1, int frameCount = 10)
        {
            if (startFrame < 0)
            {
                throw new ArgumentOutOfRangeException("startFrame");
            }
            if (frameCount < -1)
            {
                throw new ArgumentOutOfRangeException("frameCount");
            }
            StackTrace stackTrace = new StackTrace(startFrame, true);
            string text = stackTrace.ToString();
            if (text.EndsWith(Environment.NewLine))
            {
                text = text.Remove(text.Length - Environment.NewLine.Length);
            }
            if (frameCount == -1)
            {
                return text;
            }
            string[] array = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            int num = Math.Min(array.Length, frameCount);
            return string.Join(Environment.NewLine, array.Take(num));
        }

        public static string GetObjectString(object obj, string nullRet)
        {
            if (obj == null)
            {
                return nullRet;
            }
            return obj.ToString();
        }

        public static string FormatString([Localizable(true)] string format, params object[] args)
        {
            string text;
            try
            {
                text = string.Format(Thread.CurrentThread.CurrentUICulture, format, args);
            }
            catch (Exception ex)
            {
                text = string.Format("FS_EMPTY {0} {1}", ex.GetType().Name, format.ToString());
            }
            return text;
        }

        public static string BuildCompilerErrorsString(CompilerResults results)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (object obj in results.Errors)
            {
                CompilerError compilerError = (CompilerError)obj;
                if (!compilerError.IsWarning)
                {
                    stringBuilder.AppendLine(string.Format("File: {0} Line: {1} {3}: {2}", new object[]
                    {
                        Path.GetFileName(compilerError.FileName),
                        compilerError.Line,
                        compilerError.ErrorText,
                        compilerError.IsWarning ? "Warning" : "Error"
                    }));
                }
            }
            return stringBuilder.ToString();
        }

        public static int FNV1a(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return (int)bytes[3] ^ (16777619 * ((int)bytes[2] ^ (16777619 * ((int)bytes[1] ^ (16777619 * ((int)bytes[0] ^ (16777619 * (value ^ -2128831035))))))));
        }

        public static string GetRelativePath(string basePath, string path)
        {
            Uri uri = new Uri(path);
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }
            Uri uri2 = new Uri(basePath);
            return Uri.UnescapeDataString(uri2.MakeRelativeUri(uri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static string GetRelativePath(string path)
        {
            return Utilities.GetRelativePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), path);
        }

        public static void RetryingFileOperation(Action action, int retries, int delayMs)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException)
                {
                    if (i == retries - 1)
                        throw;
                    Thread.Sleep(delayMs);
                }
                catch (UnauthorizedAccessException)
                {
                    if (i == retries - 1)
                        throw;
                    Thread.Sleep(delayMs);
                }
            }
        }
    }
}
