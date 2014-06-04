﻿using CommandLine;
using CommandLine.Text;
using Sqlconsulting.DataCollector.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Sqlconsulting.DataCollector.ExtendedTSQLUploader
{
    class Program
    {
        static void Main(string[] args)
        {


            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                return;
            }

            bool verbose = options.Verbose;
            CollectorLogger logger = null;

            
            try
            {
                String SourceServerInstance = options.ServerInstance;
                Guid CollectionSetUid = new Guid(options.CollectionSetUID);
                int ItemId = options.CollectionItemID;
                int LogId = options.LogId;

                logger = new CollectorLogger(SourceServerInstance, CollectionSetUid, ItemId);

                if (verbose) logger.logMessage("Starting");

                Uploader uploader = new Uploader(SourceServerInstance, CollectionSetUid, ItemId, LogId);
                uploader.verbose = verbose;
                uploader.UploadData();

                if (verbose) logger.logMessage("Ending with success");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                if (verbose) logger.logMessage("Ending with failure");
                if (verbose) logger.logMessage(e.Message);
            }
            

        }
    }

    // Define a class to receive parsed values
    class Options
    {
        [Option('S', "ServerInstance", DefaultValue = "(local)", HelpText = "SQL Server Instance.")]
        public string ServerInstance { get; set; }

        [Option('c', "CollectionSetUID", Required = true, HelpText = "Collection set UID.")]
        public String CollectionSetUID { get; set; }

        [Option('i', "ItemID", Required = true, HelpText = "Collection item UID.")]
        public Int32 CollectionItemID { get; set; }

        [Option('l', "LogId", Required = true, HelpText = "Log Id in the DCEXEC logging tables.")]
        public Int32 LogId { get; set; }

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
