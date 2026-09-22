namespace Bots.BGBuddy
{
    partial class HeatmapWindow
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
            this.components = new System.ComponentModel.Container();
            this.pbHeatMap = new System.Windows.Forms.PictureBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.nudEnemy = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.nudFriendly = new System.Windows.Forms.NumericUpDown();
            this.nudObjective = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.pbHeatMap)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEnemy)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudFriendly)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudObjective)).BeginInit();
            this.SuspendLayout();
            //
            // pbHeatMap
            //
            this.pbHeatMap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbHeatMap.Location = new System.Drawing.Point(1, 0);
            this.pbHeatMap.Name = "pbHeatMap";
            this.pbHeatMap.Size = new System.Drawing.Size(690, 507);
            this.pbHeatMap.TabIndex = 0;
            this.pbHeatMap.TabStop = false;
            //
            // timer1
            //
            this.timer1.Enabled = true;
            this.timer1.Interval = 2000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 515);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Enemy";
            //
            // nudEnemy
            //
            this.nudEnemy.DecimalPlaces = 2;
            this.nudEnemy.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.nudEnemy.Location = new System.Drawing.Point(52, 513);
            this.nudEnemy.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudEnemy.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.nudEnemy.Name = "nudEnemy";
            this.nudEnemy.Size = new System.Drawing.Size(120, 20);
            this.nudEnemy.TabIndex = 2;
            this.nudEnemy.ValueChanged += new System.EventHandler(this.nudEnemy_ValueChanged);
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(178, 515);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(43, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Friendly";
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(353, 515);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Objective";
            //
            // nudFriendly
            //
            this.nudFriendly.DecimalPlaces = 2;
            this.nudFriendly.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.nudFriendly.Location = new System.Drawing.Point(227, 513);
            this.nudFriendly.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudFriendly.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.nudFriendly.Name = "nudFriendly";
            this.nudFriendly.Size = new System.Drawing.Size(120, 20);
            this.nudFriendly.TabIndex = 6;
            this.nudFriendly.ValueChanged += new System.EventHandler(this.nudFriendly_ValueChanged);
            //
            // nudObjective
            //
            this.nudObjective.DecimalPlaces = 2;
            this.nudObjective.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.nudObjective.Location = new System.Drawing.Point(411, 513);
            this.nudObjective.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudObjective.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.nudObjective.Name = "nudObjective";
            this.nudObjective.Size = new System.Drawing.Size(120, 20);
            this.nudObjective.TabIndex = 7;
            this.nudObjective.ValueChanged += new System.EventHandler(this.nudObjective_ValueChanged);
            //
            // HeatmapWindow
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(693, 545);
            this.Controls.Add(this.nudObjective);
            this.Controls.Add(this.nudFriendly);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nudEnemy);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pbHeatMap);
            this.Name = "HeatmapWindow";
            this.Text = "HeatmapWindow";
            ((System.ComponentModel.ISupportInitialize)(this.pbHeatMap)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEnemy)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudFriendly)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudObjective)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbHeatMap;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudEnemy;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nudFriendly;
        private System.Windows.Forms.NumericUpDown nudObjective;
    }
}
