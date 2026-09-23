using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using MahApps.Metro.Controls;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.CharacterManagement;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Localization;
using Styx.WoWInternals;

namespace CopilotBuddy
{
    public partial class SettingsWindow : MetroWindow
    {
        public SettingsWrap Wrap { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();

            SetStaticLabels();

            Wrap = new SettingsWrap
            {
                CharSettings = CharacterSettings.Instance,
                StyxSettings = GlobalSettings.Instance
            };

            List<string> groundMounts = Mount.GroundMounts.Select(m => m.Name).ToList();
            List<string> flyingMounts = Mount.FlyingMounts.Select(m => m.Name).ToList();

            if (StyxWoW.Me.Class == WoWClass.Druid)
            {
                if (SpellManager.HasSpell("Flight Form"))
                    flyingMounts.Add("Flight Form");
                if (StyxWoW.Me.Level >= 58)
                    flyingMounts.Add("Travel Form");
            }
            if (StyxWoW.Me.Race == WoWRace.Worgen && SpellManager.HasSpell("Running Wild"))
                groundMounts.Add("Running Wild");

            Wrap.GroundMounts = groundMounts.OrderBy(m => m).ToList();
            Wrap.FlyingMounts = flyingMounts.OrderBy(m => m).ToList();

            DataContext = Wrap;

            btnClassConfig.IsEnabled = RoutineManager.Current != null && RoutineManager.Current.WantButton;

            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
                cbLogLevel.Items.Add(level);
            cbLogLevel.SelectedItem = Logging.LoggingLevel;

            var classProfiles = new List<ClassProfileComboBoxEntry>();
            foreach (var classProfile in CharacterManager.ClassProfiles.Where(cp => StyxWoW.Me == null || StyxWoW.Me.Class == cp.Class))
                classProfiles.Add(new ClassProfileComboBoxEntry(classProfile));

            foreach (var entry in classProfiles)
            {
                if (classProfiles.Any(other => other != entry && entry.ClassProfile.GetSpec() == other.ClassProfile.GetSpec()))
                    entry.FullName = true;
            }

            classProfiles.Sort((a, b) => StringComparer.InvariantCultureIgnoreCase.Compare(a.ToString(), b.ToString()));
            foreach (var entry in classProfiles)
            {
                cbClassProfile.Items.Add(entry);
                if (CharacterSettings.Instance.ClassProfileName == entry.ClassProfile.Name)
                    cbClassProfile.SelectedItem = entry;
            }

            for (WoWItemQuality quality = WoWItemQuality.Uncommon; quality <= WoWItemQuality.Epic; quality += 1)
            {
                cbMaxBoEDisenchantQuality.Items.Add(quality);
                if (quality == CharacterSettings.Instance.MaxBoEDisenchantQuality)
                    cbMaxBoEDisenchantQuality.SelectedItem = quality;
            }

            if (UISettings.Instance.SettingsWindowWidth > 0)
                Width = UISettings.Instance.SettingsWindowWidth;
            if (UISettings.Instance.SettingsWindowHeight > 0)
                Height = UISettings.Instance.SettingsWindowHeight;
            Left = UISettings.Instance.SettingsWindowLocationX;
            Top = UISettings.Instance.SettingsWindowLocationY;
            WindowState = UISettings.Instance.SettingsWindowState;
        }

        private void SetStaticLabels()
        {
            Title = Globalization.SettingsWindowTitle;
            tbDevToolsText.Text = Globalization.DeveloperToolsButtonText;
            tbPluginsText.Text = Globalization.PluginsButtonText;
            tbClassConfigText.Text = Globalization.ClassConfigButtonText;
            tbBotConfigText.Text = Globalization.BotConfigButtonText;
            tbSaveAndCloseText.Text = Globalization.SaveAndCloseButtonText;
            tbChangeText.Text = Globalization.ChangeButtonText;

            gbGeneral.Header = Globalization.GeneralGroupHeader;
            gbMesh.Header = Globalization.GeneralGroupHeader;
            gbCharManager.Header = Globalization.CharManagerGroupHeader;
            gbCharManager2.Header = Globalization.CharManagerGroupHeader;
            gbLooting.Header = Globalization.LootingGroupHeader;
            gbVendors.Header = Globalization.VendorsGroupHeader;
            gbNames.Header = Globalization.MailAndRefreshmentsGroupHeader;
            gbMount.Header = Globalization.MountGroupHeader;

            tbKillBetweenHotspots.Text = Globalization.KillBetweenHotspotsCheckboxText;
            tbLogoutForInactivity.Text = Globalization.LogoutForInactivityCheckboxText;
            tbRessAtSpiritHealer.Text = Globalization.ResAtSpiritHealer;
            tbSeperatedLogFolders.Text = Globalization.SeperatedLogFolders;
            lblLogLevel.Content = Globalization.LogLevel;

            tbUseFrameLock.Text = Globalization.UseFramelockText;
            lblTps.Content = Globalization.TicksPerSecondLabelText;
            sldTps.ToolTip = Globalization.TpsTooltipText;
            lblMeshFolderPath.Content = Globalization.MeshFolderPathLabelText;

            lblClassProfile.Content = Globalization.ClassProfileLabelText;
            lblClassProfile.ToolTip = Globalization.ClassProfileToolTip;
            cbClassProfile.ToolTip = Globalization.ClassProfileToolTip;
            tbAutoEquip.Text = Globalization.AutoEquipCheckBoxText;
            chkAutoEquip.ToolTip = Globalization.AutoEquipCheckBoxToolTip;
            tbAutoEquipBoEEpics.Text = Globalization.AutoEquipBoEEpicsCheckBoxText;
            chkAutoEquipBoEEpics.ToolTip = Globalization.AutoEquipBoEEpicsCheckBoxToolTip;
            tbSelectTalents.Text = Globalization.SelectTalentsCheckBoxText;
            chkSelectTalents.ToolTip = Globalization.SelectTalentsCheckBoxToolTip;

            tbRollOnItems.Text = Globalization.RollOnItemsCheckBoxText;
            chkRollOnItems.ToolTip = Globalization.RollOnItemsCheckBoxToolTip;
            tbRollDisenchantWhenGreed.Text = Globalization.RollDisenchantWhenGreedCheckBoxText;
            chkRollDisenchantWhenGreeding.ToolTip = Globalization.RollDisenchantWhenGreedingCheckBoxToolTip;
            lblMaxBoEDisenchantQuality.ToolTip = Globalization.MaxBoEDisenchantQualityTooltip;
            cbMaxBoEDisenchantQuality.ToolTip = Globalization.MaxBoEDisenchantQualityTooltip;

            tbLootMobs.Text = Globalization.LootMobsCheckboxText;
            tbSkinMobs.Text = Globalization.SkinMobsCheckboxText;
            tbNinjaSkin.Text = Globalization.NinjaSkinCheckboxText;
            tbHarvestMinerals.Text = Globalization.HarvestMinesCheckboxText;
            tbHarvestHerbs.Text = Globalization.HarvestHerbsCheckboxText;
            lblLootRadius.Content = Globalization.LootRadiusLabelText;

            lblDrinkAmount.Content = Globalization.DrinkAmountLabelText;
            lblFoodAmount.Content = Globalization.FoodAmountLabelText;

            lblMailRecipient.Content = Globalization.MailRecipientLabelText;
            lblFoodName.Content = Globalization.FoodNameOrIdLabelText;
            lblDrinkName.Content = Globalization.DrinkNameOrIdLabelText;

            lblMountName.Content = Globalization.MountNameLabelText;
            lblFlyingMountName.Content = Globalization.FlyingMountLabelText;
            tbRandomMount.Text = Globalization.RandomMountCheckboxText;
            lblMountDistance.Content = Globalization.MountDistanceLabelText;
        }

        private void btnSaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            Wrap.CharSettings.Save();
            Wrap.StyxSettings.Save();
            DataContext = null;
            Close();
        }

        private void btnClassConfig_Click(object sender, RoutedEventArgs e)
        {
            if (RoutineManager.Current != null && RoutineManager.Current.WantButton)
                RoutineManager.Current.OnButtonPress();
        }

        private void btnBotConfig_Click(object sender, RoutedEventArgs e)
        {
            if (BotManager.Current == null)
                return;

            Form configurationForm = BotManager.Current.ConfigurationForm;
            if (configurationForm != null && !configurationForm.IsDisposed && !configurationForm.Disposing)
                configurationForm.Show();
        }

        private void btnPlugins_Click(object sender, RoutedEventArgs e)
        {
            var pluginsWindow = new PluginsWindow { Owner = this };
            pluginsWindow.Show();
        }

        private void btnDevTools_Click(object sender, RoutedEventArgs e)
        {
            Logging.Write(Colors.OrangeRed, "Developer Tools window is not available in this build.");
        }

        private void btnDiscord_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://discord.gg/ep5TcGMCcB",
                UseShellExecute = true
            });
        }

        private void btnChangeMeshPath_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog
            {
                Description = Globalization.SelectMeshDirectory
            };
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Wrap.StyxSettings.MeshesFolderPath = folderBrowserDialog.SelectedPath;
        }

        private void SaveLocation()
        {
            UISettings.Instance.SettingsWindowHeight = (int)Height;
            UISettings.Instance.SettingsWindowWidth = (int)Width;
            UISettings.Instance.SettingsWindowState = WindowState;
            if (Left == RestoreBounds.Left && Top == RestoreBounds.Top)
            {
                UISettings.Instance.SettingsWindowLocationX = (int)Left;
                UISettings.Instance.SettingsWindowLocationY = (int)Top;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveLocation();
            UISettings.Instance.Save();
        }

        private void cbLogLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbLogLevel.SelectedItem is LogLevel level)
            {
                Logging.LoggingLevel = level;
                GlobalSettings.Instance.LogLevel = level;
            }
        }

        private void cbClassProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbClassProfile.SelectedItem is ClassProfileComboBoxEntry entry)
                CharacterManager.SetClassProfile(entry.ClassProfile);
        }

        private void cbMaxBoEDisenchantQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbMaxBoEDisenchantQuality.SelectedItem is WoWItemQuality quality)
                CharacterSettings.Instance.MaxBoEDisenchantQuality = quality;
        }

        private class ClassProfileComboBoxEntry
        {
            public ClassProfileComboBoxEntry(ClassProfile classProfile)
            {
                ClassProfile = classProfile;
            }

            public ClassProfile ClassProfile { get; }

            public bool FullName { get; set; }

            public override string ToString()
            {
                string className = ClassProfileLocalization.GetClassName(ClassProfile.Class);
                string specName = ClassProfileLocalization.GetSpecName(ClassProfile.GetSpec());
                return FullName
                    ? string.Format("{0} - {1} ({2})", className, specName, ClassProfile.Name)
                    : string.Format("{0} - {1}", className, specName);
            }
        }
    }
}
