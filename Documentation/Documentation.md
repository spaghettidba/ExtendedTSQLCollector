**Installation**

* Download the appropriate package (x86 or x64) depending on the bitness of the SQL Server instance to monitor. A 32 bit instance on a 64 bit OS needs the x86 setup kit.
* Make sure the SSIS components are installed on the target SQL Server machine.
* Install the msi to the target SQL Server machine. Remember that the collection items run locally on the SQL Server box.
* Install the msi to your client machine, in order to manage data collection remotely.
* Run the ExtendedTSQLCollector application in your Start Menu. It will start the CollectionSet Manager, which will ask for a target SQL Server instance: type the instance name and hit "Connect". 
* Select the Collector Types node and hit the "Install" or "Update" button in the right panel. This step installs the collector types and their SSIS packages to the target SQL Server instance.


**Usage**

* Create a collection item using one of the collector types provided
	* [Using the TSQL Collector type](Using%20the%20TSQL%20Collector%20type.md)
	* [Using the XEReader Collector type](Using%20the%20XEReader%20Collector%20type.md)
* Start the collection set from SSMS or from the CollectionSet Manager
	* The target table in the MDW database is created automatically whenever possible. 
* Review the collected data in the MDW database

**Troubleshooting**
* A log file can be found at %ProgramFiles%\ExtendedTSQLCollector\Logs.
* Future versions of the CollectionSet Manager will show the logs directly from the UI

**Need help?**  
Twitter @spaghettidba
Email spaghettidba@sqlconsulting.it


.