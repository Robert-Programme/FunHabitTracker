# Fun Habit Tracker

Fun Habit Tracker is a small ASP.NET Core MVC application for maintaining habits and
recording whether each active habit was completed today. It stores local data in SQLite
and renders the interface with Razor views.

## Implemented Features

- Create habits with a name, description, and frequency.
- List configured habits.
- View active habits for the current day.
- Toggle today's completion state for each active habit.
- Persist local habit and completion data in SQLite.

## Requirements

- .NET 6 SDK

## Run Locally

```bash
git clone https://github.com/Robert-Programme/FunHabitTracker.git
cd FunHabitTracker
dotnet restore
dotnet run --project src/FunHabitTracker/FunHabitTracker.csproj --urls http://localhost:5016
```

Open `http://localhost:5016/Today` in a browser. The application creates its local
SQLite database on first start.

## Tests

```bash
dotnet test FunHabitTracker.sln
```

The current MSTest suite covers the `HabitService` CRUD behavior with an in-memory
Entity Framework Core database.

## Project Structure

- `src/FunHabitTracker/` contains the ASP.NET Core MVC application.
- `tests/FunHabitTracker.Tests/` contains the MSTest project.
- `FunHabitTracker.sln` is the solution entry point for build and test commands.

## Known Limitations

- The application uses local SQLite storage and has no authentication or multi-user model.
- Automated coverage currently focuses on the service layer rather than controllers or Razor views.
- Charts, reminders, rewards, and achievements are not implemented.
