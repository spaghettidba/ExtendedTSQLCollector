using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace Sqlconsulting.DataCollector.InstallCollectorType
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

            try
            {
                String SourceServerInstance = options.ServerInstance;
                CollectorTypeInstaller inst = new CollectorTypeInstaller(SourceServerInstance);
                inst.install();
            }
            finally
            {
                //print something to std err
            }
        }
    }


    // Define a class to receive parsed values
    class Options
    {
        [Option('S', "ServerInstance", DefaultValue = "(local)", HelpText = "SQL Server Instance.")]
        public string ServerInstance { get; set; }

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
