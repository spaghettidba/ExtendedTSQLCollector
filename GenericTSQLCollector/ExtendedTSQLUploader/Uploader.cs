using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Sqlconsulting.DataCollector.Utils;

namespace Sqlconsulting.DataCollector.ExtendedTSQLUploader
{
    class Uploader
    {

        public Boolean verbose { get; set; }


        /*
         * Execute the data upload process
         */
        public void UploadData(
                String SourceServerInstance,
                Guid CollectionSetUid,
                int ItemId,
                int LogId
            )
        {

            CollectorLogger logger = new CollectorLogger(SourceServerInstance, CollectionSetUid, ItemId);

            //DataTable collectedData = null;

            if (verbose) logger.logMessage("--------------------------------");
            if (verbose) logger.logMessage("      ExtendedTSQLUploader       ");
            if (verbose) logger.logMessage("--------------------------------");
            if (verbose) logger.logMessage("Copyright© sqlconsulting.it 2014");
            if (verbose) logger.logMessage("-");

            if (verbose) logger.logMessage("Loading configuration");
            //
            // Load Configuration
            //
            TSQLCollectorConfig cfg = new TSQLCollectorConfig();
            cfg.readFromDatabase(SourceServerInstance, CollectionSetUid, ItemId);

            //String collectorId = CollectionSetUid + "_" + ItemId.ToString();

            if (verbose) logger.logMessage("Updating source info");
            //
            // Update Source Info
            //
            int source_id = updateDataSource(cfg.MDWInstance, cfg.MDWDatabase, CollectionSetUid, cfg.MachineName, cfg.InstanceName, cfg.DaysUntilExpiration);


            if (verbose) logger.logMessage("Creating snapshot");
            //
            // Load the snapshot_id
            //
            int snapshot_id = createSnapshot(cfg.MDWInstance, cfg.MDWDatabase, CollectionSetUid, TSQLCollectionItemConfig.CollectorTypeUid, cfg.MachineName, cfg.InstanceName, LogId);


            
            foreach (CollectionItemConfig item in cfg.collectionItems)
            {
                TSQLCollectionItemConfig itm = (TSQLCollectionItemConfig)item;
                String collectorId = CollectorUtils.getCacheFilePrefix(SourceServerInstance, CollectionSetUid, ItemId) + "_" + itm.Index; 

                if (verbose) logger.logMessage("Creating target table " + itm.OutputTable);
                //
                // Create the target table
                //
                String targetTable = createTargetTable(cfg.MDWInstance, cfg.MDWDatabase, itm.OutputTable, SourceServerInstance, itm.Query, collectorId);


                foreach (String fileName in System.IO.Directory.GetFiles(cfg.CacheDirectory))
                {
                    System.IO.FileInfo destFile = new System.IO.FileInfo(fileName);
                    //if (verbose) logger.logMessage("Processing " + destFile.FullName);
                    //if (verbose) logger.logMessage("Searching " + collectorId);

                    if (destFile.Name.Contains(collectorId + "_") && destFile.Extension.ToLowerInvariant().Equals(".cache"))
                    {
                        if (verbose) logger.logMessage("Uploading " + destFile.FullName);

                        DataTable collectedData = null;

                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter fm = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                        //
                        // Deserialize from the binary file
                        //
                        using (System.IO.FileStream fs = new System.IO.FileStream(destFile.FullName, System.IO.FileMode.Open))
                        {
                            collectedData = (DataTable)fm.Deserialize(fs);
                            fs.Close();
                        }



                        //
                        // Add the snapshot_id column to the DataTable
                        //
                        DataColumn cl_sn = new DataColumn("snapshot_id", typeof(Int32));
                        cl_sn.DefaultValue = snapshot_id;
                        collectedData.Columns.Add(cl_sn);

                        //Write-Host "TargetTable: $targetTable"
                        //Write-Host "SourceFile: $destFile"

                        if (verbose) logger.logMessage("Writing to server");
                        CollectorUtils.WriteDataTable(cfg.MDWInstance, cfg.MDWDatabase, targetTable, collectedData);

                        if (verbose) logger.logMessage("Deleting file");
                        destFile.Delete();
                    }

                }

            }

            logger.cleanupLogFiles(cfg.DaysUntilExpiration);

        }


        private int updateDataSource(
            String TargetServerInstance,
            String TargetDatabase,
            Guid collection_set_uid,
            String machine_name,
            String named_instance,
            int days_until_expiration
)
        {
            String qry = @"
		        DECLARE @src_id int;
		        EXEC [core].[sp_update_data_source] 
			        @collection_set_uid = '{0}',
			        @machine_name = '{1}',
			        @named_instance = '{2}',
			        @days_until_expiration = {3},
			        @source_id = @src_id OUTPUT;
		        SELECT @src_id AS src_id;
	        ";

           
            qry = String.Format(qry, collection_set_uid, machine_name, named_instance, days_until_expiration);

            DataTable data = CollectorUtils.GetDataTable(TargetServerInstance, TargetDatabase, qry);
            return Convert.ToInt32(data.Rows[0]["src_id"]);
        }




        private int createSnapshot(
            String TargetServerInstance,
            String TargetDatabase,
            Guid collection_set_uid,
            Guid collector_type_uid,
            String machine_name,
            String instance_name,
            int log_id
        )
        {
            String qry = @"
		        DECLARE @snapshot_id int;
		        EXEC [core].[sp_create_snapshot] 
			        @collection_set_uid = '{0}',
			        @collector_type_uid = '{1}',
			        @machine_name = '{2}',
			        @named_instance = '{3}',
			        @log_id = {4},
			        @snapshot_id = @snapshot_id OUTPUT;
		        SELECT @snapshot_id AS snapshot_id;
	        ";

            qry = String.Format(qry, collection_set_uid, collector_type_uid, machine_name, instance_name, log_id);

            DataTable data = CollectorUtils.GetDataTable(TargetServerInstance, TargetDatabase, qry);
            return Convert.ToInt32(data.Rows[0]["snapshot_id"]);
        }



        /*
         * Creates the target table from the 
         * output definition of the query
         */
        private String createTargetTable(
                String TargetServerInstance,
                String TargetDatabase,
                String TableName,
                String SourceServerInstance,
                String SourceQuery,
                String CollectorId
            )
        {


            String sqlCheck = @"
                SELECT QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name)  AS targetTable
                FROM [{0}].sys.tables 
                WHERE name = '{1}' 
                    AND schema_id IN (SCHEMA_ID('custom_snapshots'), SCHEMA_ID('snapshots'))
                ORDER BY CASE SCHEMA_NAME(schema_id) 
                        WHEN 'custome_snapshots' THEN 1
                        WHEN 'snapshots' THEN 2 
                    END ";

            sqlCheck = String.Format(sqlCheck, TargetDatabase, TableName);

            DataTable data = CollectorUtils.GetDataTable(TargetServerInstance, TargetDatabase, sqlCheck);

            // table is not missing
            if (data.Rows.Count > 0)
            {
                return data.Rows[0]["targetTable"].ToString();
            }


            String statement = @"

	        IF NOT EXISTS (
		        SELECT *
		        FROM sys.servers
		        WHERE NAME = 'LOOPBACK'
	        )
	        BEGIN

		        DECLARE @srv nvarchar(4000);
		        SET @srv = @@SERVERNAME; -- gather this server name
        		 
		        -- Create the linked server
		        EXEC master.dbo.sp_addlinkedserver
		            @server     = N'LOOPBACK',
		            @srvproduct = N'SQLServ', -- it's not a typo: it can't be 'SQLServer'
		            @provider   = N'SQLNCLI', -- change to SQLOLEDB for SQLServer 2000
		            @datasrc    = @srv;
        		 
		        -- Set the authentication to 'current security context'
		        EXEC master.dbo.sp_addlinkedsrvlogin
		            @rmtsrvname  = N'LOOPBACK',
		            @useself     = N'True',
		            @locallogin  = NULL,
		            @rmtuser     = NULL,
		            @rmtpassword = NULL;

	        END
        	 
	        USE tempdb;
	        GO

	        IF OBJECT_ID('{0}') IS NOT NULL
		        DROP PROCEDURE [{0}]
	        GO
        	 
	        CREATE PROCEDURE [{0}]
	        AS
	        BEGIN
	            SET NOCOUNT ON;
        	 
	            {1}
	        END
	        GO

	        IF SCHEMA_ID('custom_snapshots') IS NULL
		        EXEC('CREATE SCHEMA [custom_snapshots]')

	        IF OBJECT_ID('custom_snapshots.{2}') IS NOT NULL
		        DROP TABLE [custom_snapshots].[{2}]
	        GO

	        SELECT TOP 0 *, 
		        CAST(NULL AS sysname) AS _database_name, 
		        CAST(NULL AS datetimeoffset(7)) AS _collection_time,
		        CAST(NULL AS int) AS _snapshot_id
	        INTO tempdb.[custom_snapshots].[{2}]
	        FROM OPENQUERY(LOOPBACK, 'SET FMTONLY OFF; EXEC tempdb.dbo.[{0}]');
        	 
	        DROP PROCEDURE [{0}];
	        GO
        	
	        IF EXISTS(
		        SELECT 1 
		        FROM sys.columns 
		        WHERE name = 'database_name'
		        AND object_id = OBJECT_ID('[custom_snapshots].[{2}]')
	        )
	        BEGIN
		        EXEC sp_rename '[custom_snapshots].[{2}].[database_name]', '__database_name', 'COLUMN';
	        END
	        EXEC sp_rename '[custom_snapshots].[{2}].[_database_name]', 'database_name', 'COLUMN';


	        IF EXISTS(
		        SELECT 1 
		        FROM sys.columns 
		        WHERE name = 'collection_time'
		        AND object_id = OBJECT_ID('[custom_snapshots].[{2}]')
	        )
	        BEGIN
		        EXEC sp_rename '[custom_snapshots].[{2}].[collection_time]', '__collection_time', 'COLUMN';
	        END
	        EXEC sp_rename '[custom_snapshots].[{2}].[_collection_time]', 'collection_time', 'COLUMN';

        	
	        IF EXISTS(
		        SELECT 1 
		        FROM sys.columns 
		        WHERE name = 'snapshot_id'
		        AND object_id = OBJECT_ID('[custom_snapshots].[{2}]')
	        )
	        BEGIN
		        EXEC sp_rename '[custom_snapshots].[{2}].[snapshot_id]', '__snapshot_id', 'COLUMN';
	        END
	        EXEC sp_rename '[custom_snapshots].[{2}].[_snapshot_id]', 'snapshot_id', 'COLUMN';

	        ";

            statement = String.Format(statement, CollectorId, SourceQuery, TableName);

            CollectorUtils.InvokeSqlBatch(SourceServerInstance, "tempdb", statement);

            String scriptText = CollectorUtils.ScriptTable(SourceServerInstance, "tempdb", TableName);

            CollectorUtils.InvokeSqlCmd(TargetServerInstance, TargetDatabase, scriptText);

            return "[custom_snapshots].["+ TableName +"]";
        }

    }
}
