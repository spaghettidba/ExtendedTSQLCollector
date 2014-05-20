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

        public static readonly Guid collectorTypeUid = new Guid("FD34D746-9A4D-4901-B872-3AF7CDBF7D37");





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



        public static CollectorConfig GetCollectorConfig(String ServerInstance)
        {
            String qry = @"
		        SELECT *
		        FROM [msdb].[dbo].[syscollector_config_store]
		        PIVOT(
			        MAX(parameter_value) 
			        FOR parameter_name IN (
					        [CacheDirectory]
					        ,[CacheWindow]
					        ,[CollectorEnabled]
					        ,[MDWDatabase]
					        ,[MDWInstance]
			        )
		        ) AS p
	        ";

            DataTable data = GetDataTable(ServerInstance, "msdb", qry);
            DataRow row = data.Rows[0];

            CollectorConfig cfg = new CollectorConfig();

            cfg.CacheDirectory = row["CacheDirectory"].ToString();
            cfg.CacheWindow = Convert.ToInt32(row["CacheWindow"]);
            cfg.CollectorEnabled = Convert.ToBoolean(row["CollectorEnabled"]);
            cfg.MDWDatabase = row["MDWDatabase"].ToString();
            cfg.MDWInstance = row["MDWInstance"].ToString();

            if (String.IsNullOrEmpty(cfg.CacheDirectory))
            {
                cfg.CacheDirectory = System.Environment.GetEnvironmentVariable("temp");
            }

            qry = @"
		        SELECT CAST(SERVERPROPERTY('MachineName') AS NVARCHAR(128)) AS MachineName
	                  ,ISNULL(CAST(SERVERPROPERTY('InstanceName') AS NVARCHAR(128)),'') AS InstanceName
	        ";

            data = GetDataTable(ServerInstance, "msdb", qry);
            row = data.Rows[0];

            cfg.MachineName = row["MachineName"].ToString();
            cfg.InstanceName = row["InstanceName"].ToString();

            return cfg;
        }



        public static CollectorConfig GetCollectorConfig(
                String ServerInstance,
                Guid CollectionSetUid,
                int ItemId
            )
        {
            CollectorConfig cfg = GetCollectorConfig(ServerInstance);

            String qry = @"
		        DECLARE @x xml;

		        SELECT @x = parameters
		        FROM msdb.dbo.syscollector_collection_items
		        WHERE collection_set_id = (
				        SELECT collection_set_id
				        FROM msdb.dbo.syscollector_collection_sets
				        WHERE collection_set_uid = '{0}'
			        )
		        AND collection_item_id = {1};

		        ;WITH XMLNAMESPACES('DataCollectorType' AS ns)
		        SELECT x.value('Value[1]','varchar(max)') AS query,
			        x.value('OutputTable[1]', 'varchar(max)') AS outputTable
		        FROM @x.nodes('/ns:TSQLQueryCollector/Query') Q(x)
		        ORDER BY outputTable;
	        ";

            qry = String.Format(qry, CollectionSetUid, ItemId);

            DataTable data = GetDataTable(ServerInstance, "msdb", qry);

            int i = 1;


            foreach (DataRow currentRow in data.Rows)
            {
                CollectionItemConfig cic = new CollectionItemConfig();
                cic.Query = currentRow["query"].ToString();
                cic.OutputTable = currentRow["outputTable"].ToString();
                cic.Index = i;
                i++;
                cfg.collectionItems.Add(cic);
            }



            qry = @"
        	
		        DECLARE @x xml;

		        SELECT @x = parameters
		        FROM msdb.dbo.syscollector_collection_items
		        WHERE collection_set_id = (
				        SELECT collection_set_id
				        FROM msdb.dbo.syscollector_collection_sets
				        WHERE collection_set_uid = '{0}'
			        )
		        AND collection_item_id = {1}

		        DECLARE @selectedDatabases TABLE (
			        name sysname
		        )

		        ;WITH XMLNAMESPACES('DataCollectorType' AS ns)
		        INSERT INTO @selectedDatabases 
		        SELECT x.value('.','varchar(max)') AS database_name
		        FROM @x.nodes('/ns:TSQLQueryCollector/Databases/Database') Q(x)

		        DECLARE @useSystemDB bit
		        DECLARE @UseUserDB bit

		        ;WITH XMLNAMESPACES('DataCollectorType' AS ns)
		        SELECT @useSystemDB = 
					        CASE LOWER(@x.value('(/ns:TSQLQueryCollector/Databases/@UseSystemDatabases)[1]','varchar(5)')) 
						        WHEN 'true' THEN 1 WHEN 'false' THEN 0 ELSE NULL 
					        END,
		               @useUserDB = 
			   		        CASE LOWER(@x.value('(/ns:TSQLQueryCollector/Databases/@UseUserDatabases)[1]','varchar(5)')) 
						        WHEN 'true' THEN 1 WHEN 'false' THEN 0 ELSE NULL 
					        END

		        -- delete non-existing databases
		        DELETE t
		        FROM @selectedDatabases AS t
		        WHERE name NOT IN (
			        SELECT name
			        FROM sys.databases
		        )

		        INSERT INTO @selectedDatabases
		        SELECT name
		        FROM sys.databases
		        WHERE 1 = 
			        CASE
				        WHEN @useSystemDB = 1 AND name IN ('master','model','msdb','distribution') THEN 1
				        WHEN @UseUserDB = 1 AND name NOT IN ('master','model','msdb','distribution') THEN 1
			        END
			        AND NOT EXISTS (
				        SELECT *
				        FROM @selectedDatabases
			        )

		        INSERT INTO @selectedDatabases
		        SELECT 'master'
		        WHERE NOT EXISTS (
			        SELECT *
			        FROM @selectedDatabases
		        )

		        SELECT *
		        FROM @selectedDatabases
	        ";

            qry = String.Format(qry, CollectionSetUid, ItemId);

            data = GetDataTable(ServerInstance, "msdb", qry);

            i = 1;

            foreach (DataRow currentRow in data.Rows)
            {
                cfg.Databases.Add(currentRow["name"].ToString());
            }


            qry = @"
		        SELECT days_until_expiration
		        FROM msdb.dbo.syscollector_collection_sets
		        WHERE collection_set_uid = '{0}'
	        ";

            qry = String.Format(qry, CollectionSetUid);

            data = GetDataTable(ServerInstance, "msdb", qry);

            cfg.DaysUntilExpiration = Convert.ToInt32(data.Rows[0]["days_until_expiration"]);

            return cfg;

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
