using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Data;
using System.Data.SqlClient;
using System.Collections.Specialized;
using System.IO;
using Sqlconsulting.DataCollector.Utils;
using CommandLine.Text; 
using CommandLine;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace Sqlconsulting.DataCollector.ExtendedXEReaderCollector
{
    class Program
    {

        private static ConcurrentDictionary<String, CollectionItemTask> runningTasks = new ConcurrentDictionary<String, CollectionItemTask>();

        private static Boolean initializing = true;

        static void Main(string[] args)
        {

            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                return;
            }

            bool verbose = options.Verbose;
            String SourceServerInstance = options.ServerInstance;

            CollectorLogger logger = new CollectorLogger(SourceServerInstance);

            string mutex_id = "Global\\ExtendedXEReaderCollector_" + SourceServerInstance.Replace("\\","_");

            using (Mutex mutex = new Mutex(false, mutex_id))
            {
                if (!mutex.WaitOne(0, false))
                {
                    if (verbose) logger.logMessage("Shut down. Only one instance of this program is allowed.");
                    return;
                }

                try
                {
                    
                    if (verbose) logger.logMessage("Starting");

                    // Instantiate a task that loads Collection Items from the database
                    Task.Factory.StartNew(() => loadXECollectionItems(SourceServerInstance, verbose));

                    Thread.Sleep(10000);

                    while (runningTasks.Count > 0 || initializing)
                    {
                        foreach (CollectionItemTask currentTask in runningTasks.Values)
                        {
                            if (currentTask.IsCompleted)
                            {
                                CollectionItemTask v = null;
                                runningTasks.TryRemove(currentTask.GetKey(), out v);
                                if (verbose) logger.logMessage("Task " + v.CollectionSetUid + " is completed.");
                            }
                        }
                        Thread.Sleep(100);
                    }
                    if (verbose) logger.logMessage("Running tasks " + runningTasks.Count + ". Shutting down.");

                    if (verbose) logger.logMessage("Ending with success");

                    System.Environment.Exit(0);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    if (verbose) logger.logMessage("Ending with failure");
                    if (verbose) logger.logMessage(e.Message);
                    if (verbose) logger.logMessage(e.StackTrace.ToString());
                }
            }
        }

        private static void loadXECollectionItems(String ServerInstance, Boolean Verbose)
        {
            String sql = @"
                SELECT cs.collection_set_uid, 
	                ci.collection_item_id
                FROM msdb.dbo.syscollector_collection_items AS ci
                INNER JOIN msdb.dbo.syscollector_collection_sets AS cs
	                ON ci.collection_set_id = cs.collection_set_id 
                WHERE is_running = 1
	                AND collector_type_uid = '{0}';
                ";
            sql = String.Format(sql, XEReaderCollectionItemConfig.CollectorTypeUid);

            Boolean keepLooping = true;


            try
            {
                while (keepLooping)
                {

                    DataTable dt = CollectorUtils.GetDataTable(ServerInstance, "master", sql);

                    if (dt.Rows.Count == 0)
                    {
                        keepLooping = false;
                        break;
                    }

                    foreach (DataRow dr in dt.Rows)
                    {
                        Guid CollectionSetUid = new Guid(dr["collection_set_uid"].ToString());
                        int ItemId = Int32.Parse(dr["collection_item_id"].ToString());
                        CollectionItemTask t = CollectionItemTask.Create(ServerInstance, CollectionSetUid, ItemId, Verbose);
                        if (!runningTasks.ContainsKey(t.GetKey()))
                        {
                            t.Start();
                            runningTasks.TryAdd(t.GetKey(), t);
                            initializing = false;
                        }
                    }

                    Thread.Sleep(60000);

                }
            }
            catch (Exception e)
            {
                initializing = false;
                throw e;
            }

        }


        


    }


    class CollectionItemTask : Task
    {
        public String SourceServerInstance { get; set; }
        public Guid CollectionSetUid { get; set; }
        public int ItemId { get; set; }
        public Boolean Verbose { get; set; }


        protected CollectionItemTask(Action action) : base(action)
        {
        }


        public String GetKey()
        {
            return CollectionSetUid + "_" + ItemId;
        }

        public static CollectionItemTask Create(String SourceServerInstance, Guid CollectionSetUid, int ItemId, Boolean Verbose)
        {
            CollectionItemTask task = new CollectionItemTask(() =>
            {
                try
                {
                    Collector collector = new Collector(SourceServerInstance, CollectionSetUid, ItemId);
                    collector.verbose = Verbose;
                    collector.CollectData();
                }
                catch (Exception e)
                {
                    Console.Write(e.StackTrace);
                }
            });
            task.SourceServerInstance = SourceServerInstance;
            task.CollectionSetUid = CollectionSetUid;
            task.ItemId = ItemId;

            return task;
        }


    }



    // Define a class to receive parsed values
    class Options
    {
        [Option('S', "ServerInstance", DefaultValue = "(local)", HelpText = "SQL Server Instance.")]
        public string ServerInstance { get; set; }

        [Option('v', "Verbose", DefaultValue = false, HelpText = "Enable logging to an output file.")]
        public bool Verbose { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

}
