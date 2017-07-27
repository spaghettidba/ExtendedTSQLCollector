# Using the XEReader Collector type

Here is an example of how to use the ExtendedXEReader Collector Type.
The parameters found in the XML schema are described below:

* <Session> - is the top container describing the interaction between the collector and the XE session
	* <Name> - Name of the Extended Events session to attach to
	* <OutputTable> - Name of the output table to create in MDW
	* <Definition> - XE Session definition. Insert here the CREATE SESSION statement
	* <Filter> - Filter to apply to the events collected.
	* <ColumnsList> - Comma separated list of the columns to collect from the session
* <Alert> - this is the container of the alerts fired by the collector
	* Attributes: Enabled="true/false" WriteToERRORLOG="true/false" WriteToWindowsLog="true/false"
	* <Sender> - Sender address. In this implementation it is the name of a dbmail profile.
	* <Recipient> - Target recipient for the alert.
	* <Subject> - Subject of the emails
	* <Importance> - Email importance
	* <ColumnsList> - Comma separated list of the columns to include in the email message
	* <Filter> - Filter to apply to the alerts. It can be a different filter, to reduce the number of messages sent
	* <Mode> - It can be "Atomic" or "Grouped" - In the first case, alerts are sent as soon as the event is received, in the latter alerts are grouped and sent in a single message with frequency equal to the collection frequency specified in the Delay parameter
	* <Delay> - Specifies the number of seconds to wait before grouping and sending alerts in Grouped mode.

```sql
-- Enable editing advanced configuration options
EXEC sp_configure 'advanced', 1
RECONFIGURE 
GO

-- Set the blocked process threshold
EXEC sp_configure 'blocked process threshold (s)', 15
RECONFIGURE 
GO


USE msdb;
GO
IF EXISTS (
	SELECT 1
	FROM msdb.dbo.syscollector_collection_sets
	WHERE name = N'Blocked Process Reports'
)
EXEC dbo.sp_syscollector_delete_collection_set 
    @name = N'Blocked Process Reports';
   

-----------------------------------------------------------------------------
/*
    ======
    Create Collection Set
    ======
*/
-- Create the collection set
RAISERROR ('Creating collection set "Blocked Process Reports"...',0, 1) WITH NOWAIT;
DECLARE @schedule_uid UNIQUEIDENTIFIER;
SELECT @schedule_uid = (select schedule_uid from sysschedules_localserver_view where name=N'CollectorSchedule_Every_5min');
DECLARE @collection_set_id INT;
EXEC dbo.sp_syscollector_create_collection_set 
    @name = N'Blocked Process Reports', 
    @schedule_uid = @schedule_uid,  -- 5 minutes
    @collection_mode = 0,           -- cached
    @days_until_expiration = 30,    -- retain stats for 30 days
    @description = N'Collects Blocked Process Reports using Extended Events', 
    @collection_set_id = @collection_set_id output, 
    @collection_set_uid = '9FD45270-2A37-47ED-BE6E-DEA413095420';




-- Create a collection item to capture blocked processes
DECLARE @parameters xml;
DECLARE @collection_item_id INT;
SELECT @parameters = convert(xml, N'
<ns:ExtendedXEReaderCollector xmlns:ns="DataCollectorType">
    <Session>
        <Name>blocked_processes</Name>
        <OutputTable>blocked_processes_table</OutputTable>
		<Definition>
		CREATE EVENT SESSION [blocked_processes](blocked_processes) ON SERVER ADD EVENT sqlserver.blocked_process_report
		WITH (
			MAX_MEMORY = 2048 KB
			,EVENT_RETENTION_MODE = ALLOW_SINGLE_EVENT_LOSS
			,MAX_DISPATCH_LATENCY = 30 SECONDS
			,MAX_EVENT_SIZE = 0 KB
			,MEMORY_PARTITION_MODE = NONE
			,TRACK_CAUSALITY = OFF
			,STARTUP_STATE = ON
			)
		</Definition>
		<Filter>duration &lt;= 30000000</Filter>
		<ColumnsList>blocked_process</ColumnsList>
    </Session>
	<Alert Enabled="true" WriteToERRORLOG="true" WriteToWindowsLog="false">
		<Sender>MailProfile</Sender>
		<Recipient>DbAdmins@mycompany.com</Recipient>
		<Subject>Blocked process detected</Subject>
		<Importance>High</Importance>
		<ColumnsList>blocked_process</ColumnsList>
		<Filter>duration &lt;= 30000000</Filter>
		<Mode>Atomic</Mode>
        <Delay>60</Delay>
	</Alert>
</ns:ExtendedXEReaderCollector>
');

RAISERROR ('Creating collection item "Blocked Process Reports"...',0, 1) WITH NOWAIT;
EXEC dbo.sp_syscollector_create_collection_item
    @collection_set_id = @collection_set_id,
    @collector_type_uid = N'57AFAFB4-D4BE-4E62-9C6A-D2F2EA5FC5E9', -- Collector Type
    @name = 'Test Item',
    @frequency = 60, 
    @parameters = @parameters,
    @collection_item_id = @collection_item_id OUTPUT;

GO 
```

The created collection set will resemble this:

![](Using%20the%20XEReader%20Collector%20type_XEReaderCollectionSet.png)

The email messages received will look like this one:

![](Using%20the%20XEReader%20Collector type_email.png)

## Known limitations

* Database Mail alerts are supported. The other kinds of alerts are not supported yet