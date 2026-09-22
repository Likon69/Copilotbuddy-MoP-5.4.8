using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Styx.Helpers
{
    public class UISettings : Settings
    {
        public static readonly UISettings Instance = new UISettings();

        public UISettings()
            : base(Path.Combine(Settings.CharacterSettingsDirectory, "UISettings.xml"), null)
        {
        }

        [Setting]
        [DefaultValue(true)]
        public bool EnhancedMode { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int DevToolsWindowLocationX { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int DevToolsWindowLocationY { get; set; }

        [Setting]
        public int DevToolsWindowWidth { get; set; }

        [Setting]
        public int DevToolsWindowHeight { get; set; }

        [Setting]
        [DefaultValue(WindowState.Normal)]
        public WindowState DevToolsWindowState { get; set; }

        [DefaultValue(20)]
        [Setting]
        public int MainWindowLocationX { get; set; }

        [DefaultValue(20)]
        [Setting]
        public int MainWindowLocationY { get; set; }

        [Setting]
        public int MainWindowWidth { get; set; }

        [Setting]
        public int MainWindowHeight { get; set; }

        [DefaultValue(WindowState.Normal)]
        [Setting]
        public WindowState MainWindowState { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int PluginsWindowLocationX { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int PluginsWindowLocationY { get; set; }

        [Setting]
        public int PluginsWindowWidth { get; set; }

        [Setting]
        public int PluginsWindowHeight { get; set; }

        [Setting]
        [DefaultValue(WindowState.Normal)]
        public WindowState PluginsWindowState { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int SettingsWindowLocationX { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int SettingsWindowLocationY { get; set; }

        [Setting]
        public int SettingsWindowWidth { get; set; }

        [Setting]
        public int SettingsWindowHeight { get; set; }

        [Setting]
        [DefaultValue(WindowState.Normal)]
        public WindowState SettingsWindowState { get; set; }
    }
}
