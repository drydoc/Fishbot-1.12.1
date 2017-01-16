//////////////////////////////////////////////////
//                                              //
//   See License.txt for Licensing information  //
//                                              //
//////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using FishBot;

namespace PixelMagic.GUI
{
    public partial class SelectWoWProcessToAttachTo : Form
    {
        private MainWindow parent;

        public SelectWoWProcessToAttachTo(MainWindow parent)
        {
            this.parent = parent;
            InitializeComponent();
        }

        private void refreshProcessList()
        {
            cmbWoW.Items.Clear();
            
            var processes = Process.GetProcessesByName("Wow");

            foreach (var process in processes)
            {
                cmbWoW.Items.Add($"WoW x86 [Vanilla] => {process.Id}");
            }
            
            if (cmbWoW.Items.Count > 0)
            {
                cmbWoW.SelectedIndex = 0;
                cmbWoW.Enabled = true;
                cmdConnect.Enabled = true;
            }
            else
            {
                cmbWoW.Items.Add("Please open WoW then click 'Refresh' button.");
                cmbWoW.SelectedIndex = 0;
                cmbWoW.Enabled = false;
                cmdConnect.Enabled = false;
            }
        }

        private void SelectWoWProcessToAttachTo_Load(object sender, EventArgs e)
        {
            cmbWoW.KeyDown += CmbWoW_KeyDown;
            FormClosing += SelectWoWProcessToAttachTo_FormClosing;

            cmbWoW.Focus();

            refreshProcessList();            
        }

        private void SelectWoWProcessToAttachTo_FormClosing(object sender, FormClosingEventArgs e)
        {
            //parent.process = null;            
        }

        private void CmbWoW_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                cmdConnect.PerformClick();
            }
        }
        
        private void cmdConnect_Click(object sender, EventArgs e)
        {
            if (cmbWoW.Text.Contains(">"))
            {
                var PID = int.Parse(cmbWoW.Text.Split('>')[1]);
                parent.process = Process.GetProcessById(PID);
                
                Close();
            }
            else
            {
                MessageBox.Show("Please select a valid wow process to connect to", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmdRefresh_Click(object sender, EventArgs e)
        {
            refreshProcessList();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            //parent.process = null;
            Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
