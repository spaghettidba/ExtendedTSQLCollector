SELECT 'collection_sets' AS item_type,  collection_set_uid, name, is_system
FROM msdb.dbo.syscollector_collection_sets

UNION ALL

SELECT 'collector_types' AS item_type, collector_type_uid, name, is_system
FROM msdb.dbo.syscollector_collector_types

ORDER BY item_type DESC, is_system DESC, name ASC


