using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Sqlconsulting.DataCollector.Utils;

namespace Sqlconsulting.DataCollector.ExtendedTSQLUploader
{
    class Uploader : Sqlconsulting.DataCollector.Utils.Uploader
    {

        public Boolean verbose { get; set; }


        public Uploader(
                               String SourceServerInstance,
                               Guid CollectionSetUid,
                               int ItemId,
                               int LogId
                           ) : base(SourceServerInstance, CollectionSetUid, ItemId, LogId)
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
            String TableName = itm.OutputTable;
            TSQLCollectionItemConfig itemCfg = (TSQLCollectionItemConfig)itm;

            String CollectorId = CollectorUtils.getCacheFilePrefix(SourceServerInstance, CollectionSetUid, ItemId) + "_" + itm.Index;

            String sqlCheck = @"
                SELECT QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name)  AS targetTable
                FROM [{0}].sys.tables 
                WHERE name = '{1}' 
                    AND schema_id IN (SCHEMA_ID('custom_snapshots'), SCHEMA_ID('snapshots'))
                ORDER BY CASE SCHEMA_NAME(schema_id) 
                        WHEN 'custome_snapshots' THEN 1
                        WHEN 'snapshots' THEN 2 
                    END ";

            sqlCheck = String.Format(sqlCheck, cfg.MDWInstance, itm.OutputTable);

            DataTable data = CollectorUtils.GetDataTable(cfg.MDWInstance, cfg.MDWDatabase, sqlCheck);

            // table is not missing
            if (data.Rows.Count > 0)
            {
                return data.Rows[0]["targetTable"].ToString();
            }


            String statement = @"

	        IF NOT EXISTS (
		        SELECT *
		        FROM sys.servers
		        WHERE NAME = 'LOOPBACK'
	        )
	        BEGIN

		        DECLARE @srv nvarchar(4000);
		        SET @srv = @@SERVERNAME; -- gather this server name
        		 
		        -- Create the linked server
		        EXEC master.dbo.sp_addlinkedserver
		            @server     = N'LOOPBACK',
		            @srvproduct = N'SQLServ', -- it's not a typo: it can't be 'SQLServer'
		            @provider   = N'SQLNCLI', -- change to SQLOLEDB for SQLServer 2000
		            @datasrc    = @srv;
        		 
		        -- Set the authentication to 'current security context'
		        EXEC master.dbo.sp_addlinkedsrvlogin
		            @rmtsrvname  = N'LOOPBACK',
		            @useself     = N'True',
		            @locallogin  = NULL,
		            @rmtuser     = NULL,
		            @rmtpassword = NULL;

	        END
        	 
	        USE tempdb;
	        GO

	        IF OBJECT_ID('{0}') IS NOT NULL
		        DROP PROCEDURE [{0}]
	        GO
        	 
	        CREATE PROCEDURE [{0}]
	        AS
	        BEGIN
	            SET NOCOUNT ON;
        	 
	            {1}
	        END
	        GO

	        IF SCHEMA_ID('custom_snapshots') IS NULL
		        EXEC('CREATE SCHEMA [custom_snapshots]')

	        IF OBJECT_ID('custom_snapshots.{2}') IS NOT NULL
		        DROP TABLE [custom_snapshots].[{2}]
	        GO

	        SELECT TOP 0 *, 
		        CAST(NULL AS sysname) AS _database_name, 
		        CAST(NULL AS datetimeoffset(7)) AS _collection_time,
		        CAST(NULL AS int) AS _snapshot_id
	        INTO tempdb.[custom_snapshots].[{2}]
	        FROM OPENQUERY(LOOPBACK, 'SET FMTONLY OFF; EXEC tempdb.dbo.[{0}]');
        	 
	        DROP PROCEDURE [{0}];
	        GO
        	
	        IF EXISTS(
		        SELECT 1 
		        FROM sys.columns 
		        WHERE name = 'database_name'
		        AND object_id = OBJECT_ID('[custom_snapshots].[{2}]')
	        )
	        BEGIN
		        EXEC sp_rename '[custom_snapshots].[{2}].[database_name]', '__database_name', 'COLUMN';
	        END
	        EXEC sp_rename '[custom_snapshots].[{2}].[_database_name]', 'database_name', 'COLUMN';


	        IF EXISTS(
		        SELECT 1 
		        FROM sys.columns 
		        WHERE name = 'collection_time'
		        AND object_id = OBJECT_ID('[custom_snapshots].[{2}]')
	        )
	        BEGIN
		        EXEC sp_rename '[custom_snapshots].[{2}].[collection_time]', '__collection_time', 'COLUMN';
	        END
	        EXEC sp_rename '[custom_snapshots].[{2}].[_collection_time]', 'collection_time', 'COLUMN';

        	
	        IF EXISTS(
		        SELECT 1 
		        FROM sys.columns 
		        WHERE name = 'snapshot_id'
		        AND object_id = OBJECT_ID('[custom_snapshots].[{2}]')
	        )
	        BEGIN
		        EXEC sp_rename '[custom_snapshots].[{2}].[snapshot_id]', '__snapshot_id', 'COLUMN';
	        END
	        EXEC sp_rename '[custom_snapshots].[{2}].[_snapshot_id]', 'snapshot_id', 'COLUMN';

	        ";

            statement = String.Format(statement, CollectorId, itemCfg.Query, itm.OutputTable);

            CollectorUtils.InvokeSqlBatch(SourceServerInstance, "tempdb", statement);

            String scriptText = CollectorUtils.ScriptTable(SourceServerInstance, "tempdb", TableName);

            CollectorUtils.InvokeSqlCmd(cfg.MDWInstance, cfg.MDWDatabase, scriptText);

            return "[custom_snapshots].[" + TableName + "]";
        }

    }
}
