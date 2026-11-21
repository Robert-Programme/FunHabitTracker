# Backup and Restore

Backups contain personal habit descriptions and history. FunHabitTracker processes
them locally and does not upload them.

## JSON

Use **Daten > JSON exportieren** for a portable backup. To restore, select the JSON
file, review the counts and export timestamp, and confirm replacement. Invalid,
oversized, duplicate, or incompatible data is rejected before the database changes.
An automatic SQLite safety backup is created before a confirmed import.

## SQLite

Use **Daten > SQLite sichern** for a complete technical database copy. Restore accepts
only a database that passes SQLite integrity checks and contains the current migration.
The previous database is retained under the local `Backups` directory before replacement.

## Locations and rollback

The default data root is `%LOCALAPPDATA%\FunHabitTracker`. It contains `habits.db`,
`Backups`, `Imports`, `Logs`, and Data Protection keys. Set
`FunHabitTracker__DataDirectory` or `FUNHABITTRACKER_DATA_DIR` for an isolated custom
location.

For rollback after a schema upgrade, reinstall the matching previous application
package and restore its `pre-migration` backup. Do not open a forward-migrated database
with an older package.
