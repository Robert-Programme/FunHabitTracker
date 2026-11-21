# Data path and legacy upgrade evidence

`AppPathsTests.ConfiguredRoot_IsIndependentFromWorkingDirectory` verifies that the
database path does not change with the process working directory.

`LegacyDatabaseMigrationTests.Initializer_AdoptsEnsureCreatedDatabaseAndPreservesDoneRows`
creates a representative pre-migration SQLite database, adopts it into EF migration
history, creates safety backups, maps legacy frequency to a Daily schedule, removes the
legacy false completion row, preserves the Done row, and reopens the upgraded model.

`LegacyDatabaseMigrationTests.Initializer_ImportsPopulatedLegacyLocationWhenStableDatabaseIsEmpty`
starts with an already initialized but empty stable database, imports a populated legacy
database from the prior content-root location through SQLite's backup API, preserves the
source file, creates a pre-import safety backup, and completes the schema migration. The
import is skipped whenever the stable target already contains habits.
