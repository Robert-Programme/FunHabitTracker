## 1. Platform and Test Foundation

- [x] 1.1 Select a supported .NET LTS SDK, pin it for local and CI builds, upgrade all projects, and document required developer prerequisites.
- [x] 1.2 Capture clean and representative legacy `EnsureCreated` SQLite databases as migration test fixtures without committing personal data.
- [x] 1.3 Add domain, real-SQLite integration, ASP.NET integration, and browser test layers with isolated temporary data directories.
- [x] 1.4 Add injectable clock and local-calendar providers with deterministic midnight and daylight-saving test cases.
- [x] 1.5 Repair the current Razor layout/error pipeline and add smoke tests proving every existing navigation target renders successfully before feature expansion.

## 2. Reliable Local Persistence

- [x] 2.1 Implement and test a per-user application-data path resolver for the database, logs, migration backups, and automatic safety backups.
- [x] 2.2 Replace `EnsureCreated` and swallowed startup exceptions with migration-on-start, structured diagnostics, and an actionable startup failure screen.
- [x] 2.3 Define explicit recurrence, lifecycle, and daily outcome models while preserving existing integer habit identifiers and unique habit/date outcomes.
- [x] 2.4 Create additive EF Core migrations that adopt supported legacy schemas and map free-form legacy frequency values to Daily schedules.
- [x] 2.5 Migrate Boolean completion rows to explicit Done outcomes, preserve dates and references, and verify migration idempotency against all fixtures.
- [x] 2.6 Enforce SQLite foreign keys, uniqueness, transaction boundaries, and controlled concurrency outcomes with relational integration tests.
- [x] 2.7 Create and verify automatic pre-migration backups and document rollback using the matching prior application package.

## 3. Domain and Application Services

- [x] 3.1 Implement Daily and Selected Weekdays recurrence evaluation using the injected local-calendar provider.
- [x] 3.2 Implement validated habit create, edit, archive, and restore services with request/response models that do not expose EF entities to forms.
- [x] 3.3 Implement idempotent Pending, Done, and Skipped outcome transitions, including controlled handling of missing or archived habits.
- [x] 3.4 Implement history, completion summaries, and computed streak semantics for scheduled, skipped, unscheduled, and missed dates.
- [x] 3.5 Add focused unit and SQLite integration tests covering all SCHED, TRACK, and DATA scenarios.

## 4. Today-Centered Responsive UI

- [x] 4.1 Define the product shell, navigation, typography, status language, and responsive layout for desktop and 360 CSS-pixel viewports.
- [x] 4.2 Replace direct DbContext controller logic with application-service calls and retain anti-forgery protection for every state-changing form.
- [x] 4.3 Build the Today view with due habits, Pending/Done/Skipped actions, completion summary, clear empty state, and no-JavaScript form behavior.
- [x] 4.4 Build validated create and edit flows using explicit recurrence controls and field-level server validation.
- [x] 4.5 Build habit management, archive/restore, history, and streak views while preserving access to archived history.
- [x] 4.6 Remove dead scripts, missing DOM assumptions, nested document structures, and unreferenced prototype views and styles.
- [x] 4.7 Package all required scripts, styles, icons, fonts, and validation assets locally and remove production CDN references.
- [x] 4.8 Add keyboard, accessible-name, focus, contrast, long-content, and non-color status tests for core screens.

## 5. Backup and Restore

- [x] 5.1 Define and document JSON backup schema version 1 with application metadata, habits, schedules, lifecycle state, and outcomes.
- [x] 5.2 Implement streaming JSON export and verify complete round-trip output without non-loopback network access.
- [x] 5.3 Implement size-limited JSON upload, schema/reference/uniqueness validation, and a replacement preview that requires explicit confirmation.
- [x] 5.4 Implement automatic pre-import SQLite backup and transactional dataset replacement with rollback tests for injected failures.
- [x] 5.5 Implement consistent full SQLite backup with integrity verification and user-selected destination handling.
- [x] 5.6 Implement controlled SQLite restore with write quiescence, compatibility validation, retained previous database, atomic replacement, and restart recovery tests.
- [x] 5.7 Add user-facing privacy and recovery documentation explaining that backups contain personal habit history and are never uploaded.

## 6. Offline Desktop Distribution

- [x] 6.1 Implement a loopback-only launcher with available-port selection, single-instance behavior, browser opening, and actionable launch failure handling.
- [x] 6.2 Add a self-contained `win-x64` publish profile and verify normal operation on a machine without a separately installed .NET runtime.
- [x] 6.3 Evaluate installer generators against offline installation, CI, signing, upgrade, uninstall, and license requirements; record and apply the selected option.
- [x] 6.4 Build an offline installer with Start Menu launch entry, data-preserving upgrade/uninstall defaults, version metadata, and deterministic release output.
- [x] 6.5 Run clean-install, upgrade, uninstall, blocked-network, duplicate-launch, port-conflict, and loopback-binding smoke tests and capture evidence.

## 7. End-to-End Evidence and Delivery

- [x] 7.1 Add browser journeys for create, edit, complete, skip, archive, history, JSON export/import, failure pages, and no-JavaScript operation.
- [x] 7.2 Add a release test that blocks external networking and fails on any non-loopback request during every core journey.
- [x] 7.3 Capture required responsive screenshots, offline network logs, migration evidence, backup interruption evidence, and installer reports under `artifacts/evidence/`.
- [x] 7.4 Update README, AGENTS.md, operations documentation, backup/restore guidance, data locations, and install/uninstall instructions.
- [x] 7.5 Run `dotnet build FunHabitTracker.sln --configuration Release`, `dotnet test FunHabitTracker.sln`, `npm run openspec:validate`, and `npm run factory:all` and resolve every failure.
- [x] 7.6 Perform a final clean-machine acceptance pass against every OpenSpec scenario and record requirement-to-test traceability before release.
