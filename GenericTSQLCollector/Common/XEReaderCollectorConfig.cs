using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Sqlconsulting.DataCollector.Utils
{
    public class XEReaderCollectorConfig : CollectorConfig
    {

        // TODO: fix generated Guids for packages
        public static readonly Guid CollectorPackageId = new Guid("8439ABB5-8E19-484A-9FA8-E101C4AE9D76");
        public static readonly Guid CollectorVersionId = new Guid("3DC80062-C057-40D9-88B7-703E4A8544CD");

        public static readonly Guid UploaderPackageId = new Guid("93017332-EB18-45AF-9BF2-3E7106104BBF");
        public static readonly Guid UploaderVersionId = new Guid("5032381E-ED82-4320-AA07-201B9CF047AA");

        public override void readFromDatabase(
                   String ServerInstance,
                   Guid CollectionSetUid,
                   int ItemId
               )
        {
            readFromDatabase(ServerInstance);

            String qry = @"
		        DECLARE @x xml;
                DECLARE @fre int;
                DECLARE @is_running bit;

                SELECT @x = parameters,
	                @fre = frequency,
	                @is_running = is_running
                FROM msdb.dbo.syscollector_collection_items AS ci
                CROSS APPLY (
	                SELECT collection_set_id, is_running
	                FROM msdb.dbo.syscollector_collection_sets
	                WHERE collection_set_uid = '{0}'
                ) AS cs
                WHERE ci.collection_set_id = cs.collection_set_id
                AND collection_item_id = {1}
                AND collector_type_uid = '{2}';

                ;WITH XMLNAMESPACES('DataCollectorType' AS ns)
                SELECT 
                    x.value('Name[1]','varchar(max)')         AS xe_session_name,
	                x.value('OutputTable[1]', 'varchar(max)') AS outputTable,
	                x.value('Definition[1]', 'varchar(max)')  AS xe_session_definition,
	                x.value('Filter[1]', 'varchar(max)')      AS xe_session_filter,
	                x.value('ColumnsList[1]', 'varchar(max)') AS xe_session_columnslist,
	                @fre AS frequency,
                    @is_running AS is_enabled
                FROM @x.nodes('/ns:ExtendedXEReaderCollector/Session') Q(x)
                ORDER BY outputTable;
	        ";

            qry = String.Format(qry, CollectionSetUid, ItemId, XEReaderCollectionItemConfig.CollectorTypeUid);

            DataTable data = CollectorUtils.GetDataTable(ServerInstance, "msdb", qry);

            int i = 1;


            foreach (DataRow currentRow in data.Rows)
            {
                XEReaderCollectionItemConfig cic = new XEReaderCollectionItemConfig();

                cic.SessionName = currentRow["xe_session_name"].ToString();
                cic.OutputTable = currentRow["outputTable"].ToString();
                cic.Frequency = Int32.Parse(currentRow["frequency"].ToString());
                cic.SessionDefinition = currentRow["xe_session_definition"].ToString();
                cic.Filter = currentRow["xe_session_filter"].ToString();
                cic.Columns = new List<String>(currentRow["xe_session_columnslist"].ToString().Split(','));
                cic.Enabled = Boolean.Parse(currentRow["is_enabled"].ToString());

                cic.Index = i;
                i++;
                collectionItems.Add(cic);
            }



            qry = @"
        	
		        DECLARE @x xml;

                SELECT @x = parameters
                FROM msdb.dbo.syscollector_collection_items
                WHERE collection_set_id = (
		                SELECT collection_set_id
		                FROM msdb.dbo.syscollector_collection_sets
		                WHERE collection_set_uid = '{0}'
	                )
                AND collection_item_id = {1}
                AND collector_type_uid = '{2}';

		        
                ;WITH XMLNAMESPACES('DataCollectorType' AS ns)
                SELECT 
					x.value('Sender[1]','varchar(max)')              AS alert_sender,
					x.value('Recipient[1]','varchar(max)')           AS alert_recipient,
	                x.value('Filter[1]', 'varchar(max)')             AS alert_filter,
	                x.value('ColumnsList[1]', 'varchar(max)')        AS alert_columnslist,
	                x.value('Mode[1]', 'varchar(max)')               AS alert_mode,
                    x.value('Importance[1]', 'varchar(max)')         AS alert_importance_level,
                    x.value('Delay[1]', 'varchar(max)')              AS alert_delay,
                    x.value('Subject[1]', 'varchar(max)')            AS alert_subject,
					x.value('Body[1]', 'varchar(max)')               AS alert_body,
					x.value('AttachmentFileName[1]', 'varchar(max)') AS alert_attachment_filename,
                    x.value('@WriteToERRORLOG[1]', 'varchar(max)')   AS alert_write_to_errorlog,
                    x.value('@WriteToWindowsLog[1]', 'varchar(max)') AS alert_write_to_windowslog,
					x.value('@Enabled[1]', 'varchar(max)')           AS alert_enabled
                FROM @x.nodes('/ns:ExtendedXEReaderCollector/Alert') Q(x);


	        ";

            qry = String.Format(qry, CollectionSetUid, ItemId, XEReaderCollectionItemConfig.CollectorTypeUid);

            data = CollectorUtils.GetDataTable(ServerInstance, "msdb", qry);

            i = 1;

            foreach (DataRow currentRow in data.Rows)
            {
                AlertConfig a = new AlertConfig();
                a.Sender = currentRow["alert_sender"].ToString();
                a.Recipient = currentRow["alert_recipient"].ToString();
                a.Filter = currentRow["alert_filter"].ToString();
                a.Columns = new List<String>(currentRow["alert_columnslist"].ToString().Split(','));
                a.Mode = (Sqlconsulting.DataCollector.Utils.AlertMode)
                    Enum.Parse(typeof(Sqlconsulting.DataCollector.Utils.AlertMode), currentRow["alert_mode"].ToString());
                a.Importance = (Sqlconsulting.DataCollector.Utils.ImportanceLevel)
                    Enum.Parse(typeof(Sqlconsulting.DataCollector.Utils.ImportanceLevel), currentRow["alert_importance_level"].ToString());
                a.Delay = Int32.Parse(currentRow["alert_delay"].ToString());
                a.Subject = currentRow["alert_subject"].ToString();
                a.WriteToErrorLog = Boolean.Parse(currentRow["alert_write_to_errorlog"].ToString());
                a.WriteToWindowsLog = Boolean.Parse(currentRow["alert_write_to_windowslog"].ToString());
                a.Enabled = Boolean.Parse(currentRow["alert_enabled"].ToString());

                XEReaderCollectionItemConfig itmcfg = (XEReaderCollectionItemConfig)collectionItems[0];
                itmcfg.Alerts.Add(a);
            }


            qry = @"
		        SELECT days_until_expiration
		        FROM msdb.dbo.syscollector_collection_sets
		        WHERE collection_set_uid = '{0}'
	        ";

            qry = String.Format(qry, CollectionSetUid);

            data = CollectorUtils.GetDataTable(ServerInstance, "msdb", qry);

            DaysUntilExpiration = Convert.ToInt32(data.Rows[0]["days_until_expiration"]);

        }
    }
}
