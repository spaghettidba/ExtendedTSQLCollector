﻿using Sqlconsulting.DataCollector.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public partial class Manager : Form
    {
        public static String ServerName;

        
        public Manager()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openMainForm();
        }

        private void openMainForm()
        {
            ServerName = textBox1.Text;
            //installCollectorType(serverName);
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                CollectorUtils.CheckConnection(ServerName);
                Cursor.Current = Cursors.Default;
                (new Main()).Show();
                Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void installCollectorType(String serverName)
        {
            Sqlconsulting.DataCollector.InstallCollectorType.CollectorTypeInstaller cti = new Sqlconsulting.DataCollector.InstallCollectorType.CollectorTypeInstaller(serverName);
            try
            {
                if (!cti.checkPermissions())
                {
                    MessageBox.Show("Permission denied. The collector type can be installed only by a member of the 'sysadmin' fixed server role.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    cti.install();
                    MessageBox.Show("Collector type installed successfully.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                openMainForm();
            }
        }

        private void Manager_Shown(object sender, EventArgs e)
        {
            this.textBox1.SelectAll();
            this.textBox1.Focus();
            this.textBox3.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            (new AboutBox()).ShowDialog();
        }



    }
}
