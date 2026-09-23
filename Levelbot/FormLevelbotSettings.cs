using System;
using System.Reflection;
using System.Windows.Forms;
using Styx.Helpers;
using Styx.Localization;

namespace Levelbot
{
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
