using Microsoft.SqlServer.XEvent.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sqlconsulting.DataCollector.Utils;
using System.Data;
using System.IO;

namespace Sqlconsulting.DataCollector.ExtendedXEReaderCollector
{
    class Collector
    {

        private String SourceServerInstance;
        private Guid CollectionSetUid;
        private int ItemId;

        public Boolean verbose { get; set; }

        private CollectorLogger logger;
        XEReaderCollectorConfig cfg = new XEReaderCollectorConfig();



        public Collector(String sourceServerInstance, 
            Guid CollectionSetUid,
            int ItemId
            )
        {
            this.SourceServerInstance = sourceServerInstance;
            this.CollectionSetUid = CollectionSetUid;
            this.ItemId = ItemId;
        }



        public void CollectData()
        {
            logger = new CollectorLogger(SourceServerInstance, CollectionSetUid, ItemId);

            DataTable collectedData = null;

            if (verbose) logger.logMessage("--------------------------------");
            if (verbose) logger.logMessage("    ExtendedXEReaderCollector   ");
            if (verbose) logger.logMessage("--------------------------------");
            if (verbose) logger.logMessage("Copyright© sqlconsulting.it 2014");
            if (verbose) logger.logMessage("-");

            if (verbose) logger.logMessage("Loading configuration");
            //
            // Load Configuration
            //
            cfg = new XEReaderCollectorConfig();
            cfg.readFromDatabase(SourceServerInstance, CollectionSetUid, ItemId);

            String connectionString = String.Format(@"Data Source = {0}; Initial Catalog = master; Integrated Security = SSPI",SourceServerInstance);

            if (verbose) logger.logMessage("Entering collection items loop");
            foreach (CollectionItemConfig item in cfg.collectionItems)
            {
                XEReaderCollectionItemConfig itm = (XEReaderCollectionItemConfig)item;
                if (verbose) logger.logMessage("Processing item n. " + itm.Index);
                if (verbose) logger.logMessage("Processing session " + itm.SessionDefinition);

                collectedData = null;

                DateTime lastEventFlush = new DateTime(1900, 1, 1);

                CheckSession(itm);


                // Queries an existing session
                Microsoft.SqlServer.XEvent.Linq.QueryableXEventData events = new QueryableXEventData(
                    connectionString,
                    itm.SessionName,
                    EventStreamSourceOptions.EventStream,
                    EventStreamCacheOptions.DoNotCache);

                foreach (PublishedEvent evt in events)
                {

                    try
                    {
                        DataTable dt = ReadEvent(evt);

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



                        //
                        // Process rows to fire alerts if needed
                        //
                        foreach (DataRow currentRow in dt.Select(itm.Filter))
                        {
                            //TODO: Process alerts
                        }


                        // 
                        // After the collection frequency has expired, write to a cache file
                        //
                        if (lastEventFlush.AddSeconds(itm.Frequency) <= DateTime.Now)
                        {
                            WriteCacheFile(collectedData, itm);
                            lastEventFlush = DateTime.Now;
                        }
                    }
                    catch (Exception)
                    {
                        // TODO: capture the session related events
                        throw;
                    }

                    
                }

            }


            logger.cleanupLogFiles(cfg.DaysUntilExpiration);
        }


        
        /*
         * Controls that the session is running. If it's not running
         */
        private void CheckSession(XEReaderCollectionItemConfig itm)
        {
            String sql_check_session = @"
                    SELECT name, 
	                    state = 
		                    CASE WHEN EXISTS (
				                    SELECT 1
				                    FROM sys.dm_xe_sessions AS x
				                    WHERE name = sess.name
			                    ) THEN 'ON'
			                    ELSE 'OFF'
		                    END
                    FROM sys.server_event_sessions AS sess
                    WHERE name = '{0}'
                ";

            sql_check_session = String.Format(sql_check_session, itm.SessionName);
            DataTable sess = CollectorUtils.GetDataTable(SourceServerInstance, "master", sql_check_session);

            if (sess.Rows.Count <= 0)
            {
                // TODO: create the session
            }
            else
            {
                if (sess.Rows[0]["xe_session_name"].ToString().Equals("OFF"))
                {
                    // TODO: start the session
                }
            }
        }



        
        
        
        private void WriteCacheFile(DataTable collectedData, CollectionItemConfig itm)
        {
            String ts = DateTime.Now.ToString("yyyyMMddHHmmss");
            String collectorId = CollectorUtils.getCacheFilePrefix(SourceServerInstance, CollectionSetUid, ItemId) + "_" + itm.Index;

            String destFile = Path.Combine(cfg.CacheDirectory, collectorId + "_" + ts + ".cache");

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



        private DataTable ReadEvent(PublishedEvent evt)
        {
            DataTable dt = new DataTable("events");

            //
            // Add computed columns
            //
            if (dt.Columns.Contains("collection_time"))
            {
                dt.Columns["collection_time"].ColumnName = "__collection_time";
            }
            DataColumn cl_dt = new DataColumn("collection_time", typeof(DateTime));
            cl_dt.DefaultValue = DateTime.Now;
            dt.Columns.Add(cl_dt);

            //
            // Add Name column
            //
            dt.Columns.Add("Name", typeof(String));


            //
            // Read event data
            //
            foreach (PublishedEventField fld in evt.Fields)
            {
                DataColumn dc = dt.Columns.Add(fld.Name, fld.Type);
                dc.ExtendedProperties.Add("subtype", "field");
            }

            foreach (PublishedAction act in evt.Actions)
            {
                DataColumn dc = dt.Columns.Add(act.Name, act.Type);
                dc.ExtendedProperties.Add("subtype", "action");
            }

            DataRow row = dt.NewRow();
            row.SetField("Name", evt.Name);

            foreach (PublishedEventField fld in evt.Fields)
            {
                row.SetField(fld.Name, fld.Value);
            }

            foreach (PublishedAction act in evt.Actions)
            {
                row.SetField(act.Name, act.Value);
            }

            dt.Rows.Add(row);

            return dt;
        }

    }
}
