using System;
using System.Linq;
using System.Windows.Forms;
using ArchaeologyBuddy.Resources;
using Styx.CommonBot;

namespace Bots.ArchaeologyBuddy.GUI
{
	public partial class ArchBuddySettings : Form
	{
		public ArchBuddySettings()
		{
			InitializeComponent();
            SetCollectionStrings();
		}

		private void ArchBuddySettings_Load(object sender, EventArgs e)
		{
            ArchBuddy.Instance.Initialize();
			cbContinent.SelectedIndex = 0;
			cbBlacklist.SelectedIndex = 0;
			cbSolveRace.SelectedIndex = 0;
			cbAutoSolve.Checked = ArchBuddy.Instance.Settings.AutoSolveArtifacts;
		    cbCrateArtifacts.Checked = ArchBuddy.Instance.Settings.CrateArtifacts;
			cbUseKeystones.Checked = ArchBuddy.Instance.Settings.UseKeystones;
			cbAutoTrain.Checked = ArchBuddy.Instance.Settings.AutoTrain;
			nudTrainUntil.Value = (decimal)ArchBuddy.Instance.Settings.TrainUpto;
		    
		}

        private void SetCollectionStrings()
        {
            clbBlacklistType.Items.AddRange(new[]
                                                {
                                                    ArchaeologyResources.Draenei,
                                                    ArchaeologyResources.Dwarf,
                                                    ArchaeologyResources.Fossil,
                                                    ArchaeologyResources.Nerubian,
                                                    ArchaeologyResources.NightElf,
                                                    ArchaeologyResources.Orc,
                                                    ArchaeologyResources.Tolvir,
                                                    ArchaeologyResources.Troll,
                                                    ArchaeologyResources.Vrykul,
                                                    "Mogu",
                                                    "Pandaren",
                                                    "Mantid"
                                                });

            cbBlacklist.Items.AddRange(new []
                                           {
                                               ArchaeologyResources.ByName, 
                                               ArchaeologyResources.ByType
                                           });

            cbSolveRace.Items.AddRange(new []
                                           {
                                               ArchaeologyResources.AllTypes, 
                                               ArchaeologyResources.Dwarf, 
                                               ArchaeologyResources.Draenei, 
                                               ArchaeologyResources.Fossil,
                                               ArchaeologyResources.NightElf,
                                               ArchaeologyResources.Nerubian,
                                               ArchaeologyResources.Orc,
                                               ArchaeologyResources.Tolvir,
                                               ArchaeologyResources.Troll,
                                               ArchaeologyResources.Vrykul,
                                               "Mogu",
                                               "Pandaren",
                                               "Mantid"
                                           });

            cbContinent.Items.AddRange(new []
                                           {
                                               ArchaeologyResources.SelectContinent,
                                               ArchaeologyResources.Kalimdor,
                                               ArchaeologyResources.EasternKingdoms,
                                               ArchaeologyResources.Outland,
                                               ArchaeologyResources.Northrend,
                                               "Pandaren"
                                           });
        }

		private void cbContinent_SelectedIndexChanged(object sender, EventArgs e)
		{
			clbBlacklistName.Items.Clear();

			var items = ArchBuddy.DigSites.Where(d =>
			                                         {
			                                             switch (cbContinent.SelectedIndex)
			                                             {
                                                             case 1:
			                                                     return d.ContinentId == 1;
                                                             case 2:
			                                                     return d.ContinentId == 0;
                                                             case 3:
			                                                     return d.ContinentId == 530;
                                                             case 4:
			                                                     return d.ContinentId == 571;
                                                             case 5:
                                                                 return d.ContinentId == 870;
                                                             default:
			                                                     return false;
			                                             }
			                                         }).Select(d => d.Name).ToList();

			items.Sort();
			clbBlacklistName.Items.AddRange(items.ToArray());

			for (int i = 0; i < clbBlacklistName.Items.Count; i++)
			{
				if (ArchBuddy.PermanentBlacklist.Any(d => d.Name == clbBlacklistName.Items[i].ToString()))
					clbBlacklistName.SetItemChecked(i, true);
				else
					clbBlacklistName.SetItemChecked(i, false);
			}
		}

		private void cbBlacklist_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cbBlacklist.SelectedIndex == 0)
			{
				for (int i = 0; i < clbBlacklistName.Items.Count; i++)
				{
					if (ArchBuddy.PermanentBlacklist.Any(d => d.Name == clbBlacklistName.Items[i].ToString()))
						clbBlacklistName.SetItemChecked(i, true);
					else
						clbBlacklistName.SetItemChecked(i, false);
				}
				clbBlacklistName.Show();
				cbContinent.Show();
				clbBlacklistType.Hide();
				btnSelectAll.Show();
				btnUnselectAll.Show();
			}
			else
			{
				for (int i = 0; i < clbBlacklistType.Items.Count; i++)
				{
				    clbBlacklistType.SetItemChecked(i,
				                                    ArchBuddy.PermanentBlacklist.Count(b => b.Type == clbBlacklistType.Items[i].ToString()) ==
				                                    ArchBuddy.DigSites.Count(d => d.Type == clbBlacklistType.Items[i].ToString()));
				}
				clbBlacklistName.Hide();
				cbContinent.Hide();
				clbBlacklistType.Show();
				btnSelectAll.Hide();
				btnUnselectAll.Hide();
			}
		}

		private void ArchBuddySettings_FormClosing(object sender, FormClosingEventArgs e)
		{
			ArchBuddy.Instance.Settings.AutoSolveArtifacts = cbAutoSolve.Checked;
			ArchBuddy.Instance.Settings.UseKeystones = cbUseKeystones.Checked;
			ArchBuddy.Instance.Settings.AutoTrain = cbAutoTrain.Checked;
		    ArchBuddy.Instance.Settings.CrateArtifacts = cbCrateArtifacts.Checked;
			ArchBuddy.Instance.Settings.TrainUpto = (int)nudTrainUntil.Value;
			ArchBuddy.Instance.Settings.Save();
			ArchBuddy.SaveBlacklist();
		}

	    private void btnSelectAll_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < clbBlacklistName.Items.Count; i++)
			{
				clbBlacklistName.SetItemChecked(i, true);

			    int i1 = i;
			    if (ArchBuddy.PermanentBlacklist.Any(d => d.Name == clbBlacklistName.Items[i1].ToString())) 
                    continue;
			    int i2 = i;
			    ArchBuddy.PermanentBlacklist.Add(ArchBuddy.DigSites.First(d => d.Name == clbBlacklistName.Items[i2].ToString()));
			}
		}

		private void btnUnselectAll_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < clbBlacklistName.Items.Count; i++)
			{
				clbBlacklistName.SetItemChecked(i, false);
				ArchBuddy.PermanentBlacklist.RemoveAll(d => d.Name == clbBlacklistName.Items[i].ToString());
			}
		}

		private void clbBlacklistType_MouseUp(object sender, MouseEventArgs e)
		{
			foreach (var item in clbBlacklistType.Items)
			{
				if (clbBlacklistType.GetItemChecked(clbBlacklistType.Items.IndexOf(item)))
					ArchBuddy.PermanentBlacklist.AddRange(ArchBuddy.DigSites.Where(d => d.Type == item.ToString() && !ArchBuddy.PermanentBlacklist.Exists(b => b.ID == d.ID)));
				else
					ArchBuddy.PermanentBlacklist.RemoveAll(b => b.Type == item.ToString());
			}
		}

		private void clbBlacklistName_MouseUp(object sender, MouseEventArgs e)
		{
			foreach (var item in clbBlacklistName.Items)
			{
				if (clbBlacklistName.GetItemChecked(clbBlacklistName.Items.IndexOf(item)) && !ArchBuddy.PermanentBlacklist.Exists(d => d.Name == item.ToString()))
					ArchBuddy.PermanentBlacklist.Add(ArchBuddy.DigSites.First(d => d.Name == item.ToString()));
				else if (!clbBlacklistName.GetItemChecked(clbBlacklistName.Items.IndexOf(item)))
					ArchBuddy.PermanentBlacklist.RemoveAll(d => d.Name == item.ToString());
			}
		}

		internal static int SolveRace;
		internal static bool Solve;

		private void btnSolveAll_Click(object sender, EventArgs e)
		{
			SolveRace = cbSolveRace.SelectedIndex;
			Solve = !Solve;
			btnSolveAll.Text = Solve ? ArchaeologyResources.StopSolving : ArchaeologyResources.Solve;
			if (!TreeRoot.IsRunning)
				TreeRoot.Start();
		}

		private void ArchBuddySettings_Activated(object sender, EventArgs e)
		{
			btnSolveAll.Text = Solve ? ArchaeologyResources.StopSolving : ArchaeologyResources.Solve;
		}

		internal static bool ForceTrain;

		private void btnForceTrain_Click(object sender, EventArgs e)
		{
			ForceTrain = true;
		}
	}
}
