using Sqlconsulting.DataCollector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;


namespace Sqlconsulting.DataCollector.Utils
{
    public class CollectorUtils
    {

       





        /*
         * Invokes a SQL command
         */
        public static void InvokeSqlCmd(
            String ServerInstance,
            String Database,
            String Query
        )
        {
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", ServerInstance, Database, ConnectionTimeout);

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;

            try
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(Query, conn);
                cmd.CommandTimeout = QueryTimeout;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                conn.Close();
            }
        }



        public static void InvokeSqlBatch(
            String ServerInstance,
            String Database,
            String Query
        )
        {
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", ServerInstance, Database, ConnectionTimeout);

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;
            Server server = new Server(new ServerConnection(conn));
            server.ConnectionContext.StatementTimeout = QueryTimeout;
            server.ConnectionContext.ExecuteNonQuery(Query);
        }




        /*
         * Invokes a SQL query and returns the results
         */
        public static DataTable GetDataTable(
            String ServerInstance,
            String Database,
            String Query
        )
        {
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", ServerInstance, Database, ConnectionTimeout);

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;

            DataSet ds = new DataSet();
            try
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(Query, conn);
                cmd.CommandTimeout = QueryTimeout;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
            }
            finally
            {
                conn.Close();
            }
            return ds.Tables[0];
        }



        public static void WriteDataTable(
           String ServerInstance,
           String Database,
           String TableName,
           DataTable Data,
           int BatchSize,
           int QueryTimeout,
           int ConnectionTimeout
           )
        {

            String ConnectionString;
            ConnectionString = "Server={0};Database={1};Integrated Security=True;Connect Timeout={2}";
            ConnectionString = String.Format(ConnectionString, ServerInstance, Database, ConnectionTimeout);



            using (SqlBulkCopy bulkCopy = new System.Data.SqlClient.SqlBulkCopy(ConnectionString,
                    SqlBulkCopyOptions.KeepIdentity |
                    SqlBulkCopyOptions.FireTriggers |
                    SqlBulkCopyOptions.CheckConstraints |
                    SqlBulkCopyOptions.TableLock))
            {

                bulkCopy.DestinationTableName = TableName;
                bulkCopy.BatchSize = BatchSize;
                bulkCopy.BulkCopyTimeout = QueryTimeout;

                // enters this code block when using this collector type for system collection sets
                // the database_name column is not added automatically by the "Generic T-SQL Query Collector"
                // so we have to ignore its "__database_name" counterpart
                // added automatically when the "database_name" column (no underscores) already exists
                if(TableName.StartsWith("[snapshots].")) 
                {
                    
                    foreach(string dbcol in getColumns(ServerInstance, Database, TableName))
                    {
                        if (Data.Columns.Contains("__" + dbcol))
                        {
                            bulkCopy.ColumnMappings.Add("__" + dbcol, dbcol);
                        }
                        else if (Data.Columns.Contains(dbcol))
                        {
                            bulkCopy.ColumnMappings.Add(dbcol,dbcol);
                        }
                    }
                    
                }

                bulkCopy.WriteToServer(Data);
            }
        }

        public static void WriteDataTable(
            String ServerInstance,
            String Database,
            String TableName,
            DataTable Data
            )
        {
            WriteDataTable(ServerInstance, Database, TableName, Data, 50000, 0, 15);
        }


        /*
         * Generates a CREATE TABLE script
         */
        public static String ScriptTable(
                String ServerInstance,
                String Database,
                String TableName
            )
        {

            String output = System.Environment.GetEnvironmentVariable("temp") + TableName + ".txt";

            Server srv = new Server(ServerInstance);
            Database db = new Database();

            db = srv.Databases[Database];


            ScriptingOptions options = new ScriptingOptions();
            options.ClusteredIndexes = true;
            options.Default = true;
            options.DriAll = false;
            options.Indexes = true;
            options.IncludeHeaders = false;

            StringCollection scripts = new StringCollection();

            foreach(Table tbl in db.Tables)
            {
                if (tbl.Name.Equals(TableName))
                {
                    scripts = tbl.Script(options);
                    break;
                }
            }

            String results = "";
            foreach (string s in scripts)
            {
                results += s + "\n";
            }
            return results;
        }



        public static string getCacheFilePrefix(string SourceServerInstance, Guid CollectionSetUid, int ItemId)
        {
            String results = "";
            String qry = @"
                SELECT MachineName + '_' + 'MSSQL' + Major + CASE Minor WHEN '50' THEN '_' + Minor ELSE '' END + '.' + ServiceName AS prefix
                FROM (
                       SELECT LEFT(ProductVersion,ISNULL(NULLIF(CHARINDEX('.',ProductVersion,1)-1,-1),LEN(ProductVersion))) AS Major,
                              SUBSTRING(ProductVersion,ISNULL(NULLIF(CHARINDEX('.',ProductVersion,4)-2,-2),LEN(ProductVersion)),2) AS Minor,
                              MachineName,
                              ServiceName
                       FROM (
                              SELECT CAST(SERVERPROPERTY('ProductVersion') AS varchar(50)) AS ProductVersion,
                                     CAST(SERVERPROPERTY('MachineName') AS nvarchar(128)) AS MachineName,
                                     @@SERVICENAME AS ServiceName
                       ) AS V
                ) AS V
            ";
            DataTable data = GetDataTable(SourceServerInstance, "master", qry);
            DataRow row = data.Rows[0];
            results = row["prefix"].ToString()  + "_{" + CollectionSetUid.ToString().ToUpper() + "}_" + ItemId.ToString();
            
            return results;
        }


        public static IEnumerable<string> getColumns(string SourceServerInstance, string DatabaseName, string TableName)
        {
            string qry = @"
                SELECT name
                FROM sys.columns
                WHERE object_id = OBJECT_ID('{0}')
                ORDER BY column_id
            ";
            qry = String.Format(qry, TableName);
            DataTable data = GetDataTable(SourceServerInstance, DatabaseName, qry);
            List<string> results = new List<string>();
            foreach(DataRow row in data.Rows){
                results.Add(row["name"].ToString());
            }
            return results;
        }

    }
}
