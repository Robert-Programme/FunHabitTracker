## Context

The existing application is a server-rendered ASP.NET Core MVC prototype using EF Core and SQLite. It can run without a remote service, but it has broken views, unused scheduling data, inconsistent client assets, working-directory-dependent storage, silent startup failures, and only service-level in-memory tests. The change must preserve existing local habits and completions while producing a Windows-first application that remains useful with no network connection.

The primary stakeholder is a single local user. The repository's Factory and OpenSpec controls remain the delivery mechanism. Personal habit descriptions and history are treated as sensitive local data even though the first release has no authentication or cloud boundary.

## Goals / Non-Goals

**Goals:**

- Deliver reliable today-centered tracking and lifecycle management on desktop and narrow responsive viewports.
- Keep all core data and runtime assets local, with deterministic migrations and recovery paths.
- Isolate recurrence, completion, statistics, and backup rules from MVC and EF Core presentation concerns.
- Preserve existing habits and completion history during schema evolution.
- Produce a self-contained Windows release and offline installer with repeatable verification evidence.

**Non-Goals:**

- Cloud sync, accounts, multi-user tenancy, remote APIs, and conflict resolution.
- Native mobile packaging or background mobile execution.
- Rewards, social features, and operating-system reminder scheduling.
- A client-side SPA rewrite.
- Merge-style backup import or third-party import adapters.

## Decisions

### Retain server-rendered MVC with a local-only host

The application will retain ASP.NET Core MVC and bind only to the loopback interface. A desktop launcher will start the local host, prevent accidental duplicate instances, and open the application in the user's browser. Release work will move the application from `net6.0` to a supported .NET LTS target selected and pinned before implementation, and publish it self-contained for `win-x64`.

This preserves the existing Razor and EF Core investment and avoids a high-risk SPA or native rewrite. A PWA-only design was rejected because offline page caching cannot satisfy server-backed form submissions when no local server is running. A native/MAUI rewrite was rejected because it expands the first reliable release across new UI, lifecycle, and packaging stacks simultaneously.

### Establish application services around calendar-based domain rules

Controllers will delegate to explicit services for habit lifecycle, recurrence evaluation, daily outcomes, statistics, and backup operations. Services will use request/response models rather than exposing EF entities directly to forms. `DateOnly` represents the user's local calendar date; injected clock and local-time providers make day-boundary behavior deterministic in tests.

The initial recurrence vocabulary is deliberately bounded to daily and selected weekdays with a start date. New recurrence types can be added through an explicit rule type rather than parsing free-form frequency text. A daily outcome has one of `Done` or `Skipped`; absence of a row means pending. Skipped scheduled days neither extend nor break a streak, while missed scheduled days break it.

### Use additive relational migrations and computed statistics

SQLite remains the system of record. The database will live under a stable per-user application-data directory rather than the process working directory. EF Core migrations will run before the host accepts requests, and migration failure will stop startup with an actionable error and log entry.

Existing integer habit identifiers and completion dates remain valid. Schema changes will add normalized scheduling and lifecycle fields, and convert the current Boolean completion into an explicit outcome without deleting history. Streaks and summary values will be computed from schedules and outcomes rather than stored as mutable counters.

### Make backup formats explicit and restore operations atomic

JSON export will contain a format version, export timestamp, application metadata, habits, schedules, and outcomes. JSON import will validate version, size, shape, references, and uniqueness before changing persistent data. A successful import replaces the local dataset in one transaction after creating an automatic pre-import SQLite safety backup. Merge semantics are deferred.

Full SQLite backup uses SQLite's consistent backup mechanism. Full restore closes active database access, validates the candidate database and schema compatibility, swaps files atomically where supported, and restarts the host. Backup files are user-controlled and are never uploaded.

### Package all runtime resources locally

Core pages will not load fonts, scripts, styles, icons, analytics, or validation libraries from external origins. Required dependencies will be vendored or compiled into the publish output and pinned in the repository. The release verification suite will fail when a core journey initiates a non-loopback network request.

The responsive shell will use server-rendered HTML with progressive enhancement. Core create, edit, complete, skip, archive, export, and import actions must remain functional without JavaScript. JavaScript may improve responsiveness but must guard absent elements and must not own persistence.

### Verify behavior at domain, relational, and journey levels

Pure recurrence and streak rules receive focused unit tests. SQLite integration tests use a real temporary SQLite database to cover migrations, foreign keys, uniqueness, transactions, and restore behavior. ASP.NET integration/browser tests cover the critical user journeys and error pages at desktop and narrow viewport sizes. Release evidence includes offline network assertions and installer smoke tests in addition to existing Factory gates.

## Risks / Trade-offs

- **[High] Existing SQLite files may contain schema states produced by `EnsureCreated`.** -> Detect the baseline schema, create a tested adoption migration, and take a pre-migration backup before the first upgrade.
- **[High] Restore interruption could leave no usable database.** -> Validate first, use temporary files and atomic replacement, retain the previous database until the restored database opens successfully.
- **[High] Calendar and streak semantics can surprise users around day boundaries.** -> Centralize local-date calculation, specify skip behavior, inject the clock, and test DST and midnight boundaries.
- **[Medium] A localhost-hosted desktop app still depends on a browser and local port availability.** -> Bind to loopback on an available port, use single-instance coordination, surface launch failures, and keep a documented direct-launch fallback.
- **[Medium] Self-contained releases increase installer size.** -> Accept the size cost for runtime independence and measure the signed installer as release evidence.
- **[Medium] JSON replacement import is less flexible than merge.** -> Make replacement explicit, create an automatic safety backup, show a preview summary, and defer conflict semantics.
- **[Low] Responsive server rendering offers less app-like interaction than a SPA.** -> Use small progressive enhancements only where they materially improve the daily workflow.

## Migration Plan

1. Capture representative legacy databases and add migration compatibility fixtures.
2. Pin the supported .NET LTS SDK and update build/CI before changing application behavior.
3. Introduce the stable data-path resolver, startup diagnostics, and automatic pre-migration backup.
4. Add the new schema through additive migrations and map legacy `Frequency` and Boolean completion values to daily schedules and explicit outcomes.
5. Introduce domain/application services and move existing actions behind them while preserving routes during transition.
6. Replace the UI flow and local assets, then add backup/restore and packaging.
7. Run relational, browser, offline, installer, OpenSpec, Factory, build, and test gates against both clean and upgraded installations.

Rollback retains the pre-migration database and previous application package. The upgraded package must not delete that backup automatically. Restoring the previous version requires restoring its matching pre-migration database; forward-migrated files are not assumed to be backward compatible.

## Open Questions

- Select the installer generator during the packaging task based on offline installation, CI automation, signing support, and license constraints; the required installer behavior is fixed by the spec.
- Decide final product naming and visual identity before release packaging. This does not alter the capability requirements.
