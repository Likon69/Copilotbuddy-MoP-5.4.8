using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Levelbot.ProfileCreation.Forms;

public class FormProfileCreator : Form
{
	private ProfileTreeNode _rootNode;

	private ProfileTreeNode _rightClickedNode;

	private ProfileVendorListViewItem _rightClickedVendor;

	private IContainer components;

	private TreeView _tvwProfileTree;

	private MenuStrip menuStrip1;

	private ToolStripMenuItem fileToolStripMenuItem;

	private ToolStripMenuItem _btnNewProfile;

	private ToolStripMenuItem _btnOpenProfile;

	private ToolStripSeparator toolStripSeparator1;

	private ToolStripMenuItem _btnSaveProfile;

	private ToolStripMenuItem _btnSaveProfileAs;

	private ToolStripSeparator toolStripSeparator2;

	private ToolStripMenuItem _btnExitGUI;

	private ToolStripMenuItem profileToolStripMenuItem;

	private ToolStripMenuItem _MenuBtnAddSubProfile;

	private ToolStripMenuItem _btnRemoveSubProfile;

	private ContextMenuStrip _ctxProfileRightClick;

	private ToolStripMenuItem _ctxBtnAddSubProfile;

	private ToolStripMenuItem _ctxBtnRemove;

	private Label label1;

	private TrackBar _tbarMinDurability;

	private Label _lblMinDurability;

	private Label label2;

	private NumericUpDown _nmcMinFreeBagSlots;

	private GroupBox groupBox1;

	private CheckBox _cbxSellPurple;

	private CheckBox _cbxSellBlue;

	private CheckBox _cbxSellGreen;

	private CheckBox _cbxSellWhite;

	private CheckBox _cbxSellGrey;

	private GroupBox groupBox2;

	private CheckBox _cbxMailPurple;

	private CheckBox _cbxMailBlue;

	private CheckBox _cbxMailGreen;

	private CheckBox _cbxMailWhite;

	private CheckBox _cbxMailGrey;

	private Label label3;

	private NumericUpDown _nmcMinLevel;

	private NumericUpDown _nmcMaxLevel;

	private Label label4;

	private TabControl tabControl1;

	private TabPage tabPage1;

	private TabPage tabPage2;

	private ListView _lvwVendors;

	private Button _btnAddVendor;

	private Label _lblVendorLocation;

	private Label _lblVendorType;

	private Label _lblVendorID;

	private Button _btnDeleteSelectedVendor;

	private ContextMenuStrip _ctxVendorRightClick;

	private ToolStripMenuItem deleteToolStripMenuItem;

	private Label label5;

	private ProfileTreeNode CurrentSelected => (ProfileTreeNode)_tvwProfileTree.SelectedNode;

	public FormProfileCreator()
	{
		InitializeComponent();
	}

	private void FormProfileCreator_Load(object sender, EventArgs e)
	{
		ResetTree("Root");
	}

	private void ResetTree(string rootName)
	{
		_rootNode = new ProfileTreeNode(rootName)
		{
			ContextMenuStrip = _ctxProfileRightClick
		};
		_tvwProfileTree.Nodes.Clear();
		_tvwProfileTree.Nodes.Add(_rootNode);
	}

	private void AddSubProfile(TreeNode parent, string name)
	{
		ProfileTreeNode node = new ProfileTreeNode(name);
		node.ContextMenuStrip = _ctxProfileRightClick;
		node.MinLevel = CurrentSelected.MinLevel;
		node.MaxLevel = CurrentSelected.MaxLevel;
		node.MinDurability = CurrentSelected.MinDurability;
		node.MinFreeBagSlots = CurrentSelected.MinFreeBagSlots;
		parent.Nodes.Add(node);
		parent.Expand();
	}

	private void SetSellFlag(QualityFlags flag, bool set)
	{
		if (set)
		{
			CurrentSelected.SellFlags |= flag;
		}
		else
		{
			CurrentSelected.SellFlags &= (QualityFlags)(byte)(~(int)flag);
		}
	}

	private void SetMailFlag(QualityFlags flag, bool set)
	{
		if (set)
		{
			CurrentSelected.MailFlags |= flag;
		}
		else
		{
			CurrentSelected.MailFlags &= (QualityFlags)(byte)(~(int)flag);
		}
	}

	private void UpdateSellCheckboxes(QualityFlags flags)
	{
		_cbxSellGrey.Checked = (flags & QualityFlags.Grey) != 0;
		_cbxSellWhite.Checked = (flags & QualityFlags.White) != 0;
		_cbxSellGreen.Checked = (flags & QualityFlags.Green) != 0;
		_cbxSellBlue.Checked = (flags & QualityFlags.Blue) != 0;
		_cbxSellPurple.Checked = (flags & QualityFlags.Purple) != 0;
	}

	private void UpdateMailCheckboxes(QualityFlags flags)
	{
		_cbxMailGrey.Checked = (flags & QualityFlags.Grey) != 0;
		_cbxMailWhite.Checked = (flags & QualityFlags.White) != 0;
		_cbxMailGreen.Checked = (flags & QualityFlags.Green) != 0;
		_cbxMailBlue.Checked = (flags & QualityFlags.Blue) != 0;
		_cbxMailPurple.Checked = (flags & QualityFlags.Purple) != 0;
	}

	private void SortChildren()
	{
		if (_rootNode != null)
		{
			SortNodeChildren(_rootNode);
		}
	}

	private void SortNodeChildren(TreeNode parent)
	{
		List<ProfileTreeNode> list = new List<ProfileTreeNode>();
		for (int i = 0; i < parent.Nodes.Count; i++)
		{
			list.Add((ProfileTreeNode)parent.Nodes[i]);
		}
		list.Sort((a, b) => a.MaxLevel.CompareTo(b.MaxLevel));
		parent.Nodes.Clear();
		parent.Nodes.AddRange(list.ToArray());
		for (int num = 0; num < list.Count; num++)
		{
			SortNodeChildren(list[num]);
		}
	}

	private void _btnNewProfile_Click(object sender, EventArgs e)
	{
		string name = ProfileCreatorHelper.PromptProfileName("Root");
		ResetTree(name);
	}

	private void _btnRemoveSubProfile_Click(object sender, EventArgs e)
	{
		if (_tvwProfileTree.SelectedNode == _rootNode)
		{
			MessageBox.Show("You can not remove the root profile!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		else
		{
			CurrentSelected.Remove();
		}
	}

	private void _tbarMinDurability_Scroll(object sender, EventArgs e)
	{
		float minDurability = (float)_tbarMinDurability.Value / (float)_tbarMinDurability.Maximum;
		_lblMinDurability.Text = minDurability.ToString("0.00");
		CurrentSelected.MinDurability = minDurability;
	}

	private void _nmcMinFreeBagSlots_ValueChanged(object sender, EventArgs e)
	{
		CurrentSelected.MinFreeBagSlots = (int)_nmcMinFreeBagSlots.Value;
	}

	private void _cbxSellGrey_CheckedChanged(object sender, EventArgs e)
	{
		QualityFlags qualityFlags = QualityFlags.None;
		if (sender == _cbxSellGrey)
		{
			qualityFlags |= QualityFlags.Grey;
		}
		else if (sender == _cbxSellWhite)
		{
			qualityFlags |= QualityFlags.White;
		}
		else if (sender == _cbxSellGreen)
		{
			qualityFlags |= QualityFlags.Green;
		}
		else if (sender == _cbxSellBlue)
		{
			qualityFlags |= QualityFlags.Blue;
		}
		else
		{
			if (sender != _cbxSellPurple)
			{
				return;
			}
			qualityFlags |= QualityFlags.Purple;
		}
		SetSellFlag(qualityFlags, ((CheckBox)sender).Checked);
	}

	private void _cbxMailGrey_CheckedChanged(object sender, EventArgs e)
	{
		QualityFlags qualityFlags = QualityFlags.None;
		if (sender == _cbxMailGrey)
		{
			qualityFlags |= QualityFlags.Grey;
		}
		else if (sender == _cbxMailWhite)
		{
			qualityFlags |= QualityFlags.White;
		}
		else if (sender == _cbxMailGreen)
		{
			qualityFlags |= QualityFlags.Green;
		}
		else if (sender == _cbxMailBlue)
		{
			qualityFlags |= QualityFlags.Blue;
		}
		else
		{
			if (sender != _cbxMailPurple)
			{
				return;
			}
			qualityFlags |= QualityFlags.Purple;
		}
		SetMailFlag(qualityFlags, ((CheckBox)sender).Checked);
	}

	private void _nmcMinLevel_ValueChanged(object sender, EventArgs e)
	{
		CurrentSelected.MinLevel = (int)_nmcMinLevel.Value;
		foreach (ProfileTreeNode node in CurrentSelected.Nodes)
		{
			node.MinLevel = ProfileCreatorHelper.Clamp(node.MinLevel, CurrentSelected.MinLevel, CurrentSelected.MaxLevel);
			node.RefreshText();
		}
	}

	private void _nmcMaxLevel_ValueChanged(object sender, EventArgs e)
	{
		CurrentSelected.MaxLevel = (int)_nmcMaxLevel.Value;
		foreach (ProfileTreeNode node in CurrentSelected.Nodes)
		{
			node.MaxLevel = ProfileCreatorHelper.Clamp(node.MaxLevel, CurrentSelected.MinLevel, CurrentSelected.MaxLevel);
			node.RefreshText();
		}
		SortChildren();
	}

	private void _tvwProfileTree_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
	{
		if (e.Label != null)
		{
			((ProfileTreeNode)e.Node).ProfileName = e.Label;
			((ProfileTreeNode)e.Node).RefreshText();
			e.CancelEdit = true;
		}
	}

	private void _btnAddVendor_Click(object sender, EventArgs e)
	{
		List<ProfileVendorListViewItem> list = new List<ProfileVendorListViewItem>();
		for (int i = 0; i < _lvwVendors.Items.Count; i++)
		{
			list.Add((ProfileVendorListViewItem)_lvwVendors.Items[i]);
		}
		FormFindVendors formFindVendors = new FormFindVendors(list);
		if (formFindVendors.ShowDialog() == DialogResult.OK)
		{
			ProfileVendorListViewItem chosenVendor = formFindVendors.ChosenVendor;
			if (chosenVendor != null)
			{
				ProfileVendorListViewItem value = new ProfileVendorListViewItem(chosenVendor.VendorName, chosenVendor.Entry, chosenVendor.Type, chosenVendor.Location);
				_lvwVendors.Items.Add(value);
			}
		}
	}

	private void _lvwVendors_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (_lvwVendors.SelectedItems.Count <= 0)
		{
			_lblVendorID.Text = "ID: n/a";
			_lblVendorLocation.Text = "Location: n/a";
			_lblVendorType.Text = "Type: n/a";
		}
		else
		{
			ProfileVendorListViewItem profileVendorListViewItem = (ProfileVendorListViewItem)_lvwVendors.SelectedItems[0];
			_lblVendorID.Text = "ID: " + profileVendorListViewItem.Entry;
			_lblVendorLocation.Text = "Location: " + profileVendorListViewItem.Location;
			_lblVendorType.Text = "Type: " + profileVendorListViewItem.Type;
		}
	}

	private void _tvwProfileTree_MouseUp(object sender, MouseEventArgs e)
	{
		MouseButtons button = e.Button;
		if (button == MouseButtons.Right)
		{
			TreeNode nodeAt = _tvwProfileTree.GetNodeAt(e.Location);
			_rightClickedNode = ((nodeAt != null) ? ((ProfileTreeNode)nodeAt) : null);
		}
	}

	private void _MenuBtnAddSubProfile_Click(object sender, EventArgs e)
	{
		string text = ProfileCreatorHelper.PromptProfileName();
		if (text != null)
		{
			AddSubProfile(CurrentSelected, text);
		}
	}

	private void _ctxBtnAddSubProfile_Click(object sender, EventArgs e)
	{
		string text = ProfileCreatorHelper.PromptProfileName();
		if (text != null)
		{
			AddSubProfile(_rightClickedNode, text);
		}
	}

	private void _lvwVendors_MouseDown(object sender, MouseEventArgs e)
	{
		MouseButtons button = e.Button;
		if (button == MouseButtons.Right)
		{
			ListViewItem itemAt = _lvwVendors.GetItemAt(e.X, e.Y);
			_rightClickedVendor = ((itemAt != null) ? ((ProfileVendorListViewItem)itemAt) : null);
		}
	}

	private void _btnDeleteSelectedVendor_Click(object sender, EventArgs e)
	{
		if (_lvwVendors.SelectedItems.Count > 0)
		{
			_lvwVendors.SelectedItems[0].Remove();
		}
	}

	private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (!(_rightClickedVendor == null))
		{
			_rightClickedVendor.Remove();
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
		this.components = new System.ComponentModel.Container();
		this._tvwProfileTree = new System.Windows.Forms.TreeView();
		this._ctxProfileRightClick = new System.Windows.Forms.ContextMenuStrip(this.components);
		this._ctxBtnAddSubProfile = new System.Windows.Forms.ToolStripMenuItem();
		this._ctxBtnRemove = new System.Windows.Forms.ToolStripMenuItem();
		this.menuStrip1 = new System.Windows.Forms.MenuStrip();
		this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this._btnNewProfile = new System.Windows.Forms.ToolStripMenuItem();
		this._btnOpenProfile = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
		this._btnSaveProfile = new System.Windows.Forms.ToolStripMenuItem();
		this._btnSaveProfileAs = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
		this._btnExitGUI = new System.Windows.Forms.ToolStripMenuItem();
		this.profileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this._MenuBtnAddSubProfile = new System.Windows.Forms.ToolStripMenuItem();
		this._btnRemoveSubProfile = new System.Windows.Forms.ToolStripMenuItem();
		this.label1 = new System.Windows.Forms.Label();
		this._tbarMinDurability = new System.Windows.Forms.TrackBar();
		this._lblMinDurability = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this._nmcMinFreeBagSlots = new System.Windows.Forms.NumericUpDown();
		this.groupBox1 = new System.Windows.Forms.GroupBox();
		this._cbxSellPurple = new System.Windows.Forms.CheckBox();
		this._cbxSellBlue = new System.Windows.Forms.CheckBox();
		this._cbxSellGreen = new System.Windows.Forms.CheckBox();
		this._cbxSellWhite = new System.Windows.Forms.CheckBox();
		this._cbxSellGrey = new System.Windows.Forms.CheckBox();
		this.groupBox2 = new System.Windows.Forms.GroupBox();
		this._cbxMailPurple = new System.Windows.Forms.CheckBox();
		this._cbxMailBlue = new System.Windows.Forms.CheckBox();
		this._cbxMailGreen = new System.Windows.Forms.CheckBox();
		this._cbxMailWhite = new System.Windows.Forms.CheckBox();
		this._cbxMailGrey = new System.Windows.Forms.CheckBox();
		this.label3 = new System.Windows.Forms.Label();
		this._nmcMinLevel = new System.Windows.Forms.NumericUpDown();
		this._nmcMaxLevel = new System.Windows.Forms.NumericUpDown();
		this.label4 = new System.Windows.Forms.Label();
		this.tabControl1 = new System.Windows.Forms.TabControl();
		this.tabPage1 = new System.Windows.Forms.TabPage();
		this.tabPage2 = new System.Windows.Forms.TabPage();
		this._btnDeleteSelectedVendor = new System.Windows.Forms.Button();
		this._lblVendorLocation = new System.Windows.Forms.Label();
		this._lblVendorType = new System.Windows.Forms.Label();
		this._lblVendorID = new System.Windows.Forms.Label();
		this._btnAddVendor = new System.Windows.Forms.Button();
		this._lvwVendors = new System.Windows.Forms.ListView();
		this._ctxVendorRightClick = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.label5 = new System.Windows.Forms.Label();
		this._ctxProfileRightClick.SuspendLayout();
		this.menuStrip1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this._tbarMinDurability).BeginInit();
		((System.ComponentModel.ISupportInitialize)this._nmcMinFreeBagSlots).BeginInit();
		this.groupBox1.SuspendLayout();
		this.groupBox2.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this._nmcMinLevel).BeginInit();
		((System.ComponentModel.ISupportInitialize)this._nmcMaxLevel).BeginInit();
		this.tabControl1.SuspendLayout();
		this.tabPage1.SuspendLayout();
		this.tabPage2.SuspendLayout();
		this._ctxVendorRightClick.SuspendLayout();
		base.SuspendLayout();
		this._tvwProfileTree.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this._tvwProfileTree.ContextMenuStrip = this._ctxProfileRightClick;
		this._tvwProfileTree.FullRowSelect = true;
		this._tvwProfileTree.HideSelection = false;
		this._tvwProfileTree.HotTracking = true;
		this._tvwProfileTree.LabelEdit = true;
		this._tvwProfileTree.Location = new System.Drawing.Point(279, 49);
		this._tvwProfileTree.Name = "_tvwProfileTree";
		this._tvwProfileTree.ShowRootLines = false;
		this._tvwProfileTree.Size = new System.Drawing.Size(138, 272);
		this._tvwProfileTree.TabIndex = 0;
		this._tvwProfileTree.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(_tvwProfileTree_AfterLabelEdit);
		this._tvwProfileTree.MouseUp += new System.Windows.Forms.MouseEventHandler(_tvwProfileTree_MouseUp);
		this._ctxProfileRightClick.Items.AddRange(new System.Windows.Forms.ToolStripItem[2] { this._ctxBtnAddSubProfile, this._ctxBtnRemove });
		this._ctxProfileRightClick.Name = "_ctxProfileRightClick";
		this._ctxProfileRightClick.Size = new System.Drawing.Size(157, 48);
		this._ctxBtnAddSubProfile.Name = "_ctxBtnAddSubProfile";
		this._ctxBtnAddSubProfile.Size = new System.Drawing.Size(156, 22);
		this._ctxBtnAddSubProfile.Text = "Add Sub Profile";
		this._ctxBtnAddSubProfile.Click += new System.EventHandler(_ctxBtnAddSubProfile_Click);
		this._ctxBtnRemove.Name = "_ctxBtnRemove";
		this._ctxBtnRemove.Size = new System.Drawing.Size(156, 22);
		this._ctxBtnRemove.Text = "Delete";
		this._ctxBtnRemove.Click += new System.EventHandler(_btnRemoveSubProfile_Click);
		this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.fileToolStripMenuItem, this.profileToolStripMenuItem });
		this.menuStrip1.Location = new System.Drawing.Point(0, 0);
		this.menuStrip1.Name = "menuStrip1";
		this.menuStrip1.Size = new System.Drawing.Size(428, 24);
		this.menuStrip1.TabIndex = 1;
		this.menuStrip1.Text = "menuStrip1";
		this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[7] { this._btnNewProfile, this._btnOpenProfile, this.toolStripSeparator1, this._btnSaveProfile, this._btnSaveProfileAs, this.toolStripSeparator2, this._btnExitGUI });
		this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
		this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
		this.fileToolStripMenuItem.Text = "File";
		this._btnNewProfile.Name = "_btnNewProfile";
		this._btnNewProfile.Size = new System.Drawing.Size(114, 22);
		this._btnNewProfile.Text = "New";
		this._btnNewProfile.Click += new System.EventHandler(_btnNewProfile_Click);
		this._btnOpenProfile.Name = "_btnOpenProfile";
		this._btnOpenProfile.Size = new System.Drawing.Size(114, 22);
		this._btnOpenProfile.Text = "Open";
		this.toolStripSeparator1.Name = "toolStripSeparator1";
		this.toolStripSeparator1.Size = new System.Drawing.Size(111, 6);
		this._btnSaveProfile.Name = "_btnSaveProfile";
		this._btnSaveProfile.Size = new System.Drawing.Size(114, 22);
		this._btnSaveProfile.Text = "Save";
		this._btnSaveProfileAs.Name = "_btnSaveProfileAs";
		this._btnSaveProfileAs.Size = new System.Drawing.Size(114, 22);
		this._btnSaveProfileAs.Text = "Save As";
		this.toolStripSeparator2.Name = "toolStripSeparator2";
		this.toolStripSeparator2.Size = new System.Drawing.Size(111, 6);
		this._btnExitGUI.Name = "_btnExitGUI";
		this._btnExitGUI.Size = new System.Drawing.Size(114, 22);
		this._btnExitGUI.Text = "Exit";
		this.profileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[2] { this._MenuBtnAddSubProfile, this._btnRemoveSubProfile });
		this.profileToolStripMenuItem.Name = "profileToolStripMenuItem";
		this.profileToolStripMenuItem.Size = new System.Drawing.Size(53, 20);
		this.profileToolStripMenuItem.Text = "Profile";
		this._MenuBtnAddSubProfile.Name = "_MenuBtnAddSubProfile";
		this._MenuBtnAddSubProfile.Size = new System.Drawing.Size(156, 22);
		this._MenuBtnAddSubProfile.Text = "Add Sub Profile";
		this._MenuBtnAddSubProfile.Click += new System.EventHandler(_MenuBtnAddSubProfile_Click);
		this._btnRemoveSubProfile.Name = "_btnRemoveSubProfile";
		this._btnRemoveSubProfile.Size = new System.Drawing.Size(156, 22);
		this._btnRemoveSubProfile.Text = "Delete Selected";
		this._btnRemoveSubProfile.Click += new System.EventHandler(_btnRemoveSubProfile_Click);
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(6, 37);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(74, 13);
		this.label1.TabIndex = 2;
		this.label1.Text = "Min. durability:";
		this._tbarMinDurability.BackColor = System.Drawing.Color.White;
		this._tbarMinDurability.Location = new System.Drawing.Point(77, 35);
		this._tbarMinDurability.Maximum = 100;
		this._tbarMinDurability.Name = "_tbarMinDurability";
		this._tbarMinDurability.Size = new System.Drawing.Size(79, 45);
		this._tbarMinDurability.TabIndex = 3;
		this._tbarMinDurability.TickFrequency = 5;
		this._tbarMinDurability.TickStyle = System.Windows.Forms.TickStyle.None;
		this._tbarMinDurability.Value = 40;
		this._tbarMinDurability.Scroll += new System.EventHandler(_tbarMinDurability_Scroll);
		this._lblMinDurability.AutoSize = true;
		this._lblMinDurability.Location = new System.Drawing.Point(153, 37);
		this._lblMinDurability.Name = "_lblMinDurability";
		this._lblMinDurability.Size = new System.Drawing.Size(22, 13);
		this._lblMinDurability.TabIndex = 4;
		this._lblMinDurability.Text = "0.4";
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(6, 67);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(96, 13);
		this.label2.TabIndex = 5;
		this.label2.Text = "Min. free bag slots:";
		this._nmcMinFreeBagSlots.Location = new System.Drawing.Point(108, 65);
		this._nmcMinFreeBagSlots.Maximum = new decimal(new int[4] { 50, 0, 0, 0 });
		this._nmcMinFreeBagSlots.Name = "_nmcMinFreeBagSlots";
		this._nmcMinFreeBagSlots.Size = new System.Drawing.Size(60, 20);
		this._nmcMinFreeBagSlots.TabIndex = 6;
		this._nmcMinFreeBagSlots.Value = new decimal(new int[4] { 2, 0, 0, 0 });
		this._nmcMinFreeBagSlots.ValueChanged += new System.EventHandler(_nmcMinFreeBagSlots_ValueChanged);
		this.groupBox1.Controls.Add(this._cbxSellPurple);
		this.groupBox1.Controls.Add(this._cbxSellBlue);
		this.groupBox1.Controls.Add(this._cbxSellGreen);
		this.groupBox1.Controls.Add(this._cbxSellWhite);
		this.groupBox1.Controls.Add(this._cbxSellGrey);
		this.groupBox1.Location = new System.Drawing.Point(9, 94);
		this.groupBox1.Name = "groupBox1";
		this.groupBox1.Size = new System.Drawing.Size(117, 94);
		this.groupBox1.TabIndex = 7;
		this.groupBox1.TabStop = false;
		this.groupBox1.Text = "Sell";
		this._cbxSellPurple.AutoSize = true;
		this._cbxSellPurple.Location = new System.Drawing.Point(31, 66);
		this._cbxSellPurple.Name = "_cbxSellPurple";
		this._cbxSellPurple.Size = new System.Drawing.Size(56, 17);
		this._cbxSellPurple.TabIndex = 4;
		this._cbxSellPurple.Text = "Purple";
		this._cbxSellPurple.UseVisualStyleBackColor = true;
		this._cbxSellPurple.CheckedChanged += new System.EventHandler(_cbxSellGrey_CheckedChanged);
		this._cbxSellBlue.AutoSize = true;
		this._cbxSellBlue.Location = new System.Drawing.Point(61, 43);
		this._cbxSellBlue.Name = "_cbxSellBlue";
		this._cbxSellBlue.Size = new System.Drawing.Size(47, 17);
		this._cbxSellBlue.TabIndex = 3;
		this._cbxSellBlue.Text = "Blue";
		this._cbxSellBlue.UseVisualStyleBackColor = true;
		this._cbxSellBlue.CheckedChanged += new System.EventHandler(_cbxSellGrey_CheckedChanged);
		this._cbxSellGreen.AutoSize = true;
		this._cbxSellGreen.Location = new System.Drawing.Point(7, 43);
		this._cbxSellGreen.Name = "_cbxSellGreen";
		this._cbxSellGreen.Size = new System.Drawing.Size(55, 17);
		this._cbxSellGreen.TabIndex = 2;
		this._cbxSellGreen.Text = "Green";
		this._cbxSellGreen.UseVisualStyleBackColor = true;
		this._cbxSellGreen.CheckedChanged += new System.EventHandler(_cbxSellGrey_CheckedChanged);
		this._cbxSellWhite.AutoSize = true;
		this._cbxSellWhite.Checked = true;
		this._cbxSellWhite.CheckState = System.Windows.Forms.CheckState.Checked;
		this._cbxSellWhite.Location = new System.Drawing.Point(61, 20);
		this._cbxSellWhite.Name = "_cbxSellWhite";
		this._cbxSellWhite.Size = new System.Drawing.Size(54, 17);
		this._cbxSellWhite.TabIndex = 1;
		this._cbxSellWhite.Text = "White";
		this._cbxSellWhite.UseVisualStyleBackColor = true;
		this._cbxSellWhite.CheckedChanged += new System.EventHandler(_cbxSellGrey_CheckedChanged);
		this._cbxSellGrey.AutoSize = true;
		this._cbxSellGrey.Checked = true;
		this._cbxSellGrey.CheckState = System.Windows.Forms.CheckState.Checked;
		this._cbxSellGrey.Location = new System.Drawing.Point(7, 20);
		this._cbxSellGrey.Name = "_cbxSellGrey";
		this._cbxSellGrey.Size = new System.Drawing.Size(48, 17);
		this._cbxSellGrey.TabIndex = 0;
		this._cbxSellGrey.Text = "Grey";
		this._cbxSellGrey.UseVisualStyleBackColor = true;
		this._cbxSellGrey.CheckedChanged += new System.EventHandler(_cbxSellGrey_CheckedChanged);
		this.groupBox2.Controls.Add(this._cbxMailPurple);
		this.groupBox2.Controls.Add(this._cbxMailBlue);
		this.groupBox2.Controls.Add(this._cbxMailGreen);
		this.groupBox2.Controls.Add(this._cbxMailWhite);
		this.groupBox2.Controls.Add(this._cbxMailGrey);
		this.groupBox2.Location = new System.Drawing.Point(130, 94);
		this.groupBox2.Name = "groupBox2";
		this.groupBox2.Size = new System.Drawing.Size(117, 94);
		this.groupBox2.TabIndex = 8;
		this.groupBox2.TabStop = false;
		this.groupBox2.Text = "Mail";
		this._cbxMailPurple.AutoSize = true;
		this._cbxMailPurple.Checked = true;
		this._cbxMailPurple.CheckState = System.Windows.Forms.CheckState.Checked;
		this._cbxMailPurple.Location = new System.Drawing.Point(31, 66);
		this._cbxMailPurple.Name = "_cbxMailPurple";
		this._cbxMailPurple.Size = new System.Drawing.Size(56, 17);
		this._cbxMailPurple.TabIndex = 4;
		this._cbxMailPurple.Text = "Purple";
		this._cbxMailPurple.UseVisualStyleBackColor = true;
		this._cbxMailPurple.CheckedChanged += new System.EventHandler(_cbxMailGrey_CheckedChanged);
		this._cbxMailBlue.AutoSize = true;
		this._cbxMailBlue.Checked = true;
		this._cbxMailBlue.CheckState = System.Windows.Forms.CheckState.Checked;
		this._cbxMailBlue.Location = new System.Drawing.Point(61, 43);
		this._cbxMailBlue.Name = "_cbxMailBlue";
		this._cbxMailBlue.Size = new System.Drawing.Size(47, 17);
		this._cbxMailBlue.TabIndex = 3;
		this._cbxMailBlue.Text = "Blue";
		this._cbxMailBlue.UseVisualStyleBackColor = true;
		this._cbxMailBlue.CheckedChanged += new System.EventHandler(_cbxMailGrey_CheckedChanged);
		this._cbxMailGreen.AutoSize = true;
		this._cbxMailGreen.Checked = true;
		this._cbxMailGreen.CheckState = System.Windows.Forms.CheckState.Checked;
		this._cbxMailGreen.Location = new System.Drawing.Point(7, 43);
		this._cbxMailGreen.Name = "_cbxMailGreen";
		this._cbxMailGreen.Size = new System.Drawing.Size(55, 17);
		this._cbxMailGreen.TabIndex = 2;
		this._cbxMailGreen.Text = "Green";
		this._cbxMailGreen.UseVisualStyleBackColor = true;
		this._cbxMailGreen.CheckedChanged += new System.EventHandler(_cbxMailGrey_CheckedChanged);
		this._cbxMailWhite.AutoSize = true;
		this._cbxMailWhite.Location = new System.Drawing.Point(61, 20);
		this._cbxMailWhite.Name = "_cbxMailWhite";
		this._cbxMailWhite.Size = new System.Drawing.Size(54, 17);
		this._cbxMailWhite.TabIndex = 1;
		this._cbxMailWhite.Text = "White";
		this._cbxMailWhite.UseVisualStyleBackColor = true;
		this._cbxMailWhite.CheckedChanged += new System.EventHandler(_cbxMailGrey_CheckedChanged);
		this._cbxMailGrey.AutoSize = true;
		this._cbxMailGrey.Location = new System.Drawing.Point(7, 20);
		this._cbxMailGrey.Name = "_cbxMailGrey";
		this._cbxMailGrey.Size = new System.Drawing.Size(48, 17);
		this._cbxMailGrey.TabIndex = 0;
		this._cbxMailGrey.Text = "Grey";
		this._cbxMailGrey.UseVisualStyleBackColor = true;
		this._cbxMailGrey.CheckedChanged += new System.EventHandler(_cbxMailGrey_CheckedChanged);
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(6, 8);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(55, 13);
		this.label3.TabIndex = 9;
		this.label3.Text = "Min. level:";
		this._nmcMinLevel.Location = new System.Drawing.Point(67, 6);
		this._nmcMinLevel.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
		this._nmcMinLevel.Name = "_nmcMinLevel";
		this._nmcMinLevel.Size = new System.Drawing.Size(34, 20);
		this._nmcMinLevel.TabIndex = 10;
		this._nmcMinLevel.Value = new decimal(new int[4] { 1, 0, 0, 0 });
		this._nmcMinLevel.ValueChanged += new System.EventHandler(_nmcMinLevel_ValueChanged);
		this._nmcMaxLevel.Location = new System.Drawing.Point(168, 6);
		this._nmcMaxLevel.Maximum = new decimal(new int[4] { 81, 0, 0, 0 });
		this._nmcMaxLevel.Minimum = new decimal(new int[4] { 2, 0, 0, 0 });
		this._nmcMaxLevel.Name = "_nmcMaxLevel";
		this._nmcMaxLevel.Size = new System.Drawing.Size(34, 20);
		this._nmcMaxLevel.TabIndex = 12;
		this._nmcMaxLevel.Value = new decimal(new int[4] { 80, 0, 0, 0 });
		this._nmcMaxLevel.ValueChanged += new System.EventHandler(_nmcMaxLevel_ValueChanged);
		this.label4.AutoSize = true;
		this.label4.Location = new System.Drawing.Point(107, 8);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(55, 13);
		this.label4.TabIndex = 11;
		this.label4.Text = "Max level:";
		this.tabControl1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.tabControl1.Controls.Add(this.tabPage1);
		this.tabControl1.Controls.Add(this.tabPage2);
		this.tabControl1.Location = new System.Drawing.Point(12, 27);
		this.tabControl1.Name = "tabControl1";
		this.tabControl1.SelectedIndex = 0;
		this.tabControl1.Size = new System.Drawing.Size(261, 294);
		this.tabControl1.TabIndex = 13;
		this.tabPage1.Controls.Add(this.groupBox2);
		this.tabPage1.Controls.Add(this.groupBox1);
		this.tabPage1.Controls.Add(this._lblMinDurability);
		this.tabPage1.Controls.Add(this.label2);
		this.tabPage1.Controls.Add(this._nmcMinFreeBagSlots);
		this.tabPage1.Controls.Add(this.label3);
		this.tabPage1.Controls.Add(this.label1);
		this.tabPage1.Controls.Add(this._tbarMinDurability);
		this.tabPage1.Controls.Add(this._nmcMaxLevel);
		this.tabPage1.Controls.Add(this.label4);
		this.tabPage1.Controls.Add(this._nmcMinLevel);
		this.tabPage1.Location = new System.Drawing.Point(4, 22);
		this.tabPage1.Name = "tabPage1";
		this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
		this.tabPage1.Size = new System.Drawing.Size(253, 268);
		this.tabPage1.TabIndex = 0;
		this.tabPage1.Text = "General";
		this.tabPage1.UseVisualStyleBackColor = true;
		this.tabPage2.Controls.Add(this._btnDeleteSelectedVendor);
		this.tabPage2.Controls.Add(this._lblVendorLocation);
		this.tabPage2.Controls.Add(this._lblVendorType);
		this.tabPage2.Controls.Add(this._lblVendorID);
		this.tabPage2.Controls.Add(this._btnAddVendor);
		this.tabPage2.Controls.Add(this._lvwVendors);
		this.tabPage2.Location = new System.Drawing.Point(4, 22);
		this.tabPage2.Name = "tabPage2";
		this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
		this.tabPage2.Size = new System.Drawing.Size(338, 268);
		this.tabPage2.TabIndex = 1;
		this.tabPage2.Text = "Vendors";
		this.tabPage2.UseVisualStyleBackColor = true;
		this._btnDeleteSelectedVendor.Location = new System.Drawing.Point(87, 229);
		this._btnDeleteSelectedVendor.Name = "_btnDeleteSelectedVendor";
		this._btnDeleteSelectedVendor.Size = new System.Drawing.Size(75, 23);
		this._btnDeleteSelectedVendor.TabIndex = 10;
		this._btnDeleteSelectedVendor.Text = "Delete";
		this._btnDeleteSelectedVendor.UseVisualStyleBackColor = true;
		this._btnDeleteSelectedVendor.Click += new System.EventHandler(_btnDeleteSelectedVendor_Click);
		this._lblVendorLocation.Location = new System.Drawing.Point(168, 50);
		this._lblVendorLocation.Name = "_lblVendorLocation";
		this._lblVendorLocation.Size = new System.Drawing.Size(146, 114);
		this._lblVendorLocation.TabIndex = 9;
		this._lblVendorLocation.Text = "Location:";
		this._lblVendorType.AutoSize = true;
		this._lblVendorType.Location = new System.Drawing.Point(168, 28);
		this._lblVendorType.Name = "_lblVendorType";
		this._lblVendorType.Size = new System.Drawing.Size(34, 13);
		this._lblVendorType.TabIndex = 8;
		this._lblVendorType.Text = "Type:";
		this._lblVendorID.AutoSize = true;
		this._lblVendorID.Location = new System.Drawing.Point(168, 6);
		this._lblVendorID.Name = "_lblVendorID";
		this._lblVendorID.Size = new System.Drawing.Size(21, 13);
		this._lblVendorID.TabIndex = 7;
		this._lblVendorID.Text = "ID:";
		this._btnAddVendor.Location = new System.Drawing.Point(6, 229);
		this._btnAddVendor.Name = "_btnAddVendor";
		this._btnAddVendor.Size = new System.Drawing.Size(75, 23);
		this._btnAddVendor.TabIndex = 1;
		this._btnAddVendor.Text = "Add New..";
		this._btnAddVendor.UseVisualStyleBackColor = true;
		this._btnAddVendor.Click += new System.EventHandler(_btnAddVendor_Click);
		this._lvwVendors.GridLines = true;
		this._lvwVendors.Location = new System.Drawing.Point(6, 6);
		this._lvwVendors.MultiSelect = false;
		this._lvwVendors.Name = "_lvwVendors";
		this._lvwVendors.Size = new System.Drawing.Size(156, 217);
		this._lvwVendors.TabIndex = 0;
		this._lvwVendors.UseCompatibleStateImageBehavior = false;
		this._lvwVendors.View = System.Windows.Forms.View.List;
		this._lvwVendors.SelectedIndexChanged += new System.EventHandler(_lvwVendors_SelectedIndexChanged);
		this._lvwVendors.MouseDown += new System.Windows.Forms.MouseEventHandler(_lvwVendors_MouseDown);
		this._ctxVendorRightClick.Items.AddRange(new System.Windows.Forms.ToolStripItem[1] { this.deleteToolStripMenuItem });
		this._ctxVendorRightClick.Name = "_ctxVendorRightClick";
		this._ctxVendorRightClick.Size = new System.Drawing.Size(108, 26);
		this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
		this.deleteToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
		this.deleteToolStripMenuItem.Text = "Delete";
		this.deleteToolStripMenuItem.Click += new System.EventHandler(deleteToolStripMenuItem_Click);
		this.label5.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.label5.AutoSize = true;
		this.label5.Location = new System.Drawing.Point(279, 33);
		this.label5.Name = "label5";
		this.label5.Size = new System.Drawing.Size(60, 13);
		this.label5.TabIndex = 14;
		this.label5.Text = "Profile tree:";
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(428, 333);
		base.Controls.Add(this.tabControl1);
		base.Controls.Add(this._tvwProfileTree);
		base.Controls.Add(this.label5);
		base.Controls.Add(this.menuStrip1);
		base.MainMenuStrip = this.menuStrip1;
		base.Name = "FormProfileCreator";
		this.Text = "Profile Creator";
		base.Load += new System.EventHandler(FormProfileCreator_Load);
		this._ctxProfileRightClick.ResumeLayout(false);
		this.menuStrip1.ResumeLayout(false);
		this.menuStrip1.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this._tbarMinDurability).EndInit();
		((System.ComponentModel.ISupportInitialize)this._nmcMinFreeBagSlots).EndInit();
		this.groupBox1.ResumeLayout(false);
		this.groupBox1.PerformLayout();
		this.groupBox2.ResumeLayout(false);
		this.groupBox2.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this._nmcMinLevel).EndInit();
		((System.ComponentModel.ISupportInitialize)this._nmcMaxLevel).EndInit();
		this.tabControl1.ResumeLayout(false);
		this.tabPage1.ResumeLayout(false);
		this.tabPage1.PerformLayout();
		this.tabPage2.ResumeLayout(false);
		this.tabPage2.PerformLayout();
		this._ctxVendorRightClick.ResumeLayout(false);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
