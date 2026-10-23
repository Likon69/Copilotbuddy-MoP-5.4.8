using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace MiniLoader
{
	internal static class Program
	{
		[STAThread]
		private static int Main(string[] args)
		{
			string dllPath = Console.ReadLine();
			if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
			{
				return 1;
			}

			Environment.SetEnvironmentVariable("COPILOT_SAFELAUNCHED", "1");

			string appDir = Path.GetDirectoryName(Path.GetFullPath(dllPath));
			string depsFile = Path.ChangeExtension(dllPath, ".deps.json");

			AssemblyLoadContext loadContext = AssemblyLoadContext.Default;
			loadContext.Resolving += (context, assemblyName) =>
			{
				string candidate = Path.Combine(appDir, assemblyName.Name + ".dll");
				if (File.Exists(candidate))
				{
					return context.LoadFromAssemblyPath(candidate);
				}
				return null;
			};

			Assembly assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(dllPath));
			MethodInfo entryPoint = assembly.EntryPoint;
			if (entryPoint == null)
			{
				return 1;
			}

			ParameterInfo[] parameters = entryPoint.GetParameters();
			if (parameters.Length == 0)
			{
				entryPoint.Invoke(null, null);
			}
			else
			{
				entryPoint.Invoke(null, new object[] { args });
			}

			return 0;
		}
	}
}
