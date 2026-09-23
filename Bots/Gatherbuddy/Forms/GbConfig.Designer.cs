namespace Bots.Gatherbuddy.GUI
{
    partial class GbConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GbConfig));
            this.tabGbConfig = new System.Windows.Forms.TabControl();
            this._tabPaths = new System.Windows.Forms.TabPage();
            this._cbRandomizeHotspots = new System.Windows.Forms.CheckBox();
            this._cbPathingType = new System.Windows.Forms.ComboBox();
            this._lblPathingType = new System.Windows.Forms.Label();
            this._lblHeightModifier = new System.Windows.Forms.Label();
            this.nudHeight = new System.Windows.Forms.NumericUpDown();
            this._tabGather = new System.Windows.Forms.TabPage();
            this.btnStatusReport = new System.Windows.Forms.Button();
            this._gbGatherOptions = new System.Windows.Forms.GroupBox();
            this._cbNoNinja = new System.Windows.Forms.CheckBox();
            this._cbIngoreElites = new System.Windows.Forms.CheckBox();
            this._gbGather = new System.Windows.Forms.GroupBox();
            this._cbbMining = new System.Windows.Forms.CheckBox();
            this._cbbHerbalism = new System.Windows.Forms.CheckBox();
            this._gbBagsFullOptions = new System.Windows.Forms.GroupBox();
            this._tbMailAlt = new System.Windows.Forms.TextBox();
            this._rbMail = new System.Windows.Forms.RadioButton();
            this._rbGuildVault = new System.Windows.Forms.RadioButton();
            this._rbHearthAndExit = new System.Windows.Forms.RadioButton();
            this.btnForceVendor = new System.Windows.Forms.Button();
            this._tabManage = new System.Windows.Forms.TabPage();
            this._cbRepairFromGuild = new System.Windows.Forms.CheckBox();
            this._cbLootMobs = new System.Windows.Forms.CheckBox();
            this._cbWaitRezSickness = new System.Windows.Forms.CheckBox();
            this._cbSpiritRez = new System.Windows.Forms.CheckBox();
            this._nudBottingHours = new System.Windows.Forms.NumericUpDown();
            this._lbStop = new System.Windows.Forms.Label();
            this._tabSelection = new System.Windows.Forms.TabPage();
            this.btnUnSelectAllMine = new System.Windows.Forms.Button();
            this.btnSelectAllMine = new System.Windows.Forms.Button();
            this.btnUnSelectAllHerb = new System.Windows.Forms.Button();
            this.btnSelectAllHerb = new System.Windows.Forms.Button();
            this.lblCollectOnly = new System.Windows.Forms.Label();
            this.clbMinerals = new System.Windows.Forms.CheckedListBox();
            this.clbHerbs = new System.Windows.Forms.CheckedListBox();
            this._tabAdvanced = new System.Windows.Forms.TabPage();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this._gbBlacklisting = new System.Windows.Forms.GroupBox();
            this._lblSeconds = new System.Windows.Forms.Label();
            this._nudBlacklistTimer = new System.Windows.Forms.NumericUpDown();
            this._lblBlacklistANodeAfter = new System.Windows.Forms.Label();
            this._cbFaceNodes = new System.Windows.Forms.CheckBox();
            this._lblRandomizationFactor = new System.Windows.Forms.Label();
            this.nudRandomizationFactor = new System.Windows.Forms.NumericUpDown();
            this.cbDiagnosticLogging = new System.Windows.Forms.CheckBox();
            this.tabGbConfig.SuspendLayout();
            this._tabPaths.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudRandomizationFactor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHeight)).BeginInit();
            this._tabGather.SuspendLayout();
            this._gbGatherOptions.SuspendLayout();
            this._gbGather.SuspendLayout();
            this._gbBagsFullOptions.SuspendLayout();
            this._tabManage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nudBottingHours)).BeginInit();
            this._tabSelection.SuspendLayout();
            this._tabAdvanced.SuspendLayout();
            this._gbBlacklisting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nudBlacklistTimer)).BeginInit();
            this.SuspendLayout();
            // 
            // tabGbConfig
            // 
            this.tabGbConfig.Controls.Add(this._tabPaths);
            this.tabGbConfig.Controls.Add(this._tabGather);
            this.tabGbConfig.Controls.Add(this._tabManage);
            this.tabGbConfig.Controls.Add(this._tabSelection);
            this.tabGbConfig.Controls.Add(this._tabAdvanced);
            resources.ApplyResources(this.tabGbConfig, "tabGbConfig");
            this.tabGbConfig.Name = "tabGbConfig";
            this.tabGbConfig.SelectedIndex = 0;
            // 
            // _tabPaths
            //
            this._tabPaths.Controls.Add(this._lblRandomizationFactor);
            this._tabPaths.Controls.Add(this.nudRandomizationFactor);
            this._tabPaths.Controls.Add(this._cbRandomizeHotspots);
            this._tabPaths.Controls.Add(this._cbPathingType);
            this._tabPaths.Controls.Add(this._lblPathingType);
            this._tabPaths.Controls.Add(this._lblHeightModifier);
            this._tabPaths.Controls.Add(this.nudHeight);
            resources.ApplyResources(this._tabPaths, "_tabPaths");
            this._tabPaths.Name = "_tabPaths";
            this._tabPaths.UseVisualStyleBackColor = true;
            // 
            // _cbRandomizeHotspots
            // 
            resources.ApplyResources(this._cbRandomizeHotspots, "_cbRandomizeHotspots");
            this._cbRandomizeHotspots.Name = "_cbRandomizeHotspots";
            this._cbRandomizeHotspots.UseVisualStyleBackColor = true;
            //
            // _lblRandomizationFactor
            //
            resources.ApplyResources(this._lblRandomizationFactor, "_lblRandomizationFactor");
            this._lblRandomizationFactor.Name = "_lblRandomizationFactor";
            //
            // nudRandomizationFactor
            //
            resources.ApplyResources(this.nudRandomizationFactor, "nudRandomizationFactor");
            this.nudRandomizationFactor.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.nudRandomizationFactor.Minimum = new decimal(new int[] {
            300,
            0,
            0,
            -2147483648});
            this.nudRandomizationFactor.Name = "nudRandomizationFactor";
            //
            // _cbPathingType
            // 
            this._cbPathingType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbPathingType.FormattingEnabled = true;
            this._cbPathingType.Items.AddRange(new object[] {
            resources.GetString("_cbPathingType.Items"),
            resources.GetString("_cbPathingType.Items1")});
            resources.ApplyResources(this._cbPathingType, "_cbPathingType");
            this._cbPathingType.Name = "_cbPathingType";
            // 
            // _lblPathingType
            // 
            resources.ApplyResources(this._lblPathingType, "_lblPathingType");
            this._lblPathingType.Name = "_lblPathingType";
            // 
            // _lblHeightModifier
            // 
            resources.ApplyResources(this._lblHeightModifier, "_lblHeightModifier");
            this._lblHeightModifier.Name = "_lblHeightModifier";
            // 
            // nudHeight
            // 
            resources.ApplyResources(this.nudHeight, "nudHeight");
            this.nudHeight.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.nudHeight.Minimum = new decimal(new int[] {
            300,
            0,
            0,
            -2147483648});
            this.nudHeight.Name = "nudHeight";
            // 
            // _tabGather
            // 
            this._tabGather.Controls.Add(this.btnStatusReport);
            this._tabGather.Controls.Add(this._gbGatherOptions);
            this._tabGather.Controls.Add(this._gbGather);
            this._tabGather.Controls.Add(this._gbBagsFullOptions);
            this._tabGather.Controls.Add(this.btnForceVendor);
            resources.ApplyResources(this._tabGather, "_tabGather");
            this._tabGather.Name = "_tabGather";
            this._tabGather.UseVisualStyleBackColor = true;
            // 
            // btnStatusReport
            // 
            resources.ApplyResources(this.btnStatusReport, "btnStatusReport");
            this.btnStatusReport.Name = "btnStatusReport";
            this.btnStatusReport.UseVisualStyleBackColor = true;
            this.btnStatusReport.Click += new System.EventHandler(this.btnStatusReport_Click);
            // 
            // _gbGatherOptions
            // 
            this._gbGatherOptions.Controls.Add(this._cbFaceNodes);
            this._gbGatherOptions.Controls.Add(this._cbNoNinja);
            this._gbGatherOptions.Controls.Add(this._cbIngoreElites);
            resources.ApplyResources(this._gbGatherOptions, "_gbGatherOptions");
            this._gbGatherOptions.Name = "_gbGatherOptions";
            this._gbGatherOptions.TabStop = false;
            // 
            // _cbNoNinja
            // 
            resources.ApplyResources(this._cbNoNinja, "_cbNoNinja");
            this._cbNoNinja.Name = "_cbNoNinja";
            this._cbNoNinja.UseVisualStyleBackColor = true;
            // 
            // _cbIngoreElites
            // 
            resources.ApplyResources(this._cbIngoreElites, "_cbIngoreElites");
            this._cbIngoreElites.Name = "_cbIngoreElites";
            this._cbIngoreElites.UseVisualStyleBackColor = true;
            // 
            // _gbGather
            // 
            this._gbGather.Controls.Add(this._cbbMining);
            this._gbGather.Controls.Add(this._cbbHerbalism);
            resources.ApplyResources(this._gbGather, "_gbGather");
            this._gbGather.Name = "_gbGather";
            this._gbGather.TabStop = false;
            // 
            // _cbbMining
            // 
            resources.ApplyResources(this._cbbMining, "_cbbMining");
            this._cbbMining.Name = "_cbbMining";
            this._cbbMining.UseVisualStyleBackColor = true;
            // 
            // _cbbHerbalism
            // 
            resources.ApplyResources(this._cbbHerbalism, "_cbbHerbalism");
            this._cbbHerbalism.Name = "_cbbHerbalism";
            this._cbbHerbalism.UseVisualStyleBackColor = true;
            // 
            // _gbBagsFullOptions
            // 
            this._gbBagsFullOptions.Controls.Add(this._tbMailAlt);
            this._gbBagsFullOptions.Controls.Add(this._rbMail);
            this._gbBagsFullOptions.Controls.Add(this._rbGuildVault);
            this._gbBagsFullOptions.Controls.Add(this._rbHearthAndExit);
            resources.ApplyResources(this._gbBagsFullOptions, "_gbBagsFullOptions");
            this._gbBagsFullOptions.Name = "_gbBagsFullOptions";
            this._gbBagsFullOptions.TabStop = false;
            // 
            // _tbMailAlt
            // 
            resources.ApplyResources(this._tbMailAlt, "_tbMailAlt");
            this._tbMailAlt.Name = "_tbMailAlt";
            // 
            // _rbMail
            // 
            resources.ApplyResources(this._rbMail, "_rbMail");
            this._rbMail.Name = "_rbMail";
            this._rbMail.TabStop = true;
            this._rbMail.UseVisualStyleBackColor = true;
            // 
            // _rbGuildVault
            // 
            resources.ApplyResources(this._rbGuildVault, "_rbGuildVault");
            this._rbGuildVault.Name = "_rbGuildVault";
            this._rbGuildVault.TabStop = true;
            this._rbGuildVault.UseVisualStyleBackColor = true;
            // 
            // _rbHearthAndExit
            // 
            resources.ApplyResources(this._rbHearthAndExit, "_rbHearthAndExit");
            this._rbHearthAndExit.Name = "_rbHearthAndExit";
            this._rbHearthAndExit.TabStop = true;
            this._rbHearthAndExit.UseVisualStyleBackColor = true;
            // 
            // btnForceVendor
            // 
            resources.ApplyResources(this.btnForceVendor, "btnForceVendor");
            this.btnForceVendor.Name = "btnForceVendor";
            this.btnForceVendor.UseVisualStyleBackColor = true;
            this.btnForceVendor.Click += new System.EventHandler(this.btnForceVendor_Click);
            // 
            // _tabManage
            // 
            this._tabManage.Controls.Add(this._cbRepairFromGuild);
            this._tabManage.Controls.Add(this._cbLootMobs);
            this._tabManage.Controls.Add(this._cbWaitRezSickness);
            this._tabManage.Controls.Add(this._cbSpiritRez);
            this._tabManage.Controls.Add(this._nudBottingHours);
            this._tabManage.Controls.Add(this._lbStop);
            resources.ApplyResources(this._tabManage, "_tabManage");
            this._tabManage.Name = "_tabManage";
            this._tabManage.UseVisualStyleBackColor = true;
            // 
            // _cbRepairFromGuild
            // 
            resources.ApplyResources(this._cbRepairFromGuild, "_cbRepairFromGuild");
            this._cbRepairFromGuild.Name = "_cbRepairFromGuild";
            this._cbRepairFromGuild.UseVisualStyleBackColor = true;
            // 
            // _cbLootMobs
            // 
            resources.ApplyResources(this._cbLootMobs, "_cbLootMobs");
            this._cbLootMobs.Name = "_cbLootMobs";
            this._cbLootMobs.UseVisualStyleBackColor = true;
            // 
            // _cbWaitRezSickness
            // 
            resources.ApplyResources(this._cbWaitRezSickness, "_cbWaitRezSickness");
            this._cbWaitRezSickness.Name = "_cbWaitRezSickness";
            this._cbWaitRezSickness.UseVisualStyleBackColor = true;
            // 
            // _cbSpiritRez
            // 
            resources.ApplyResources(this._cbSpiritRez, "_cbSpiritRez");
            this._cbSpiritRez.Name = "_cbSpiritRez";
            this._cbSpiritRez.UseVisualStyleBackColor = true;
            // 
            // _nudBottingHours
            // 
            this._nudBottingHours.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            resources.ApplyResources(this._nudBottingHours, "_nudBottingHours");
            this._nudBottingHours.Name = "_nudBottingHours";
            // 
            // _lbStop
            // 
            resources.ApplyResources(this._lbStop, "_lbStop");
            this._lbStop.Name = "_lbStop";
            //
            // _tabSelection
            // 
            this._tabSelection.Controls.Add(this.btnUnSelectAllMine);
            this._tabSelection.Controls.Add(this.btnSelectAllMine);
            this._tabSelection.Controls.Add(this.btnUnSelectAllHerb);
            this._tabSelection.Controls.Add(this.btnSelectAllHerb);
            this._tabSelection.Controls.Add(this.lblCollectOnly);
            this._tabSelection.Controls.Add(this.clbMinerals);
            this._tabSelection.Controls.Add(this.clbHerbs);
            resources.ApplyResources(this._tabSelection, "_tabSelection");
            this._tabSelection.Name = "_tabSelection";
            this._tabSelection.UseVisualStyleBackColor = true;
            // 
            // btnUnSelectAllMine
            // 
            resources.ApplyResources(this.btnUnSelectAllMine, "btnUnSelectAllMine");
            this.btnUnSelectAllMine.Name = "btnUnSelectAllMine";
            this.btnUnSelectAllMine.UseVisualStyleBackColor = true;
            this.btnUnSelectAllMine.Click += new System.EventHandler(this.btnUnSelectAllMine_Click);
            // 
            // btnSelectAllMine
            // 
            resources.ApplyResources(this.btnSelectAllMine, "btnSelectAllMine");
            this.btnSelectAllMine.Name = "btnSelectAllMine";
            this.btnSelectAllMine.UseVisualStyleBackColor = true;
            this.btnSelectAllMine.Click += new System.EventHandler(this.btnSelectAllMine_Click);
            // 
            // btnUnSelectAllHerb
            // 
            resources.ApplyResources(this.btnUnSelectAllHerb, "btnUnSelectAllHerb");
            this.btnUnSelectAllHerb.Name = "btnUnSelectAllHerb";
            this.btnUnSelectAllHerb.UseVisualStyleBackColor = true;
            this.btnUnSelectAllHerb.Click += new System.EventHandler(this.btnUnSelectAllHerb_Click);
            // 
            // btnSelectAllHerb
            // 
            resources.ApplyResources(this.btnSelectAllHerb, "btnSelectAllHerb");
            this.btnSelectAllHerb.Name = "btnSelectAllHerb";
            this.btnSelectAllHerb.UseVisualStyleBackColor = true;
            this.btnSelectAllHerb.Click += new System.EventHandler(this.btnSelectAllHerb_Click);
            // 
            // lblCollectOnly
            // 
            resources.ApplyResources(this.lblCollectOnly, "lblCollectOnly");
            this.lblCollectOnly.Name = "lblCollectOnly";
            // 
            // clbMinerals
            // 
            this.clbMinerals.CheckOnClick = true;
            this.clbMinerals.FormattingEnabled = true;
            resources.ApplyResources(this.clbMinerals, "clbMinerals");
            this.clbMinerals.Name = "clbMinerals";
            this.clbMinerals.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbMinerals_ItemCheck);
            // 
            // clbHerbs
            // 
            this.clbHerbs.CheckOnClick = true;
            this.clbHerbs.FormattingEnabled = true;
            resources.ApplyResources(this.clbHerbs, "clbHerbs");
            this.clbHerbs.Name = "clbHerbs";
            this.clbHerbs.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbHerbs_ItemCheck);
            // 
            // _tabAdvanced
            //
            this._tabAdvanced.Controls.Add(this.cbDiagnosticLogging);
            this._tabAdvanced.Controls.Add(this.label7);
            this._tabAdvanced.Controls.Add(this.label6);
            this._tabAdvanced.Controls.Add(this._gbBlacklisting);
            resources.ApplyResources(this._tabAdvanced, "_tabAdvanced");
            this._tabAdvanced.Name = "_tabAdvanced";
            this._tabAdvanced.UseVisualStyleBackColor = true;
            //
            // cbDiagnosticLogging
            //
            resources.ApplyResources(this.cbDiagnosticLogging, "cbDiagnosticLogging");
            this.cbDiagnosticLogging.Name = "cbDiagnosticLogging";
            this.cbDiagnosticLogging.UseVisualStyleBackColor = true;
            //
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // _gbBlacklisting
            // 
            this._gbBlacklisting.Controls.Add(this._lblSeconds);
            this._gbBlacklisting.Controls.Add(this._nudBlacklistTimer);
            this._gbBlacklisting.Controls.Add(this._lblBlacklistANodeAfter);
            resources.ApplyResources(this._gbBlacklisting, "_gbBlacklisting");
            this._gbBlacklisting.Name = "_gbBlacklisting";
            this._gbBlacklisting.TabStop = false;
            // 
            // _lblSeconds
            // 
            resources.ApplyResources(this._lblSeconds, "_lblSeconds");
            this._lblSeconds.Name = "_lblSeconds";
            // 
            // _nudBlacklistTimer
            // 
            resources.ApplyResources(this._nudBlacklistTimer, "_nudBlacklistTimer");
            this._nudBlacklistTimer.Name = "_nudBlacklistTimer";
            // 
            // _lblBlacklistANodeAfter
            // 
            resources.ApplyResources(this._lblBlacklistANodeAfter, "_lblBlacklistANodeAfter");
            this._lblBlacklistANodeAfter.Name = "_lblBlacklistANodeAfter";
            // 
            // _cbFaceNodes
            // 
            resources.ApplyResources(this._cbFaceNodes, "_cbFaceNodes");
            this._cbFaceNodes.Name = "_cbFaceNodes";
            this._cbFaceNodes.UseVisualStyleBackColor = true;
            // 
            // GbConfig
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabGbConfig);
            this.Name = "GbConfig";
            this.Load += new System.EventHandler(this.GbConfig_Load);
            this.tabGbConfig.ResumeLayout(false);
            this._tabPaths.ResumeLayout(false);
            this._tabPaths.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudRandomizationFactor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHeight)).EndInit();
            this._tabGather.ResumeLayout(false);
            this._gbGatherOptions.ResumeLayout(false);
            this._gbGatherOptions.PerformLayout();
            this._gbGather.ResumeLayout(false);
            this._gbGather.PerformLayout();
            this._gbBagsFullOptions.ResumeLayout(false);
            this._gbBagsFullOptions.PerformLayout();
            this._tabManage.ResumeLayout(false);
            this._tabManage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nudBottingHours)).EndInit();
            this._tabSelection.ResumeLayout(false);
            this._tabSelection.PerformLayout();
            this._tabAdvanced.ResumeLayout(false);
            this._tabAdvanced.PerformLayout();
            this._gbBlacklisting.ResumeLayout(false);
            this._gbBlacklisting.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nudBlacklistTimer)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabGbConfig;
		private System.Windows.Forms.TabPage _tabPaths;
        private System.Windows.Forms.TabPage _tabGather;
		private System.Windows.Forms.TabPage _tabManage;
        private System.Windows.Forms.CheckBox _cbbHerbalism;
        private System.Windows.Forms.CheckBox _cbbMining;
        private System.Windows.Forms.GroupBox _gbBagsFullOptions;
        private System.Windows.Forms.TextBox _tbMailAlt;
        private System.Windows.Forms.RadioButton _rbMail;
        private System.Windows.Forms.RadioButton _rbGuildVault;
		private System.Windows.Forms.RadioButton _rbHearthAndExit;
        private System.Windows.Forms.CheckBox _cbWaitRezSickness;
        private System.Windows.Forms.CheckBox _cbSpiritRez;
        private System.Windows.Forms.NumericUpDown _nudBottingHours;
        private System.Windows.Forms.Label _lbStop;
		private System.Windows.Forms.Label _lblHeightModifier;
		private System.Windows.Forms.NumericUpDown nudHeight;
		private System.Windows.Forms.ComboBox _cbPathingType;
		private System.Windows.Forms.Label _lblPathingType;
		private System.Windows.Forms.CheckBox _cbNoNinja;
		private System.Windows.Forms.TabPage _tabSelection;
		private System.Windows.Forms.Label lblCollectOnly;
		private System.Windows.Forms.CheckedListBox clbMinerals;
		private System.Windows.Forms.CheckedListBox clbHerbs;
		private System.Windows.Forms.Button btnForceVendor;
		private System.Windows.Forms.CheckBox _cbIngoreElites;
		private System.Windows.Forms.GroupBox _gbGatherOptions;
		private System.Windows.Forms.GroupBox _gbGather;
		private System.Windows.Forms.Button btnStatusReport;
		private System.Windows.Forms.TabPage _tabAdvanced;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.GroupBox _gbBlacklisting;
		private System.Windows.Forms.Label _lblSeconds;
		private System.Windows.Forms.NumericUpDown _nudBlacklistTimer;
		private System.Windows.Forms.Label _lblBlacklistANodeAfter;
		private System.Windows.Forms.CheckBox _cbLootMobs;
		private System.Windows.Forms.Button btnUnSelectAllMine;
		private System.Windows.Forms.Button btnSelectAllMine;
		private System.Windows.Forms.Button btnUnSelectAllHerb;
		private System.Windows.Forms.Button btnSelectAllHerb;
		private System.Windows.Forms.CheckBox _cbRepairFromGuild;
        private System.Windows.Forms.CheckBox _cbRandomizeHotspots;
        private System.Windows.Forms.CheckBox _cbFaceNodes;
        private System.Windows.Forms.Label _lblRandomizationFactor;
        private System.Windows.Forms.NumericUpDown nudRandomizationFactor;
        private System.Windows.Forms.CheckBox cbDiagnosticLogging;
    }
}