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
using System.Threading;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public partial class CollectionSetControl : UserControl
    {

        private Main _main;

        public CollectionSetControl(Main main)
        {
            _main = main;
            InitializeComponent();

            // Fill Combo for Collection Modes
            comboBox1.Items.Clear();
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.Items.Add(new ComboboxItem(0, "Cached"));
            comboBox1.Items.Add(new ComboboxItem(1, "Non-Cached"));
            comboBox1.SelectedIndex = 0;

            // Fill Combo for proxies
            FillCombo(comboBox2, "SELECT proxy_id, name FROM msdb.dbo.sysproxies", true);

            // Fill Combo for schedules
            FillCombo(comboBox3, "SELECT schedule_uid, name FROM msdb.dbo.sysschedules WHERE name LIKE 'CollectorSchedule[_]%'");

            // Fill Combo for Loggin Levels
            comboBox4.Items.Clear();
            comboBox4.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox4.Items.Add(new ComboboxItem(0, "0"));
            comboBox4.Items.Add(new ComboboxItem(1, "1"));
            comboBox4.Items.Add(new ComboboxItem(2, "2"));
            comboBox4.SelectedIndex = 0;
        }


        public CollectionSetControl(Guid collectionSetUid, Main main) : this(main)
        {
            fillValues(collectionSetUid);
        }


        private void FillCombo(ComboBox cb, String sql, Boolean nullable)
        {
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", sql);
            cb.Items.Clear();
            if (nullable)
            {
                cb.Items.Add(new ComboboxItem(null, ""));
            }
            foreach (DataRow dr in dt.Rows)
            {
                cb.Items.Add(new ComboboxItem(dr[0], dr[1].ToString()));
            }
            cb.SelectedIndex = 0;
        }

        private void FillCombo(ComboBox cb, String sql)
        {
            FillCombo(cb, sql, false);
        }

        private void ComboSetValue(ComboBox cb, object value)
        {
            for (int index = 0; index < cb.Items.Count; index++)
            {
                ComboboxItem currentValue = (ComboboxItem)cb.Items[index];
                if (value.Equals(currentValue.Value))
                {
                    cb.SelectedIndex = index;
                }
            }
            
        }


        private void fillValues(Guid collectionSetUid)
        {
            String sql = @"
                SELECT *
                FROM msdb.dbo.syscollector_collection_sets
                WHERE collection_set_uid = '{0}'
            ";
            sql = String.Format(sql, collectionSetUid.ToString());
            DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", sql);

            foreach (DataRow dr in dt.Rows)
            {
                textBox1.Text = dr["collection_set_id"].ToString();
                textBox7.Text = dr["collection_set_uid"].ToString();
                textBox2.Text = dr["name"].ToString();
                ComboSetValue(comboBox1,Convert.ToInt32(dr["collection_mode"]));
                if (!(dr["proxy_id"] == DBNull.Value))
                {
                    ComboSetValue(comboBox2, Convert.ToInt32(dr["proxy_id"]));
                }
                ComboSetValue(comboBox3, dr["schedule_uid"]);
                ComboSetValue(comboBox4, Convert.ToInt32(dr["logging_level"]));
                textBox3.Text = dr["days_until_expiration"].ToString();
                textBox4.Text = dr["description"].ToString();
                checkBox1.Checked = Boolean.Parse(dr["is_system"].ToString());
                if (Convert.ToInt32(dr["is_running"]) == 0)
                {
                    if (textBox1.Text != "")
                    {
                        textBox5.Text = "Not running";
                        button1.Enabled = true;
                        button2.Enabled = false;
                    }
                }
                else 
                {
                    if (textBox1.Text != "")
                    {
                        textBox5.Text = "Running";
                        button1.Enabled = false;
                        button2.Enabled = true;
                    }
                }
                
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

            String sql = @"dbo.sp_syscollector_{0}_collection_set";
            sql = String.Format(sql, verb);

            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", Manager.ServerName, "msdb", ConnectionTimeout);

            Guid collection_set_uid = new Guid();
            int collection_set_id = 0;

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
                        collection_set_uid = new Guid(textBox7.Text);
                        collection_set_id = Convert.ToInt32(textBox1.Text);
                        //cmd.Parameters["@collection_set_uid"].Value = collection_set_uid;
                        cmd.Parameters["@collection_set_id"].Value = collection_set_id;
                        cmd.Parameters["@new_name"].Value = textBox2.Text;
                    }
                    else
                    {
                        cmd.Parameters["@collection_set_id"].Direction = ParameterDirection.Output;
                        cmd.Parameters["@name"].Value = textBox2.Text;
                    }
                    cmd.Parameters["@collection_mode"].Value = ((ComboboxItem)comboBox1.SelectedItem).Value;
                    if (comboBox2.SelectedItem != null)
                    {
                        cmd.Parameters["@proxy_id"].Value = ((ComboboxItem)comboBox2.SelectedItem).Value;
                    }
                    cmd.Parameters["@schedule_uid"].Value = ((ComboboxItem)comboBox3.SelectedItem).Value;
                    cmd.Parameters["@logging_level"].Value = ((ComboboxItem)comboBox4.SelectedItem).Value;
                    cmd.Parameters["@days_until_expiration"].Value = textBox3.Text;
                    cmd.Parameters["@description"].Value = textBox4.Text;


                    cmd.ExecuteNonQuery();

                    if (verb.Equals("create"))
                    {
                        collection_set_uid = new Guid(cmd.Parameters["@collection_set_uid"].Value.ToString());
                        collection_set_id = Convert.ToInt32(cmd.Parameters["@collection_set_id"].Value.ToString());
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

            fillValues(collection_set_uid);
            _main.RefreshTreeView();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String sql = @"dbo.sp_syscollector_start_collection_set";

            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", Manager.ServerName, "msdb", ConnectionTimeout);

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


                    cmd.Parameters["@collection_set_id"].Value = Convert.ToInt32(textBox1.Text);
                    cmd.ExecuteNonQuery();

                    Thread.Sleep(1000);
                    button1.Enabled = false;
                    button2.Enabled = true;
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


            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String sql = @"dbo.sp_syscollector_stop_collection_set";

            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", Manager.ServerName, "msdb", ConnectionTimeout);

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


                    cmd.Parameters["@collection_set_id"].Value = Convert.ToInt32(textBox1.Text);
                    cmd.ExecuteNonQuery();

                    Thread.Sleep(1000);
                    button1.Enabled = true;
                    button2.Enabled = false; 
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


            
        }

        

    }
}
