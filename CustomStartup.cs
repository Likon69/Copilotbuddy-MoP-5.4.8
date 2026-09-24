using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Styx.Common;
using Styx.SafeLaunch;

namespace CopilotBuddy
{
	public static class CustomStartup
	{
		[STAThread]
		public static void Main()
		{
			Directory.CreateDirectory(Path.Combine(Utilities.AssemblyDirectory, "Cache"));

			SafeLauncher.Launch();

			FieldInfo resourceField = typeof(Application).GetField("_resourceAssembly", BindingFlags.Static | BindingFlags.NonPublic);
			resourceField.SetValue(null, typeof(App).Assembly);
			App app = new App();
			app.InitializeComponent();
			app.Run(new MainWindow());
		}
	}
}
