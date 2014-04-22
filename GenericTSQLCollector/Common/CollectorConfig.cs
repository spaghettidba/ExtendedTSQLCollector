using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlconsulting.DataCollector.Utils
{
    public class CollectorConfig
    {
        public String CacheDirectory { get; set; }
        public int CacheWindow { get; set; }
        public Boolean CollectorEnabled { get; set; }
        public String MDWDatabase { get; set; }
        public String MDWInstance { get; set; }
        public int DaysUntilExpiration { get; set; }
        public List<String> Databases = new List<String>();
        public List<CollectionItemConfig> collectionItems = new List<CollectionItemConfig>();

    }
}
