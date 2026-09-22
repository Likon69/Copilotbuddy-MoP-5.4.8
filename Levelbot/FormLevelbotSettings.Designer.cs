using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Levelbot
{
    public partial class FormLevelbotSettings
    {
        // Note: HB 5.4.8's InitializeComponent() drives every control property through a
        // ComponentResourceManager over FormLevelbotSettings.resources. That blob carries only
        // designer plumbing (control layout: Location/Size/TabIndex/Font, plus a default Form
        // icon and placeholder captions that SetStaticLabels() immediately overwrites at Load) -
        // no translatable content of its own. The layout values below are copied verbatim from
        // that resources blob. Following this project's own precedent for a WinForms designer
        // file with no localizable content (Styx/CommonBot/Routines/RoutineSelectionForm), the
        // values are hardcoded here instead of adding a new .resx; the default Form icon is not
        // reproduced (no icon asset exists to source it from without inventing one).
        private IContainer components;
        private SplitContainer splitContainer1;
        private Button BtnSaveAndClose;
        private Button BtnSave;
        private CheckBox cbGroundMountFarmingMode;

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
            splitContainer1 = new SplitContainer();
            cbGroundMountFarmingMode = new CheckBox();
            BtnSaveAndClose = new Button();
            BtnSave = new Button();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();

            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            splitContainer1.Panel1.Controls.Add(cbGroundMountFarmingMode);
            splitContainer1.Panel2.Controls.Add(BtnSaveAndClose);
            splitContainer1.Panel2.Controls.Add(BtnSave);
            splitContainer1.Size = new Size(217, 67);
            splitContainer1.SplitterDistance = 38;
            splitContainer1.TabIndex = 0;

            cbGroundMountFarmingMode.AutoSize = true;
            cbGroundMountFarmingMode.Font = new Font("Trebuchet MS", 8.75f);
            cbGroundMountFarmingMode.Location = new Point(12, 12);
            cbGroundMountFarmingMode.Name = "cbGroundMountFarmingMode";
            cbGroundMountFarmingMode.Size = new Size(185, 22);
            cbGroundMountFarmingMode.TabIndex = 60;
            cbGroundMountFarmingMode.Text = "Ground mount farming mode";
            cbGroundMountFarmingMode.UseVisualStyleBackColor = true;

            BtnSaveAndClose.Location = new Point(93, 3);
            BtnSaveAndClose.Name = "BtnSaveAndClose";
            BtnSaveAndClose.Size = new Size(105, 23);
            BtnSaveAndClose.TabIndex = 108;
            BtnSaveAndClose.Text = "Save and Close";
            BtnSaveAndClose.UseVisualStyleBackColor = true;
            BtnSaveAndClose.Click += BtnSaveAndClose_Click;

            BtnSave.Location = new Point(12, 3);
            BtnSave.Name = "BtnSave";
            BtnSave.Size = new Size(75, 23);
            BtnSave.TabIndex = 109;
            BtnSave.Text = "Save";
            BtnSave.UseVisualStyleBackColor = true;
            BtnSave.Click += BtnSave_Click;

            AutoScaleDimensions = new SizeF(6f, 13f);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(217, 67);
            Controls.Add(splitContainer1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormLevelbotSettings";
            Text = "Levelbot Settings";
            FormClosing += FormLevelbotSettings_FormClosing;
            Load += FormLevelbotSettings_Load;

            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
