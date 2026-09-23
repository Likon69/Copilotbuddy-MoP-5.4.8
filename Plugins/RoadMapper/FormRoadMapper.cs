using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Styx;
using Styx.Common;
using Styx.Helpers;
using Styx.WoWInternals;

namespace RoadMapper;

internal class FormRoadMapper : Form
{
	private class RecordingOptions
	{
		public string FilePath { get; set; }

		public int Timeout { get; set; }
	}

	private volatile bool _isRecording;

	private Thread _recordingThread;

	private IContainer components;

	private SaveFileDialog _saveFileDialog;

	private TextBox _txtSavePath;

	private Button _btnFindSavePath;

	private Label _lblPath;

	private Label label1;

	private NumericUpDown _nmcRecordInterval;

	private Button _btnStartStop;

	public FormRoadMapper()
	{
		InitializeComponent();
	}

	private void _btnStartStop_Click(object sender, EventArgs e)
	{
		ToggleRecording();
	}

	private void ToggleRecording()
	{
		if (_isRecording)
		{
			_isRecording = false;
			_btnStartStop.Text = "Start";
			_btnFindSavePath.Enabled = true;
			_nmcRecordInterval.Enabled = true;
			return;
		}
		if (string.IsNullOrEmpty(_txtSavePath.Text))
		{
			MessageBox.Show("Please select a save file");
			return;
		}
		_btnFindSavePath.Enabled = false;
		_nmcRecordInterval.Enabled = false;
		_isRecording = true;
		_btnStartStop.Text = "Stop";
		_recordingThread = new Thread(RecordRoad)
		{
			IsBackground = true
		};
		_recordingThread.Start(new RecordingOptions
		{
			FilePath = _txtSavePath.Text,
			Timeout = (int)_nmcRecordInterval.Value
		});
	}

	private void RecordRoad(object state)
	{
		RecordingOptions options = (RecordingOptions)state;
		using StreamWriter streamWriter = new StreamWriter(File.Create(options.FilePath));
		streamWriter.AutoFlush = true;
		ObjectManager.Update();
		streamWriter.WriteLine(StyxWoW.Me.MapName);
		WoWPoint other = WoWPoint.Zero;
		while (_isRecording)
		{
			WoWPoint location = StyxWoW.Me.Location;
			if (location.DistanceSqr(other) > 4f)
			{
				streamWriter.WriteLine($"{Styx.Helpers.Extensions.ToStringInvariant(location.X)},{Styx.Helpers.Extensions.ToStringInvariant(location.Y)},{Styx.Helpers.Extensions.ToStringInvariant(location.Z)}");
				Logging.Write("Added point at <{0}, {1}, {2}>", Styx.Helpers.Extensions.ToStringInvariant(location.X), Styx.Helpers.Extensions.ToStringInvariant(location.Y), Styx.Helpers.Extensions.ToStringInvariant(location.Z));
				other = location;
			}
			StyxWoW.Sleep(options.Timeout);
			ObjectManager.Update();
		}
	}

	private void _btnFindSavePath_Click(object sender, EventArgs e)
	{
		if (_saveFileDialog.ShowDialog() == DialogResult.OK)
		{
			if (File.Exists(_saveFileDialog.FileName))
			{
				MessageBox.Show("You can not select a file that already exists.");
			}
			else
			{
				_txtSavePath.Text = _saveFileDialog.FileName;
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
		this._saveFileDialog = new System.Windows.Forms.SaveFileDialog();
		this._txtSavePath = new System.Windows.Forms.TextBox();
		this._btnFindSavePath = new System.Windows.Forms.Button();
		this._lblPath = new System.Windows.Forms.Label();
		this.label1 = new System.Windows.Forms.Label();
		this._nmcRecordInterval = new System.Windows.Forms.NumericUpDown();
		this._btnStartStop = new System.Windows.Forms.Button();
		((System.ComponentModel.ISupportInitialize)this._nmcRecordInterval).BeginInit();
		base.SuspendLayout();
		this._saveFileDialog.DefaultExt = "txt";
		this._saveFileDialog.OverwritePrompt = false;
		this._txtSavePath.Location = new System.Drawing.Point(50, 12);
		this._txtSavePath.Name = "_txtSavePath";
		this._txtSavePath.ReadOnly = true;
		this._txtSavePath.Size = new System.Drawing.Size(189, 20);
		this._txtSavePath.TabIndex = 0;
		this._btnFindSavePath.Location = new System.Drawing.Point(245, 10);
		this._btnFindSavePath.Name = "_btnFindSavePath";
		this._btnFindSavePath.Size = new System.Drawing.Size(27, 23);
		this._btnFindSavePath.TabIndex = 1;
		this._btnFindSavePath.Text = "...";
		this._btnFindSavePath.UseVisualStyleBackColor = true;
		this._btnFindSavePath.Click += new System.EventHandler(_btnFindSavePath_Click);
		this._lblPath.AutoSize = true;
		this._lblPath.Location = new System.Drawing.Point(12, 15);
		this._lblPath.Name = "_lblPath";
		this._lblPath.Size = new System.Drawing.Size(32, 13);
		this._lblPath.TabIndex = 2;
		this._lblPath.Text = "Path:";
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(12, 40);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(104, 13);
		this.label1.TabIndex = 3;
		this.label1.Text = "Record interval (ms):";
		this._nmcRecordInterval.Location = new System.Drawing.Point(122, 38);
		this._nmcRecordInterval.Maximum = new decimal(new int[4] { 2000, 0, 0, 0 });
		this._nmcRecordInterval.Minimum = new decimal(new int[4] { 50, 0, 0, 0 });
		this._nmcRecordInterval.Name = "_nmcRecordInterval";
		this._nmcRecordInterval.Size = new System.Drawing.Size(73, 20);
		this._nmcRecordInterval.TabIndex = 4;
		this._nmcRecordInterval.Value = new decimal(new int[4] { 400, 0, 0, 0 });
		this._btnStartStop.Location = new System.Drawing.Point(105, 64);
		this._btnStartStop.Name = "_btnStartStop";
		this._btnStartStop.Size = new System.Drawing.Size(75, 23);
		this._btnStartStop.TabIndex = 5;
		this._btnStartStop.Text = "Start";
		this._btnStartStop.UseVisualStyleBackColor = true;
		this._btnStartStop.Click += new System.EventHandler(_btnStartStop_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(284, 95);
		base.Controls.Add(this._btnStartStop);
		base.Controls.Add(this._nmcRecordInterval);
		base.Controls.Add(this.label1);
		base.Controls.Add(this._lblPath);
		base.Controls.Add(this._btnFindSavePath);
		base.Controls.Add(this._txtSavePath);
		base.Name = "FormRoadMapper";
		this.Text = "Road Mapper";
		((System.ComponentModel.ISupportInitialize)this._nmcRecordInterval).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
