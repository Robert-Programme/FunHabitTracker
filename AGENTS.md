# Agent Guide

## Purpose

FunHabitTracker is a small ASP.NET Core MVC application for tracking daily habits. Users can create habits, see active habits for the current day, and toggle whether each habit is done today.

## Stack

- .NET 6
- ASP.NET Core MVC with Razor views
- Entity Framework Core
- SQLite for local app data
- MSTest with EF Core InMemory for service tests

## Project Layout

- `FunHabitTracker.sln` - solution file.
- `src/FunHabitTracker/Program.cs` - app startup, dependency injection, routing, SQLite setup, and Data Protection key storage.
- `src/FunHabitTracker/Data/AppDbContext.cs` - EF Core context and model configuration.
- `src/FunHabitTracker/Models/` - `Habit` and `HabitCompletion` entities.
- `src/FunHabitTracker/Controllers/HomeController.cs` - today's habit dashboard, completion toggling, and quick habit creation.
- `src/FunHabitTracker/Controllers/HabitsController.cs` - habit list and create form.
- `src/FunHabitTracker/Views/` - Razor pages for the dashboard and habit CRUD screens.
- `tests/FunHabitTracker.Tests/` - MSTest coverage for `HabitService`.

## Local Commands

Run from the repository root:

```powershell
dotnet restore
dotnet test
dotnet run --project src\FunHabitTracker\FunHabitTracker.csproj
```

To bind to a predictable local URL:

```powershell
$env:ASPNETCORE_URLS='http://localhost:5016'
dotnet run --project src\FunHabitTracker\FunHabitTracker.csproj
```

## Runtime Data

The app uses SQLite with `Data Source=habits.db`. Because the working directory can differ by launch method, the database is created relative to the process working directory. Local database files are ignored by Git.

ASP.NET Data Protection keys are stored under `src/FunHabitTracker/.aspnet-data-protection-keys/` so the app can run in restricted users or sandboxes that cannot access the Windows profile key-ring path. This directory is runtime-only and ignored by Git.

## Domain Notes

- `Habit` is the main tracked item. It has `Name`, optional `Description`, `Frequency`, `IsActive`, and `CreatedAt`.
- `HabitCompletion` records one date-specific state for a habit.
- `AppDbContext` enforces a unique index on `(HabitId, Date)`, so there should be at most one completion row per habit per day.
- `HomeController.Today` shows active habits and joins each habit with today's completion row.
- `HomeController.ToggleToday` creates a completion row if missing, otherwise flips `IsDone`.

## Test Notes

Current tests cover `HabitService` CRUD behavior with EF Core InMemory. Controller actions and Razor rendering are not covered yet. If changing dashboard behavior, add controller or integration tests rather than relying only on service tests.

## Known Constraints

- The README describes planned features such as charts, reminders, rewards, and achievements. The current implemented surface is smaller: daily tracking, habit creation, listing, and done toggling.
- `Program.cs` currently calls `EnsureCreated()` for local convenience. If introducing migrations-based deployments, replace this with an explicit migration strategy.
- The app targets `net6.0`. Build machines need a compatible .NET SDK/runtime installed.

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
