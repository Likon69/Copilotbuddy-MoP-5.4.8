using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using Styx.Common;

namespace Styx.SafeLaunch
{
	internal static class SafeLauncher
	{
		private static readonly Random random_0 = new Random(Guid.NewGuid().GetHashCode());

		public static void Launch()
		{
			FilePermissionHelper filePermissionHelper = new FilePermissionHelper();
			bool isSystem;
			using (WindowsIdentity current = WindowsIdentity.GetCurrent())
			{
				isSystem = current.IsSystem;
			}
			if (!isSystem)
			{
				string path = Path.Combine(Utilities.AssemblyDirectory, "Cache", SafeLauncher.GenerateRandomFileName("PsExec") + ".etmp");
				try
				{
					File.Delete(path);
				}
				catch
				{
				}
				string text = Path.Combine(Utilities.AssemblyDirectory, "Cache", SafeLauncher.GenerateRandomFolderName("PsExec") + ".etmp");
				if (!File.Exists(text))
				{
					using (GlobalSemaphore.Acquire(string.Format("{0}", text).GetHashCode().ToString()))
					{
						if (!File.Exists(text))
						{
							SafeLauncher.ExtractEmbeddedResource("CopilotBuddy.SafeLaunch.PsExec.exe", text);
						}
					}
				}
				string text2 = Guid.NewGuid().ToString("N");
				using (NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream("Global\\" + text2))
				{
					string arg = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Concat(new string[]
					{
						string.Format("/startpipe={0}", text2)
					}).Select(new Func<string, string>(ArgumentQuoter.QuoteArgument)));
					string psArgs = string.Format("-accepteula -nobanner -i -s \"{0}\" {1}", Environment.ProcessPath, arg);
					Process.Start(new ProcessStartInfo(text, psArgs)
					{
						CreateNoWindow = true,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true
					});
					namedPipeServerStream.WaitForConnection();
					namedPipeServerStream.CopyTo(Console.OpenStandardOutput());
				}
				Environment.Exit(0);
			}
			if (Environment.GetEnvironmentVariable("COPILOT_SAFELAUNCHED") == "1")
			{
				return;
			}
			string path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), SafeLauncher.GenerateRandomFileName("MiniLoader") + ".exe");
			try
			{
				File.Delete(path2);
			}
			catch
			{
			}
			string text3 = SafeLauncher.GenerateRandomFolderName("MiniLoader");
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			string text4 = Path.Combine(folderPath, text3);
			DirectoryInfo directoryInfo = null;
			if (!Directory.Exists(text4))
			{
				directoryInfo = Directory.CreateDirectory(text4);
			}
			filePermissionHelper.miniLoaderPath = Path.Combine(text4, text3 + ".exe");
			if (!File.Exists(filePermissionHelper.miniLoaderPath))
			{
				using (GlobalSemaphore.Acquire(string.Format("{0}", filePermissionHelper.miniLoaderPath).GetHashCode().ToString()))
				{
					if (!File.Exists(filePermissionHelper.miniLoaderPath))
					{
						SafeLauncher.ExtractEmbeddedResource("CopilotBuddy.SafeLaunch.MiniLoader.exe", filePermissionHelper.miniLoaderPath);
						Utilities.RetryingFileOperation(new Action(filePermissionHelper.SetPermissions), 5, 20);
					}
				}
			}
			if (directoryInfo != null)
			{
				string[] directories = Directory.GetDirectories(folderPath);
				DirectoryInfo directoryInfo2 = (directories.Length != 0) ? new DirectoryInfo(directories[SafeLauncher.random_0.Next(directories.Length)]) : null;
				directoryInfo.CreationTime = SafeLauncher.RandomizeTimestamp(directoryInfo2.CreationTime);
				directoryInfo.LastAccessTime = SafeLauncher.RandomizeTimestamp(directoryInfo2.LastAccessTime);
				directoryInfo.LastWriteTime = SafeLauncher.RandomizeTimestamp(directoryInfo2.LastWriteTime);
			}
			string text5 = Environment.GetCommandLineArgs().SingleOrDefault(new Func<string, bool>(ArgumentQuoter.IsStartPipeArgument));
			string arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Where(new Func<string, bool>(ArgumentQuoter.IsNotStartPipeArgument)).Select(new Func<string, string>(ArgumentQuoter.QuoteArgument)));
			ProcessStartInfo startInfo = new ProcessStartInfo(filePermissionHelper.miniLoaderPath, arguments)
			{
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = false,
				WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
			};
			DateTime dateTime = default(DateTime);
			Stopwatch stopwatch = new Stopwatch();
			Process process;
			try
			{
				DateTime value = Process.GetProcessesByName("System")[0].StartTime.ToUniversalTime().AddMilliseconds(800.0 + SafeLauncher.random_0.NextDouble() * 5000.0);
				SafeLauncher.SystemTime systemTime = new SafeLauncher.SystemTime(value);
				dateTime = DateTime.UtcNow;
				stopwatch.Restart();
				SafeLauncher.SetSystemTime(ref systemTime);
				process = Process.Start(startInfo);
			}
			finally
			{
				if (stopwatch.IsRunning)
				{
					SafeLauncher.SystemTime systemTime2 = new SafeLauncher.SystemTime(dateTime.AddTicks(stopwatch.ElapsedTicks));
					SafeLauncher.SetSystemTime(ref systemTime2);
				}
			}
			using (process)
			{
				process.StandardInput.WriteLine(Assembly.GetExecutingAssembly().Location);
				Console.WriteLine("Launched process with PID {0}", process.Id);
				if (text5 != null)
				{
					using (NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream("Global\\" + text5.Substring("/startpipe=".Length)))
					{
						using (StreamWriter streamWriter = new StreamWriter(namedPipeClientStream, new UTF8Encoding(false), 4096, true))
						{
							namedPipeClientStream.Connect();
							streamWriter.WriteLine("Launched process with PID {0}", process.Id);
						}
					}
				}
			}
			Environment.Exit(0);
		}

		private static string GenerateRandomFolderName(string seed)
		{
			SafeLauncher.RandomState randomState;
			randomState.random = new Random(string.Format("|{0}|{1}|{2}|2", Assembly.GetExecutingAssembly().Location, seed, SafeLauncher.GetMachineGuid() ?? "stoplooking").GetHashCode());
			List<char> list = new List<char>();
			List<char> list2 = new List<char>();
			StringBuilder stringBuilder = new StringBuilder(8);
			for (int i = 0; i < stringBuilder.Capacity; i++)
			{
				List<char> list3 = (i % 2 == 0) ? list2 : list;
				if (list3.Count == 0)
				{
					list3.AddRange((i % 2 == 0) ? "tnshrd" : "aeiou");
					SafeLauncher.Shuffle(list3, ref randomState);
				}
				stringBuilder.Append(list3.Last<char>());
				list3.RemoveAt(list3.Count - 1);
			}
			stringBuilder[0] = char.ToUpper(stringBuilder[0]);
			return stringBuilder.ToString();
		}

		private static string GetMachineGuid()
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", false);
			return (string)((registryKey != null) ? registryKey.GetValue("MachineGuid") : null);
		}

		private static string GenerateRandomFileName(string seed)
		{
			Random random = new Random(string.Format("|{0}|{1}|1|", Assembly.GetExecutingAssembly().Location, seed).GetHashCode());
			StringBuilder stringBuilder = new StringBuilder(10);
			for (int i = 0; i < stringBuilder.Capacity; i++)
			{
				stringBuilder.Append("abcdefghijklmnopqrstuvwxyz"[random.Next("abcdefghijklmnopqrstuvwxyz".Length)]);
			}
			return stringBuilder.ToString();
		}

		private static void ExtractEmbeddedResource(string resourceName, string targetPath)
		{
			ResourceExtractor resourceExtractor = new ResourceExtractor();
			resourceExtractor.targetPath = targetPath;
			resourceExtractor.resourceName = resourceName;
			Utilities.RetryingFileOperation(new Action(resourceExtractor.Extract), 5, 20);
		}

		private static void SetFilePermissions(string path)
		{
			FileSecurity fileSecurity = new FileSecurity();
			fileSecurity.SetAccessRuleProtection(true, false);
			fileSecurity.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().Name, FileSystemRights.FullControl, AccessControlType.Allow));
			FileSystemAclExtensions.SetAccessControl(new FileInfo(path), fileSecurity);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetSystemTime(ref SafeLauncher.SystemTime systemTime);

		[CompilerGenerated]
		internal static DateTime RandomizeTimestamp(DateTime baseTime)
		{
			DateTime dateTime = baseTime.AddDays(20.0);
			if (dateTime > DateTime.Now)
			{
				dateTime = DateTime.Now;
			}
			return dateTime - TimeSpan.FromDays(SafeLauncher.random_0.NextDouble() * 20.0 * 2.0);
		}

		[CompilerGenerated]
		internal static void Shuffle(List<char> list, ref SafeLauncher.RandomState state)
		{
			for (int i = list.Count - 1; i > 0; i--)
			{
				int index = state.random.Next(0, i + 1);
				char value = list[i];
				list[i] = list[index];
				list[index] = value;
			}
		}

		private struct SystemTime
		{
			public SystemTime(DateTime value)
			{
				checked
				{
					this.year = (ushort)value.Year;
					this.month = (ushort)value.Month;
					this.dayOfWeek = (ushort)value.DayOfWeek;
					this.day = (ushort)value.Day;
					this.hour = (ushort)value.Hour;
					this.minute = (ushort)value.Minute;
					this.second = (ushort)value.Second;
					this.millisecond = (ushort)value.Millisecond;
				}
			}

			public ushort year;

			public ushort month;

			public ushort dayOfWeek;

			public ushort day;

			public ushort hour;

			public ushort minute;

			public ushort second;

			public ushort millisecond;
		}

		[CompilerGenerated]
		private sealed class FilePermissionHelper
		{
			internal void SetPermissions()
			{
				SafeLauncher.SetFilePermissions(this.miniLoaderPath);
			}

			public string miniLoaderPath;
		}

		[CompilerGenerated]
		[Serializable]
		private sealed class ArgumentQuoter
		{
			internal static string QuoteArgument(string arg)
			{
				return string.Format("\"{0}\"", arg);
			}

			internal static bool IsStartPipeArgument(string arg)
			{
				return arg.StartsWith("/startpipe=", StringComparison.OrdinalIgnoreCase);
			}

			internal static bool IsNotStartPipeArgument(string arg)
			{
				return !arg.StartsWith("/startpipe=", StringComparison.OrdinalIgnoreCase);
			}

			public static readonly SafeLauncher.ArgumentQuoter Instance = new SafeLauncher.ArgumentQuoter();
		}

		[CompilerGenerated]
		[StructLayout(LayoutKind.Auto)]
		internal struct RandomState
		{
			public Random random;
		}

		[CompilerGenerated]
		private sealed class ResourceExtractor
		{
			internal void Extract()
			{
				Directory.CreateDirectory(Path.GetDirectoryName(this.targetPath));
				using (Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(this.resourceName))
				{
					using (Stream stream = File.Create(this.targetPath))
					{
						manifestResourceStream.CopyTo(stream);
					}
				}
			}

			public string targetPath;

			public string resourceName;
		}
	}
}
