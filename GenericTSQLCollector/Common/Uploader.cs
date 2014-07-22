using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;


namespace Sqlconsulting.DataCollector.Utils
{
    public abstract class Uploader
    {

        public Boolean verbose { get; set; }

        protected Guid CollectionSetUid { get; set; }
        protected int ItemId { get; set; }
        protected String SourceServerInstance { get; set; }
        protected int LogId { get; set; }

        protected CollectorLogger logger;


        public Uploader(
                String SourceServerInstance,
                Guid CollectionSetUid,
                int ItemId,
                int LogId
            )
        {
            this.CollectionSetUid = CollectionSetUid;
            this.ItemId = ItemId;
            this.SourceServerInstance = SourceServerInstance;
            this.LogId = LogId;
                 
        }


        /*
         * Execute the data upload process
         */
        public void UploadData()
        {

            logger = new CollectorLogger(SourceServerInstance, CollectionSetUid, ItemId);

            String displayName;
            String[] names = this.GetType().Namespace.Split('.');
            displayName = names[names.Length - 1];

            if (verbose) logger.logMessage("--------------------------------");
            if (verbose) logger.logMessage("   " + displayName);
            if (verbose) logger.logMessage("--------------------------------");
            if (verbose) logger.logMessage("Copyright© sqlconsulting.it 2014");
            if (verbose) logger.logMessage("-");

            if (verbose) logger.logMessage("Loading configuration");
            //
            // Load Configuration
            //
            CollectorConfig cfg = null;
            Guid CollectorTypeUid = new Guid("00000000-0000-0000-0000-000000000000");
            if(this.GetType().Namespace.Equals("Sqlconsulting.DataCollector.ExtendedXEReaderUploader"))
            {
                cfg = new XEReaderCollectorConfig();
                CollectorTypeUid = XEReaderCollectionItemConfig.CollectorTypeUid;
            }
            else if(this.GetType().Namespace.Equals("Sqlconsulting.DataCollector.ExtendedTSQLUploader"))
            {
                cfg = new TSQLCollectorConfig();
                CollectorTypeUid = TSQLCollectionItemConfig.CollectorTypeUid;
            }
            cfg.readFromDatabase(SourceServerInstance, CollectionSetUid, ItemId);

            //String collectorId = CollectionSetUid + "_" + ItemId.ToString();

            if (verbose) logger.logMessage("Updating source info");
            //
            // Update Source Info
            //
            int source_id = updateDataSource(cfg.MDWInstance, cfg.MDWDatabase, CollectionSetUid, cfg.MachineName, cfg.InstanceName, cfg.DaysUntilExpiration);
            int snapshot_id = -1;

            foreach (CollectionItemConfig item in cfg.collectionItems)
            {
                String collectorId = CollectorUtils.getCacheFilePrefix(SourceServerInstance, CollectionSetUid, ItemId) + "_" + item.Index; 

                //
                // Create the target table
                //
                String targetTable = createTargetTable(cfg, item);
                Boolean tableCreated = (targetTable != null);


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
                        // Load the snapshot_id
                        //
                        if (snapshot_id < 0)
                        {
                            if (verbose) logger.logMessage("Creating snapshot");
                            snapshot_id = createSnapshot(cfg.MDWInstance, cfg.MDWDatabase, CollectionSetUid, CollectorTypeUid, cfg.MachineName, cfg.InstanceName, LogId);
                        }



                        //
                        // Add the snapshot_id column to the DataTable
                        //
                        DataColumn cl_sn = new DataColumn("snapshot_id", typeof(Int32));
                        cl_sn.DefaultValue = snapshot_id;
                        collectedData.Columns.Add(cl_sn);

                        //
                        // Check again if table needs to be created
                        //
                        if (!tableCreated)
                        {
                            targetTable = createTargetTable(cfg, item, collectedData);
                            tableCreated = true;
                        }

                        if (verbose) logger.logMessage("Writing to server... " + targetTable);
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

            if (named_instance.Trim().Equals(""))
            {
                named_instance = "MSSQLSERVER";
            }
            qry = String.Format(qry, collection_set_uid, machine_name, named_instance, days_until_expiration);

            DataTable data = CollectorUtils.GetDataTable(TargetServerInstance, TargetDatabase, qry);
            return Convert.ToInt32(data.Rows[0]["src_id"]);
        }




        protected int createSnapshot(
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

            if (instance_name.Trim().Equals(""))
            {
                instance_name = "MSSQLSERVER";
            }

            qry = String.Format(qry, collection_set_uid, collector_type_uid, machine_name, instance_name, log_id);

            DataTable data = CollectorUtils.GetDataTable(TargetServerInstance, TargetDatabase, qry);
            return Convert.ToInt32(data.Rows[0]["snapshot_id"]);
        }



        /*
         * Creates the target table from the 
         * output definition of the query
         */
        protected abstract String createTargetTable(
                CollectorConfig cfg,
                CollectionItemConfig itm
            );

        
        protected String createTargetTable(
                CollectorConfig cfg,
                CollectionItemConfig itm,
                DataTable data
            )
        {
            int ConnectionTimeout = 15;
            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", cfg.MDWInstance, cfg.MDWDatabase, ConnectionTimeout);

            using(SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();

                DataTableTSQLAdapter adapter = new DataTableTSQLAdapter(conn);
                adapter.DestinationTableName = "[custom_snapshots].[" + itm.OutputTable + "]";
                adapter.CreateFromDataTable(data);

            }
            return "[custom_snapshots].[" + itm.OutputTable + "]";
        }


        protected String checkTable(CollectorConfig cfg, CollectionItemConfig itm)
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

            sqlCheck = String.Format(sqlCheck, cfg.MDWDatabase, itm.OutputTable);

            DataTable data = CollectorUtils.GetDataTable(cfg.MDWInstance, cfg.MDWDatabase, sqlCheck);

            // table is not missing
            if (data.Rows.Count > 0)
            {
                return data.Rows[0]["targetTable"].ToString();
            }
            else
            {
                if (verbose) logger.logMessage("Creating target table " + itm.OutputTable);
                return null;
            }
        }
    }
}
