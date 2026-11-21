## ADDED Requirements

### Requirement: BACKUP-001 JSON export is complete and versioned
The system SHALL export all user-owned habits, schedules, lifecycle state, and daily outcomes to a documented, versioned JSON format without contacting a remote service. The export SHALL include an export timestamp and enough metadata to validate compatibility before import.

**Risk:** Medium
**Evidence:** `tests/FunHabitTracker.IntegrationTests/JsonBackupTests.cs`, `artifacts/evidence/json-schema-v1.md`

#### Scenario: Export a populated tracker
- **WHEN** the user requests a JSON export
- **THEN** the resulting local file contains the current format version and every persisted habit, schedule, and outcome

#### Scenario: Export while offline
- **WHEN** no internet connection is available
- **THEN** JSON export completes without any non-loopback network request

### Requirement: BACKUP-002 JSON import validates before atomic replacement
The system SHALL validate an imported JSON file's size, format version, schema, references, dates, and uniqueness before changing local data. A valid import SHALL create a pre-import SQLite safety backup and replace the dataset in one transaction; an invalid or failed import SHALL leave the original dataset unchanged. The UI MUST present a replacement summary and require explicit confirmation.

**Risk:** High
**Evidence:** `tests/FunHabitTracker.IntegrationTests/JsonRestoreTests.cs`, `artifacts/evidence/import-security-cases.md`

#### Scenario: Replace data from a valid backup
- **WHEN** the user confirms a compatible and valid JSON import
- **THEN** a safety backup is created and the imported dataset becomes active as one atomic operation

#### Scenario: Reject malformed or oversized input
- **WHEN** an import exceeds the configured limit or violates the backup schema
- **THEN** the system rejects it with a safe validation message and the existing dataset remains unchanged

#### Scenario: Roll back a failed import
- **WHEN** persistence fails during a validated import
- **THEN** the transaction rolls back and the original dataset remains usable

### Requirement: BACKUP-003 Full SQLite backup and controlled restore are available
The system SHALL create a consistent user-selected SQLite backup and SHALL restore only a database that passes integrity and compatible-schema checks. Restore MUST prevent concurrent writes, retain the previous database until the replacement opens successfully, and provide clear restart behavior.

**Risk:** High
**Evidence:** `tests/FunHabitTracker.IntegrationTests/SqliteBackupRestoreTests.cs`, `artifacts/evidence/sqlite-restore-interruption.md`

#### Scenario: Create a consistent database backup
- **WHEN** the user requests a full database backup while the application is running
- **THEN** the resulting file passes SQLite integrity checks and represents one consistent state

#### Scenario: Reject an incompatible database
- **WHEN** the user selects a corrupted or unsupported SQLite database for restore
- **THEN** restore is rejected before replacement and the active database remains unchanged

#### Scenario: Restore a compatible database
- **WHEN** the user confirms restore of a validated compatible database
- **THEN** writes are quiesced, the previous database is retained, the replacement becomes active, and the application returns to a usable state
