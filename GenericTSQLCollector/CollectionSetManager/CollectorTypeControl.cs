using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sqlconsulting.DataCollector.Utils;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public partial class CollectorTypeControl : UserControl
    {
        public CollectorTypeControl(Guid collectorTypeUid)
        {
            InitializeComponent();
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
    }
}
