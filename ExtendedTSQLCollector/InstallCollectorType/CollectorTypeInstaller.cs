using Sqlconsulting.DataCollector.Utils;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
//using System.Threading.Tasks;

namespace Sqlconsulting.DataCollector.InstallCollectorType
{
    public class CollectorTypeInstaller
    {
        private String ServerInstance;
        private const int QueryTimeout = 600;
 
        private String installPath;

        public CollectorTypeInstaller(String ServerInstance)
        {
            this.ServerInstance = ServerInstance;
            installPath = Path.Combine(System.Environment.GetEnvironmentVariable("programfiles"), "ExtendedTSQLCollector");
            installPath = Path.Combine(installPath, "SSIS_Packages");

            DirectoryInfo di = new DirectoryInfo(installPath);
            if (!di.Exists)
            {
                installPath = installPath.Replace(" (x86)", "");
            }
        }

        public Boolean checkPermissions()
        {
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", ServerInstance, "master", ConnectionTimeout);

            String tsql;
            Boolean result;

            
            

            using(SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();

                tsql = @"
                    SELECT is_sysadmin = CAST(ISNULL(IS_SRVROLEMEMBER('sysadmin'),0) AS bit)
                ";

                SqlCommand cmd = new SqlCommand(tsql, conn);
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandTimeout = QueryTimeout;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {

                    if (reader.Read())
                    {
                        result = reader.GetBoolean(0);
                    }
                    else
                    {
                        throw new ArgumentException("Unable to query permissions.");
                    }
                }

            }

            return result;
        }



        public void install()
        {
            TSQLCollectorConfig cfg = new TSQLCollectorConfig();
            cfg.readFromDatabase(ServerInstance);
            putPackage("ExtendedTSQLCollect", Path.Combine(installPath, "ExtendedTSQLCollect.dtsx"), TSQLCollectorConfig.CollectorPackageId, TSQLCollectorConfig.CollectorVersionId);
            putPackage("ExtendedTSQLUpload", Path.Combine(installPath, "ExtendedTSQLUpload.dtsx"), TSQLCollectorConfig.UploaderPackageId, TSQLCollectorConfig.UploaderVersionId);
            putPackage("ExtendedXEReaderCollect", Path.Combine(installPath, "ExtendedXEReaderCollect.dtsx"), XEReaderCollectorConfig.CollectorPackageId, XEReaderCollectorConfig.CollectorVersionId);
            putPackage("ExtendedXEReaderUpload", Path.Combine(installPath, "ExtendedXEReaderUpload.dtsx"), XEReaderCollectorConfig.UploaderPackageId, XEReaderCollectorConfig.UploaderVersionId);
            installTSQLCollectorType();
            installXEReaderCollectorType();
            if (!String.IsNullOrEmpty(cfg.MDWDatabase))
            {
                installCollectorTypeInMDW(cfg, TSQLCollectionItemConfig.CollectorTypeUid);
                installCollectorTypeInMDW(cfg, XEReaderCollectionItemConfig.CollectorTypeUid);
            }
        }


        private void putPackage(String packageName, String path, Guid packageId, Guid VersionId)
        {
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", ServerInstance, "msdb", ConnectionTimeout);

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;

            try
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("msdb.dbo.sp_ssis_putpackage", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = QueryTimeout;

                cmd.Parameters.AddWithValue("@name", packageName);
                cmd.Parameters.AddWithValue("@id", packageId);
                cmd.Parameters.AddWithValue("@description", "Custom Data Collector Package");
                cmd.Parameters.AddWithValue("@createdate", DateTime.Now);
                cmd.Parameters.AddWithValue("@folderid", "8877FE4B-A938-4a51-84B9-C5BDAD74B0AD");
                cmd.Parameters.AddWithValue("@packageData", readPackageFile(path));
                cmd.Parameters.AddWithValue("@packageformat", 1);
                cmd.Parameters.AddWithValue("@packagetype", 5);
                cmd.Parameters.AddWithValue("@vermajor", 1);
                cmd.Parameters.AddWithValue("@verminor", 0);
                cmd.Parameters.AddWithValue("@verbuild", 0);
                cmd.Parameters.AddWithValue("@vercomments", "");
                cmd.Parameters.AddWithValue("@verid", VersionId);

                cmd.ExecuteNonQuery();
            }
            finally
            {
                conn.Close();
            }
        }


        private void installTSQLCollectorType()
        {
            String name = "Extended T-SQL Query Collector Type";
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", ServerInstance, "msdb", ConnectionTimeout);

            String paramSchema;
            String formatter;
            String tsql;


            // 1. READ PARAMETERS FROM EXISTING TSQL COLLECTOR

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;

            try
            {
                conn.Open();

                tsql = @"
                    SELECT parameter_schema, parameter_formatter
                    FROM syscollector_collector_types 
                    WHERE name = 'Generic T-SQL Query Collector Type'
                ";

                SqlCommand cmd = new SqlCommand(tsql, conn);
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandTimeout = QueryTimeout;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {

                    if (reader.Read())
                    {
                        paramSchema = reader.GetString(0);
                        formatter = reader.GetString(1);
                    }
                    else
                    {
                        throw new ArgumentException("Missing information from the collector types table.");
                    }
                }
            }
            finally
            {
                conn.Close();
            }



            // CREATE/UPDATE COLLECTOR TYPE

            conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;

            try
            {
                conn.Open();

                SqlCommand cmd = null;
                if (!CheckCollectorTypeInstallStatus(name))
                {
                    cmd = new SqlCommand("msdb.dbo.sp_syscollector_create_collector_type", conn);
                }
                else
                {
                    cmd = new SqlCommand("msdb.dbo.sp_syscollector_update_collector_type", conn);
                }
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = QueryTimeout;

                cmd.Parameters.AddWithValue("@collector_type_uid", TSQLCollectionItemConfig.CollectorTypeUid);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parameter_schema", paramSchema);
                cmd.Parameters.AddWithValue("@parameter_formatter", formatter);
                cmd.Parameters.AddWithValue("@collection_package_id", TSQLCollectorConfig.CollectorPackageId);
                cmd.Parameters.AddWithValue("@upload_package_id", TSQLCollectorConfig.UploaderPackageId);

                cmd.ExecuteNonQuery();
            }
            finally
            {
                conn.Close();
            }
        }


        public Boolean CheckCollectorTypeInstallStatus(String collectorTypeName)
        {
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", ServerInstance, "msdb", ConnectionTimeout);

            String tsql;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;

            int collectorCount;

            try
            {
                conn.Open();

                tsql = @"
                   SELECT COUNT(*) FROM msdb.dbo.syscollector_collector_types WHERE name = '{0}'
                ";
                tsql = String.Format(tsql, collectorTypeName);

                SqlCommand cmd = new SqlCommand(tsql, conn);
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandTimeout = QueryTimeout;

                collectorCount = (int)cmd.ExecuteScalar();
            }
            finally
            {
                conn.Close();
            }
            return collectorCount > 0;
        }



        private void installXEReaderCollectorType()
        {
            String name = "Extended XE Reader Collector Type";
            int ConnectionTimeout = 15;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", ServerInstance, "msdb", ConnectionTimeout);

            String paramSchema;
            String formatter;

            paramSchema = Properties.Resources.XEReaderParamSchema;
            formatter = Properties.Resources.XEReaderParamFormatter;


            // CREATE/UPDATE COLLECTOR TYPE
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;

            try
            {
                conn.Open();

                SqlCommand cmd = null;
                if (!CheckCollectorTypeInstallStatus(name))
                {
                    cmd = new SqlCommand("msdb.dbo.sp_syscollector_create_collector_type", conn);
                }
                else
                {
                    cmd = new SqlCommand("msdb.dbo.sp_syscollector_update_collector_type", conn);
                }
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = QueryTimeout;

                cmd.Parameters.AddWithValue("@collector_type_uid", XEReaderCollectionItemConfig.CollectorTypeUid);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parameter_schema", paramSchema);
                cmd.Parameters.AddWithValue("@parameter_formatter", formatter);
                cmd.Parameters.AddWithValue("@collection_package_id", XEReaderCollectorConfig.CollectorPackageId);
                cmd.Parameters.AddWithValue("@upload_package_id", XEReaderCollectorConfig.UploaderPackageId);

                cmd.ExecuteNonQuery();
            }
            finally
            {
                conn.Close();
            }
        }


        private void installCollectorTypeInMDW(CollectorConfig cfg, Guid CollectorTypeGuid)
        {
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", cfg.MDWInstance, cfg.MDWDatabase, ConnectionTimeout);

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;

            try
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("core.sp_add_collector_type", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = QueryTimeout;

                cmd.Parameters.AddWithValue("@collector_type_uid", CollectorTypeGuid);

                cmd.ExecuteNonQuery();
            }
            finally
            {
                conn.Close();
            }
        }


        private byte[] readPackageFile(String path)
        {
            FileStream fs = null;
            BinaryReader br = null;
            FileInfo fi = new FileInfo(path);
            byte[] result = null;
            try
            {
                fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read);
                br = new BinaryReader(fs);
                result = br.ReadBytes((int)fi.Length);
            }
            finally
            {
                try
                {
                    fs.Close();
                    br.Close();
                }
                catch (Exception)
                {
                    //ignore
                }
            }
            return result;
        }

    }
}
