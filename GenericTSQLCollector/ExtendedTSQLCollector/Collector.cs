using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Sqlconsulting.DataCollector.Utils;
using System.IO;

namespace Sqlconsulting.DataCollector.ExtendedTSQLCollector
{
    public class Collector
    {
        public Boolean verbose { get; set; }
 

        public void CollectData(
                String SourceServerInstance,
                Guid CollectionSetUid,
                int ItemId
            )
        {

            CollectorLogger logger = new CollectorLogger(SourceServerInstance, CollectionSetUid, ItemId);

            DataTable collectedData = null;

            if (verbose) logger.logMessage("--------------------------------");
            if (verbose) logger.logMessage("      ExtendedTSQLCollector      ");
            if (verbose) logger.logMessage("--------------------------------");
            if (verbose) logger.logMessage("Copyright© sqlconsulting.it 2014");
            if (verbose) logger.logMessage("-");

            if (verbose) logger.logMessage("Loading configuration");
            //
            // Load Configuration
            //
            CollectorConfig cfg = CollectorUtils.GetCollectorConfig(SourceServerInstance, CollectionSetUid, ItemId);


            if (verbose) logger.logMessage("Entering collection items loop");
            foreach (CollectionItemConfig itm in cfg.collectionItems)
            {
                if (verbose) logger.logMessage("Processing item n. " + itm.Index);
                if (verbose) logger.logMessage("Processing query " + itm.Query);

                collectedData = null;

                String ts = DateTime.Now.ToString("yyyyMMddHHmmss");
                String collectorId = CollectorUtils.getCacheFilePrefix(SourceServerInstance, CollectionSetUid, ItemId) + "_" + itm.Index; 

                String destFile = Path.Combine(cfg.CacheDirectory, collectorId + "_" + ts + ".cache");

                //
                // Iterate through the enabled databases
                //
                if (verbose) logger.logMessage("Entering databases loop");
                foreach (String currentDatabase in cfg.Databases)
                {

                    if (verbose) logger.logMessage("Processing database " + currentDatabase);
                    //
                    // Execute the query in the collection item
                    //

                    DataTable dt = CollectorUtils.GetDataTable(SourceServerInstance, currentDatabase, itm.Query);

                    //
                    // Add computed columns
                    //
                    if (dt.Columns.Contains("database_name"))
                    {
                        dt.Columns["database_name"].ColumnName = "__database_name";
                    }
                    DataColumn cl_db = new DataColumn("database_name", typeof(String));
                    cl_db.DefaultValue = currentDatabase;
                    dt.Columns.Add(cl_db);


                    if (dt.Columns.Contains("collection_time"))
                    {
                        dt.Columns["collection_time"].ColumnName = "__collection_time";
                    }
                    DataColumn cl_dt = new DataColumn("collection_time", typeof(DateTime));
                    cl_dt.DefaultValue = DateTime.Now;
                    dt.Columns.Add(cl_dt);


                    //
                    // Merge collected data in a single DataTable
                    //
                    if (collectedData != null)
                    {
                        collectedData.Merge(dt);
                    }
                    else
                    {
                        collectedData = dt;
                        collectedData.DataSet.RemotingFormat = System.Data.SerializationFormat.Binary;
                        collectedData.RemotingFormat = System.Data.SerializationFormat.Binary;
                    }

                }

                if (verbose) logger.logMessage("Saving to cache file " + destFile);
                //
                // Save data to a binary cache file
                //
                if (File.Exists(destFile))
                {
                    File.Delete(destFile);
                }



                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter fm = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (FileStream fs = new FileStream(destFile, FileMode.CreateNew))
                {
                    fm.Serialize(fs, collectedData);
                    fs.Close();
                }
                if (verbose) logger.logMessage("File saved successfully");

            }


            logger.cleanupLogFiles(cfg.DaysUntilExpiration);

        }

    }
}
