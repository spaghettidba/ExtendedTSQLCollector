using Sqlconsulting.DataCollector.Utils;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace Sqlconsulting.DataCollector.InstallCollectorType
{
    public class CollectorTypeInstaller
    {
        private String ServerInstance;

        private static readonly Guid CollectorPackageId = new Guid("B015DBE6-0B9E-4D12-AF52-9386943332E7");
        private static readonly Guid CollectorVersionId = new Guid("6C116DC0-5C2A-43CC-B69F-E36AB8D1250B");

        private static readonly Guid UploaderPackageId = new Guid("F2577F08-EED4-40EF-A9B9-46C00CFF86AE");
        private static readonly Guid UploaderVersionId = new Guid("8A9B1A23-3047-4731-88A5-065408AF75B5");

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

        public void install()
        {
            CollectorConfig cfg = CollectorUtils.GetCollectorConfig(ServerInstance);
            putPackage("ExtendedTSQLCollect", Path.Combine(installPath,"ExtendedTSQLCollect.dtsx"), CollectorPackageId, CollectorVersionId);
            putPackage("ExtendedTSQLUpload", Path.Combine(installPath, "ExtendedTSQLUpload.dtsx"), UploaderPackageId, UploaderVersionId);
            installCollectorType("Extended T-SQL Query Collector Type");
            installCollectorTypeInMDW(cfg);
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


        private void installCollectorType(String name)
        {
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", ServerInstance, "msdb", ConnectionTimeout);

            String paramSchema;
            String formatter;
            String tsql;


            // 1. READ PARAMTERS FROM EXISTING TSQL COLLECTOR

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

                SqlDataReader reader = cmd.ExecuteReader();

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
            finally
            {
                conn.Close();
            }



            // 2. DELETE EXISTING COLLECTOR TYPE

            conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;

            try
            {
                conn.Open();

                tsql = @"
                    IF EXISTS (SELECT * FROM msdb.dbo.syscollector_collector_types WHERE name = '{0}')
                    BEGIN TRY
                        
                        EXEC msdb.dbo.sp_syscollector_delete_collector_type 
                            @collector_type_uid = '{1}', 
                            @name = '{0}';
                    END TRY
                    BEGIN CATCH
                        PRINT '' --IGNORE
                    END CATCH;
                ";
                tsql = String.Format(tsql, name, CollectorUtils.collectorTypeUid.ToString());

                SqlCommand cmd = new SqlCommand(tsql, conn);
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandTimeout = QueryTimeout;

                cmd.ExecuteNonQuery();
            }
            finally
            {
                conn.Close();
            }
            


            // 3. CREATE COLLECTOR TYPE

            conn = new SqlConnection();
            conn.ConnectionString = ConnectionString;

            try
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("msdb.dbo.sp_syscollector_create_collector_type", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = QueryTimeout;

                cmd.Parameters.AddWithValue("@collector_type_uid", CollectorUtils.collectorTypeUid);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parameter_schema", paramSchema);
                cmd.Parameters.AddWithValue("@parameter_formatter", formatter);
                cmd.Parameters.AddWithValue("@collection_package_id", CollectorPackageId);
                cmd.Parameters.AddWithValue("@upload_package_id", UploaderPackageId);

                cmd.ExecuteNonQuery();
            }
            catch (SqlException)
            {
                //ignore: it means that the collector type is already installed
            }
            finally
            {
                conn.Close();
            }
        }


        private void installCollectorTypeInMDW(CollectorConfig cfg)
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

                cmd.Parameters.AddWithValue("@collector_type_uid", CollectorUtils.collectorTypeUid);

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
