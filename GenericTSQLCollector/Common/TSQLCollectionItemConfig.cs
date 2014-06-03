using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Sqlconsulting.DataCollector.Utils
{
    public class TSQLCollectionItemConfig : CollectionItemConfig
    {

        static TSQLCollectionItemConfig()
        {
            CollectorTypeUid = new Guid("FD34D746-9A4D-4901-B872-3AF7CDBF7D37");
        }

        public String Query { get; set; }

    }
}
