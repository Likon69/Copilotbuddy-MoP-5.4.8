using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Infralution.Localization.Wpf;
using Styx.CommonBot;
using Styx.Localization;

namespace NewMixedMode;

public class FormChooser : Form
{
	private IContainer components;

	private Label lblPrimary;

	private ComboBox cmbPrimaryBots;

	private ComboBox cmbSecondaryBots;

	private Label lblSecondary;

	private Button btnPrimarySettings;

	private Button btnSecondarySettings;

	public FormChooser()
	{
		InitializeComponent();
		SetLabels();
	}

	private void SetLabels()
	{
		Text = Globalization.MixedMode_Title;
		lblPrimary.Text = Globalization.MixedMode_Primary;
		lblSecondary.Text = Globalization.MixedMode_Secondary;
		btnPrimarySettings.Text = Globalization.MixedMode_Settings;
		btnSecondarySettings.Text = Globalization.MixedMode_Settings;
	}

	private void FormChooser_Load(object sender, EventArgs e)
	{
		foreach (KeyValuePair<string, BotBase> item in BotManager.Instance.Bots.OrderBy((KeyValuePair<string, BotBase> keyValuePair) => keyValuePair.Key))
		{
			if (!(item.Value is MixedModeEx))
			{
				if (item.Value.IsPrimaryType)
				{
					cmbPrimaryBots.Items.Add(item.Value);
				}
				else
				{
					cmbSecondaryBots.Items.Add(item.Value);
				}
			}
		}
		MixedModeSettings mixedModeSettings = MixedModeEx.Instance.Settings;
		Dictionary<string, BotBase>.ValueCollection values = BotManager.Instance.Bots.Values;
		if (!string.IsNullOrEmpty(mixedModeSettings.PrimaryBot))
		{
			cmbPrimaryBots.SelectedItem = values.FirstOrDefault((BotBase botBase) => botBase.Name == mixedModeSettings.PrimaryBot);
		}
		if (!string.IsNullOrEmpty(mixedModeSettings.SecondaryBot))
		{
			cmbSecondaryBots.SelectedItem = values.FirstOrDefault((BotBase botBase) => botBase.Name == mixedModeSettings.SecondaryBot);
		}
		CultureManager.UICultureChanged += CultureManager_UICultureChanged;
	}

	private void CultureManager_UICultureChanged(object sender, EventArgs e)
	{
		SetLabels();
	}

	private void cmbPrimaryBots_SelectedIndexChanged(object sender, EventArgs e)
	{
		MixedModeEx.Instance.PrimaryBot = (BotBase)cmbPrimaryBots.SelectedItem;
		MixedModeEx.Instance.Settings.PrimaryBot = MixedModeEx.Instance.PrimaryBot.Name;
		MixedModeEx.Instance.Settings.Save();
	}

	private void cmbSecondaryBots_SelectedIndexChanged(object sender, EventArgs e)
	{
		MixedModeEx.Instance.SecondaryBot = (BotBase)cmbSecondaryBots.SelectedItem;
		MixedModeEx.Instance.Settings.SecondaryBot = MixedModeEx.Instance.SecondaryBot.Name;
		MixedModeEx.Instance.Settings.Save();
	}

	private void btnPrimarySettings_Click(object sender, EventArgs e)
	{
		((BotBase)cmbPrimaryBots.SelectedItem).ConfigurationForm?.ShowDialog();
	}

	private void btnSecondarySettings_Click(object sender, EventArgs e)
	{
		((BotBase)cmbSecondaryBots.SelectedItem).ConfigurationForm?.ShowDialog();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewMixedMode.FormChooser));
		this.lblPrimary = new System.Windows.Forms.Label();
		this.cmbPrimaryBots = new System.Windows.Forms.ComboBox();
		this.cmbSecondaryBots = new System.Windows.Forms.ComboBox();
		this.lblSecondary = new System.Windows.Forms.Label();
		this.btnPrimarySettings = new System.Windows.Forms.Button();
		this.btnSecondarySettings = new System.Windows.Forms.Button();
		base.SuspendLayout();
		resources.ApplyResources(this.lblPrimary, "lblPrimary");
		this.lblPrimary.Name = "lblPrimary";
		this.cmbPrimaryBots.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmbPrimaryBots.FormattingEnabled = true;
		resources.ApplyResources(this.cmbPrimaryBots, "cmbPrimaryBots");
		this.cmbPrimaryBots.Name = "cmbPrimaryBots";
		this.cmbPrimaryBots.SelectedIndexChanged += new System.EventHandler(cmbPrimaryBots_SelectedIndexChanged);
		this.cmbSecondaryBots.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmbSecondaryBots.FormattingEnabled = true;
		resources.ApplyResources(this.cmbSecondaryBots, "cmbSecondaryBots");
		this.cmbSecondaryBots.Name = "cmbSecondaryBots";
		this.cmbSecondaryBots.SelectedIndexChanged += new System.EventHandler(cmbSecondaryBots_SelectedIndexChanged);
		resources.ApplyResources(this.lblSecondary, "lblSecondary");
		this.lblSecondary.Name = "lblSecondary";
		resources.ApplyResources(this.btnPrimarySettings, "btnPrimarySettings");
		this.btnPrimarySettings.Name = "btnPrimarySettings";
		this.btnPrimarySettings.UseVisualStyleBackColor = true;
		this.btnPrimarySettings.Click += new System.EventHandler(btnPrimarySettings_Click);
		resources.ApplyResources(this.btnSecondarySettings, "btnSecondarySettings");
		this.btnSecondarySettings.Name = "btnSecondarySettings";
		this.btnSecondarySettings.UseVisualStyleBackColor = true;
		this.btnSecondarySettings.Click += new System.EventHandler(btnSecondarySettings_Click);
		resources.ApplyResources(this, "$this");
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.btnSecondarySettings);
		base.Controls.Add(this.btnPrimarySettings);
		base.Controls.Add(this.cmbSecondaryBots);
		base.Controls.Add(this.lblSecondary);
		base.Controls.Add(this.cmbPrimaryBots);
		base.Controls.Add(this.lblPrimary);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "FormChooser";
		base.ShowIcon = false;
		base.Load += new System.EventHandler(FormChooser_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
