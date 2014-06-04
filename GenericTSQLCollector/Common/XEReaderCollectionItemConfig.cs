using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlconsulting.DataCollector.Utils
{
    public class XEReaderCollectionItemConfig : CollectionItemConfig
    {

        static XEReaderCollectionItemConfig()
        {
            CollectorTypeUid = new Guid("57AFAFB4-D4BE-4E62-9C6A-D2F2EA5FC5E9");
        }


        public String SessionName { get; set; }
        public String SessionDefinition { get; set; }
        public String Filter { get; set; }
        public List<String> Columns = new List<String>();

        public List<AlertConfig> Alerts = new List<AlertConfig>();
    }
}
