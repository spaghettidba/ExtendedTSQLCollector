using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sqlconsulting.DataCollector.Utils
{
    public class CollectorLogger
    {

        private String outputFolder;
        private String outputFile;
        private String prefix;

        public CollectorLogger(string SourceServerInstance, Guid CollectionSetUid, int ItemId)
        {
            prefix = CollectorUtils.getCacheFilePrefix(SourceServerInstance, CollectionSetUid, ItemId);
            prefix += "_" + DateTime.Now.ToString("yyyyMMdd");
            outputFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            outputFolder = Path.Combine(outputFolder, "Logs");
            outputFile = Path.Combine(outputFolder, prefix + "_Collector.log");

            System.IO.DirectoryInfo targetFolder = new System.IO.DirectoryInfo(outputFolder);

            if (!targetFolder.Exists)
            {
                targetFolder.Create();
            }
        }


        public CollectorLogger(string SourceServerInstance) : this(SourceServerInstance, new Guid("00000000-0000-0000-0000-000000000000"), 0)
        {
            
        }


        /*
        * Log messages
        */
        public void logMessage(String message)
        {

            FileInfo fi = new FileInfo(outputFile);
            if(!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outputFile, true))
            {
                file.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message);
            }

            
        }


        public void cleanupLogFiles(Int32 days)
        {
            // Cleanup old log entries
            DateTime border = DateTime.Now.AddDays(days * -1);

            foreach (String fileName in System.IO.Directory.GetFiles(outputFolder))
            {
                System.IO.FileInfo currentFile = new System.IO.FileInfo(fileName);
                if (currentFile.LastWriteTime <= border)
                {
                    currentFile.Delete();
                }
            }
        }

    }
}
