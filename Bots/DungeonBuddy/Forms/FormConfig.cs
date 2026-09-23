using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Profiles.Handlers;
using Styx;
using Styx.Patchables;
using Styx.Pathing;
using Styx.WoWInternals;

namespace Bots.DungeonBuddy.Forms;

public class FormConfig : Form
{
	private bool _ignoreTreeviewAfterCheckEvent;

	private PathView _pathView;

	private IContainer components;

	private TableLayoutPanel tableLayoutMain;

	private Label label1;

	private TabControl tabControlMain;

	private TabPage tabPageCommon;

	private PropertyGrid propertyGridMain;

	private TabPage tabPageDungeons;

	private CheckBox cbShowAllDungeons;

	private Label lblDungeonWarningText;

	private TreeView twDungeonSelection;

	private Button btnUnselectAllDungeons;

	private Button btnSelectAllDungeons;

	private TabPage tabPageDebug;

	private Button btnDumpNavigationInfo;

	private Button debugBtn3;

	private TabControl tabControl1;

	private TabPage tabPage1;

	private TreeView twDebug;

	private TabPage tabPage2;

	private TreeView twBossTree;

	private TableLayoutPanel tableLayoutDungeons;

	private Panel bottomDungeonsPanel;

	private Panel TopDungeonsPanel;

	private ComboBox cbDungeonSelection;

	private Label label2;

	private TabPage tabPageRaids;

	private TabPage tabPageScenarios;

	private TableLayoutPanel tableLayoutRaids;

	private TreeView twRaidSelection;

	private Panel panelRaidTop;

	private ComboBox cbRaidSelection;

	private Label label3;

	private TableLayoutPanel tableLayoutPanel1;

	private TreeView twScenarioSelection;

	private Panel panelScenariosBottom;

	private Button btnUnselectAllScenarios;

	private Button btnSelectAllScenarios;

	private CheckBox cbShowAllScenarios;

	private Panel panelScenarioTop;

	private ComboBox cbScenarioSelection;

	private Label label4;

	private Label lblScenarioWarningText;

	private Label lblRaidWarningText;

	internal static FormConfig Instance { get; private set; }

	internal static bool IsShown
	{
		get
		{
			if (Instance != null && !Instance.Disposing && !Instance.IsDisposed)
			{
				return Instance.Visible;
			}
			return false;
		}
	}

	public FormConfig()
	{
		InitializeComponent();
		Instance = this;
	}

	private void FormConfig_Load(object sender, EventArgs e)
	{
		propertyGridMain.SelectedObject = DungeonBuddySettings.Instance;
		cbShowAllDungeons.Checked = DungeonBuddySettings.Instance.ShowAllDungeons;
		if (!DungeonManager.Initialized)
		{
			DungeonManager.OnInitialized += DungeonManager_OnInitialzied;
			SetDungeonWarningText("Please wait a moment while Dungeonbuddy is initializing");
			SetScenaraioWarningText("Please wait a moment while Dungeonbuddy is initializing");
			SetRaidWarningText("Please wait a moment while Dungeonbuddy is initializing");
		}
		else
		{
			SetDefaultWarningText();
		}
	}

	private void DungeonManager_OnInitialzied(object sender, EventArgs e)
	{
		BeginInvoke((Action)delegate
		{
			if (IsShown)
			{
				DungeonBuddySettings instance = DungeonBuddySettings.Instance;
				lblDungeonWarningText.Visible = instance.DungeonType == DungeonType.None;
				lblScenarioWarningText.Visible = instance.ScenarioType == ScenarioType.None;
				lblRaidWarningText.Visible = instance.RaidType == RaidType.None;
				SetDefaultWarningText();
				UpdateDungeonList();
				UpdateRaidList();
				UpdateScenarioList();
			}
		});
		DungeonManager.OnInitialized -= DungeonManager_OnInitialzied;
	}

	private void SetDefaultWarningText()
	{
		SetDungeonWarningText("Dungeonbuddy is not setup to run {0}. If you want to run {0} then you must select a different option from the combo box above", "dungeons");
		SetScenaraioWarningText("Dungeonbuddy is not setup to run {0}. If you want to run {0} then you must select a different option from the combo box above", "scenarios");
		SetRaidWarningText("Dungeonbuddy is not setup to run {0}. If you want to run {0} then you must select a different option from the combo box above", "raids");
	}

	private static void GenerateTreeView(TreeView treeView, bool debug, IEnumerable<LfgDungeons> dungeons, uint[] selectedNodes, string defaultGroupName = "Unknown")
	{
		List<IGrouping<uint, LfgDungeons>> list = (from d in dungeons
			orderby d.ExpansionId descending, d.LfgDifficulty descending, d.QueueMinLevel, d.OrderIndex
			group d by d.GroupId).ToList();
		foreach (IGrouping<uint, LfgDungeons> item in list)
		{
			string lfgGroupName = GetLfgGroupName(item.Key, defaultGroupName);
			treeView.Nodes.Add(lfgGroupName, lfgGroupName);
			foreach (LfgDungeons item2 in item)
			{
				string text = ((item2.LfgDifficulty == LfgDungeons.Difficulty.Heroic || item2.LfgDifficulty == LfgDungeons.Difficulty.TenManHeroic || item2.LfgDifficulty == LfgDungeons.Difficulty.TwentyFiveManHeroic) ? (item2.Name + " [Heroic]") : item2.Name);
				if (debug)
				{
					text = $"{text} [Id: {item2.Id}]";
					treeView.Nodes[lfgGroupName].Nodes.Add(text, text);
				}
				else
				{
					TreeNode treeNode = treeView.Nodes[lfgGroupName].Nodes.Add(text, text);
					treeNode.Tag = item2.Id;
				}
			}
		}
		if (!treeView.CheckBoxes)
		{
			return;
		}
		foreach (TreeNode item3 in treeView.Nodes.OfType<TreeNode>().SelectMany(n => n.Nodes.OfType<TreeNode>()))
		{
			uint value = (uint)item3.Tag;
			if (selectedNodes.Contains(value))
			{
				if (!item3.Parent.IsExpanded)
				{
					item3.Parent.Expand();
				}
				item3.Checked = true;
			}
		}
	}

	private static string GetLfgGroupName(uint groupId, string defaultName)
	{
		try
		{
			WoWDb.Row row = StyxWoW.Db[ClientDb.LfgDungeonGroup].GetRow(groupId);
			return (row == null || !row.IsValid) ? defaultName : row.GetStringField(1u);
		}
		catch (Exception)
		{
			return "Default";
		}
	}

	private bool DoTreeviewChecks(TreeView treeview, TreeNode treeNode, bool singleSelectionMode, ref uint[] dungeonIds)
	{
		if (_ignoreTreeviewAfterCheckEvent)
		{
			return false;
		}
		_ignoreTreeviewAfterCheckEvent = true;
		try
		{
			List<uint> list2;
			uint item;
			if (treeNode.GetNodeCount(includeSubTrees: false) > 0)
			{
				if (singleSelectionMode)
				{
					treeNode.Checked = false;
					return false;
				}
				List<TreeNode> list = treeNode.Nodes.Cast<TreeNode>().ToList();
				list2 = dungeonIds.ToList();
				foreach (TreeNode item2 in list)
				{
					item2.Checked = treeNode.Checked;
					item = (uint)item2.Tag;
					if (item2.Checked && !list2.Contains(item))
					{
						list2.Add(item);
					}
					else if (!item2.Checked && list2.Contains(item))
					{
						list2.Remove(item);
					}
				}
				dungeonIds = list2.ToArray();
				return true;
			}
			list2 = dungeonIds.ToList();
			item = (uint)treeNode.Tag;
			if (treeNode.Checked)
			{
				if (singleSelectionMode)
				{
					list2.Clear();
					foreach (TreeNode node in treeview.Nodes)
					{
						if (node.GetNodeCount(includeSubTrees: false) > 0)
						{
							foreach (TreeNode node2 in node.Nodes)
							{
								if (node2 != treeNode)
								{
									node2.Checked = false;
								}
							}
						}
						node.Checked = false;
					}
				}
				if (!list2.Contains(item))
				{
					list2.Add(item);
				}
				if (treeNode.Parent.Nodes.Cast<TreeNode>().All(node => node.Checked))
				{
					treeNode.Parent.Checked = true;
				}
			}
			else
			{
				if (list2.Contains(item))
				{
					list2.Remove(item);
				}
				if (treeNode.Parent.Checked)
				{
					treeNode.Parent.Checked = false;
				}
			}
			dungeonIds = list2.ToArray();
		}
		finally
		{
			_ignoreTreeviewAfterCheckEvent = false;
		}
		return true;
	}

	private void tabControlMain_SelectedIndexChanged(object sender, EventArgs e)
	{
		switch (tabControlMain.SelectedIndex)
		{
		case 1:
			cbDungeonSelection.SuspendLayout();
			UpdateDungeonCombo();
			cbDungeonSelection.ResumeLayout();
			break;
		case 2:
			cbScenarioSelection.SuspendLayout();
			UpdateScenarioCombo();
			cbScenarioSelection.ResumeLayout();
			break;
		case 3:
			cbRaidSelection.SuspendLayout();
			UpdateRaidCombo();
			cbRaidSelection.ResumeLayout();
			break;
		case 4:
			twDebug.Nodes.Clear();
			GenerateTreeView(twDebug, debug: true, LfgDungeons.Collection.Where(u => u.SubType != LfgDungeons.LfgType.Zone), null);
			twBossTree.Nodes.Clear();
			{
				foreach (Boss bossEncounter in BossManager.BossEncounters)
				{
					if (!bossEncounter.IsRandomSpawnBoss())
					{
						TreeNode treeNode = new TreeNode(bossEncounter.Name);
						treeNode.Checked = bossEncounter.IsAlive;
						TreeNode node = treeNode;
						twBossTree.Nodes.Add(node);
					}
				}
				break;
			}
		}
	}

	private void FormConfig_FormClosing(object sender, FormClosingEventArgs e)
	{
		DungeonBuddySettings.Instance.Save();
	}

	private void UpdateDungeonCombo()
	{
		cbDungeonSelection.Items.Clear();
		cbDungeonSelection.Items.Add("None");
		cbDungeonSelection.Items.Add("Farm");
		cbDungeonSelection.Items.Add("Specific Dungeon");
		LfgDungeons[] source = LfgQueue.GetJoinableRandomDungeons();
		LfgDungeons[] array = source.Where(d => d.LfgDifficulty == LfgDungeons.Difficulty.Normal && !d.IsHolidayEvent).ToArray();
		cbDungeonSelection.Items.AddRange(array);
		LfgDungeons[] array2 = source.Where(d => d.LfgDifficulty == LfgDungeons.Difficulty.Heroic && !d.IsHolidayEvent).ToArray();
		cbDungeonSelection.Items.AddRange(array2);
		LfgDungeons[] array3 = source.Where(d => d.IsHolidayEvent).ToArray();
		cbDungeonSelection.Items.AddRange(array3);
		if (DungeonBuddySettings.Instance.DungeonType == DungeonType.SpecialEvent)
		{
			LfgDungeons lfgDungeons = array3.FirstOrDefault(d => d.Id == DungeonBuddySettings.Instance.SelectedRandomDungeonId);
			if (lfgDungeons != null)
			{
				cbDungeonSelection.SelectedIndex = cbDungeonSelection.Items.IndexOf(lfgDungeons);
				return;
			}
			Logger.Write("Holiday Event {0} is not available. Setting Dungeon Type to 'None'", (DungeonBuddySettings.Instance.SelectedRandomDungeonId != 0) ? LfgDungeons.FromId(DungeonBuddySettings.Instance.SelectedRandomDungeonId).Name : string.Empty);
			DungeonBuddySettings.Instance.DungeonType = DungeonType.None;
			DungeonBuddySettings.Instance.SelectedRandomDungeonId = 0u;
		}
		else if (DungeonBuddySettings.Instance.DungeonType == DungeonType.RandomHeroic)
		{
			LfgDungeons lfgDungeons2 = array2.FirstOrDefault(d => d.Id == DungeonBuddySettings.Instance.SelectedRandomDungeonId) ?? array2.FirstOrDefault();
			if (lfgDungeons2 != null)
			{
				cbDungeonSelection.SelectedIndex = cbDungeonSelection.Items.IndexOf(lfgDungeons2);
				if (lfgDungeons2.Id != DungeonBuddySettings.Instance.SelectedRandomDungeonId)
				{
					if (DungeonBuddySettings.Instance.SelectedRandomDungeonId != 0)
					{
						Logger.Write("{0} was not found. Switching to {1}", LfgDungeons.FromId(DungeonBuddySettings.Instance.SelectedRandomDungeonId).Name, lfgDungeons2.Name);
					}
					DungeonBuddySettings.Instance.SelectedRandomDungeonId = lfgDungeons2.Id;
				}
				return;
			}
			Logger.Write("No heroic dungeons are available. Switching to random normals");
			DungeonBuddySettings.Instance.DungeonType = DungeonType.RandomDungeon;
			DungeonBuddySettings.Instance.SelectedRandomDungeonId = 0u;
		}
		if (DungeonBuddySettings.Instance.DungeonType == DungeonType.RandomDungeon)
		{
			LfgDungeons lfgDungeons3 = array.FirstOrDefault(d => d.Id == DungeonBuddySettings.Instance.SelectedRandomDungeonId) ?? array.FirstOrDefault();
			if (lfgDungeons3 != null)
			{
				cbDungeonSelection.SelectedIndex = cbDungeonSelection.Items.IndexOf(lfgDungeons3);
				if (lfgDungeons3.Id != DungeonBuddySettings.Instance.SelectedRandomDungeonId)
				{
					if (DungeonBuddySettings.Instance.SelectedRandomDungeonId != 0)
					{
						Logger.Write("{0} was not found. Switching to {1}", LfgDungeons.FromId(DungeonBuddySettings.Instance.SelectedRandomDungeonId).Name, lfgDungeons3.Name);
					}
					DungeonBuddySettings.Instance.SelectedRandomDungeonId = lfgDungeons3.Id;
				}
				return;
			}
			Logger.Write("No random dungeons are available. Switching to specific dungeons");
			DungeonBuddySettings.Instance.DungeonType = DungeonType.Specific;
			DungeonBuddySettings.Instance.SelectedRandomDungeonId = 0u;
		}
		if (DungeonBuddySettings.Instance.DungeonType == DungeonType.Specific)
		{
			cbDungeonSelection.SelectedIndex = 2;
		}
		if (DungeonBuddySettings.Instance.DungeonType == DungeonType.Farm)
		{
			cbDungeonSelection.SelectedIndex = 1;
		}
		if (DungeonBuddySettings.Instance.DungeonType == DungeonType.None)
		{
			cbDungeonSelection.SelectedIndex = 0;
		}
	}

	private void UpdateDungeonList()
	{
		if (!DungeonManager.Initialized)
		{
			return;
		}
		twDungeonSelection.Nodes.Clear();
		if (DungeonBuddySettings.Instance.DungeonType != DungeonType.Specific && DungeonBuddySettings.Instance.DungeonType != DungeonType.Farm)
		{
			twDungeonSelection.Enabled = false;
			return;
		}
		twDungeonSelection.Enabled = true;
		IEnumerable<LfgDungeons> source = (cbShowAllDungeons.Checked ? LfgDungeons.Dungeons : LfgQueue.GetAvailableDungeons());
		GenerateTreeView(twDungeonSelection, debug: false, source.Where(r => DungeonManager.HasScriptForDungeon(r.Id)), DungeonBuddySettings.Instance.SelectedDungeonIds);
	}

	private void SetDungeonWarningText(string format, params object[] args)
	{
		lblDungeonWarningText.Text = string.Format(format, args);
	}

	private void btnSelectAll_Click(object sender, EventArgs e)
	{
		foreach (TreeNode item in twDungeonSelection.Nodes.Cast<TreeNode>())
		{
			item.Checked = true;
		}
	}

	private void btnUnselectAll_Click(object sender, EventArgs e)
	{
		foreach (TreeNode item in twDungeonSelection.Nodes.Cast<TreeNode>())
		{
			item.Checked = false;
		}
	}

	private void twDungeonSelection_AfterCheck(object sender, TreeViewEventArgs e)
	{
		uint[] dungeonIds = DungeonBuddySettings.Instance.SelectedDungeonIds;
		if (DoTreeviewChecks(twDungeonSelection, e.Node, DungeonBuddySettings.Instance.DungeonType == DungeonType.Farm, ref dungeonIds))
		{
			DungeonBuddySettings.Instance.SelectedDungeonIds = dungeonIds;
		}
	}

	private void cbDungeonSelection_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cbDungeonSelection.SelectedItem is LfgDungeons lfgDungeons)
		{
			if (lfgDungeons.IsHolidayEvent)
			{
				DungeonBuddySettings.Instance.DungeonType = DungeonType.SpecialEvent;
			}
			else if (lfgDungeons.LfgDifficulty == LfgDungeons.Difficulty.Normal)
			{
				DungeonBuddySettings.Instance.DungeonType = DungeonType.RandomDungeon;
			}
			else if (lfgDungeons.LfgDifficulty == LfgDungeons.Difficulty.Heroic)
			{
				DungeonBuddySettings.Instance.DungeonType = DungeonType.RandomHeroic;
			}
			DungeonBuddySettings.Instance.SelectedRandomDungeonId = lfgDungeons.Id;
		}
		else
		{
			if (cbDungeonSelection.SelectedIndex == 1)
			{
				DungeonBuddySettings.Instance.DungeonType = DungeonType.Farm;
			}
			else if (cbDungeonSelection.SelectedIndex == 2)
			{
				DungeonBuddySettings.Instance.DungeonType = DungeonType.Specific;
			}
			else
			{
				DungeonBuddySettings.Instance.DungeonType = DungeonType.None;
			}
			DungeonBuddySettings.Instance.SelectedRandomDungeonId = 0u;
		}
		DungeonType dungeonType = DungeonBuddySettings.Instance.DungeonType;
		lblDungeonWarningText.Visible = dungeonType == DungeonType.None || (!DungeonManager.Initialized && (dungeonType == DungeonType.Farm || dungeonType == DungeonType.Specific));
		UpdateDungeonList();
	}

	private void UpdateRaidCombo()
	{
		cbRaidSelection.Items.Clear();
		cbRaidSelection.Items.Add("None");
		cbRaidSelection.Items.Add("Farm");
		cbRaidSelection.Items.Add("Specific Raid");
		cbRaidSelection.Items.Add("Queue For Raids Not Completed");
		switch (DungeonBuddySettings.Instance.RaidType)
		{
		case RaidType.None:
			cbRaidSelection.SelectedIndex = 0;
			break;
		case RaidType.Farm:
			cbRaidSelection.SelectedIndex = 1;
			break;
		case RaidType.Specific:
			cbRaidSelection.SelectedIndex = 2;
			break;
		case RaidType.AutoQueueBest:
			cbRaidSelection.SelectedIndex = 3;
			break;
		}
		UpdateRaidList();
	}

	private void UpdateRaidList()
	{
		if (!DungeonManager.Initialized)
		{
			return;
		}
		RaidType raidType = DungeonBuddySettings.Instance.RaidType;
		twRaidSelection.Nodes.Clear();
		if (raidType != RaidType.None)
		{
			twRaidSelection.Enabled = true;
			IEnumerable<LfgDungeons> source = ((raidType == RaidType.Farm) ? LfgDungeons.Raids : LfgDungeons.LfRaids);
			GenerateTreeView(twRaidSelection, debug: false, source.Where(r => DungeonManager.HasScriptForDungeon(r.Id)), DungeonBuddySettings.Instance.SelectedRaidIds, "Looking for Raid");
		}
		else
		{
			twRaidSelection.Enabled = false;
		}
	}

	private void SetRaidWarningText(string format, params object[] args)
	{
		lblRaidWarningText.Text = string.Format(format, args);
	}

	private void cbRaidSelection_SelectedIndexChanged(object sender, EventArgs e)
	{
		RaidType selectedIndex = (RaidType)cbRaidSelection.SelectedIndex;
		if (selectedIndex == RaidType.None)
		{
			DungeonBuddySettings.Instance.RaidType = RaidType.None;
			DungeonBuddySettings.Instance.SelectedRaidIds = new uint[0];
		}
		else
		{
			IEnumerable<LfgDungeons> raids = ((selectedIndex == RaidType.Farm) ? LfgDungeons.Raids : LfgDungeons.LfRaids);
			List<uint> list = DungeonBuddySettings.Instance.SelectedRaidIds.ToList();
			list.RemoveAll(id => raids.All(r => r.Id != id));
			DungeonBuddySettings.Instance.SelectedRaidIds = list.ToArray();
		}
		DungeonBuddySettings.Instance.RaidType = selectedIndex;
		lblRaidWarningText.Visible = selectedIndex == RaidType.None || !DungeonManager.Initialized;
		UpdateRaidList();
	}

	private void twRaidSelection_AfterCheck(object sender, TreeViewEventArgs e)
	{
		uint[] dungeonIds = DungeonBuddySettings.Instance.SelectedRaidIds;
		if (DoTreeviewChecks(twRaidSelection, e.Node, DungeonBuddySettings.Instance.RaidType == RaidType.Farm, ref dungeonIds))
		{
			DungeonBuddySettings.Instance.SelectedRaidIds = dungeonIds;
		}
	}

	private void UpdateScenarioCombo()
	{
		cbScenarioSelection.Items.Clear();
		cbScenarioSelection.Items.Add("None");
		cbScenarioSelection.Items.Add("Specifc");
		LfgDungeons[] array = LfgQueue.GetJoinableRandomScenarios();
		cbScenarioSelection.Items.AddRange(array);
		if (DungeonBuddySettings.Instance.ScenarioType == ScenarioType.Random && DungeonBuddySettings.Instance.SelectedScenarioId != 0)
		{
			LfgDungeons lfgDungeons = array.FirstOrDefault(d => d.Id == DungeonBuddySettings.Instance.SelectedScenarioId);
			if (lfgDungeons != null)
			{
				cbScenarioSelection.SelectedIndex = cbScenarioSelection.Items.IndexOf(lfgDungeons);
				return;
			}
			Logger.Write("{0} is not available. Setting Scenario Type to 'None'", (DungeonBuddySettings.Instance.SelectedScenarioId != 0) ? LfgDungeons.FromId(DungeonBuddySettings.Instance.SelectedScenarioId).Name : string.Empty);
			DungeonBuddySettings.Instance.ScenarioType = ScenarioType.None;
			DungeonBuddySettings.Instance.SelectedScenarioId = 0u;
		}
		if (DungeonBuddySettings.Instance.ScenarioType == ScenarioType.Specific)
		{
			cbScenarioSelection.SelectedIndex = 1;
		}
		if (DungeonBuddySettings.Instance.ScenarioType == ScenarioType.None)
		{
			cbScenarioSelection.SelectedIndex = 0;
		}
		UpdateScenarioList();
	}

	private void UpdateScenarioList()
	{
		if (!DungeonManager.Initialized)
		{
			return;
		}
		twScenarioSelection.Nodes.Clear();
		if (DungeonBuddySettings.Instance.ScenarioType == ScenarioType.Specific)
		{
			twScenarioSelection.Enabled = true;
			GenerateTreeView(twScenarioSelection, debug: false, from s in LfgQueue.GetAvailableScenarios()
				where DungeonManager.HasScriptForDungeon(s.Id)
				select s, DungeonBuddySettings.Instance.SpecifiedScenariosIds);
		}
		else
		{
			twScenarioSelection.Enabled = false;
		}
	}

	private void SetScenaraioWarningText(string format, params object[] args)
	{
		lblScenarioWarningText.Text = string.Format(format, args);
	}

	private void twScenarioSelection_AfterCheck(object sender, TreeViewEventArgs e)
	{
		uint[] dungeonIds = DungeonBuddySettings.Instance.SpecifiedScenariosIds;
		if (DoTreeviewChecks(twScenarioSelection, e.Node, singleSelectionMode: false, ref dungeonIds))
		{
			DungeonBuddySettings.Instance.SpecifiedScenariosIds = dungeonIds;
		}
	}

	private void cbScenarioSelection_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cbScenarioSelection.SelectedItem is LfgDungeons)
		{
			LfgDungeons lfgDungeons = (LfgDungeons)cbScenarioSelection.SelectedItem;
			DungeonBuddySettings.Instance.ScenarioType = ScenarioType.Random;
			DungeonBuddySettings.Instance.SelectedScenarioId = lfgDungeons.Id;
		}
		else
		{
			DungeonBuddySettings.Instance.ScenarioType = ((cbScenarioSelection.SelectedIndex == 1) ? ScenarioType.Specific : ScenarioType.None);
			DungeonBuddySettings.Instance.SelectedScenarioId = 0u;
		}
		ScenarioType scenarioType = DungeonBuddySettings.Instance.ScenarioType;
		lblScenarioWarningText.Visible = scenarioType == ScenarioType.None || (!DungeonManager.Initialized && scenarioType == ScenarioType.Specific);
		UpdateScenarioList();
	}

	private void btnSelectAllScenarios_Click(object sender, EventArgs e)
	{
		foreach (TreeNode item in twScenarioSelection.Nodes.Cast<TreeNode>())
		{
			item.Checked = true;
		}
	}

	private void btnUnselectAllScenarios_Click(object sender, EventArgs e)
	{
		foreach (TreeNode item in twScenarioSelection.Nodes.Cast<TreeNode>())
		{
			item.Checked = false;
		}
	}

	private void cbShowAllDungeons_CheckedChanged(object sender, EventArgs e)
	{
		DungeonBuddySettings.Instance.ShowAllDungeons = cbShowAllDungeons.Checked;
		tabControlMain_SelectedIndexChanged(null, EventArgs.Empty);
	}

	private void cbShowAllScenarios_CheckedChanged(object sender, EventArgs e)
	{
		DungeonBuddySettings.Instance.ShowAllScenarios = cbShowAllScenarios.Checked;
		tabControlMain_SelectedIndexChanged(null, EventArgs.Empty);
	}

	private void navigationButton_Click(object sender, EventArgs e)
	{
		if (ProfileManager.CurrentProfile == null)
		{
			Logger.Write("No dungeon profile is loaded");
			return;
		}
		Logger.Write("****** Boss mesh navigation status for {0} ******", ProfileManager.CurrentProfile.Name);
		foreach (Boss bossEncounter in ProfileManager.CurrentProfile.BossEncounters)
		{
			Logger.Write("{0} Can be fully navigated to?: {1}", bossEncounter.Name, bossEncounter.IsRandomSpawnBoss() || Navigator.CanNavigateFully(StyxWoW.Me.Location, bossEncounter.PathBreadCrumbs.Peek()));
		}
	}

	private void debugBtn3_Click(object sender, EventArgs e)
	{
		if (_pathView == null || _pathView.Disposing || _pathView.IsDisposed)
		{
			_pathView = new PathView();
		}
		_pathView.Show();
	}

	private void twBossTree_AfterCheck(object sender, TreeViewEventArgs e)
	{
		Boss boss = BossManager.BossEncounters.FirstOrDefault(b => b.Name == e.Node.Text);
		if (boss != null)
		{
			if (e.Node.Checked)
			{
				boss.Reset();
			}
			else
			{
				boss.MarkAsDead("Manually set via GUI");
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.tableLayoutMain = new System.Windows.Forms.TableLayoutPanel();
		this.label1 = new System.Windows.Forms.Label();
		this.tabControlMain = new System.Windows.Forms.TabControl();
		this.tabPageCommon = new System.Windows.Forms.TabPage();
		this.propertyGridMain = new System.Windows.Forms.PropertyGrid();
		this.tabPageDungeons = new System.Windows.Forms.TabPage();
		this.lblDungeonWarningText = new System.Windows.Forms.Label();
		this.tableLayoutDungeons = new System.Windows.Forms.TableLayoutPanel();
		this.twDungeonSelection = new System.Windows.Forms.TreeView();
		this.bottomDungeonsPanel = new System.Windows.Forms.Panel();
		this.btnUnselectAllDungeons = new System.Windows.Forms.Button();
		this.btnSelectAllDungeons = new System.Windows.Forms.Button();
		this.cbShowAllDungeons = new System.Windows.Forms.CheckBox();
		this.TopDungeonsPanel = new System.Windows.Forms.Panel();
		this.cbDungeonSelection = new System.Windows.Forms.ComboBox();
		this.label2 = new System.Windows.Forms.Label();
		this.tabPageScenarios = new System.Windows.Forms.TabPage();
		this.lblScenarioWarningText = new System.Windows.Forms.Label();
		this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
		this.twScenarioSelection = new System.Windows.Forms.TreeView();
		this.panelScenariosBottom = new System.Windows.Forms.Panel();
		this.btnUnselectAllScenarios = new System.Windows.Forms.Button();
		this.btnSelectAllScenarios = new System.Windows.Forms.Button();
		this.cbShowAllScenarios = new System.Windows.Forms.CheckBox();
		this.panelScenarioTop = new System.Windows.Forms.Panel();
		this.cbScenarioSelection = new System.Windows.Forms.ComboBox();
		this.label4 = new System.Windows.Forms.Label();
		this.tabPageRaids = new System.Windows.Forms.TabPage();
		this.lblRaidWarningText = new System.Windows.Forms.Label();
		this.tableLayoutRaids = new System.Windows.Forms.TableLayoutPanel();
		this.twRaidSelection = new System.Windows.Forms.TreeView();
		this.panelRaidTop = new System.Windows.Forms.Panel();
		this.cbRaidSelection = new System.Windows.Forms.ComboBox();
		this.label3 = new System.Windows.Forms.Label();
		this.tabPageDebug = new System.Windows.Forms.TabPage();
		this.tabControl1 = new System.Windows.Forms.TabControl();
		this.tabPage1 = new System.Windows.Forms.TabPage();
		this.twDebug = new System.Windows.Forms.TreeView();
		this.tabPage2 = new System.Windows.Forms.TabPage();
		this.twBossTree = new System.Windows.Forms.TreeView();
		this.debugBtn3 = new System.Windows.Forms.Button();
		this.btnDumpNavigationInfo = new System.Windows.Forms.Button();
		this.tableLayoutMain.SuspendLayout();
		this.tabControlMain.SuspendLayout();
		this.tabPageCommon.SuspendLayout();
		this.tabPageDungeons.SuspendLayout();
		this.tableLayoutDungeons.SuspendLayout();
		this.bottomDungeonsPanel.SuspendLayout();
		this.TopDungeonsPanel.SuspendLayout();
		this.tabPageScenarios.SuspendLayout();
		this.tableLayoutPanel1.SuspendLayout();
		this.panelScenariosBottom.SuspendLayout();
		this.panelScenarioTop.SuspendLayout();
		this.tabPageRaids.SuspendLayout();
		this.tableLayoutRaids.SuspendLayout();
		this.panelRaidTop.SuspendLayout();
		this.tabPageDebug.SuspendLayout();
		this.tabControl1.SuspendLayout();
		this.tabPage1.SuspendLayout();
		this.tabPage2.SuspendLayout();
		base.SuspendLayout();
		this.tableLayoutMain.ColumnCount = 1;
		this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutMain.Controls.Add(this.label1, 0, 1);
		this.tableLayoutMain.Controls.Add(this.tabControlMain, 0, 0);
		this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
		this.tableLayoutMain.Name = "tableLayoutMain";
		this.tableLayoutMain.RowCount = 2;
		this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30f));
		this.tableLayoutMain.Size = new System.Drawing.Size(384, 511);
		this.tableLayoutMain.TabIndex = 1;
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Impact", 17f);
		this.label1.Location = new System.Drawing.Point(3, 481);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(156, 28);
		this.label1.TabIndex = 5;
		this.label1.Text = "DungeonBuddy";
		this.tabControlMain.Controls.Add(this.tabPageCommon);
		this.tabControlMain.Controls.Add(this.tabPageDungeons);
		this.tabControlMain.Controls.Add(this.tabPageScenarios);
		this.tabControlMain.Controls.Add(this.tabPageRaids);
		this.tabControlMain.Controls.Add(this.tabPageDebug);
		this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tabControlMain.Location = new System.Drawing.Point(3, 3);
		this.tabControlMain.Name = "tabControlMain";
		this.tabControlMain.SelectedIndex = 0;
		this.tabControlMain.Size = new System.Drawing.Size(378, 475);
		this.tabControlMain.TabIndex = 4;
		this.tabControlMain.SelectedIndexChanged += new System.EventHandler(tabControlMain_SelectedIndexChanged);
		this.tabPageCommon.Controls.Add(this.propertyGridMain);
		this.tabPageCommon.Location = new System.Drawing.Point(4, 22);
		this.tabPageCommon.Name = "tabPageCommon";
		this.tabPageCommon.Padding = new System.Windows.Forms.Padding(3);
		this.tabPageCommon.Size = new System.Drawing.Size(370, 449);
		this.tabPageCommon.TabIndex = 0;
		this.tabPageCommon.Text = "Common";
		this.tabPageCommon.UseVisualStyleBackColor = true;
		this.propertyGridMain.Dock = System.Windows.Forms.DockStyle.Fill;
		this.propertyGridMain.Location = new System.Drawing.Point(3, 3);
		this.propertyGridMain.Name = "propertyGridMain";
		this.propertyGridMain.Size = new System.Drawing.Size(364, 443);
		this.propertyGridMain.TabIndex = 0;
		this.tabPageDungeons.Controls.Add(this.lblDungeonWarningText);
		this.tabPageDungeons.Controls.Add(this.tableLayoutDungeons);
		this.tabPageDungeons.Location = new System.Drawing.Point(4, 22);
		this.tabPageDungeons.Margin = new System.Windows.Forms.Padding(0);
		this.tabPageDungeons.Name = "tabPageDungeons";
		this.tabPageDungeons.Size = new System.Drawing.Size(370, 449);
		this.tabPageDungeons.TabIndex = 1;
		this.tabPageDungeons.Text = "Dungeon";
		this.tabPageDungeons.UseVisualStyleBackColor = true;
		this.lblDungeonWarningText.AutoEllipsis = true;
		this.lblDungeonWarningText.AutoSize = true;
		this.lblDungeonWarningText.Font = new System.Drawing.Font("Microsoft Sans Serif", 14f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblDungeonWarningText.Location = new System.Drawing.Point(40, 40);
		this.lblDungeonWarningText.MaximumSize = new System.Drawing.Size(300, 0);
		this.lblDungeonWarningText.Name = "lblDungeonWarningText";
		this.lblDungeonWarningText.Size = new System.Drawing.Size(132, 24);
		this.lblDungeonWarningText.TabIndex = 3;
		this.lblDungeonWarningText.Text = "Warning Label";
		this.lblDungeonWarningText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.tableLayoutDungeons.ColumnCount = 1;
		this.tableLayoutDungeons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutDungeons.Controls.Add(this.twDungeonSelection, 0, 1);
		this.tableLayoutDungeons.Controls.Add(this.bottomDungeonsPanel, 0, 2);
		this.tableLayoutDungeons.Controls.Add(this.TopDungeonsPanel, 0, 0);
		this.tableLayoutDungeons.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tableLayoutDungeons.Location = new System.Drawing.Point(0, 0);
		this.tableLayoutDungeons.Margin = new System.Windows.Forms.Padding(0);
		this.tableLayoutDungeons.Name = "tableLayoutDungeons";
		this.tableLayoutDungeons.RowCount = 3;
		this.tableLayoutDungeons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25f));
		this.tableLayoutDungeons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutDungeons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25f));
		this.tableLayoutDungeons.Size = new System.Drawing.Size(370, 449);
		this.tableLayoutDungeons.TabIndex = 6;
		this.twDungeonSelection.CheckBoxes = true;
		this.twDungeonSelection.Dock = System.Windows.Forms.DockStyle.Fill;
		this.twDungeonSelection.Location = new System.Drawing.Point(3, 28);
		this.twDungeonSelection.Name = "twDungeonSelection";
		this.twDungeonSelection.Size = new System.Drawing.Size(364, 393);
		this.twDungeonSelection.TabIndex = 4;
		this.twDungeonSelection.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(twDungeonSelection_AfterCheck);
		this.bottomDungeonsPanel.Controls.Add(this.btnUnselectAllDungeons);
		this.bottomDungeonsPanel.Controls.Add(this.btnSelectAllDungeons);
		this.bottomDungeonsPanel.Controls.Add(this.cbShowAllDungeons);
		this.bottomDungeonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.bottomDungeonsPanel.Location = new System.Drawing.Point(3, 427);
		this.bottomDungeonsPanel.Name = "bottomDungeonsPanel";
		this.bottomDungeonsPanel.Size = new System.Drawing.Size(364, 19);
		this.bottomDungeonsPanel.TabIndex = 5;
		this.btnUnselectAllDungeons.Dock = System.Windows.Forms.DockStyle.Left;
		this.btnUnselectAllDungeons.Location = new System.Drawing.Point(188, 0);
		this.btnUnselectAllDungeons.Name = "btnUnselectAllDungeons";
		this.btnUnselectAllDungeons.Size = new System.Drawing.Size(75, 19);
		this.btnUnselectAllDungeons.TabIndex = 2;
		this.btnUnselectAllDungeons.Text = "Unselect All";
		this.btnUnselectAllDungeons.UseVisualStyleBackColor = true;
		this.btnUnselectAllDungeons.Click += new System.EventHandler(btnUnselectAll_Click);
		this.btnSelectAllDungeons.Dock = System.Windows.Forms.DockStyle.Left;
		this.btnSelectAllDungeons.Location = new System.Drawing.Point(119, 0);
		this.btnSelectAllDungeons.Name = "btnSelectAllDungeons";
		this.btnSelectAllDungeons.Size = new System.Drawing.Size(69, 19);
		this.btnSelectAllDungeons.TabIndex = 1;
		this.btnSelectAllDungeons.Text = "Select All";
		this.btnSelectAllDungeons.UseVisualStyleBackColor = true;
		this.btnSelectAllDungeons.Click += new System.EventHandler(btnSelectAll_Click);
		this.cbShowAllDungeons.AutoSize = true;
		this.cbShowAllDungeons.Dock = System.Windows.Forms.DockStyle.Left;
		this.cbShowAllDungeons.Location = new System.Drawing.Point(0, 0);
		this.cbShowAllDungeons.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
		this.cbShowAllDungeons.Name = "cbShowAllDungeons";
		this.cbShowAllDungeons.Size = new System.Drawing.Size(119, 19);
		this.cbShowAllDungeons.TabIndex = 5;
		this.cbShowAllDungeons.Text = "Show All Dungeons";
		this.cbShowAllDungeons.UseVisualStyleBackColor = true;
		this.cbShowAllDungeons.CheckedChanged += new System.EventHandler(cbShowAllDungeons_CheckedChanged);
		this.TopDungeonsPanel.Controls.Add(this.cbDungeonSelection);
		this.TopDungeonsPanel.Controls.Add(this.label2);
		this.TopDungeonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.TopDungeonsPanel.Location = new System.Drawing.Point(3, 3);
		this.TopDungeonsPanel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
		this.TopDungeonsPanel.Name = "TopDungeonsPanel";
		this.TopDungeonsPanel.Size = new System.Drawing.Size(364, 22);
		this.TopDungeonsPanel.TabIndex = 6;
		this.cbDungeonSelection.BackColor = System.Drawing.SystemColors.Window;
		this.cbDungeonSelection.DisplayMember = "Name";
		this.cbDungeonSelection.Dock = System.Windows.Forms.DockStyle.Fill;
		this.cbDungeonSelection.FormattingEnabled = true;
		this.cbDungeonSelection.Location = new System.Drawing.Point(44, 0);
		this.cbDungeonSelection.Margin = new System.Windows.Forms.Padding(0);
		this.cbDungeonSelection.Name = "cbDungeonSelection";
		this.cbDungeonSelection.Size = new System.Drawing.Size(320, 21);
		this.cbDungeonSelection.TabIndex = 8;
		this.cbDungeonSelection.SelectedIndexChanged += new System.EventHandler(cbDungeonSelection_SelectedIndexChanged);
		this.label2.AutoSize = true;
		this.label2.Dock = System.Windows.Forms.DockStyle.Left;
		this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(0, 0);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(44, 18);
		this.label2.TabIndex = 7;
		this.label2.Text = "Type";
		this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.tabPageScenarios.Controls.Add(this.lblScenarioWarningText);
		this.tabPageScenarios.Controls.Add(this.tableLayoutPanel1);
		this.tabPageScenarios.Location = new System.Drawing.Point(4, 22);
		this.tabPageScenarios.Name = "tabPageScenarios";
		this.tabPageScenarios.Size = new System.Drawing.Size(370, 449);
		this.tabPageScenarios.TabIndex = 4;
		this.tabPageScenarios.Text = "Scenarios";
		this.tabPageScenarios.UseVisualStyleBackColor = true;
		this.lblScenarioWarningText.AutoEllipsis = true;
		this.lblScenarioWarningText.AutoSize = true;
		this.lblScenarioWarningText.Font = new System.Drawing.Font("Microsoft Sans Serif", 14f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblScenarioWarningText.Location = new System.Drawing.Point(40, 40);
		this.lblScenarioWarningText.MaximumSize = new System.Drawing.Size(300, 0);
		this.lblScenarioWarningText.Name = "lblScenarioWarningText";
		this.lblScenarioWarningText.Size = new System.Drawing.Size(132, 24);
		this.lblScenarioWarningText.TabIndex = 7;
		this.lblScenarioWarningText.Text = "Warning Label";
		this.lblScenarioWarningText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.tableLayoutPanel1.ColumnCount = 1;
		this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel1.Controls.Add(this.twScenarioSelection, 0, 1);
		this.tableLayoutPanel1.Controls.Add(this.panelScenariosBottom, 0, 2);
		this.tableLayoutPanel1.Controls.Add(this.panelScenarioTop, 0, 0);
		this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
		this.tableLayoutPanel1.Name = "tableLayoutPanel1";
		this.tableLayoutPanel1.RowCount = 4;
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
		this.tableLayoutPanel1.Size = new System.Drawing.Size(370, 449);
		this.tableLayoutPanel1.TabIndex = 0;
		this.twScenarioSelection.CheckBoxes = true;
		this.twScenarioSelection.Dock = System.Windows.Forms.DockStyle.Fill;
		this.twScenarioSelection.Location = new System.Drawing.Point(3, 28);
		this.twScenarioSelection.Name = "twScenarioSelection";
		this.twScenarioSelection.Size = new System.Drawing.Size(364, 373);
		this.twScenarioSelection.TabIndex = 6;
		this.twScenarioSelection.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(twScenarioSelection_AfterCheck);
		this.panelScenariosBottom.Controls.Add(this.btnUnselectAllScenarios);
		this.panelScenariosBottom.Controls.Add(this.btnSelectAllScenarios);
		this.panelScenariosBottom.Controls.Add(this.cbShowAllScenarios);
		this.panelScenariosBottom.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panelScenariosBottom.Location = new System.Drawing.Point(3, 407);
		this.panelScenariosBottom.Name = "panelScenariosBottom";
		this.panelScenariosBottom.Size = new System.Drawing.Size(364, 19);
		this.panelScenariosBottom.TabIndex = 0;
		this.btnUnselectAllScenarios.Dock = System.Windows.Forms.DockStyle.Left;
		this.btnUnselectAllScenarios.Location = new System.Drawing.Point(186, 0);
		this.btnUnselectAllScenarios.Name = "btnUnselectAllScenarios";
		this.btnUnselectAllScenarios.Size = new System.Drawing.Size(75, 19);
		this.btnUnselectAllScenarios.TabIndex = 7;
		this.btnUnselectAllScenarios.Text = "Unselect All";
		this.btnUnselectAllScenarios.UseVisualStyleBackColor = true;
		this.btnUnselectAllScenarios.Click += new System.EventHandler(btnUnselectAllScenarios_Click);
		this.btnSelectAllScenarios.Dock = System.Windows.Forms.DockStyle.Left;
		this.btnSelectAllScenarios.Location = new System.Drawing.Point(117, 0);
		this.btnSelectAllScenarios.Name = "btnSelectAllScenarios";
		this.btnSelectAllScenarios.Size = new System.Drawing.Size(69, 19);
		this.btnSelectAllScenarios.TabIndex = 6;
		this.btnSelectAllScenarios.Text = "Select All";
		this.btnSelectAllScenarios.UseVisualStyleBackColor = true;
		this.btnSelectAllScenarios.Click += new System.EventHandler(btnSelectAllScenarios_Click);
		this.cbShowAllScenarios.AutoSize = true;
		this.cbShowAllScenarios.Dock = System.Windows.Forms.DockStyle.Left;
		this.cbShowAllScenarios.Location = new System.Drawing.Point(0, 0);
		this.cbShowAllScenarios.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
		this.cbShowAllScenarios.Name = "cbShowAllScenarios";
		this.cbShowAllScenarios.Size = new System.Drawing.Size(117, 19);
		this.cbShowAllScenarios.TabIndex = 8;
		this.cbShowAllScenarios.Text = "Show All Scenarios";
		this.cbShowAllScenarios.UseVisualStyleBackColor = true;
		this.cbShowAllScenarios.CheckedChanged += new System.EventHandler(cbShowAllScenarios_CheckedChanged);
		this.panelScenarioTop.Controls.Add(this.cbScenarioSelection);
		this.panelScenarioTop.Controls.Add(this.label4);
		this.panelScenarioTop.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panelScenarioTop.Location = new System.Drawing.Point(3, 3);
		this.panelScenarioTop.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
		this.panelScenarioTop.Name = "panelScenarioTop";
		this.panelScenarioTop.Size = new System.Drawing.Size(364, 22);
		this.panelScenarioTop.TabIndex = 1;
		this.cbScenarioSelection.BackColor = System.Drawing.SystemColors.Window;
		this.cbScenarioSelection.DisplayMember = "Name";
		this.cbScenarioSelection.Dock = System.Windows.Forms.DockStyle.Fill;
		this.cbScenarioSelection.FormattingEnabled = true;
		this.cbScenarioSelection.Location = new System.Drawing.Point(44, 0);
		this.cbScenarioSelection.Margin = new System.Windows.Forms.Padding(0);
		this.cbScenarioSelection.Name = "cbScenarioSelection";
		this.cbScenarioSelection.Size = new System.Drawing.Size(320, 21);
		this.cbScenarioSelection.TabIndex = 15;
		this.cbScenarioSelection.SelectedIndexChanged += new System.EventHandler(cbScenarioSelection_SelectedIndexChanged);
		this.label4.AutoSize = true;
		this.label4.Dock = System.Windows.Forms.DockStyle.Left;
		this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label4.Location = new System.Drawing.Point(0, 0);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(44, 18);
		this.label4.TabIndex = 14;
		this.label4.Text = "Type";
		this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.tabPageRaids.Controls.Add(this.lblRaidWarningText);
		this.tabPageRaids.Controls.Add(this.tableLayoutRaids);
		this.tabPageRaids.Location = new System.Drawing.Point(4, 22);
		this.tabPageRaids.Name = "tabPageRaids";
		this.tabPageRaids.Size = new System.Drawing.Size(370, 449);
		this.tabPageRaids.TabIndex = 3;
		this.tabPageRaids.Text = "Raids";
		this.tabPageRaids.UseVisualStyleBackColor = true;
		this.lblRaidWarningText.AutoEllipsis = true;
		this.lblRaidWarningText.AutoSize = true;
		this.lblRaidWarningText.Font = new System.Drawing.Font("Microsoft Sans Serif", 14f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblRaidWarningText.Location = new System.Drawing.Point(40, 40);
		this.lblRaidWarningText.MaximumSize = new System.Drawing.Size(300, 0);
		this.lblRaidWarningText.Name = "lblRaidWarningText";
		this.lblRaidWarningText.Size = new System.Drawing.Size(132, 24);
		this.lblRaidWarningText.TabIndex = 6;
		this.lblRaidWarningText.Text = "Warning Label";
		this.lblRaidWarningText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.tableLayoutRaids.ColumnCount = 1;
		this.tableLayoutRaids.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutRaids.Controls.Add(this.twRaidSelection, 0, 1);
		this.tableLayoutRaids.Controls.Add(this.panelRaidTop, 0, 0);
		this.tableLayoutRaids.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tableLayoutRaids.Location = new System.Drawing.Point(0, 0);
		this.tableLayoutRaids.Margin = new System.Windows.Forms.Padding(0);
		this.tableLayoutRaids.Name = "tableLayoutRaids";
		this.tableLayoutRaids.RowCount = 3;
		this.tableLayoutRaids.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25f));
		this.tableLayoutRaids.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutRaids.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
		this.tableLayoutRaids.Size = new System.Drawing.Size(370, 449);
		this.tableLayoutRaids.TabIndex = 0;
		this.twRaidSelection.CheckBoxes = true;
		this.twRaidSelection.Dock = System.Windows.Forms.DockStyle.Fill;
		this.twRaidSelection.Location = new System.Drawing.Point(3, 28);
		this.twRaidSelection.Name = "twRaidSelection";
		this.twRaidSelection.Size = new System.Drawing.Size(364, 398);
		this.twRaidSelection.TabIndex = 5;
		this.twRaidSelection.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(twRaidSelection_AfterCheck);
		this.panelRaidTop.Controls.Add(this.cbRaidSelection);
		this.panelRaidTop.Controls.Add(this.label3);
		this.panelRaidTop.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panelRaidTop.Location = new System.Drawing.Point(3, 3);
		this.panelRaidTop.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
		this.panelRaidTop.Name = "panelRaidTop";
		this.panelRaidTop.Size = new System.Drawing.Size(364, 22);
		this.panelRaidTop.TabIndex = 0;
		this.cbRaidSelection.BackColor = System.Drawing.SystemColors.Window;
		this.cbRaidSelection.DisplayMember = "Name";
		this.cbRaidSelection.Dock = System.Windows.Forms.DockStyle.Fill;
		this.cbRaidSelection.FormattingEnabled = true;
		this.cbRaidSelection.Location = new System.Drawing.Point(44, 0);
		this.cbRaidSelection.Margin = new System.Windows.Forms.Padding(0);
		this.cbRaidSelection.Name = "cbRaidSelection";
		this.cbRaidSelection.Size = new System.Drawing.Size(320, 21);
		this.cbRaidSelection.TabIndex = 13;
		this.cbRaidSelection.SelectedIndexChanged += new System.EventHandler(cbRaidSelection_SelectedIndexChanged);
		this.label3.AutoSize = true;
		this.label3.Dock = System.Windows.Forms.DockStyle.Left;
		this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label3.Location = new System.Drawing.Point(0, 0);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(44, 18);
		this.label3.TabIndex = 12;
		this.label3.Text = "Type";
		this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.tabPageDebug.Controls.Add(this.tabControl1);
		this.tabPageDebug.Controls.Add(this.debugBtn3);
		this.tabPageDebug.Controls.Add(this.btnDumpNavigationInfo);
		this.tabPageDebug.Location = new System.Drawing.Point(4, 22);
		this.tabPageDebug.Name = "tabPageDebug";
		this.tabPageDebug.Padding = new System.Windows.Forms.Padding(3);
		this.tabPageDebug.Size = new System.Drawing.Size(370, 449);
		this.tabPageDebug.TabIndex = 2;
		this.tabPageDebug.Text = "Debug";
		this.tabPageDebug.UseVisualStyleBackColor = true;
		this.tabControl1.Controls.Add(this.tabPage1);
		this.tabControl1.Controls.Add(this.tabPage2);
		this.tabControl1.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.tabControl1.Location = new System.Drawing.Point(3, 35);
		this.tabControl1.Name = "tabControl1";
		this.tabControl1.SelectedIndex = 0;
		this.tabControl1.Size = new System.Drawing.Size(364, 411);
		this.tabControl1.TabIndex = 3;
		this.tabPage1.Controls.Add(this.twDebug);
		this.tabPage1.Location = new System.Drawing.Point(4, 22);
		this.tabPage1.Name = "tabPage1";
		this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
		this.tabPage1.Size = new System.Drawing.Size(356, 385);
		this.tabPage1.TabIndex = 0;
		this.tabPage1.Text = "Dungeon List";
		this.tabPage1.UseVisualStyleBackColor = true;
		this.twDebug.Dock = System.Windows.Forms.DockStyle.Fill;
		this.twDebug.Location = new System.Drawing.Point(3, 3);
		this.twDebug.Name = "twDebug";
		this.twDebug.Size = new System.Drawing.Size(350, 379);
		this.twDebug.TabIndex = 3;
		this.tabPage2.Controls.Add(this.twBossTree);
		this.tabPage2.Location = new System.Drawing.Point(4, 22);
		this.tabPage2.Name = "tabPage2";
		this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
		this.tabPage2.Size = new System.Drawing.Size(356, 385);
		this.tabPage2.TabIndex = 1;
		this.tabPage2.Text = "Boss List";
		this.tabPage2.UseVisualStyleBackColor = true;
		this.twBossTree.CheckBoxes = true;
		this.twBossTree.Dock = System.Windows.Forms.DockStyle.Fill;
		this.twBossTree.Location = new System.Drawing.Point(3, 3);
		this.twBossTree.Name = "twBossTree";
		this.twBossTree.Size = new System.Drawing.Size(350, 379);
		this.twBossTree.TabIndex = 4;
		this.twBossTree.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(twBossTree_AfterCheck);
		this.debugBtn3.Location = new System.Drawing.Point(5, 6);
		this.debugBtn3.Name = "debugBtn3";
		this.debugBtn3.Size = new System.Drawing.Size(75, 23);
		this.debugBtn3.TabIndex = 0;
		this.debugBtn3.Text = "Show Path";
		this.debugBtn3.UseVisualStyleBackColor = true;
		this.debugBtn3.Click += new System.EventHandler(debugBtn3_Click);
		this.btnDumpNavigationInfo.Location = new System.Drawing.Point(86, 6);
		this.btnDumpNavigationInfo.Name = "btnDumpNavigationInfo";
		this.btnDumpNavigationInfo.Size = new System.Drawing.Size(75, 23);
		this.btnDumpNavigationInfo.TabIndex = 0;
		this.btnDumpNavigationInfo.Text = "Nav Info";
		this.btnDumpNavigationInfo.UseVisualStyleBackColor = true;
		this.btnDumpNavigationInfo.Click += new System.EventHandler(navigationButton_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(384, 511);
		base.Controls.Add(this.tableLayoutMain);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		this.MinimumSize = new System.Drawing.Size(400, 550);
		base.Name = "FormConfig";
		base.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
		this.Text = "DungeonBuddy Config";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(FormConfig_FormClosing);
		base.Load += new System.EventHandler(FormConfig_Load);
		this.tableLayoutMain.ResumeLayout(false);
		this.tableLayoutMain.PerformLayout();
		this.tabControlMain.ResumeLayout(false);
		this.tabPageCommon.ResumeLayout(false);
		this.tabPageDungeons.ResumeLayout(false);
		this.tabPageDungeons.PerformLayout();
		this.tableLayoutDungeons.ResumeLayout(false);
		this.bottomDungeonsPanel.ResumeLayout(false);
		this.bottomDungeonsPanel.PerformLayout();
		this.TopDungeonsPanel.ResumeLayout(false);
		this.TopDungeonsPanel.PerformLayout();
		this.tabPageScenarios.ResumeLayout(false);
		this.tabPageScenarios.PerformLayout();
		this.tableLayoutPanel1.ResumeLayout(false);
		this.panelScenariosBottom.ResumeLayout(false);
		this.panelScenariosBottom.PerformLayout();
		this.panelScenarioTop.ResumeLayout(false);
		this.panelScenarioTop.PerformLayout();
		this.tabPageRaids.ResumeLayout(false);
		this.tabPageRaids.PerformLayout();
		this.tableLayoutRaids.ResumeLayout(false);
		this.panelRaidTop.ResumeLayout(false);
		this.panelRaidTop.PerformLayout();
		this.tabPageDebug.ResumeLayout(false);
		this.tabControl1.ResumeLayout(false);
		this.tabPage1.ResumeLayout(false);
		this.tabPage2.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
