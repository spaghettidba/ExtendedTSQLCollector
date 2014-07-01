using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Sqlconsulting.DataCollector.Utils;

namespace Sqlconsulting.DataCollector.ExtendedXEReaderCollectorTrigger
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

            CollectorLogger logger = new CollectorLogger(options.ServerInstance);

            try
            {

                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);

                String targetProcess = Path.Combine(Path.GetDirectoryName(path), "ExtendedXEReaderCollector.exe");
                String arguments = "-S " + options.ServerInstance + " -v " + options.Verbose;

                Process.Start(targetProcess, arguments);
            }
            catch(Exception e)
            {
                logger.logMessage(e.StackTrace);
            }
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
