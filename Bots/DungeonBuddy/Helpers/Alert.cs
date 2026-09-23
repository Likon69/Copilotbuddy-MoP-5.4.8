using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using Styx.CommonBot;
using Styx.WoWInternals.WoWObjects;

namespace Bots.DungeonBuddy.Helpers;

public static class Alert
{
	internal class AutoCloseAlert : Form
	{
		private readonly Button _defaultButton;

		private readonly string _defaultButtonBaseText;

		private readonly bool _showCancelButton;

		private readonly int _showTimeInSeconds;

		private readonly bool _useAudibleCue;

		private readonly IContainer components;

		private Timer timer1;

		private Button buttonCancel;

		private Button buttonOk;

		private FlowLayoutPanel flowLayoutPanel1;

		private Label labelMsg;

		private int _secondsElapsed;

		private TableLayoutPanel tableLayoutPanel1;

		private string DefaultButtonText => $"{_defaultButtonBaseText} [Seconds Remaining: {_showTimeInSeconds - _secondsElapsed}]";

		internal AutoCloseAlert(string title, string message, int showTimeInSeconds, bool useAudibleCue, bool showCancelButton, string okButtonText, string cancelButtonText = "")
		{
			_showTimeInSeconds = showTimeInSeconds;
			_useAudibleCue = useAudibleCue;
			_showCancelButton = showCancelButton;
			_defaultButtonBaseText = (showCancelButton ? cancelButtonText : okButtonText);
			InitializeComponent(title, message, okButtonText, cancelButtonText);
			_defaultButton = (showCancelButton ? buttonCancel : buttonOk);
		}

		private void InitializeComponent(string title, string message, string okButtonText, string cancelButtonText)
		{
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOk = new System.Windows.Forms.Button();
			this.labelMsg = new System.Windows.Forms.Label();
			this.tableLayoutPanel1.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			base.SuspendLayout();
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.labelMsg, 0, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.TabIndex = 0;
			this.flowLayoutPanel1.AutoSize = true;
			if (this._showCancelButton)
			{
				this.flowLayoutPanel1.Controls.Add(this.buttonCancel);
			}
			this.flowLayoutPanel1.Controls.Add(this.buttonOk);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
			this.buttonCancel.AutoSize = true;
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Dock = System.Windows.Forms.DockStyle.Right;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Text = (this._showCancelButton ? this.DefaultButtonText : cancelButtonText);
			this.labelMsg.TabIndex = 3;
			this.buttonOk.AutoSize = true;
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Dock = System.Windows.Forms.DockStyle.Right;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Text = ((!this._showCancelButton) ? this.DefaultButtonText : okButtonText);
			this.labelMsg.AutoSize = true;
			this.labelMsg.BackColor = System.Drawing.SystemColors.Info;
			this.labelMsg.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelMsg.Font = new System.Drawing.Font("Consolas", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.labelMsg.Name = "labelMsg";
			this.labelMsg.Text = message;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			base.ClientSize = new System.Drawing.Size(350, 160);
			base.Controls.Add(this.tableLayoutPanel1);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			base.Name = "AutoCloseAlert";
			this.Text = title;
			base.AcceptButton = this.buttonOk;
			base.CancelButton = this.buttonCancel;
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			base.TopMost = true;
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			base.ResumeLayout(false);
		}

		protected override void OnLoad(EventArgs e)
		{
			timer1 = new Timer
			{
				Interval = 1000,
				Enabled = true
			};
			timer1.Tick += Timer1_Tick;
			base.OnLoad(e);
		}

		private void Timer1_Tick(object sender, EventArgs e)
		{
			if (++_secondsElapsed > _showTimeInSeconds)
			{
				_defaultButton.PerformClick();
				timer1.Dispose();
			}
			_defaultButton.Text = DefaultButtonText;
			if (_secondsElapsed % 2 == 0)
			{
				SystemSounds.Exclamation.Play();
			}
		}

		protected override void OnShown(EventArgs e)
		{
			if (_useAudibleCue)
			{
				SystemSounds.Exclamation.Play();
			}
			buttonOk.Focus();
			base.OnShown(e);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				timer1.Dispose();
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
	}

	public static void Show(string title, string message, int showTimeInSeconds, bool useAudibleCue = true, bool showCancelButton = true, Action okAction = null, Action cancelAction = null, string okButtonText = "Continue", string cancelButtonText = "Stop", bool temporaryDisableTargeting = true, bool temporaryDisableMovement = true)
	{
		System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			if (temporaryDisableMovement)
			{
				ScriptHelpers.DisableMovement(() => true);
			}
			if (temporaryDisableTargeting)
			{
				Targeting.Instance.RemoveTargetsFilter += RemoveAllTargetsFilter;
			}
			DialogResult dialogResult = DialogResult.None;
			try
			{
				using AutoCloseAlert autoCloseAlert = new AutoCloseAlert(title, message, showTimeInSeconds, useAudibleCue, showCancelButton, okButtonText, cancelButtonText);
				dialogResult = autoCloseAlert.ShowDialog();
			}
			finally
			{
				try
				{
					if (dialogResult == DialogResult.OK && okAction != null)
					{
						okAction();
					}
					else if (dialogResult == DialogResult.Cancel && cancelAction != null)
					{
						cancelAction();
					}
				}
				catch (Exception ex)
				{
					Logger.Write("AutoCloseAlert Exception: {0}", ex);
				}
				if (temporaryDisableTargeting)
				{
					Targeting.Instance.RemoveTargetsFilter -= RemoveAllTargetsFilter;
				}
				if (temporaryDisableMovement)
				{
					ScriptHelpers.RestoreMovement();
				}
			}
		});
	}

	private static void RemoveAllTargetsFilter(List<WoWObject> objects)
	{
		objects.RemoveAll(obj => true);
	}
}
