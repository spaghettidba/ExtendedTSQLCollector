using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sqlconsulting.DataCollector.Utils;
using System.Data.SqlClient;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public partial class CollectorTypeControl : UserControl
    {
        private Main _main;

        public CollectorTypeControl(Main main)
        {
            _main = main;
            InitializeComponent();
        }


        public CollectorTypeControl(Guid collectorTypeUid, Main main) : this(main)
        {
            fillValues(collectorTypeUid);
        }



        private void fillValues(Guid collectorTypeUid)
        {
            String sql = @"
                SELECT *
                FROM msdb.dbo.syscollector_collector_types
                WHERE collector_type_uid = '{0}'
            ";
            sql = String.Format(sql, collectorTypeUid.ToString());
            DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", sql);

            foreach (DataRow dr in dt.Rows)
            {
                textBox1.Text = dr["collector_type_uid"].ToString();
                textBox2.Text = dr["name"].ToString();
                textBox3.Text = dr["parameter_schema"].ToString();
                textBox4.Text = dr["parameter_formatter"].ToString();
                textBox5.Text = dr["collection_package_path"].ToString();
                button1.Tag = dr["collection_package_id"].ToString();
                textBox6.Text = dr["upload_package_path"].ToString();
                button2.Tag = dr["upload_package_id"].ToString();
                checkBox1.Checked = Boolean.Parse(dr["is_system"].ToString());
            }

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            String verb = "create";
            if(textBox1.Text.Length==0)
            {
                verb = "create";
            }
            else {
                verb = "update";
            }

            String sql = @"dbo.sp_syscollector_{0}_collector_type";
            sql = String.Format(sql, verb);

            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", Manager.ServerName, "msdb", ConnectionTimeout);

            Guid collector_type_uid = new Guid();

            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = ConnectionString;

                try
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = QueryTimeout;
                    SqlCommandBuilder.DeriveParameters(cmd);

                    if (verb.Equals("update"))
                    {
                        collector_type_uid = new Guid(textBox1.Text);
                        cmd.Parameters["@collector_type_uid"].Value = collector_type_uid; 
                    }
                    cmd.Parameters["@name"].Value = textBox2.Text;
                    cmd.Parameters["@parameter_schema"].Value = textBox3.Text;
                    cmd.Parameters["@parameter_formatter"].Value = textBox4.Text;
                    cmd.Parameters["@collection_package_id"].Value = new Guid(button1.Tag.ToString());
                    cmd.Parameters["@upload_package_id"].Value = new Guid(button2.Tag.ToString());


                    cmd.ExecuteNonQuery();

                    if (verb.Equals("create"))
                    {
                        collector_type_uid = new Guid(cmd.Parameters["@collector_type_uid"].Value.ToString()); 
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    conn.Close();
                }

            }

            fillValues(collector_type_uid);
            _main.RefreshTreeView();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SelectPackageDialog dialog = new SelectPackageDialog();
            dialog.ShowDialog(this);
            if(dialog.DialogResult.Equals(DialogResult.OK))
            {
                button1.Tag = dialog.SelectedPackage;
                textBox5.Text = dialog.SelectedPackagePath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SelectPackageDialog dialog = new SelectPackageDialog();
            dialog.ShowDialog(this);
            if (dialog.DialogResult.Equals(DialogResult.OK))
            {
                button2.Tag = dialog.SelectedPackage;
                textBox6.Text = dialog.SelectedPackagePath;
            }
        }
    }
}
