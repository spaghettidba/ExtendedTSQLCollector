using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlconsulting.DataCollector.Utils
{
    class Alert
    {

        public enum ImportanceLevel
        {
            Low,
            Regular,
            High
        };

        public enum GroupingLevel
        {
            Event,
            CollectionInterval
        };


        public String Operator { get; set; }
        public int Delay { get; set; }
        public Boolean WriteToWindowsLog { get; set; }
        public Boolean WriteToErrorLog { get; set; }
        public String Subject { get; set; }
        public String Filter { get; set; }
        public GroupingLevel Grouping { get; set; }
    }
}


