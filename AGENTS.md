# Agent Guide

## Purpose

FunHabitTracker is a small ASP.NET Core MVC application for tracking daily habits. Users can create habits, see active habits for the current day, and toggle whether each habit is done today.

## Stack

- .NET 10 LTS
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
node --test tests/js/site.test.cjs
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

Service tests cover `HabitService` CRUD behavior with EF Core InMemory. `CreateHabitIntegrationTests` uses WebApplicationFactory and an isolated SQLite in-memory database to cover the create form, validation, persistence, antiforgery, and local validation scripts in Development and Production. These tests run with the normal `dotnet test` command. The JavaScript regression uses Node.js 20+ and the built-in test runner; no npm install is needed.

Pinned browser validation libraries, upstream licenses, source URLs, and checksums are under `src/FunHabitTracker/wwwroot/vendor/`. If changing dashboard behavior, add controller or integration tests rather than relying only on service tests.

## Known Constraints

- The README describes planned features such as charts, reminders, rewards, and achievements. The current implemented surface is smaller: daily tracking, habit creation, listing, and done toggling.
- `Program.cs` currently calls `EnsureCreated()` for local convenience. If introducing migrations-based deployments, replace this with an explicit migration strategy.
- The app targets `net10.0`. Build machines need the .NET 10 SDK pinned in `global.json` and a compatible runtime.
