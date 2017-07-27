# Using the TSQL Collector type

Here is an example of the usage of the ExtendedTSQLCollcector Collector Type.
The XML schema of the parameters is the same exact schema used by the Generic T-SQL Query Collector Type included in SQL Server.

```sql
USE msdb;
GO
-----------------------------------------------------------------------------
/*
    ======
    Create Collection Set
    ======
*/
-- Create the collection set
RAISERROR ('Creating collection set "Single-use Cached Plans"...',0, 1) WITH NOWAIT;
DECLARE @schedule_uid UNIQUEIDENTIFIER;
SELECT @schedule_uid = (select schedule_uid from sysschedules_localserver_view where name=N'CollectorSchedule_Every_6h');
DECLARE @collection_set_id INT;
EXEC dbo.sp_syscollector_create_collection_set 
    @name = N'Single Use Cached Plans', 
    @schedule_uid = @schedule_uid,  -- 6 hours
    @collection_mode = 0,           -- noncached
    @days_until_expiration = 30,    -- retain stats for 30 days
    @description = N'Collects Single Use Cached Plans', 
    @collection_set_id = @collection_set_id output, 
    @collection_set_uid = '989413FA-FB42-4B99-A8D1-40C658C8DA6C';



-- Create a collection item to capture cached plans
DECLARE @parameters xml;
DECLARE @collection_item_id INT;
SELECT @parameters = convert(xml, N'
<ns:TSQLQueryCollector xmlns:ns="DataCollectorType">
    <Query>
        <Value>
            SELECT TOP(50) T.[text](text) AS [QueryText](QueryText), cp.size_in_bytes, CAST(P.query_plan AS nvarchar(max)) AS query_plan
            FROM sys.dm_exec_cached_plans AS cp WITH (NOLOCK)
            CROSS APPLY sys.dm_exec_sql_text(plan_handle) AS T
            CROSS APPLY sys.dm_exec_query_plan(plan_handle) AS P
            WHERE cp.cacheobjtype = N''Compiled Plan'' 
            AND cp.objtype = N''Adhoc'' 
            AND cp.usecounts = 1
            ORDER BY cp.size_in_bytes DESC OPTION (RECOMPILE);
        </Value>
        <OutputTable>cached_plans</OutputTable>
    </Query>
</ns:TSQLQueryCollector>
');

RAISERROR ('Creating collection item "Single Use Cached Plans"...',0, 1) WITH NOWAIT;
EXEC dbo.sp_syscollector_create_collection_item
    @collection_set_id = @collection_set_id,
    @collector_type_uid = N'FD34D746-9A4D-4901-B872-3AF7CDBF7D37', -- Collector Type
    @name = 'Single Use Cached Plans Collection Item',
    @frequency = 60, 
    @parameters = @parameters,
    @collection_item_id = @collection_item_id OUTPUT;

GO 
```

After running the script, you will find a custom collection set definded like this:

![](Using%20the%20TSQL%20Collector%20type_collectionset.png)

# Warning

XML columns cannot be added automatically to the target table. If your collection item collects XML columns, create the target table upfront, otherwise cast the XML column to nvarchar(max) in the collection item query.
