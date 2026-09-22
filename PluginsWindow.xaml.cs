using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Styx.Helpers;
using Styx.Localization;
using Styx.Plugins;

namespace CopilotBuddy
{
    /// <summary>
    /// Ported from Honorbuddy 5.4.8 Honorbuddy.PluginsWindow (logic unchanged; window chrome
    /// follows the WotLK reference project's MahApps styling).
    /// </summary>
    public partial class PluginsWindow : MetroWindow
    {
        public PluginsWindow()
        {
            InitializeComponent();

            SetStaticLabels();

            lstPlugins.ItemsSource = PluginManager.Plugins;

            if (UISettings.Instance.PluginsWindowWidth > 0)
                Width = UISettings.Instance.PluginsWindowWidth;
            if (UISettings.Instance.PluginsWindowHeight > 0)
                Height = UISettings.Instance.PluginsWindowHeight;
            Left = UISettings.Instance.PluginsWindowLocationX;
            Top = UISettings.Instance.PluginsWindowLocationY;
            WindowState = UISettings.Instance.PluginsWindowState;
        }

        private void SetStaticLabels()
        {
            Title = Globalization.PluginsWindowTitle;
            tbAuthorLabelText.Text = Globalization.AuthorLabelText;
            tbVersionLabelText.Text = Globalization.VersionLabelText;
            tbSettingsButtonText.Text = Globalization.PluginSettingsButtonText;
            tbRecompileButtonText.Text = Globalization.PluginRecompileAllButtonText;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (lstPlugins.SelectedItem is PluginContainer pluginContainer && pluginContainer.Plugin != null)
                pluginContainer.Plugin.OnButtonPress();
        }

        private void lstPlugins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstPlugins.SelectedItem is PluginContainer pluginContainer)
            {
                tbSettingsButtonText.Text = pluginContainer.ButtonText;
                lblAuthor.Content = pluginContainer.Author;
                lblVersion.Content = pluginContainer.Version;
                btnSettings.IsEnabled = pluginContainer.WantButton;
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CharacterSettings.Instance.EnabledPlugins = PluginManager.Plugins
                .Where(p => p.Enabled)
                .Select(p => p.Name)
                .ToArray();
        }

        private void btnRecompile_Click(object sender, RoutedEventArgs e)
        {
            PluginManager.RefreshPlugins(CharacterSettings.Instance.EnabledPlugins);
            lstPlugins.ItemsSource = PluginManager.Plugins;
        }

        private void SaveLocation()
        {
            UISettings.Instance.PluginsWindowHeight = (int)Height;
            UISettings.Instance.PluginsWindowWidth = (int)Width;
            UISettings.Instance.PluginsWindowState = WindowState;
            if (Left == RestoreBounds.Left && Top == RestoreBounds.Top)
            {
                UISettings.Instance.PluginsWindowLocationX = (int)Left;
                UISettings.Instance.PluginsWindowLocationY = (int)Top;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveLocation();
            UISettings.Instance.Save();
        }
    }
}
