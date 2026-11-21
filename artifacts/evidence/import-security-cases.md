# Import validation evidence

Import enforces a 5 MB configurable JSON limit, version and product identifiers, record
caps, positive and unique IDs, valid names, recurrence values, selected weekdays,
referential integrity, valid outcomes, and unique habit/date pairs. Invalid JSON is
rejected without changing existing data. Confirmed replacement creates a pre-import
SQLite safety backup and commits inside one EF transaction.
