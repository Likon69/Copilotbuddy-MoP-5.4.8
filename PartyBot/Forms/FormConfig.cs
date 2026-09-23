using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace PartyBot.Forms;

public class FormConfig : Form
{
	private IContainer components;

	private TableLayoutPanel tableLayoutPanel1;

	private Label label1;

	private PropertyGrid propertyGrid;

	public FormConfig()
	{
		InitializeComponent();
	}

	private void FormConfig_Load(object sender, EventArgs e)
	{
		propertyGrid.SelectedObject = PartyBotSettings.Instance;
	}

	private void FormConfig_FormClosing(object sender, FormClosingEventArgs e)
	{
		PartyBotSettings.Instance.Save();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PartyBot.Forms.FormConfig));
		this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
		this.label1 = new System.Windows.Forms.Label();
		this.propertyGrid = new System.Windows.Forms.PropertyGrid();
		this.tableLayoutPanel1.SuspendLayout();
		base.SuspendLayout();
		resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
		this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
		this.tableLayoutPanel1.Controls.Add(this.propertyGrid, 0, 0);
		this.tableLayoutPanel1.Name = "tableLayoutPanel1";
		resources.ApplyResources(this.label1, "label1");
		this.label1.Name = "label1";
		resources.ApplyResources(this.propertyGrid, "propertyGrid");
		this.propertyGrid.Name = "propertyGrid";
		resources.ApplyResources(this, "$this");
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.tableLayoutPanel1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "FormConfig";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(FormConfig_FormClosing);
		base.Load += new System.EventHandler(FormConfig_Load);
		this.tableLayoutPanel1.ResumeLayout(false);
		this.tableLayoutPanel1.PerformLayout();
		base.ResumeLayout(false);
	}
}
