# Fun Habit Tracker

A small single-user ASP.NET Core MVC app for tracking daily habits with SQLite.

## Implemented features

- Create habits with an optional description and active status.
- List all habits and show active habits on today's dashboard.
- Mark habits done or undone for the current day, preserving earlier completion records.
- Validate names and default an empty frequency to Daily.

Charts, reminders, achievements, and weekly scheduling are not implemented.

## Run locally

Install the .NET 10 SDK pinned in `global.json`. Node.js 20+ is needed only for the JavaScript test.

```sh
git clone https://github.com/Robert-Programme/FunHabitTracker.git
cd FunHabitTracker
dotnet restore
dotnet run --project src/FunHabitTracker --urls http://localhost:5016
```

Open http://localhost:5016/Home/Today. No account or login is required; the current app has a shared single-user data model.

SQLite is created as `habits.db` relative to the process working directory. Startup fails if that path cannot be initialized. Data Protection keys are persisted under the app content root in `.aspnet-data-protection-keys/`. These runtime files are ignored by Git. `EnsureCreated` initializes new databases; existing-schema upgrades require a separate migration strategy.

## Verify

```sh
dotnet test --configuration Release
node --test tests/js/site.test.cjs
dotnet format FunHabitTracker.sln --verify-no-changes --no-restore
dotnet list FunHabitTracker.sln package --vulnerable --include-transitive
```

Tests include SQLite-backed HTTP/controller regressions, an EF Core 6 schema compatibility fixture, startup failure checks, and a shared-script regression. CI also runs CodeQL and a history secret scan. See `.github/workflows/portfolio-quality.yml` for the commit-bound checks.

The browser validation libraries are served locally. Their versions, source URLs, checksums, and upstream licenses are in `src/FunHabitTracker/wwwroot/vendor/`.

## License

A project license has not yet been selected. Upstream library licenses are retained with the third-party assets.
