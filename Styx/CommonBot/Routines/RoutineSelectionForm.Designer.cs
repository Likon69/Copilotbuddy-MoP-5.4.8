using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Styx.CommonBot.Routines
{
    public partial class RoutineSelectionForm
    {
        private IContainer _components;
        private Label label1;
        private GroupBox groupBox1;
        private ListBox lstAvailable;
        private Button btnSelect;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && _components != null)
            {
                _components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            label1 = new Label();
            groupBox1 = new GroupBox();
            lstAvailable = new ListBox();
            btnSelect = new Button();
            btnCancel = new Button();
            groupBox1.SuspendLayout();
            SuspendLayout();
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(381, 31);
            label1.TabIndex = 0;
            label1.Text = "We have determined that you have multiple custom classes for your current character class, please select the one you wish to use!";
            groupBox1.Controls.Add(lstAvailable);
            groupBox1.Location = new Point(12, 43);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(381, 148);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "Available";
            lstAvailable.BackColor = SystemColors.Control;
            lstAvailable.BorderStyle = BorderStyle.None;
            lstAvailable.Dock = DockStyle.Fill;
            lstAvailable.FormattingEnabled = true;
            lstAvailable.Location = new Point(3, 16);
            lstAvailable.Name = "lstAvailable";
            lstAvailable.Size = new Size(375, 129);
            lstAvailable.TabIndex = 0;
            lstAvailable.SelectedIndexChanged += lstAvailable_SelectedIndexChanged;
            btnSelect.DialogResult = DialogResult.OK;
            btnSelect.Location = new Point(318, 197);
            btnSelect.Name = "btnSelect";
            btnSelect.Size = new Size(75, 23);
            btnSelect.TabIndex = 2;
            btnSelect.Text = "Select";
            btnSelect.UseVisualStyleBackColor = true;
            btnSelect.Click += btnSelect_Click;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(237, 197);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            AcceptButton = btnSelect;
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(405, 232);
            Controls.Add(btnCancel);
            Controls.Add(btnSelect);
            Controls.Add(groupBox1);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RoutineSelectionForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            Text = "Select a custom class";
            TopMost = true;
            Load += RoutineSelectionForm_Load;
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
