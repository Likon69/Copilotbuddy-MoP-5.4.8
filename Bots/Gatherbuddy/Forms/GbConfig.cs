using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx;


namespace Bots.Gatherbuddy.GUI
{
    public partial class GbConfig : Form
    {
        public GbConfig()
        {
            InitializeComponent();
        }

        private void GbConfig_Load(object sender, EventArgs e)
        {
			UpdateGuiSettings();
			RegisterSettingsEventHandlers();
        }

        private void RegisterSettingsEventHandlers()
        {
            // Gatherbuddy settings.
            _cbPathingType.SelectedIndexChanged += (sender, args) => GatherbuddySettings.Instance.PathingType =
                _cbPathingType.SelectedIndex == 0 ?
                PathType.Bounce :
                PathType.Circle;

            _cbbHerbalism.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.GatherHerbs = _cbbHerbalism.Checked;
            _cbbMining.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.GatherMinerals = _cbbMining.Checked;
            _rbGuildVault.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.UseGuildVault = _rbGuildVault.Checked;
            _rbMail.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.MailToAlt = _rbMail.Checked;
            nudHeight.ValueChanged += (sender, args) => GatherbuddySettings.Instance.HeightModifier = (float)nudHeight.Value;
            _rbHearthAndExit.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.HearthAndExit = _rbHearthAndExit.Checked;
            _cbWaitRezSickness.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.WaitRezSickness = _cbWaitRezSickness.Checked;
            _nudBottingHours.ValueChanged += (sender, args) => GatherbuddySettings.Instance.BottingHours = (float)_nudBottingHours.Value;
            _cbNoNinja.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.NoNinja = _cbNoNinja.Checked;
			_cbSpiritRez.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.UseSpiritHealer = _cbSpiritRez.Checked;
			_cbIngoreElites.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.IgnoreElites = _cbIngoreElites.Checked;
			_nudBlacklistTimer.ValueChanged += (sender, args) => GatherbuddySettings.Instance.BlacklistTimer = (int)_nudBlacklistTimer.Value;
			_cbLootMobs.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.LootMobs = _cbLootMobs.Checked;
			_cbRepairFromGuild.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.RepairFromGuildBank = _cbRepairFromGuild.Checked;
            _cbRandomizeHotspots.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.RandomizeHotspots = _cbRandomizeHotspots.Checked;
            _cbFaceNodes.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.FaceNodes = _cbFaceNodes.Checked;
            nudRandomizationFactor.ValueChanged += (sender, args) => GatherbuddySettings.Instance.RandomizationFactor = (double)nudRandomizationFactor.Value;
            cbDiagnosticLogging.CheckedChanged += (sender, args) => GatherbuddySettings.Instance.DiagnosticLogging = cbDiagnosticLogging.Checked;

            // LevelBot settings.
            _tbMailAlt.TextChanged += (sender, args) => CharacterSettings.Instance.MailRecipient = _tbMailAlt.Text;

            FormClosing += (sender, args) =>
            {
                GatherbuddyBot.SaveAvoidList();
                GatherbuddySettings.Instance.Save();
                CharacterSettings.Instance.Save();

                if (TreeRoot.IsRunning)
                {
                    try
                    {
                        Profile.CurrentProfile = new Profile();
                    }
                    catch (ProfileException ex)
                    {
                        Logging.WriteQuiet("Couldn't load new profile: {0}", ex.Message);
                    }
                }
            };
        }

        private void UpdateGuiSettings()
        {
            var gbSettings = GatherbuddySettings.Instance;
            _cbPathingType.SelectedIndex = gbSettings.PathingType == PathType.Bounce ? 0 : 1;
            _cbbHerbalism.Checked = gbSettings.GatherHerbs;
            _cbbMining.Checked = gbSettings.GatherMinerals;
            _rbGuildVault.Checked = gbSettings.UseGuildVault;
            _rbHearthAndExit.Checked = gbSettings.HearthAndExit;
            _cbWaitRezSickness.Checked = gbSettings.WaitRezSickness;
            _rbMail.Checked = gbSettings.MailToAlt;
            _cbNoNinja.Checked = gbSettings.NoNinja;
			_cbIngoreElites.Checked = gbSettings.IgnoreElites;
			_cbSpiritRez.Checked = gbSettings.UseSpiritHealer;
            _nudBottingHours.Value = (decimal)gbSettings.BottingHours;
			nudHeight.Value = (decimal)gbSettings.HeightModifier;
			_nudBlacklistTimer.Value = (decimal)gbSettings.BlacklistTimer;
            _cbLootMobs.Checked = gbSettings.LootMobs;
            _cbRandomizeHotspots.Checked = gbSettings.RandomizeHotspots;
			_cbRepairFromGuild.Checked = gbSettings.RepairFromGuildBank;
            _cbFaceNodes.Checked = gbSettings.FaceNodes;
            nudRandomizationFactor.Value = (decimal)gbSettings.RandomizationFactor;
            cbDiagnosticLogging.Checked = gbSettings.DiagnosticLogging;
            _tbMailAlt.Text = CharacterSettings.Instance.MailRecipient;

			if (StyxWoW.Me == null)
				return;

			var herbSkill = StyxWoW.Me.GetSkill(SkillLine.Herbalism);
			if (herbSkill != null)
			{
				foreach (var item in
				    GatherbuddyBot.herbList.OrderBy(h => h.Name).Where(item => herbSkill.CurrentValue >= item.RequiredSkill))
				{
				    clbHerbs.Items.Add(item.Name, !GatherbuddyBot.nodeAvoidList.Contains(item.Entry));
				}
			}

			var mineSkill = StyxWoW.Me.GetSkill(SkillLine.Mining);
			if (mineSkill != null)
			{
				foreach (var item in
				    GatherbuddyBot.mineList.OrderBy(m => m.Name).Where(item => mineSkill.CurrentValue >= item.RequiredSkill))
				{
				    clbMinerals.Items.Add(item.Name, !GatherbuddyBot.nodeAvoidList.Contains(item.Entry));
				}
			}
        }

		private void clbHerbs_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			string name = clbHerbs.Items[e.Index].ToString();
			uint entry = GatherbuddyBot.herbList.First(h => h.Name == name).Entry;
			if (e.NewValue == CheckState.Checked)
			{
				if (GatherbuddyBot.nodeAvoidList.Contains(entry))
					GatherbuddyBot.nodeAvoidList.Remove(entry);

			    RemoveExtraHerbs(entry);
			}
			else
			{
				if (!GatherbuddyBot.nodeAvoidList.Contains(entry))
					GatherbuddyBot.nodeAvoidList.Add(entry);

                AddExtraHerbs(entry);
			}
		}

        private void RemoveExtraHerbs(uint entry)
        {
            if (entry == 209349) // Green Tea Leaf
            {
                if (GatherbuddyBot.nodeAvoidList.Contains(215405))
                    GatherbuddyBot.nodeAvoidList.Remove(215405);
            }

            if (entry == 209353) // Rain Poppy
            {
                if (GatherbuddyBot.nodeAvoidList.Contains(215408))
                    GatherbuddyBot.nodeAvoidList.Remove(215408);
            }

            if (entry == 209350) // Silkweed
            {
                if (GatherbuddyBot.nodeAvoidList.Contains(215406))
                    GatherbuddyBot.nodeAvoidList.Remove(215406);
            }

            if (entry == 209351) // Snow Lily
            {
                if (GatherbuddyBot.nodeAvoidList.Contains(215407))
                    GatherbuddyBot.nodeAvoidList.Remove(215407);
            }

            if (entry == 214510) // Sha-Touched Herb
            {
                if (GatherbuddyBot.nodeAvoidList.Contains(215403))
                    GatherbuddyBot.nodeAvoidList.Remove(215403);
                if (GatherbuddyBot.nodeAvoidList.Contains(215411))
                    GatherbuddyBot.nodeAvoidList.Remove(215411);
                if (GatherbuddyBot.nodeAvoidList.Contains(215412))
                    GatherbuddyBot.nodeAvoidList.Remove(215412);
            }

            if (entry == 209355) // Fools Cap
            {
                if (GatherbuddyBot.nodeAvoidList.Contains(215410))
                    GatherbuddyBot.nodeAvoidList.Remove(215410);
            }
        }

        private void AddExtraHerbs(uint entry)
        {
            if (entry == 209349) // Green Tea Leaf
            {
                if (!GatherbuddyBot.nodeAvoidList.Contains(215405))
                    GatherbuddyBot.nodeAvoidList.Add(215405);
            }

            if (entry == 209353) // Rain Poppy
            {
                if (!GatherbuddyBot.nodeAvoidList.Contains(215408))
                    GatherbuddyBot.nodeAvoidList.Add(215408);
            }

            if (entry == 209350) // Silkweed
            {
                if (!GatherbuddyBot.nodeAvoidList.Contains(215406))
                    GatherbuddyBot.nodeAvoidList.Add(215406);
            }

            if (entry == 209351) // Snow Lily
            {
                if (!GatherbuddyBot.nodeAvoidList.Contains(215407))
                    GatherbuddyBot.nodeAvoidList.Add(215407);
            }

            if (entry == 214510) // Sha-Touched Herb
            {
                if (!GatherbuddyBot.nodeAvoidList.Contains(215403))
                    GatherbuddyBot.nodeAvoidList.Add(215403);
                if (!GatherbuddyBot.nodeAvoidList.Contains(215411))
                    GatherbuddyBot.nodeAvoidList.Add(215411);
                if (!GatherbuddyBot.nodeAvoidList.Contains(215412))
                    GatherbuddyBot.nodeAvoidList.Add(215412);
            }

            if (entry == 209355) // Fools Cap
            {
                if (!GatherbuddyBot.nodeAvoidList.Contains(215410))
                    GatherbuddyBot.nodeAvoidList.Add(215410);
            }
        }

		private void btnForceVendor_Click(object sender, EventArgs e)
		{
			Vendors.ForceSell = true;
            if (GatherbuddySettings.Instance.UseGuildVault)
            {
                Logging.Write("Will do Guild Vault run after next harvest.");
            }
            else if (GatherbuddySettings.Instance.MailToAlt)
            {
                Logging.Write("Mail loot now.");
            }
            else
            {
                Logging.Write("Hearth and exit.");
            }
		}

        private void clbMinerals_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            string name = clbMinerals.Items[e.Index].ToString();
            uint entry = GatherbuddyBot.mineList.First(h => h.Name == name).Entry;
            if (e.NewValue == CheckState.Checked)
            {
                if (GatherbuddyBot.nodeAvoidList.Contains(entry))
                    GatherbuddyBot.nodeAvoidList.Remove(entry);

                RemoveExtraMines(entry);
            }
            else
            {
                if (!GatherbuddyBot.nodeAvoidList.Contains(entry))
                    GatherbuddyBot.nodeAvoidList.Add(entry);

                AddExtraMines(entry);
            }
        }

        private void RemoveExtraMines(uint entry)
        {
            if (entry == 209311) // Ghost Iron Deposit
            {
                if (GatherbuddyBot.nodeAvoidList.Contains(215413))
                    GatherbuddyBot.nodeAvoidList.Remove(215413);
            }

            if (entry == 209328) // Rich Ghost Iron Deposit
            {
                if (GatherbuddyBot.nodeAvoidList.Contains(215415))
                    GatherbuddyBot.nodeAvoidList.Remove(215415);
            }

            if (entry == 209313) // Trillium Vein
            {
                if (GatherbuddyBot.nodeAvoidList.Contains(215418))
                    GatherbuddyBot.nodeAvoidList.Remove(215418);
            }

        }

        private void AddExtraMines(uint entry)
        {
            if (entry == 209311) // Ghost Iron Deposit
            {
                if (!GatherbuddyBot.nodeAvoidList.Contains(215413))
                    GatherbuddyBot.nodeAvoidList.Add(215413);
            }

            if (entry == 209328) // Rich Ghost Iron Deposit
            {
                if (!GatherbuddyBot.nodeAvoidList.Contains(215415))
                    GatherbuddyBot.nodeAvoidList.Add(215415);
            }

            if (entry == 209313) // Trillium Vein
            {
                if (!GatherbuddyBot.nodeAvoidList.Contains(215418))
                    GatherbuddyBot.nodeAvoidList.Add(215418);
            }

        }

        private void btnStatusReport_Click(object sender, EventArgs e)
		{
			GatherbuddyBot.StatusReport();
		}

		private void btnSelectAllHerb_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < clbHerbs.Items.Count; i++)
			{
				clbHerbs.SetItemChecked(i, true);
			}
		}

		private void btnUnSelectAllHerb_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < clbHerbs.Items.Count; i++)
			{
				clbHerbs.SetItemChecked(i, false);
			}
		}

		private void btnSelectAllMine_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < clbMinerals.Items.Count; i++)
			{
				clbMinerals.SetItemChecked(i, true);
			}
		}

		private void btnUnSelectAllMine_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < clbMinerals.Items.Count; i++)
			{
				clbMinerals.SetItemChecked(i, false);
			}
		}


		//public static string GetGatherbuddyProfileWrapper(string profileName)
		//{
		//    try
		//    {
		//        string ret = Styx.Auth.Service.Use(
		//            auth =>
		//            {
		//                var resp = auth.GetGatherbuddyProfile(profileName);
		//                if (resp.Success)
		//                {
		//                    return Encoding.ASCII.GetString(resp.Data);
		//                }
		//                return null;
		//            }) as string;

		//        return ret;
		//    }
		//    catch (Exception e)
		//    {
		//        Logging.Write("Error getting Gatherbuddy profiles! " + e.Message);
		//        return null;
		//    }
		//}

		//public static string[] GetGatherbuddyProfileListWrapper()
		//{
		//    try
		//    {
		//        string[] ret = Styx.Auth.Service.Use(
		//            auth =>
		//            {
		//                var resp = auth.GetGatherbuddyProfileListInternal();
		//                if (resp.Success)
		//                {
		//                    string[] list = resp.Body.Split(new[] { "~@~" }, StringSplitOptions.RemoveEmptyEntries);
		//                    return list;
		//                }
		//                return null;
		//            }) as string[];

		//        return ret;
		//    }
		//    catch (Exception e)
		//    {
		//        Logging.Write("Error getting Gatherbuddy profiles! " + e.Message);
		//        return null;
		//    }
		//}
    }
}
