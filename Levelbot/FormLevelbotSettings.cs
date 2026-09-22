using System;
using System.Reflection;
using System.Windows.Forms;
using Styx.Helpers;
using Styx.Localization;

namespace Levelbot
{
    // Ported from Honorbuddy 5.4.8 Levelbot.FormLevelbotSettings. HB 5.4.8 re-applies the static
    // labels whenever Infralution.Localization.Wpf.CultureManager.UICultureChanged fires, so the
    // form's text updates live if the user switches language. Infralution isn't referenced by this
    // project (see SettingsWindow.xaml.cs for the same, earlier decision in this codebase), so that
    // live re-localization hook has no equivalent to fall back to; the labels are still set once,
    // from Styx.Localization.Globalization, when the form loads.
    [Obfuscation(Exclude = true, ApplyToMembers = false)]
    public partial class FormLevelbotSettings : Form
    {
        public FormLevelbotSettings()
        {
            InitializeComponent();
        }

        private void SetStaticLabels()
        {
            Text = Globalization.LevelBot_Title;
            cbGroundMountFarmingMode.Text = Globalization.LevelBot_GroundMountFarmingMode;
            BtnSave.Text = Globalization.LevelBot_Save;
            BtnSaveAndClose.Text = Globalization.LevelBot_SaveAndClose;
        }

        private void FormLevelbotSettings_Load(object sender, EventArgs e)
        {
            LoadSettings();
            SetStaticLabels();
        }

        private void RegisterSettingsEventHandlers()
        {
            LevelbotSettings settings = LevelbotSettings.Instance;
            cbGroundMountFarmingMode.CheckedChanged += (sender, e) => settings.GroundMountFarmingMode = cbGroundMountFarmingMode.Checked;
        }

        private void UpdateGuiSettings()
        {
            LevelbotSettings settings = LevelbotSettings.Instance;
            cbGroundMountFarmingMode.Checked = settings.GroundMountFarmingMode;
        }

        private static void SaveSettings()
        {
            LevelbotSettings.Instance.Save();
        }

        private void LoadSettings()
        {
            LevelbotSettings.Instance.Load();
            UpdateGuiSettings();
            RegisterSettingsEventHandlers();
        }

        private void BtnSaveAndClose_Click(object sender, EventArgs e)
        {
            SaveSettings();
            Close();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void FormLevelbotSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }
    }
}
