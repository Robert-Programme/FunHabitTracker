# Agent Guide

## Purpose

FunHabitTracker is a small ASP.NET Core MVC application for tracking daily habits. Users can create habits, see active habits for the current day, and toggle whether each habit is done today.

## Stack

- .NET 8 LTS (SDK 8.0.204 pinned in `global.json`)
- ASP.NET Core MVC with Razor views
- WPF, Windows Forms `NotifyIcon`, and WebView2 for the Windows shell
- Entity Framework Core
- SQLite for local app data
- MSTest with EF Core InMemory for service tests

## Project Layout

- `FunHabitTracker.sln` - solution file.
- `src/FunHabitTracker/Program.cs` - browser fallback entry point.
- `src/FunHabitTracker/Infrastructure/HabitTrackerWebApplication.cs` - reusable loopback host composition, startup, migrations, and shutdown.
- `src/FunHabitTracker.Desktop/` - Windows shell, tray lifecycle, embedded browser, activation protocol, desktop settings, and diagnostics.
- `src/FunHabitTracker/Data/AppDbContext.cs` - EF Core context and model configuration.
- `src/FunHabitTracker/Infrastructure/` - local calendar, stable data paths, and database initialization.
- `src/FunHabitTracker/Models/` - habit, recurrence, completion, and outcome entities.
- `src/FunHabitTracker/Controllers/HomeController.cs` - today's habit dashboard, completion toggling, and quick habit creation.
- `src/FunHabitTracker/Controllers/HabitsController.cs` - habit list and create form.
- `src/FunHabitTracker/Views/` - Razor pages for the dashboard and habit CRUD screens.
- `tests/FunHabitTracker.Tests/` - MSTest unit, real-SQLite, migration, application, backup, and offline journey coverage.
- `tests/FunHabitTracker.Desktop.Tests/` - desktop activation, navigation, settings, failure, and lifecycle coverage.
- `tests/browser/desktop-acceptance.cjs` - keyboard, responsive, high-contrast, screenshot, and loopback-only browser acceptance.

## Local Commands

Run from the repository root:

```powershell
dotnet restore
dotnet test
dotnet run --project src\FunHabitTracker\FunHabitTracker.csproj
dotnet run --project src\FunHabitTracker.Desktop\FunHabitTracker.Desktop.csproj
```

To bind to a predictable local URL:

```powershell
$env:ASPNETCORE_URLS='http://localhost:5016'
dotnet run --project src\FunHabitTracker\FunHabitTracker.csproj
```

## Runtime Data

The app stores SQLite data under `%LOCALAPPDATA%\FunHabitTracker` by default. Override it with `FunHabitTracker__DataDirectory` or `FUNHABITTRACKER_DATA_DIR` for isolated development and tests. Local database files are ignored by Git.

On first startup with an empty default database, the initializer imports a populated legacy `habits.db` from the application content root or working directory and leaves the source untouched. Custom data directories do not perform this discovery unless `FunHabitTracker__ImportLegacyDatabaseLocation=true` is explicitly set.

ASP.NET Data Protection keys, backups, import staging, and startup logs live under the same stable per-user data root. These directories are runtime-only and ignored by Git.

## Domain Notes

- `Habit` is the main tracked item. New behavior uses `StartDate`, `RecurrenceType`, `ScheduledDays`, and archive state; `Frequency` and `IsActive` remain only for legacy database compatibility.
- `HabitCompletion` records one date-specific `Done` or `Skipped` outcome; absence means Pending.
- `AppDbContext` enforces a unique index on `(HabitId, Date)`, so there should be at most one completion row per habit per day.
- `DailyTrackingService` evaluates due habits, outcomes, history, summaries, and streaks.
- `HabitService` owns validated create, edit, archive, and restore behavior.
- `BackupService` owns versioned JSON and consistent SQLite backup/restore.

## Test Notes

Tests include pure recurrence/streak rules, real SQLite constraints and migration adoption, Razor route smoke coverage, no-JavaScript core journeys, backup validation, and installer smoke evidence. Keep relational behavior on SQLite tests rather than relying only on EF InMemory.

## Known Constraints

- Cloud sync, accounts, native mobile packaging, reminders, rewards, and achievements remain outside the current release.
- The desktop owner uses a random loopback port, an embedded WebView2 window, and a tray icon. Browser mode remains a supported fallback. Neither mode may bind to a non-loopback interface.
- Import replaces the complete local dataset after validation and an automatic safety backup; merge semantics are not supported.

## Factory Workflow

All features and material behavior changes use OpenSpec before implementation:

1. Explore the repository and existing specs without changing application code.
2. Run `$openspec-propose "change description"` and review proposal, design,
   requirements, risk, and tasks.
3. Implement only after the change artifacts are coherent and approved.
4. Use `$openspec-apply-change` to execute the approved tasks.
5. Run `npm run openspec:validate`, `npm run factory:all`, and the project tests.
6. Archive completed changes with `$openspec-archive-change` after all gates pass.

Small documentation-only corrections may skip a new proposal when they do not
change behavior, contracts, dependencies, architecture, security, or operations.

## Definition of Done

Work is complete only when:

- The implementation is traceable to stable requirement IDs and acceptance criteria.
- `npm run openspec:validate` passes.
- `npm run factory:all` passes.
- `dotnet test` passes.
- Relevant documentation and operational notes are updated.
- Generated evidence contains no secrets and failed gates have not been bypassed.

## Agent Boundaries

- Default autonomy is read-only analysis. Repository changes require an explicit task.
- Work on a dedicated branch or isolated worktree for material changes.
- Do not disable tests, scanners, traceability, or CI to obtain a passing result.
- Do not include production secrets, personal data, local databases, or Data
  Protection keys in prompts, tests, logs, commits, or evidence.
- Do not directly modify production. A runtime failure must use rollback first;
  repairs go through a new verified pull request.
- Direct production autonomy level A7 is forbidden.
- Preserve unrelated user changes in a dirty worktree and stage only task-owned files.

## Factory Commands

```powershell
npm ci
npm run openspec:validate
npm run factory:spec
npm run factory:build
npm run factory:test
npm run factory:security
npm run factory:all
```

The authoritative gate policy is `quality-policy.yaml`. `ci/factory.py` must call
the real project toolchain rather than substituting placeholder checks.

## Agent skills

### Issue tracker

Issues are tracked in GitHub under `Robert-Programme/FunHabitTracker`. See `docs/agents/issue-tracker.md`.

### Triage labels

The five canonical triage roles use their default label names. See `docs/agents/triage-labels.md`.

### Domain docs

The project uses a single-context layout with `GLOSSARY.md` at the repository root and ADRs under `docs/adr/`. See `docs/agents/domain.md`.
