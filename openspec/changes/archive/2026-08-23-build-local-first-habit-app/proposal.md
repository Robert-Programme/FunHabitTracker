## Why

FunHabitTracker currently demonstrates basic habit creation and daily toggling, but core user flows are incomplete or unreliable and the documented product promises are not implemented. This change establishes a dependable, installable, local-first habit tracker whose essential workflows work without an internet connection and whose data remains under the user's control.

## What Changes

- Replace the prototype navigation and screens with a responsive, today-centered product flow for completing, skipping, creating, editing, archiving, and reviewing habits.
- Introduce explicit recurrence schedules so the Today view shows only habits due on the selected local date.
- Separate habit, scheduling, completion, statistics, and backup behavior into testable domain/application services.
- Make SQLite lifecycle management deterministic through migrations, a stable per-user data location, observable startup failures, and transactional writes.
- Add local history, streak, and completion summaries derived from recorded habit events.
- Add versioned JSON backup export and validated import, plus an explicit full SQLite backup/restore path when the application is not writing to the database.
- Provide a self-contained Windows-first installation and launch experience that requires no internet connection for normal operation.
- Remove runtime CDN dependencies from core screens and package all required static assets locally.
- Add end-to-end and relational integration coverage for critical user journeys and failure cases.

### Non-goals

- User accounts, multi-user isolation, cloud storage, and cross-device synchronization.
- Native Android or iOS applications.
- Social features, shared challenges, or competitive leaderboards.
- Advanced rewards, achievements, or reminder delivery; these can be proposed after the reliable local-first foundation exists.
- Import compatibility with unspecified third-party habit trackers.

## Capabilities

### New Capabilities

- `daily-habit-tracking`: Today-centered habit completion, skip states, history, streaks, and summaries.
- `habit-scheduling`: Habit lifecycle management and explicit recurrence rules evaluated in local calendar time.
- `local-data-reliability`: Deterministic SQLite storage, schema migration, transactional behavior, and actionable failure handling.
- `backup-restore`: Versioned JSON export/import and controlled full-database backup/restore.
- `offline-desktop-distribution`: Self-contained Windows installation and operation without network dependencies.

### Modified Capabilities

None. The existing `factory-process` capability governs delivery controls but its externally observable requirements do not change.

## Impact

- **Application areas:** MVC controllers and views, domain models, application services, EF Core context and migrations, static assets, configuration, startup, and error handling.
- **Database schema:** Affected. Recurrence, lifecycle state, richer daily outcomes, and backup metadata require additive migrations and explicit compatibility handling for existing local data.
- **Security:** Affected. Imports must be size-limited and schema-validated, state-changing requests must retain CSRF protection, and backups may contain sensitive personal habit data.
- **Operations:** Affected. The project gains self-contained packaging, stable per-user data paths, migration-on-start behavior, backup procedures, and release evidence.
- **Dependencies:** Any UI or packaging dependency must support fully local runtime use and be pinned and covered by repository security checks.
- **Compatibility:** Existing habits and completion records must survive migration. Cloud and mobile compatibility are intentionally deferred.
