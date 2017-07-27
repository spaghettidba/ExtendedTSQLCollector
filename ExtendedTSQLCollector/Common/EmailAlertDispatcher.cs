using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using System.Reflection;
using System.Data.SqlClient;

namespace Sqlconsulting.DataCollector.Utils
{
    public class EmailAlertDispatcher : AlertDispatcher
    {
        public String Sender { get; set; }
        public String Recipient { get; set; }
        public String Subject { get; set; }

        public Sqlconsulting.DataCollector.Utils.ImportanceLevel Importance { get; set; }

        public EmailAlertDispatcher(String serverName, AlertConfig cfg, DataRow row) : base(serverName, cfg, row)
        {
            Sender = cfg.Sender;
            Recipient = cfg.Recipient;
            Subject = cfg.Subject;
        }


        public override void dispatch()
        {
            // Create empty DataTable and add the row to process
            DataTable table = _row.Table.Clone();
            table.Rows.Add(_row.ItemArray);

            // Remove the columns not included in the output
            List<String> columnsToRemove = new List<String>();
            foreach (DataColumn dc in table.Columns)
            {
                if (!_config.Columns.Contains(dc.ColumnName))
                {
                    columnsToRemove.Add(dc.ColumnName);
                }
            }
            foreach (String colName in columnsToRemove)
            {
                table.Columns.Remove(colName);
            }


            // Transform the DataTable to HTML
            string xmlString = string.Empty;
            using (TextWriter writer = new StringWriter())
            {
                table.WriteXml(writer);
                xmlString = writer.ToString();
            }
            
            XDocument result = new XDocument();
            using (XmlWriter writer = result.CreateWriter())
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                String xsltString = Common.Properties.Resources.DataTableHtmlXslt;
                xslt.Load(XmlReader.Create(new StringReader(xsltString)));
                xslt.Transform(XDocument.Parse(xmlString).CreateReader(), writer);
            }

            string htmlDocument =
                  "<STYLE>"
                + Common.Properties.Resources.HtmlTableStyle
                + "</STYLE>" 
                + result.ToString();


            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", _serverName, "msdb", ConnectionTimeout);
            String sql = @"msdb.dbo.sp_send_dbmail";

            using(SqlConnection conn = new SqlConnection()) {
                conn.ConnectionString = ConnectionString;

                try
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = QueryTimeout;
                    SqlCommandBuilder.DeriveParameters(cmd);

                    cmd.Parameters["@profile_name"].Value = Sender;
                    cmd.Parameters["@recipients"].Value = Recipient;
                    cmd.Parameters["@subject"].Value = Subject;
                    cmd.Parameters["@body"].Value = htmlDocument;
                    cmd.Parameters["@body_format"].Value = "HTML";

                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    conn.Close();
                }

            }

            return;
        }
    }
}
