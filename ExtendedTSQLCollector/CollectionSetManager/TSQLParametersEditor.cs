using Sqlconsulting.DataCollector.CollectionSetManager;
using Sqlconsulting.DataCollector.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public partial class TSQLParametersEditor : Form
    {

        public string ReturnValue { get; set; }

        public TSQLParametersEditor(string text)
        {
            InitializeComponent();

            ReturnValue = text;

            initDataGrid(text);
        }

        private void initDataGrid(string text)
        {
            try
            {
                TSQLCollectionItemConfig[] items = parse(text);
                for(int i=0;i<items.Length;i++)
                {
                    string[] contents = new string[2];
                    contents[0] = items[i].OutputTable;
                    contents[1] = items[i].Query;
                    dataGridView1.Rows.Add(contents);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TSQLParametersEditor_Load(object sender, EventArgs e)
        {
            this.Icon = Owner.Icon;
        }

        private TSQLCollectionItemConfig[] parse(string text)
        {
            String sql = @"
                ;WITH XMLNAMESPACES('DataCollectorType' AS ns)
                SELECT x.value('Value[1]','varchar(max)') AS query,
	                x.value('OutputTable[1]', 'varchar(max)') AS outputTable
                FROM @x.nodes('/ns:TSQLQueryCollector/Query') Q(x)
                ORDER BY outputTable;
            ";

            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", Manager.ServerName, "msdb", ConnectionTimeout);

            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = ConnectionString;
                TSQLCollectionItemConfig[] results;

                try
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = QueryTimeout;
                    cmd.Parameters.Add("x", SqlDbType.Xml, -1);
                    cmd.Parameters[0].Value = text;

                    DataSet ds = new DataSet();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    DataTable dt = ds.Tables[0];

                    results = new TSQLCollectionItemConfig[dt.Rows.Count];

                    for (int i = 0; i < dt.Rows.Count;i++ )
                    {
                        DataRow dr = dt.Rows[i];
                        TSQLCollectionItemConfig cfg = new TSQLCollectionItemConfig();
                        cfg.Query = dr["query"].ToString();
                        cfg.OutputTable = dr["outputTable"].ToString();
                        results[i] = cfg;
                    }

                }
                finally
                {
                    conn.Close();
                }
                return results;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ReturnValue =  "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n";
            ReturnValue += "<ns:TSQLQueryCollector xmlns:ns=\"DataCollectorType\">\r\n";

            foreach(DataGridViewRow row in dataGridView1.Rows)
            {
                if (!String.IsNullOrEmpty((String)row.Cells[0].Value) || !String.IsNullOrEmpty((String)row.Cells[1].Value))
                ReturnValue += new XElement("Query",
                    new XElement("Value", row.Cells[1].Value),
                    new XElement("OutputTable", row.Cells[0].Value));
            }

            ReturnValue += "</ns:TSQLQueryCollector>";

            this.Close();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        private void dataGridView1_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {

                DataGridViewCell cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                Object value = cell.Value;
                String text = value == null ? "" : value.ToString();
                XMLEditor dialog = new XMLEditor(text, "SQL");
                dialog.ShowDialog(this);
                cell.Value = dialog.ReturnValue;
            }
        }
    }
}
