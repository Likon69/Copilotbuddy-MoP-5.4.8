using Bots.BGBuddy.Resources;

namespace Bots.BGBuddy.Forms
{
    partial class ConfigWindow
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
            this.cbQueue1 = new System.Windows.Forms.ComboBox();
            this.cbQueue2 = new System.Windows.Forms.ComboBox();
            this.lblQueue1 = new System.Windows.Forms.Label();
            this.lblQueue2 = new System.Windows.Forms.Label();
            this.cbLootCorpses = new System.Windows.Forms.CheckBox();
            this.lblPullDistance = new System.Windows.Forms.Label();
            this.nudPullDistance = new System.Windows.Forms.NumericUpDown();
            this.gbQueue = new System.Windows.Forms.GroupBox();
            this.gbSettings = new System.Windows.Forms.GroupBox();
            this.btnHeatmapWindow = new System.Windows.Forms.Button();
            this.cbDisableMounting = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.nudPullDistance)).BeginInit();
            this.gbQueue.SuspendLayout();
            this.gbSettings.SuspendLayout();
            this.SuspendLayout();
            //
            // cbQueue1
            //
            this.cbQueue1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbQueue1.FormattingEnabled = true;
            this.cbQueue1.Location = new System.Drawing.Point(98, 31);
            this.cbQueue1.Name = "cbQueue1";
            this.cbQueue1.Size = new System.Drawing.Size(146, 21);
            this.cbQueue1.TabIndex = 0;
            //
            // cbQueue2
            //
            this.cbQueue2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbQueue2.FormattingEnabled = true;
            this.cbQueue2.Location = new System.Drawing.Point(98, 55);
            this.cbQueue2.Name = "cbQueue2";
            this.cbQueue2.Size = new System.Drawing.Size(146, 21);
            this.cbQueue2.TabIndex = 1;
            //
            // lblQueue1
            //
            this.lblQueue1.AutoSize = true;
            this.lblQueue1.Location = new System.Drawing.Point(18, 34);
            this.lblQueue1.Name = "lblQueue1";
            this.lblQueue1.Size = new System.Drawing.Size(55, 13);
            this.lblQueue1.TabIndex = 2;
            this.lblQueue1.Text = "Queue #1";
            //
            // lblQueue2
            //
            this.lblQueue2.AutoSize = true;
            this.lblQueue2.Location = new System.Drawing.Point(18, 58);
            this.lblQueue2.Name = "lblQueue2";
            this.lblQueue2.Size = new System.Drawing.Size(55, 13);
            this.lblQueue2.TabIndex = 3;
            this.lblQueue2.Text = "Queue #2";
            //
            // cbLootCorpses
            //
            this.cbLootCorpses.AutoSize = true;
            this.cbLootCorpses.Enabled = false;
            this.cbLootCorpses.Location = new System.Drawing.Point(21, 81);
            this.cbLootCorpses.Name = "cbLootCorpses";
            this.cbLootCorpses.Size = new System.Drawing.Size(88, 17);
            this.cbLootCorpses.TabIndex = 4;
            this.cbLootCorpses.Text = global::Bots.BGBuddy.Resources.BGBuddyResources.LootCorpses;
            this.cbLootCorpses.UseVisualStyleBackColor = true;
            //
            // lblPullDistance
            //
            this.lblPullDistance.AutoSize = true;
            this.lblPullDistance.Location = new System.Drawing.Point(18, 26);
            this.lblPullDistance.Name = "lblPullDistance";
            this.lblPullDistance.Size = new System.Drawing.Size(69, 13);
            this.lblPullDistance.TabIndex = 5;
            this.lblPullDistance.Text = "Pull Distance";
            //
            // nudPullDistance
            //
            this.nudPullDistance.Location = new System.Drawing.Point(124, 24);
            this.nudPullDistance.Name = "nudPullDistance";
            this.nudPullDistance.Size = new System.Drawing.Size(120, 20);
            this.nudPullDistance.TabIndex = 6;
            //
            // gbQueue
            //
            this.gbQueue.Controls.Add(this.lblQueue1);
            this.gbQueue.Controls.Add(this.cbQueue1);
            this.gbQueue.Controls.Add(this.cbQueue2);
            this.gbQueue.Controls.Add(this.lblQueue2);
            this.gbQueue.Location = new System.Drawing.Point(12, 12);
            this.gbQueue.Name = "gbQueue";
            this.gbQueue.Size = new System.Drawing.Size(264, 92);
            this.gbQueue.TabIndex = 7;
            this.gbQueue.TabStop = false;
            this.gbQueue.Text = "Queue";
            //
            // gbSettings
            //
            this.gbSettings.Controls.Add(this.cbDisableMounting);
            this.gbSettings.Controls.Add(this.btnHeatmapWindow);
            this.gbSettings.Controls.Add(this.lblPullDistance);
            this.gbSettings.Controls.Add(this.cbLootCorpses);
            this.gbSettings.Controls.Add(this.nudPullDistance);
            this.gbSettings.Location = new System.Drawing.Point(12, 110);
            this.gbSettings.Name = "gbSettings";
            this.gbSettings.Size = new System.Drawing.Size(264, 116);
            this.gbSettings.TabIndex = 8;
            this.gbSettings.TabStop = false;
            this.gbSettings.Text = "Settings";
            //
            // btnHeatmapWindow
            //
            this.btnHeatmapWindow.Location = new System.Drawing.Point(151, 87);
            this.btnHeatmapWindow.Name = "btnHeatmapWindow";
            this.btnHeatmapWindow.Size = new System.Drawing.Size(107, 23);
            this.btnHeatmapWindow.TabIndex = 9;
            this.btnHeatmapWindow.Text = "HeatMap Window";
            this.btnHeatmapWindow.UseVisualStyleBackColor = true;
            this.btnHeatmapWindow.Visible = false;
            this.btnHeatmapWindow.Click += new System.EventHandler(this.btnHeatmapWindow_Click);
            //
            // cbDisableMounting
            //
            this.cbDisableMounting.AutoSize = true;
            this.cbDisableMounting.Enabled = false;
            this.cbDisableMounting.Location = new System.Drawing.Point(21, 54);
            this.cbDisableMounting.Name = "cbDisableMounting";
            this.cbDisableMounting.Size = new System.Drawing.Size(108, 17);
            this.cbDisableMounting.TabIndex = 10;
            this.cbDisableMounting.Text = "Disable Mounting";
            this.cbDisableMounting.UseVisualStyleBackColor = true;
            //
            // ConfigWindow
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(288, 234);
            this.Controls.Add(this.gbSettings);
            this.Controls.Add(this.gbQueue);
            this.Name = "ConfigWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BGBuddy Config";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConfigWindow_FormClosing);
            this.Load += new System.EventHandler(this.ConfigWindow_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudPullDistance)).EndInit();
            this.gbQueue.ResumeLayout(false);
            this.gbQueue.PerformLayout();
            this.gbSettings.ResumeLayout(false);
            this.gbSettings.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cbQueue1;
        private System.Windows.Forms.ComboBox cbQueue2;
        private System.Windows.Forms.Label lblQueue1;
        private System.Windows.Forms.Label lblQueue2;
        private System.Windows.Forms.CheckBox cbLootCorpses;
        private System.Windows.Forms.Label lblPullDistance;
        private System.Windows.Forms.NumericUpDown nudPullDistance;
        private System.Windows.Forms.GroupBox gbQueue;
        private System.Windows.Forms.GroupBox gbSettings;
        private System.Windows.Forms.Button btnHeatmapWindow;
        private System.Windows.Forms.CheckBox cbDisableMounting;


    }
}
