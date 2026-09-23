using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Styx.CommonBot.Profiles;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Levelbot.ProfileCreation.Forms;

public class FormFindVendors : Form
{
	private List<ProfileVendorListViewItem> _excludeFromSearch;

	private IContainer components;

	private Button _btnOk;

	private Button _btnCancel;

	private ListView _lvwVendors;

	private Button _btnUpdateVendors;

	private Label _lblVendorID;

	private Label _lblVendorType;

	private Label _lblVendorLocation;

	public ProfileVendorListViewItem ChosenVendor { get; private set; }

	public FormFindVendors(List<ProfileVendorListViewItem> excludeFromSearch)
	{
		InitializeComponent();
		_excludeFromSearch = excludeFromSearch;
	}

	private void _btnUpdateVendors_Click(object sender, EventArgs e)
	{
		UpdateVendors();
	}

	private void FormFindVendors_Load(object sender, EventArgs e)
	{
		UpdateVendors();
	}

	private void UpdateVendors()
	{
		ObjectManager.Update();
		List<WoWUnit> objectsOfType = ObjectManager.GetObjectsOfType<WoWUnit>();
		_lvwVendors.Items.Clear();
		for (int i = 0; i < objectsOfType.Count; i++)
		{
			WoWUnit woWUnit = objectsOfType[i];
			if (woWUnit.IsFriendly && woWUnit.IsRepairMerchant)
			{
				ProfileVendorListViewItem profileVendorListViewItem = new ProfileVendorListViewItem(woWUnit.Name, woWUnit.Entry, Vendor.VendorType.Repair, woWUnit.Location);
				if (_excludeFromSearch == null || !_excludeFromSearch.Contains(profileVendorListViewItem))
				{
					_lvwVendors.Items.Add(profileVendorListViewItem);
				}
			}
		}
	}

	private void _btnCancel_Click(object sender, EventArgs e)
	{
		base.DialogResult = DialogResult.Cancel;
		ChosenVendor = null;
		Close();
	}

	private void _btnOk_Click(object sender, EventArgs e)
	{
		if (_lvwVendors.SelectedItems.Count <= 0)
		{
			MessageBox.Show("You have not selected any vendors!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		ChosenVendor = (ProfileVendorListViewItem)_lvwVendors.SelectedItems[0];
		base.DialogResult = DialogResult.OK;
		Close();
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
		this._btnOk = new System.Windows.Forms.Button();
		this._btnCancel = new System.Windows.Forms.Button();
		this._lvwVendors = new System.Windows.Forms.ListView();
		this._btnUpdateVendors = new System.Windows.Forms.Button();
		this._lblVendorID = new System.Windows.Forms.Label();
		this._lblVendorType = new System.Windows.Forms.Label();
		this._lblVendorLocation = new System.Windows.Forms.Label();
		base.SuspendLayout();
		this._btnOk.Location = new System.Drawing.Point(12, 117);
		this._btnOk.Name = "_btnOk";
		this._btnOk.Size = new System.Drawing.Size(75, 23);
		this._btnOk.TabIndex = 0;
		this._btnOk.Text = "OK";
		this._btnOk.UseVisualStyleBackColor = true;
		this._btnOk.Click += new System.EventHandler(_btnOk_Click);
		this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this._btnCancel.Location = new System.Drawing.Point(93, 117);
		this._btnCancel.Name = "_btnCancel";
		this._btnCancel.Size = new System.Drawing.Size(75, 23);
		this._btnCancel.TabIndex = 1;
		this._btnCancel.Text = "Cancel";
		this._btnCancel.UseVisualStyleBackColor = true;
		this._btnCancel.Click += new System.EventHandler(_btnCancel_Click);
		this._lvwVendors.Location = new System.Drawing.Point(12, 12);
		this._lvwVendors.MultiSelect = false;
		this._lvwVendors.Name = "_lvwVendors";
		this._lvwVendors.Size = new System.Drawing.Size(156, 99);
		this._lvwVendors.TabIndex = 2;
		this._lvwVendors.UseCompatibleStateImageBehavior = false;
		this._lvwVendors.View = System.Windows.Forms.View.List;
		this._lvwVendors.SelectedIndexChanged += new System.EventHandler(_lvwVendors_SelectedIndexChanged);
		this._btnUpdateVendors.Location = new System.Drawing.Point(174, 12);
		this._btnUpdateVendors.Name = "_btnUpdateVendors";
		this._btnUpdateVendors.Size = new System.Drawing.Size(75, 23);
		this._btnUpdateVendors.TabIndex = 3;
		this._btnUpdateVendors.Text = "Update List";
		this._btnUpdateVendors.UseVisualStyleBackColor = true;
		this._btnUpdateVendors.Click += new System.EventHandler(_btnUpdateVendors_Click);
		this._lblVendorID.AutoSize = true;
		this._lblVendorID.Location = new System.Drawing.Point(174, 38);
		this._lblVendorID.Name = "_lblVendorID";
		this._lblVendorID.Size = new System.Drawing.Size(21, 13);
		this._lblVendorID.TabIndex = 4;
		this._lblVendorID.Text = "ID:";
		this._lblVendorType.AutoSize = true;
		this._lblVendorType.Location = new System.Drawing.Point(174, 58);
		this._lblVendorType.Name = "_lblVendorType";
		this._lblVendorType.Size = new System.Drawing.Size(34, 13);
		this._lblVendorType.TabIndex = 5;
		this._lblVendorType.Text = "Type:";
		this._lblVendorLocation.Location = new System.Drawing.Point(174, 78);
		this._lblVendorLocation.Name = "_lblVendorLocation";
		this._lblVendorLocation.Size = new System.Drawing.Size(119, 62);
		this._lblVendorLocation.TabIndex = 6;
		this._lblVendorLocation.Text = "Location:";
		base.AcceptButton = this._btnOk;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.CancelButton = this._btnCancel;
		base.ClientSize = new System.Drawing.Size(305, 151);
		base.Controls.Add(this._lblVendorLocation);
		base.Controls.Add(this._lblVendorType);
		base.Controls.Add(this._lblVendorID);
		base.Controls.Add(this._btnUpdateVendors);
		base.Controls.Add(this._lvwVendors);
		base.Controls.Add(this._btnCancel);
		base.Controls.Add(this._btnOk);
		base.Name = "FormFindVendors";
		this.Text = "Find a vendor";
		base.Load += new System.EventHandler(FormFindVendors_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
