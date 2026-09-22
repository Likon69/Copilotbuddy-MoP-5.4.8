using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Styx.CommonBot.Routines
{
    public partial class RoutineSelectionForm : Form
    {
        internal RoutineSelectionForm(IEnumerable<ICombatRoutine> available)
        {
            InitializeComponent();
            lstAvailable.SuspendLayout();
            foreach (ICombatRoutine combatRoutine in available)
            {
                lstAvailable.Items.Add(combatRoutine);
            }
            lstAvailable.ResumeLayout();
            TopMost = true;
        }

        internal CombatRoutine SelectedRoutine { get; set; }

        private void RoutineSelectionForm_Load(object sender, EventArgs e)
        {
            Text = string.Format("Name : {0} - Class : {1} - {2}", StyxWoW.Me.Name, StyxWoW.Me.Class, Text);
            Application.DoEvents();
            TopLevel = true;
            TopMost = true;
        }

        private void lstAvailable_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedRoutine = lstAvailable.SelectedItem as CombatRoutine;
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            if (SelectedRoutine == null)
            {
                MessageBox.Show("You have not selected a routine to use, please do so, or hit the Cancel button!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            SelectedRoutine = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
