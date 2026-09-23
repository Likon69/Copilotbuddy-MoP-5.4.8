using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Styx.Common;
using Styx.CommonBot;
using Styx.Helpers;

namespace Bots.Professionbuddy
{
	public class PBLog
	{
		private const string NormalHeader = "PB: ";

		private static RichTextBox _rtbLog;

		public static void Warn(string msg)
		{
			LogInvoker(LogLevel.Verbose, Colors.DodgerBlue, "PB Warning: ", Colors.DarkOrange, msg);
		}

		public static void Warn(string format, params object[] args)
		{
			Warn(string.Format(format, args));
		}

		public static void Fatal(string msg)
		{
			LogInvoker(LogLevel.Verbose, Colors.DodgerBlue, "PB Fatal: ", Colors.Red, msg);
			TreeRoot.Stop();
		}

		public static void Fatal(string format, params object[] args)
		{
			Fatal(string.Format(format, args));
			TreeRoot.Stop();
		}

		public static void Debug(string msg)
		{
			LogInvoker(LogLevel.Diagnostic, Colors.DodgerBlue, NormalHeader, Colors.LimeGreen, msg);
		}

		public static void Debug(string format, params object[] args)
		{
			Debug(string.Format(format, args));
		}

		public static void Log(string msg)
		{
			LogInvoker(LogLevel.Normal, Colors.DodgerBlue, NormalHeader, Colors.LightSteelBlue, msg);
		}

		public static void Log(string format, params object[] args)
		{
			Log(string.Format(format, args));
		}

		public static void Log(System.Drawing.Color headerColor, string header, System.Drawing.Color msgColor, string format, params object[] args)
		{
			LogInvoker(
				LogLevel.Normal,
				System.Windows.Media.Color.FromArgb(headerColor.A, headerColor.R, headerColor.G, headerColor.B),
				header,
				System.Windows.Media.Color.FromArgb(msgColor.A, msgColor.R, msgColor.G, msgColor.B),
				string.Format(format, args));
		}

		public static void Log(
			System.Windows.Media.Color headerColor,
			string header,
			System.Windows.Media.Color msgColor,
			string format,
			params object[] args)
		{
			LogInvoker(LogLevel.Normal, headerColor, header, msgColor, string.Format(format, args));
		}

		public static void Log(
			LogLevel logLevel,
			System.Windows.Media.Color headerColor,
			string header,
			System.Windows.Media.Color msgColor,
			string format,
			params object[] args)
		{
			LogInvoker(logLevel, headerColor, header, msgColor, string.Format(format, args));
		}

		private static void LogInvoker(
			LogLevel level,
			System.Windows.Media.Color headerColor,
			string header,
			System.Windows.Media.Color msgColor,
			string msg)
		{
			if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
				LogInternal(level, headerColor, header, msgColor, msg);
			else
				Application.Current.Dispatcher.BeginInvoke(
					new LogDelegate(LogInternal),
					level,
					headerColor,
					header,
					msgColor,
					msg);
		}

		private static void LogInternal(
			LogLevel level,
			System.Windows.Media.Color headerColor,
			string header,
			System.Windows.Media.Color msgColor,
			string msg)
		{
			if (level == LogLevel.None)
				return;
			try
			{
				if (GlobalSettings.Instance.LogLevel >= level)
				{
					if (_rtbLog == null)
						_rtbLog = (RichTextBox)Application.Current.MainWindow.FindName("rtbLog");

					var para = (Paragraph)_rtbLog.Document.Blocks.FirstBlock;
					para.Inlines.Add(new Run(header) { Foreground = new SolidColorBrush(headerColor) });
					para.Inlines.Add(new Run(msg + Environment.NewLine) { Foreground = new SolidColorBrush(msgColor) });
					_rtbLog.ScrollToEnd();
				}
				Logging.WriteToFileSync(level, header + msg);
			}
			catch
			{
				Logging.Write(header + msg);
			}
		}

		private delegate void LogDelegate(
			LogLevel level,
			System.Windows.Media.Color headerColor,
			string header,
			System.Windows.Media.Color msgColor,
			string msg);
	}
}
