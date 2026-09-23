using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Levelbot.ProfileCreation.Forms;

public class FormSelectProfileName : Form
{
	private IContainer components;

	private TextBox _txtProfileName;

	private Button _btnOk;

	private Button _btnCancel;

	public string ProfileName { get; private set; }

	public FormSelectProfileName()
	{
		InitializeComponent();
	}

	private void _btnOk_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrEmpty(_txtProfileName.Text))
		{
			MessageBox.Show("You have not entered a profile name!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		ProfileName = _txtProfileName.Text;
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private void _btnCancel_Click(object sender, EventArgs e)
	{
		ProfileName = "";
		base.DialogResult = DialogResult.Cancel;
		Close();
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
		this._txtProfileName = new System.Windows.Forms.TextBox();
		this._btnOk = new System.Windows.Forms.Button();
		this._btnCancel = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this._txtProfileName.Location = new System.Drawing.Point(12, 12);
		this._txtProfileName.Name = "_txtProfileName";
		this._txtProfileName.Size = new System.Drawing.Size(260, 20);
		this._txtProfileName.TabIndex = 0;
		this._btnOk.Location = new System.Drawing.Point(64, 38);
		this._btnOk.Name = "_btnOk";
		this._btnOk.Size = new System.Drawing.Size(75, 23);
		this._btnOk.TabIndex = 1;
		this._btnOk.Text = "OK";
		this._btnOk.UseVisualStyleBackColor = true;
		this._btnOk.Click += new System.EventHandler(_btnOk_Click);
		this._btnCancel.Location = new System.Drawing.Point(145, 38);
		this._btnCancel.Name = "_btnCancel";
		this._btnCancel.Size = new System.Drawing.Size(75, 23);
		this._btnCancel.TabIndex = 2;
		this._btnCancel.Text = "Cancel";
		this._btnCancel.UseVisualStyleBackColor = true;
		this._btnCancel.Click += new System.EventHandler(_btnCancel_Click);
		base.AcceptButton = this._btnOk;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.CancelButton = this._btnCancel;
		base.ClientSize = new System.Drawing.Size(284, 70);
		base.Controls.Add(this._btnCancel);
		base.Controls.Add(this._btnOk);
		base.Controls.Add(this._txtProfileName);
		base.Name = "FormSelectProfileName";
		this.Text = "Please enter a name";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
