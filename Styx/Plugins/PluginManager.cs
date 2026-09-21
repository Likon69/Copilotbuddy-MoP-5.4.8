using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Styx.Common;
using Styx.Helpers;

namespace Styx.Plugins
{
    public static class PluginManager
    {
        static PluginManager()
        {
            if (Plugins == null)
                Plugins = new List<PluginContainer>();

            var roflmao = new DataTable(); // Why do this? "using System.Data;" is auto added when you start a solution. Some developers forget it there.
            // When we release Honorbuddy, the compiler is smart enough to know that we aren't actually using it, so it removes it. But then we get compiler
            // errors in some plug-ins that use it.

            // XXXX - Contributor002 6/21/2010 - There's a simple fix. LOAD THE DAMNED ASSEMBLY MANUALLY. <-- that's not near as simple as constructing something from it, lol.
            //                              The other solution, is to not even load it at all. Just add it as a reference in the compiler arguments.

            Logging.WriteDiagnostic(roflmao.ToString());
        }

        public static bool IsInitialized { get; private set; }

        public static List<PluginContainer> Plugins { get; private set; }

        internal static void Pulse()
        {
            foreach (PluginContainer pluginContainer in Plugins)
            {
                if (!pluginContainer.Enabled)
                    continue;

                pluginContainer.Plugin.Pulse();
            }
        }

        private static readonly object _initializeLocker = new object();

        /// <summary>
        /// Initializes the plugin manager, which means loads and compiles all plugins it can find the /Plugins/ folder.
        /// </summary>
        public static void Initialize(params string[] defaultEnabled)
        {
            lock (_initializeLocker)
            {
                if (IsInitialized)
                    return;

                RefreshPlugins(defaultEnabled);

                Logging.Write("Installed plugins");
                foreach (PluginContainer pluginContainer in Plugins)
                {
                    Logging.Write("    {0} - {1}", pluginContainer.Name, pluginContainer.Enabled ? "Enabled" : "Disabled");
                }

                IsInitialized = true;
            }
        }

        internal static void AddPlugin(HBPlugin plugin, bool enabled)
        {
            var pluginContainer = new PluginContainer(plugin);
            Plugins.Add(pluginContainer);
            Plugins.Sort();
            pluginContainer.Enabled = enabled;
        }

        /// <summary>
        /// Gets the routine directory.
        /// </summary>
        /// <remarks>Created 2012-06-03</remarks>
        public static string PluginsDirectory
        {
            get
            {
                string path = GlobalSettings.Instance.PluginsPath;
                if (!Path.IsPathRooted(path))
                    path = Path.Combine(Utilities.AssemblyDirectory, path);
                return path;
            }
        }

        private static AssemblyLoader<HBPlugin> _pluginsLoader;
        private static bool _isBuildingPlugins;

        public static void RefreshPlugins(params string[] defaultEnabled)
        {
            lock (_initializeLocker)
            {
                if (_isBuildingPlugins)
                    return;

                try
                {
                    _isBuildingPlugins = true;

                    foreach (PluginContainer pluginContainer in Plugins)
                    {
                        pluginContainer.Enabled = false;
                    }

                    Plugins.Clear();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    string pluginsPath = Path.Combine(Utilities.AssemblyDirectory, PluginsDirectory);
                    if (!Directory.Exists(pluginsPath))
                    {
                        Directory.CreateDirectory(pluginsPath);
                    }
                    else
                    {
                        if (_pluginsLoader == null)
                            _pluginsLoader = new AssemblyLoader<HBPlugin>(PluginsDirectory, GlobalSettings.Instance.ReloadPluginsOnFileChange);

                        _pluginsLoader.Reload("RefreshPlugins Called");
                        foreach (HBPlugin plugin in _pluginsLoader.Instances)
                        {
                            bool enabled = (defaultEnabled != null && defaultEnabled.Contains(plugin.Name)) || plugin.Name == "Refreshment Detection";
                            AddPlugin(plugin, enabled);
                        }
                    }
                }
                finally
                {
                    _isBuildingPlugins = false;
                }
            }
        }
    }
}
