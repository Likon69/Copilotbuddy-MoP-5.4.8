namespace Bots.ArchaeologyBuddy.GUI
{
	partial class ArchBuddySettings
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ArchBuddySettings));
            this.cbBlacklist = new System.Windows.Forms.ComboBox();
            this.clbBlacklistName = new System.Windows.Forms.CheckedListBox();
            this.clbBlacklistType = new System.Windows.Forms.CheckedListBox();
            this.cbContinent = new System.Windows.Forms.ComboBox();
            this.btnSelectAll = new System.Windows.Forms.Button();
            this.btnUnselectAll = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.cbUseKeystones = new System.Windows.Forms.CheckBox();
            this.btnSolveAll = new System.Windows.Forms.Button();
            this.cbSolveRace = new System.Windows.Forms.ComboBox();
            this.cbAutoSolve = new System.Windows.Forms.CheckBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.lblTrainUpto = new System.Windows.Forms.Label();
            this.nudTrainUntil = new System.Windows.Forms.NumericUpDown();
            this.btnForceTrain = new System.Windows.Forms.Button();
            this.cbAutoTrain = new System.Windows.Forms.CheckBox();
            this.cbCrateArtifacts = new System.Windows.Forms.CheckBox();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTrainUntil)).BeginInit();
            this.SuspendLayout();
            // 
            // cbBlacklist
            // 
            this.cbBlacklist.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBlacklist.FormattingEnabled = true;
            resources.ApplyResources(this.cbBlacklist, "cbBlacklist");
            this.cbBlacklist.Name = "cbBlacklist";
            this.cbBlacklist.SelectedIndexChanged += new System.EventHandler(this.cbBlacklist_SelectedIndexChanged);
            // 
            // clbBlacklistName
            // 
            this.clbBlacklistName.CheckOnClick = true;
            this.clbBlacklistName.FormattingEnabled = true;
            resources.ApplyResources(this.clbBlacklistName, "clbBlacklistName");
            this.clbBlacklistName.Name = "clbBlacklistName";
            this.clbBlacklistName.MouseUp += new System.Windows.Forms.MouseEventHandler(this.clbBlacklistName_MouseUp);
            // 
            // clbBlacklistType
            // 
            this.clbBlacklistType.CheckOnClick = true;
            this.clbBlacklistType.FormattingEnabled = true;
            resources.ApplyResources(this.clbBlacklistType, "clbBlacklistType");
            this.clbBlacklistType.Name = "clbBlacklistType";
            this.clbBlacklistType.MouseUp += new System.Windows.Forms.MouseEventHandler(this.clbBlacklistType_MouseUp);
            // 
            // cbContinent
            // 
            this.cbContinent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbContinent.FormattingEnabled = true;
            resources.ApplyResources(this.cbContinent, "cbContinent");
            this.cbContinent.Name = "cbContinent";
            this.cbContinent.SelectedIndexChanged += new System.EventHandler(this.cbContinent_SelectedIndexChanged);
            // 
            // btnSelectAll
            // 
            resources.ApplyResources(this.btnSelectAll, "btnSelectAll");
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.UseVisualStyleBackColor = true;
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);
            // 
            // btnUnselectAll
            // 
            resources.ApplyResources(this.btnUnselectAll, "btnUnselectAll");
            this.btnUnselectAll.Name = "btnUnselectAll";
            this.btnUnselectAll.UseVisualStyleBackColor = true;
            this.btnUnselectAll.Click += new System.EventHandler(this.btnUnselectAll_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabPage1.Controls.Add(this.cbCrateArtifacts);
            this.tabPage1.Controls.Add(this.cbUseKeystones);
            this.tabPage1.Controls.Add(this.btnSolveAll);
            this.tabPage1.Controls.Add(this.cbSolveRace);
            this.tabPage1.Controls.Add(this.cbAutoSolve);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            // 
            // cbUseKeystones
            // 
            resources.ApplyResources(this.cbUseKeystones, "cbUseKeystones");
            this.cbUseKeystones.Name = "cbUseKeystones";
            this.cbUseKeystones.UseVisualStyleBackColor = true;
            // 
            // btnSolveAll
            // 
            resources.ApplyResources(this.btnSolveAll, "btnSolveAll");
            this.btnSolveAll.Name = "btnSolveAll";
            this.btnSolveAll.UseVisualStyleBackColor = true;
            this.btnSolveAll.Click += new System.EventHandler(this.btnSolveAll_Click);
            // 
            // cbSolveRace
            // 
            this.cbSolveRace.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSolveRace.FormattingEnabled = true;
            resources.ApplyResources(this.cbSolveRace, "cbSolveRace");
            this.cbSolveRace.Name = "cbSolveRace";
            // 
            // cbAutoSolve
            // 
            resources.ApplyResources(this.cbAutoSolve, "cbAutoSolve");
            this.cbAutoSolve.Name = "cbAutoSolve";
            this.cbAutoSolve.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabPage2.Controls.Add(this.btnUnselectAll);
            this.tabPage2.Controls.Add(this.cbBlacklist);
            this.tabPage2.Controls.Add(this.btnSelectAll);
            this.tabPage2.Controls.Add(this.clbBlacklistName);
            this.tabPage2.Controls.Add(this.clbBlacklistType);
            this.tabPage2.Controls.Add(this.cbContinent);
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
            // 
            // tabPage3
            // 
            this.tabPage3.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabPage3.Controls.Add(this.lblTrainUpto);
            this.tabPage3.Controls.Add(this.nudTrainUntil);
            this.tabPage3.Controls.Add(this.btnForceTrain);
            this.tabPage3.Controls.Add(this.cbAutoTrain);
            resources.ApplyResources(this.tabPage3, "tabPage3");
            this.tabPage3.Name = "tabPage3";
            // 
            // lblTrainUpto
            // 
            resources.ApplyResources(this.lblTrainUpto, "lblTrainUpto");
            this.lblTrainUpto.Name = "lblTrainUpto";
            // 
            // nudTrainUntil
            // 
            resources.ApplyResources(this.nudTrainUntil, "nudTrainUntil");
            this.nudTrainUntil.Maximum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.nudTrainUntil.Minimum = new decimal(new int[] {
            75,
            0,
            0,
            0});
            this.nudTrainUntil.Name = "nudTrainUntil";
            this.nudTrainUntil.Value = new decimal(new int[] {
            75,
            0,
            0,
            0});
            // 
            // btnForceTrain
            // 
            resources.ApplyResources(this.btnForceTrain, "btnForceTrain");
            this.btnForceTrain.Name = "btnForceTrain";
            this.btnForceTrain.UseVisualStyleBackColor = true;
            this.btnForceTrain.Click += new System.EventHandler(this.btnForceTrain_Click);
            // 
            // cbAutoTrain
            // 
            resources.ApplyResources(this.cbAutoTrain, "cbAutoTrain");
            this.cbAutoTrain.Name = "cbAutoTrain";
            this.cbAutoTrain.UseVisualStyleBackColor = true;
            // 
            // cbCrateArtifacts
            // 
            resources.ApplyResources(this.cbCrateArtifacts, "cbCrateArtifacts");
            this.cbCrateArtifacts.Name = "cbCrateArtifacts";
            this.cbCrateArtifacts.UseVisualStyleBackColor = true;
            // 
            // ArchBuddySettings
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ArchBuddySettings";
            this.Activated += new System.EventHandler(this.ArchBuddySettings_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ArchBuddySettings_FormClosing);
            this.Load += new System.EventHandler(this.ArchBuddySettings_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTrainUntil)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ComboBox cbBlacklist;
		private System.Windows.Forms.CheckedListBox clbBlacklistName;
		private System.Windows.Forms.CheckedListBox clbBlacklistType;
		private System.Windows.Forms.ComboBox cbContinent;
		private System.Windows.Forms.Button btnSelectAll;
		private System.Windows.Forms.Button btnUnselectAll;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.Button btnSolveAll;
		private System.Windows.Forms.ComboBox cbSolveRace;
        private System.Windows.Forms.CheckBox cbAutoSolve;
		private System.Windows.Forms.CheckBox cbUseKeystones;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.Button btnForceTrain;
		private System.Windows.Forms.CheckBox cbAutoTrain;
		private System.Windows.Forms.Label lblTrainUpto;
		private System.Windows.Forms.NumericUpDown nudTrainUntil;
        private System.Windows.Forms.CheckBox cbCrateArtifacts;
	}
}