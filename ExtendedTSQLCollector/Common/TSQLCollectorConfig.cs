using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Sqlconsulting.DataCollector.Utils
{
    public class TSQLCollectorConfig : CollectorConfig
    {

        public static readonly Guid CollectorPackageId = new Guid("77D28C8D-A529-445B-B5F6-31861D099594");
        public static readonly Guid CollectorVersionId = new Guid("89C719FC-DDBD-45CA-BB27-5833E89962A9");
        
        public static readonly Guid UploaderPackageId = new Guid("F0A974DF-4553-4ACF-AAC3-719246DBF5CF");
        public static readonly Guid UploaderVersionId = new Guid("A7450869-7C6E-427E-8278-F00D79172E38");


        public override void readFromDatabase(
            String ServerInstance,
            Guid CollectionSetUid,
            int ItemId
        )
        {
            readFromDatabase(ServerInstance);

            String qry = @"
		        DECLARE @x xml;
				DECLARE @fre int;

		        SELECT @x = parameters,
					@fre = frequency
		        FROM msdb.dbo.syscollector_collection_items
		        WHERE collection_set_id = (
				        SELECT collection_set_id
				        FROM msdb.dbo.syscollector_collection_sets
				        WHERE collection_set_uid = '{0}'
			        )
		        AND collection_item_id = {1}
                AND collector_type_uid = '{2}';

		        ;WITH XMLNAMESPACES('DataCollectorType' AS ns)
		        SELECT x.value('Value[1]','varchar(max)') AS query,
			        x.value('OutputTable[1]', 'varchar(max)') AS outputTable,
					@fre AS frequency
		        FROM @x.nodes('/ns:TSQLQueryCollector/Query') Q(x)
		        ORDER BY outputTable;
	        ";

            qry = String.Format(qry, CollectionSetUid, ItemId, TSQLCollectionItemConfig.CollectorTypeUid);

            DataTable data = CollectorUtils.GetDataTable(ServerInstance, "msdb", qry);

            int i = 1;


            foreach (DataRow currentRow in data.Rows)
            {
                TSQLCollectionItemConfig cic = new TSQLCollectionItemConfig();
                cic.Query = currentRow["query"].ToString();
                cic.OutputTable = currentRow["outputTable"].ToString();
                cic.Frequency = Int32.Parse(currentRow["frequency"].ToString());
                cic.Index = i;
                i++;
                collectionItems.Add(cic);
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

            data = CollectorUtils.GetDataTable(ServerInstance, "msdb", qry);

            i = 1;

            foreach (DataRow currentRow in data.Rows)
            {
                Databases.Add(currentRow["name"].ToString());
            }


            qry = @"
		        SELECT days_until_expiration
		        FROM msdb.dbo.syscollector_collection_sets
		        WHERE collection_set_uid = '{0}'
	        ";

            qry = String.Format(qry, CollectionSetUid);

            data = CollectorUtils.GetDataTable(ServerInstance, "msdb", qry);

            DaysUntilExpiration = Convert.ToInt32(data.Rows[0]["days_until_expiration"]);

        }

    }
}
