using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Sqlconsulting.DataCollector.Utils
{
    public abstract class CollectionItemConfig
    {

        public static Guid CollectorTypeUid = new Guid("FD34D746-9A4D-4901-B872-3AF7CDBF7D37");

        public String OutputTable { get; set; }
        public int Frequency { get; set; }
        public int Index { get; set; }


    }

    
}
