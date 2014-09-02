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
    public partial class CollectionItemControl : UserControl
    {
        private Main _main;
        private Guid _collectionSetUid;

        public CollectionItemControl(Main main)
        {
            InitializeComponent();
            _main = main;
            // Fill Combo for Collector Types
            FillCombo(comboBox1, "SELECT collector_type_uid, name FROM syscollector_collector_types ORDER BY name");

        }

        public CollectionItemControl(Guid collectionSetUid, Main main) : this(main)
        {
            _collectionSetUid = collectionSetUid;
            String sql = @"
                SELECT collection_set_id
                FROM syscollector_collection_sets
                WHERE collection_set_uid = '{0}'
            ";
            sql = String.Format(sql, _collectionSetUid);
            int collectionSetId = Convert.ToInt32(CollectorUtils.GetScalar(Manager.ServerName, "msdb", sql));
            textBox1.Text = collectionSetId.ToString();
        }

        public CollectionItemControl(int collectionItemId, Main main) : this(main)
        {
            fillValues(collectionItemId);
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


        private void fillValues(int collectionItemId)
        {
            String sql = @"
                SELECT *
                FROM msdb.dbo.syscollector_collection_items
                WHERE collection_item_id = '{0}'
            ";
            sql = String.Format(sql, collectionItemId);
            DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", sql);

            foreach (DataRow dr in dt.Rows)
            {
                textBox1.Text = dr["collection_set_id"].ToString();
                textBox7.Text = dr["collection_item_id"].ToString();
                textBox2.Text = dr["name"].ToString();
                ComboSetValue(comboBox1,(Guid)dr["collector_type_uid"]);
                textBox3.Text = dr["frequency"].ToString();
                textBox4.Text = dr["parameters"].ToString();
            }

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            String verb = "create";
            if(textBox7.Text.Length==0)
            {
                verb = "create";
            }
            else {
                verb = "update";
            }

            String sql = @"dbo.sp_syscollector_{0}_collection_item";
            sql = String.Format(sql, verb);

            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", Manager.ServerName, "msdb", ConnectionTimeout);

            int collection_item_id = -1;
            int collection_set_id;

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
                        collection_item_id = Convert.ToInt32(textBox7.Text);
                        collection_set_id = Convert.ToInt32(textBox1.Text);
                        //cmd.Parameters["@collection_set_id"].Value = collection_set_id;
                        cmd.Parameters["@collection_item_id"].Value = collection_item_id;
                        cmd.Parameters["@new_name"].Value = textBox2.Text;
                    }
                    else
                    {
                        collection_set_id = Convert.ToInt32(textBox1.Text);
                        cmd.Parameters["@collection_set_id"].Value = collection_set_id;
                        cmd.Parameters["@name"].Value = textBox2.Text;
                        cmd.Parameters["@collector_type_uid"].Value = ((ComboboxItem)comboBox1.SelectedItem).Value;
                        cmd.Parameters["@collection_item_id"].Direction = ParameterDirection.Output;
                    }
                    
                    cmd.Parameters["@frequency"].Value = Convert.ToInt32(textBox3.Text);
                    cmd.Parameters["@parameters"].Value = textBox4.Text;


                    cmd.ExecuteNonQuery();

                    if (verb.Equals("create"))
                    {
                        collection_item_id = Convert.ToInt32(cmd.Parameters["@collection_set_id"].Value.ToString());
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

            fillValues(collection_item_id);
            _main.RefreshTreeView();
        }

    }
}
