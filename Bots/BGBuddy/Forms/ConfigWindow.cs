using System;
using System.Linq;
using System.Windows.Forms;
using Bots.BGBuddy.Resources;
using Styx.CommonBot;
using Styx.Localization;

using Styx.WoWInternals;

namespace Bots.BGBuddy.Forms
{
    /// <summary>
    /// Ported from Honorbuddy 5.4.8 Bots.BGBuddy.Forms.ConfigWindow. Live re-localization
    /// (Infralution.Localization.Wpf.CultureManager.UICultureChanged) is not reproduced:
    /// Infralution isn't referenced by this project (see SettingsWindow.xaml.cs for the same,
    /// earlier decision in this codebase), so that HB 5.4.8 hook has no equivalent to fall
    /// back to; the labels are still set once, from Styx.Localization.Globalization, when the
    /// window is constructed.
    /// </summary>
    public partial class ConfigWindow : Form
    {
        public ConfigWindow()
        {
            InitializeComponent();
            SetStaticLabels();
        }

        private void SetStaticLabels()
        {
            Text = Globalization.BGBuddy_Title;
            gbQueue.Text = Globalization.BGBuddy_Queue;
            lblQueue1.Text = Globalization.BGBuddy_Queue1;
            lblQueue2.Text = Globalization.BGBuddy_Queue2;
            gbSettings.Text = Globalization.BGBuddy_Settings;
            lblPullDistance.Text = Globalization.BGBuddy_PullDistance;
            cbLootCorpses.Text = Globalization.BGBuddy_LootCorpses;
            cbDisableMounting.Text = Globalization.BGBuddy_DisableMounting;

            PopulateQueueComboBoxes();
        }

        private void PopulateQueueComboBoxes()
        {
            cbQueue1.Items.Clear();
            cbQueue2.Items.Clear();

            var items = (from name in Enum.GetNames(typeof(BattlegroundType))
                         select BgNameFromEnum((BattlegroundType)Enum.Parse(typeof(BattlegroundType), name))).ToArray();

            cbQueue1.Items.AddRange(items);
            cbQueue2.Items.AddRange(items);

            cbQueue1.SelectedItem = BgNameFromEnum(BGBuddySettings.Instance.Queue1);
            cbQueue2.SelectedItem = BgNameFromEnum(BGBuddySettings.Instance.Queue2);
        }

        private void ConfigWindow_Load(object sender, EventArgs e)
        {
            if (BGBuddySettings.Instance.FirstTime)
            {
                BGBuddySettings.Instance.Queue1 = BattlegroundType.RandomBattleground;
                BGBuddySettings.Instance.FirstTime = false;
                BGBuddySettings.Instance.Save();
            }

            cbLootCorpses.Checked = BGBuddySettings.Instance.LootCorpses;
            cbDisableMounting.Checked = BGBuddySettings.Instance.DisableMounting;
            nudPullDistance.Value = (decimal)BGBuddySettings.Instance.PullDistance;

#if DEBUG
            btnHeatmapWindow.Visible = true;
#endif
        }

        private void ConfigWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            BGBuddySettings.Instance.Queue1 = BgTypeFromName(cbQueue1.SelectedItem.ToString());
            BGBuddySettings.Instance.Queue2 = BgTypeFromName(cbQueue2.SelectedItem.ToString());
            BGBuddySettings.Instance.LootCorpses = cbLootCorpses.Checked;
            BGBuddySettings.Instance.PullDistance = (double) nudPullDistance.Value;
            BGBuddySettings.Instance.DisableMounting = cbDisableMounting.Checked;
            BGBuddySettings.Instance.Save();
        }

        private static BattlegroundType BgTypeFromName(string name)
        {
            if (string.Equals(name, Globalization.BGBuddy_None))
                return BattlegroundType.None;
            if (string.Equals(name, Globalization.BGBuddy_RandomBattleground))
                return BattlegroundType.RandomBattleground;
            if (string.Equals(name, Globalization.BGBuddy_AlteracValley))
                return BattlegroundType.AV;
            if (string.Equals(name, Globalization.BGBuddy_ArathiBasin))
                return BattlegroundType.AB;
            if (string.Equals(name, Globalization.BGBuddy_BattleForGilneas))
                return BattlegroundType.BfG;
            if (string.Equals(name, Globalization.BGBuddy_EyeOfTheStorm))
                return BattlegroundType.EotS;
            if (string.Equals(name, Globalization.BGBuddy_IsleOfConquest))
                return BattlegroundType.IoC;
            if (string.Equals(name, Globalization.BGBuddy_StrandOfTheAncients))
                return BattlegroundType.SotA;
            if (string.Equals(name, Globalization.BGBuddy_TwinPeaks))
                return BattlegroundType.TP;
            if (string.Equals(name, Globalization.BGBuddy_WarsongGulch))
                return BattlegroundType.WSG;
            if (string.Equals(name, Globalization.BGBuddy_TempleOfKotmogu))
                return BattlegroundType.ToK;
            if (string.Equals(name, Globalization.BGBuddy_SilvershardMine))
                return BattlegroundType.SM;
            if (string.Equals(name, Globalization.BGBuddy_DeepwindGorge))
                return BattlegroundType.DG;

            return BattlegroundType.None;
        }

        private static string BgNameFromEnum(BattlegroundType bg)
        {
            switch (bg)
            {
                case BattlegroundType.None:
                    return Globalization.BGBuddy_None;
                case BattlegroundType.AV:
                    return Globalization.BGBuddy_AlteracValley;
                case BattlegroundType.WSG:
                    return Globalization.BGBuddy_WarsongGulch;
                case BattlegroundType.AB:
                    return Globalization.BGBuddy_ArathiBasin;
                case BattlegroundType.EotS:
                    return Globalization.BGBuddy_EyeOfTheStorm;
                case BattlegroundType.SotA:
                    return Globalization.BGBuddy_StrandOfTheAncients;
                case BattlegroundType.IoC:
                    return Globalization.BGBuddy_IsleOfConquest;
                case BattlegroundType.RandomBattleground:
                    return Globalization.BGBuddy_RandomBattleground;
                case BattlegroundType.BfG:
                    return Globalization.BGBuddy_BattleForGilneas;
                case BattlegroundType.TP:
                    return Globalization.BGBuddy_TwinPeaks;
                case BattlegroundType.ToK:
                    return Globalization.BGBuddy_TempleOfKotmogu;
                case BattlegroundType.SM:
                    return Globalization.BGBuddy_SilvershardMine;
                case BattlegroundType.DG:
                    return Globalization.BGBuddy_DeepwindGorge;
                default:
                    return Globalization.BGBuddy_None;
            }
        }

        private void btnHeatmapWindow_Click(object sender, EventArgs e)
        {
            new HeatmapWindow().Show();
        }
    }
}
