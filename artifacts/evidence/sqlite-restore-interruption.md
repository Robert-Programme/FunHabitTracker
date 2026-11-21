# SQLite restore recovery evidence

`BackupServiceTests.SqliteBackupRestore_ReplacesOnlyWithValidDatabase` verifies a
consistent backup, a later mutation, integrity/schema validation, and restoration of the
backed-up state. Restore retains a `pre-restore` database and copies it back if opening or
integrity-checking the replacement fails. Database mutation is serialized through a
process-wide semaphore and SQLite connection pools are cleared before file replacement.
