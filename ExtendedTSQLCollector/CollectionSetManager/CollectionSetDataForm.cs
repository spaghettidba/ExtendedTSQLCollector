using Sqlconsulting.DataCollector.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public partial class CollectionSetDataForm : Form
    {
        private Int32 collectionItemId;
        private String MDWDatabase;
        private String MDWInstance;

        private Andora.UserControlLibrary.DateRangeSlider dateRangeSlider1 = new Andora.UserControlLibrary.DateRangeSlider();

        public CollectionSetDataForm(Int32 collectionItemId)
        {
            this.collectionItemId = collectionItemId;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            InitializeComponent();
            dateRangeSlider1.OnValueChanged += new Andora.UserControlLibrary.DateRangeSlider.ValueChanged(dateRangeSlider1_ValueChanged);
            elementHost1.Child = dateRangeSlider1;

        }

        private void CollectionSetDataForm_Load(object sender, EventArgs e)
        {
            this.Icon = Owner.Icon;

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int w = Width >= screen.Width ? screen.Width : (screen.Width + Width) / 2;
            int h = Height >= screen.Height ? screen.Height : (screen.Height + Height) / 2;
            this.Location = new Point((screen.Width - w) / 2, (screen.Height - h) / 2);
            this.Size = new Size(w, h);

            FillTablesCombo();

            CollectorConfig config = new TSQLCollectorConfig();
            config.readFromDatabase(Manager.ServerName);
            MDWInstance = config.MDWInstance;
            MDWDatabase = config.MDWDatabase;

            FillTimeSlider();

            

        }

   
        private void FillTablesCombo()
        {
            String sql = @"
                SELECT nds.v.value('.','sysname') AS OutputTable
                FROM msdb.dbo.syscollector_collection_items AS ci
                CROSS APPLY parameters.nodes('//OutputTable') AS nds(v)
                WHERE collection_item_id = {0}
            ";
            sql = String.Format(sql, collectionItemId);

            DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", sql);

            foreach (DataRow dr in dt.Rows)
            {
                comboBox1.Items.Add(dr["OutputTable"]);
            }
        }

        private void FillTimeSlider() 
        {
            if (String.IsNullOrEmpty(comboBox1.Text))
                return;
            String sql = @"
                SELECT TOP(1) QUOTENAME(OBJECT_SCHEMA_NAME(object_id)) + '.' + QUOTENAME(OBJECT_NAME(object_id))
                FROM sys.tables
                WHERE name = '{0}'
                ORDER BY OBJECT_SCHEMA_NAME(object_id) DESC";
            sql = String.Format(sql, comboBox1.Text);
            String selectedTable = (String)CollectorUtils.GetScalar(MDWInstance, MDWDatabase, sql);
            
            sql = @"
                SELECT MIN(sn.snapshot_time) AS mintime, MAX(sn.snapshot_time) AS maxtime
                FROM core.snapshots AS sn
                INNER JOIN {0} AS source
	                ON source.snapshot_id = sn.snapshot_id
            ";
            sql = String.Format(sql, selectedTable);
            DataTable dt = CollectorUtils.GetDataTable(MDWInstance, MDWDatabase, sql);

            if(dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                if (dr[0] == null || dr[1] == null)
                    return;
                if (dr[0].GetType() == typeof(DBNull) || dr[1].GetType() == typeof(DBNull))
                    return;


                DateTime min = ((DateTimeOffset)dr[0]).DateTime;
                DateTime max = ((DateTimeOffset)dr[1]).DateTime;

                dateRangeSlider1.Minimum = min;
                dateRangeSlider1.Maximum = max;

                dateRangeSlider1.LowerValue = min;
                dateRangeSlider1.UpperValue = max;

                dateRangeSlider1.LowerValue = min;
                dateRangeSlider1.UpperValue = max;

            }
            
        }



        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = dateRangeSlider1.LowerValue.ToString("yyyy-MM-dd hh:mm");
            textBox2.Text = dateRangeSlider1.UpperValue.ToString("yyyy-MM-dd hh:mm");

            LoadGridData();
        }

        private void LoadGridData()
        {
            if (String.IsNullOrEmpty(comboBox1.Text))
                return;
            String sql = "SELECT SERVERPROPERTY('ServerName') AS SN";
            String SN = (String)CollectorUtils.GetScalar(Manager.ServerName, "master", sql);

            sql = @"
                SELECT TOP(1) QUOTENAME(OBJECT_SCHEMA_NAME(object_id)) + '.' + QUOTENAME(OBJECT_NAME(object_id))
                FROM sys.tables
                WHERE name = '{0}'
                ORDER BY OBJECT_SCHEMA_NAME(object_id) DESC";
            sql = String.Format(sql, comboBox1.Text);
            String selectedTable = (String)CollectorUtils.GetScalar(MDWInstance, MDWDatabase, sql);

            sql = @"
                SELECT source.*
                FROM core.snapshots AS sn
                INNER JOIN {3} AS source
	                ON source.snapshot_id = sn.snapshot_id
                WHERE sn.instance_name = '{0}'
                    AND sn.snapshot_time BETWEEN '{1}' AND '{2}'
                ORDER BY snapshot_time;
            ";
            DateTime dateFrom = DateTime.Parse(textBox1.Text);
            DateTime dateTo = DateTime.Parse(textBox2.Text);

            sql = String.Format(sql, SN, dateFrom.ToString("yyyy-MM-dd hh:mm:ss.fff"), dateTo.ToString("yyyy-MM-dd hh:mm:ss.fff"),selectedTable);
            DataTable dt = CollectorUtils.GetDataTable(MDWInstance, MDWDatabase, sql);
            dataGridView1.DataSource = dt;
        }

        private void elementHost1_ChildChanged(object sender, ChildChangedEventArgs e)
        {
            //dateRangeSlider1.ValueChanged += dateRangeSlider1_ValueChanged;
            //dateRangeSlider1.OnValueChanged += new Andora.UserControlLibrary.DateRangeSlider.ValueChanged(dateRangeSlider1_ValueChanged);
         
        }

        void dateRangeSlider1_ValueChanged(object sender, object param)
        {
            //throw new NotImplementedException();
            textBox1.Text = dateRangeSlider1.LowerValue.ToString("yyyy-MM-dd hh:mm");
            textBox2.Text = dateRangeSlider1.UpperValue.ToString("yyyy-MM-dd hh:mm");
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            FillTimeSlider();
        }



    }
}
