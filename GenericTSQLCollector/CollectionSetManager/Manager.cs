﻿using System;
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
            String serverName = textBox1.Text;
            installCollectorType(serverName);
        }

        private void installCollectorType(String serverName)
        {
            try
            {
                Sqlconsulting.DataCollector.InstallCollectorType.CollectorTypeInstaller cti = new Sqlconsulting.DataCollector.InstallCollectorType.CollectorTypeInstaller(serverName);
                cti.install();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
