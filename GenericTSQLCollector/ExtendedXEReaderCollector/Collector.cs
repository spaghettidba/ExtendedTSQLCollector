using Microsoft.SqlServer.XEvent.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sqlconsulting.DataCollector.Utils;
using System.Data;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;

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


        private Thread collectorThread;


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

            collectorThread = Thread.CurrentThread;
            Task.Factory.StartNew(() => checkCollectionSetEnabled());

            if (verbose) logger.logMessage("Entering collection items loop");
            foreach (CollectionItemConfig item in cfg.collectionItems)
            {
                XEReaderCollectionItemConfig itm = (XEReaderCollectionItemConfig)item;
                if (verbose) logger.logMessage("Processing item n. " + itm.Index);
                if (verbose) logger.logMessage("Processing session " + itm.SessionDefinition);

                collectedData = null;


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
                        // Apply filter
                        //
                        DataView dw = new DataView(dt);
                        dw.RowFilter = itm.Filter;
                        dt = dw.ToTable();

                        //
                        // Merge collected data in a single DataTable
                        //
                        if (collectedData != null && dt.Rows.Count > 0)
                        {
                            collectedData.Merge(dt);
                        }
                        else
                        {
                            collectedData = dt;
                            collectedData.RemotingFormat = System.Data.SerializationFormat.Binary;
                        }



                        //
                        // Process rows to fire alerts if needed
                        //
                        foreach (AlertConfig currentAlert in itm.Alerts)
                        {
                            foreach (DataRow currentRow in dt.Select(currentAlert.Filter))
                            {
                                //TODO: Process alerts
                            }
                        }
                        


                        // 
                        // Write to a cache file
                        //
                        if(collectedData.Rows.Count > 0)
                            WriteCacheFile(collectedData, itm);


                    }
                    catch (Exception e)
                    {
                        // TODO: capture the session related exceptions
                        throw e;
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
                // create the session
                CollectorUtils.InvokeSqlCmd(SourceServerInstance, "master", itm.SessionDefinition);
                // repeat the check to see if 
                // the session has been started
                sess = CollectorUtils.GetDataTable(SourceServerInstance, "master", sql_check_session);
            }

            if (sess.Rows[0]["state"].ToString().Equals("OFF"))
            {
                // TODO: start the session
                String sql_startSession = @"
                    ALTER EVENT SESSION [{0}] 
                    ON SERVER
                    STATE = start;
                ";
                sql_startSession = String.Format(sql_startSession, itm.SessionName);
                CollectorUtils.InvokeSqlCmd(SourceServerInstance, "master", sql_startSession);
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

            collectedData.Rows.Clear();
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
                DataColumn dc = null;
                if (fld.Type.IsSerializable)
                {
                    dc = dt.Columns.Add(fld.Name, fld.Type);
                }
                else
                {
                    dc = dt.Columns.Add(fld.Name, typeof(String));
                }
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
                if (fld.Type.IsSerializable)
                {
                    row.SetField(fld.Name, fld.Value);
                }
                else
                {
                    row.SetField(fld.Name, fld.Value.ToString());
                }
            }

            foreach (PublishedAction act in evt.Actions)
            {
                row.SetField(act.Name, act.Value);
            }

            dt.Rows.Add(row);

            return dt;
        }



        private void checkCollectionSetEnabled()
        {
            String sql = @"
                SELECT is_running
                FROM msdb.dbo.syscollector_collection_sets
                WHERE collection_set_uid = '{0}'
            ";
            sql = String.Format(sql,this.CollectionSetUid);

            while (true)
            {
                Boolean is_running = (Boolean)CollectorUtils.GetScalar(SourceServerInstance, "master", sql);

                if (!is_running)
                {
                    stopCollector();
                }
                else
                {
                    Thread.Sleep(60000);
                }
            }
        }



        private void stopCollector()
        {
            collectorThread.Abort();
        }

    }
}
