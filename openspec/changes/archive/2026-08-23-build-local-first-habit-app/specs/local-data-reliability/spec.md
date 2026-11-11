## ADDED Requirements

### Requirement: DATA-001 Local data uses a stable location and tested migrations
The system SHALL store its SQLite database in a documented per-user application-data location independent of the launch working directory. Startup SHALL apply ordered EF Core migrations, preserve compatible existing habits and outcomes, and create a recoverable pre-migration backup before modifying a legacy database.

**Risk:** High
**Evidence:** `tests/FunHabitTracker.IntegrationTests/LegacyDatabaseMigrationTests.cs`, `artifacts/evidence/data-path-and-upgrade.md`

#### Scenario: Launch from different working directories
- **WHEN** the same installed application is launched from two different working directories
- **THEN** both launches resolve the same per-user database path

#### Scenario: Upgrade a legacy database
- **WHEN** the application starts with a supported pre-change database containing habits and completions
- **THEN** it creates a safety backup, applies migrations once, and exposes the preserved data through the new model

#### Scenario: Reopen an already migrated database
- **WHEN** the application restarts after a successful migration
- **THEN** startup performs no destructive schema action and all data remains available

### Requirement: DATA-002 Data failures are atomic and actionable
The system MUST enforce relational references and one outcome per habit per date. Multi-record application commands SHALL commit atomically. Startup, migration, write, and restore failures MUST be logged with diagnostic context and presented as an actionable error instead of being silently ignored.

**Risk:** High
**Evidence:** `tests/FunHabitTracker.IntegrationTests/SqliteConstraintTests.cs`, `tests/FunHabitTracker.IntegrationTests/FailureHandlingTests.cs`

#### Scenario: Duplicate outcome is prevented
- **WHEN** two commands attempt to create an outcome for the same habit and date
- **THEN** the database contains one valid outcome and the application returns a controlled result

#### Scenario: Transaction fails midway
- **WHEN** an import or lifecycle command fails after staging multiple changes
- **THEN** none of those changes are committed

#### Scenario: Migration cannot complete
- **WHEN** startup cannot migrate or open the configured database
- **THEN** the host does not serve the normal application and the user receives recovery guidance that identifies the log and backup locations
