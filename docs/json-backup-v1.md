# JSON Backup Format v1

FunHabitTracker JSON backups are UTF-8 JSON documents with these top-level fields:

- `formatVersion`: integer `1`.
- `exportedAtUtc`: ISO-8601 UTC timestamp.
- `product`: literal `FunHabitTracker`.
- `habits`: all habit definitions, schedules, lifecycle state, and timestamps.
- `outcomes`: all date-specific `Done` or `Skipped` outcomes.

Habit and outcome IDs are positive integers. Habit names are required and limited to
200 characters. Every outcome references an exported habit, and each habit/date pair
is unique. Recurrence values are `Daily` or `SelectedWeekdays`; a selected-weekday
schedule contains at least one weekday flag.

Import is replacement-based, not merge-based. The application validates the complete
document, shows a record-count preview, asks for confirmation, creates a local SQLite
safety backup, and then replaces data in one transaction.
