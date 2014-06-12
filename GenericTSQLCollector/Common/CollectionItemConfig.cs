using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Sqlconsulting.DataCollector.Utils
{
    public abstract class CollectionItemConfig
    {

        public static Guid CollectorTypeUid;

        public String OutputTable { get; set; }
        public int Frequency { get; set; }
        public int Index { get; set; }
        public Boolean Enabled { get; set; }

    }

    
}
