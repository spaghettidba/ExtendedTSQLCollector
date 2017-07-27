using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Sqlconsulting.DataCollector.Utils;

namespace Sqlconsulting.DataCollector.ExtendedXEReaderUploader
{
    public class Uploader : Sqlconsulting.DataCollector.Utils.Uploader
    {


        public Uploader(
                String SourceServerInstance,
                Guid CollectionSetUid,
                int ItemId,
                int LogId
            ): base(SourceServerInstance, CollectionSetUid, ItemId, LogId)
        {
           
        }



       /*
         * Creates the target table from the 
         * output definition of the query
         */
        protected override String createTargetTable(
                CollectorConfig cfg,
                CollectionItemConfig itm
            )
        {
            return checkTable(cfg,itm);
        }

    }
}
