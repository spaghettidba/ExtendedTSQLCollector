using Sqlconsulting.DataCollector.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public partial class XEReaderParametersEditor : Form
    {

        public string ReturnValue { get; set; }

        public XEReaderParametersEditor(string text)
        {
            InitializeComponent();

            ReturnValue = text;

            initData(text);
        }

        private void XEReaderParametersEditor_Load(object sender, EventArgs e)
        {
            this.Icon = Owner.Icon;
        }

        private void initData(string text)
        {
            XEReaderCollectionItemConfig cfg = parse(text);

            // XE Session
            textBox1.Text = cfg.SessionName;
            textBox2.Text = Regex.Replace(cfg.SessionDefinition, @"\r\n?|\n", "\r\n");
            textBox3.Text = cfg.OutputTable;
            textBox5.Text = cfg.Filter;
            textBox4.Text = String.Join(",", cfg.Columns.ToArray());

            // Alert
            if (cfg.Alerts.Count > 0)
            {
                AlertConfig ac = cfg.Alerts[0];
                textBox6.Text = ac.Sender;
                textBox7.Text = ac.Recipient;
                textBox8.Text = ac.Subject;
                foreach (string itm in comboBox2.Items)
                {
                    if (itm.Equals(ac.Importance.ToString(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        comboBox2.SelectedItem = itm;
                    }
                }
                textBox10.Text = String.Join(",", ac.Columns.ToArray());
                textBox11.Text = ac.Filter;
                checkBox1.Checked = ac.Enabled;
                checkBox2.Checked = ac.WriteToErrorLog;
                checkBox3.Checked = ac.WriteToWindowsLog;
                foreach (string itm in comboBox1.Items)
                {
                    if (itm.Equals(ac.Mode.ToString(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        comboBox1.SelectedItem = itm;
                    }
                }
                textBox12.Text = ac.Delay.ToString();
            }
        }

        private XEReaderCollectionItemConfig parse(string text)
        {
            string sql = @"
                WITH XMLNAMESPACES('DataCollectorType' AS ns),
                XESession AS (
	                SELECT 
		                x.value('Name[1]','varchar(max)')         AS xe_session_name,
		                x.value('OutputTable[1]', 'varchar(max)') AS outputTable,
		                x.value('Definition[1]', 'varchar(max)')  AS xe_session_definition,
		                x.value('Filter[1]', 'varchar(max)')      AS xe_session_filter,
		                x.value('ColumnsList[1]', 'varchar(max)') AS xe_session_columnslist
	                FROM @x.nodes('/ns:ExtendedXEReaderCollector/Session') Q(x)
                ),
                XEAlert AS (
	                SELECT 
		                x.value('Sender[1]','varchar(max)')              AS alert_sender,
		                x.value('Recipient[1]','varchar(max)')           AS alert_recipient,
		                x.value('Filter[1]', 'varchar(max)')             AS alert_filter,
		                x.value('ColumnsList[1]', 'varchar(max)')        AS alert_columnslist,
		                x.value('Mode[1]', 'varchar(max)')               AS alert_mode,
		                x.value('Importance[1]', 'varchar(max)')         AS alert_importance_level,
		                x.value('Delay[1]', 'varchar(max)')              AS alert_delay,
		                x.value('Subject[1]', 'varchar(max)')            AS alert_subject,
		                x.value('Body[1]', 'varchar(max)')               AS alert_body,
		                x.value('AttachmentFileName[1]', 'varchar(max)') AS alert_attachment_filename,
		                x.value('@WriteToERRORLOG[1]', 'varchar(max)')   AS alert_write_to_errorlog,
		                x.value('@WriteToWindowsLog[1]', 'varchar(max)') AS alert_write_to_windowslog,
		                x.value('@Enabled[1]', 'varchar(max)')           AS alert_enabled
	                FROM @x.nodes('/ns:ExtendedXEReaderCollector/Alert') Q(x)
                )
                SELECT *
                FROM XESession
                CROSS JOIN XEAlert
            ";
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", Manager.ServerName, "msdb", ConnectionTimeout);

            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = ConnectionString;
                XEReaderCollectionItemConfig cfg = new XEReaderCollectionItemConfig();
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


                    if (dt.Rows.Count > 0)
                    {
                        DataRow currentRow = dt.Rows[0];

                        cfg.SessionName = currentRow["xe_session_name"].ToString();
                        cfg.OutputTable = currentRow["outputTable"].ToString();
                        cfg.SessionDefinition = currentRow["xe_session_definition"].ToString();
                        cfg.Filter = currentRow["xe_session_filter"].ToString();
                        cfg.Columns = new List<String>(currentRow["xe_session_columnslist"].ToString().Split(','));
                        cfg.Enabled = true;

                        AlertConfig a = new AlertConfig();
                        a.Sender = currentRow["alert_sender"].ToString();
                        a.Recipient = currentRow["alert_recipient"].ToString();
                        a.Filter = currentRow["alert_filter"].ToString();
                        a.Columns = new List<String>(currentRow["alert_columnslist"].ToString().Split(','));
                        a.Mode = (Sqlconsulting.DataCollector.Utils.AlertMode)
                            Enum.Parse(typeof(Sqlconsulting.DataCollector.Utils.AlertMode), currentRow["alert_mode"].ToString());
                        a.Importance = (Sqlconsulting.DataCollector.Utils.ImportanceLevel)
                            Enum.Parse(typeof(Sqlconsulting.DataCollector.Utils.ImportanceLevel), currentRow["alert_importance_level"].ToString());
                        a.Delay = Int32.Parse(currentRow["alert_delay"].ToString());
                        a.Subject = currentRow["alert_subject"].ToString();
                        a.WriteToErrorLog = Boolean.Parse(currentRow["alert_write_to_errorlog"].ToString());
                        a.WriteToWindowsLog = Boolean.Parse(currentRow["alert_write_to_windowslog"].ToString());
                        a.Enabled = Boolean.Parse(currentRow["alert_enabled"].ToString());

                        cfg.Alerts.Add(a);
                    }
                }
                finally
                {
                    conn.Close();
                }
                return cfg;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ReturnValue = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n";
            ReturnValue += "<ns:ExtendedXEReaderCollector xmlns:ns=\"DataCollectorType\">\r\n";
            ReturnValue += new XElement("Session",
                        new XElement("Name", textBox1.Text),
                        new XElement("OutputTable", textBox3.Text),
                        new XElement("Definition", textBox2.Text),
                        new XElement("Filter", textBox5.Text),
                        new XElement("ColumnsList", textBox4.Text));

            
            XElement alertXElement = new XElement("Alert",
                       new XElement("Sender", textBox6.Text),
                       new XElement("Recipient", textBox7.Text),
                       new XElement("Subject", textBox8.Text),
                       new XElement("Importance", comboBox2.SelectedItem),
                       new XElement("ColumnsList", textBox10.Text),
                       new XElement("Filter", textBox11.Text),
                       new XElement("Mode", comboBox1.SelectedItem),
                       new XElement("Delay", textBox12.Text));

            alertXElement.SetAttributeValue("Enabled", checkBox1.Checked);
            alertXElement.SetAttributeValue("WriteToERRORLOG", checkBox2.Checked);
            alertXElement.SetAttributeValue("WriteToWindowsLog", checkBox3.Checked);

            ReturnValue += alertXElement;
            ReturnValue += "</ns:ExtendedXEReaderCollector>";

            this.Close();
        }
    }
}
