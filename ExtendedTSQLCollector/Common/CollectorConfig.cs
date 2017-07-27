using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;


namespace Sqlconsulting.DataCollector.Utils
{
    public abstract class CollectorConfig
    {
        public String CacheDirectory { get; set; }
        public int CacheWindow { get; set; }
        public Boolean CollectorEnabled { get; set; }
        public String MDWDatabase { get; set; }
        public String MDWInstance { get; set; }
        public int DaysUntilExpiration { get; set; }
        public List<String> Databases = new List<String>();
        public List<CollectionItemConfig> collectionItems = new List<CollectionItemConfig>();
        public String MachineName { get; set; }
        public String InstanceName { get; set; }

        public virtual void readFromDatabase(String ServerInstance)
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

            DataTable data = CollectorUtils.GetDataTable(ServerInstance, "msdb", qry);
            DataRow row = data.Rows[0];

            CacheDirectory = row["CacheDirectory"].ToString();
            CacheWindow = Convert.ToInt32(row["CacheWindow"]);
            CollectorEnabled = Convert.ToBoolean(row["CollectorEnabled"]);
            MDWDatabase = row["MDWDatabase"].ToString();
            MDWInstance = row["MDWInstance"].ToString();

            if (String.IsNullOrEmpty(CacheDirectory))
            {
                CacheDirectory = System.Environment.GetEnvironmentVariable("temp");
            }

            qry = @"
		        SELECT CAST(SERVERPROPERTY('MachineName') AS NVARCHAR(128)) AS MachineName
	                  ,ISNULL(CAST(SERVERPROPERTY('InstanceName') AS NVARCHAR(128)),'') AS InstanceName
	        ";

            data = CollectorUtils.GetDataTable(ServerInstance, "msdb", qry);
            row = data.Rows[0];

            MachineName = row["MachineName"].ToString();
            InstanceName = row["InstanceName"].ToString();

        }

        public abstract void readFromDatabase(
                   String ServerInstance,
                   Guid CollectionSetUid,
                   int ItemId
               );

    }
}
