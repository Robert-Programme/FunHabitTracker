# Fun Habit Tracker

Fun Habit Tracker is a Windows-first, local-first habit application. Its core workflows
run on a loopback-only ASP.NET Core host, keep data in SQLite, and do not require an
internet connection.

## Features

- Today view with reversible Pending, Done, and Skipped states and a one-click completion marker.
- Daily or selected-weekday schedules with a local start date.
- Habit create, edit, archive, restore, history, completion summaries, and streaks.
- Versioned JSON export/import with preview, confirmation, validation, and safety backup.
- Consistent SQLite backup and validated restore.
- Responsive server-rendered UI that keeps core actions functional without JavaScript.
- Full-view navigation that clearly identifies the current Today, Habits, or Data section.
- Compact Windows desktop window with taskbar-safe Minimize, close-to-tray, notification-area
  access, exclusive compact/full switching, and optional start at sign in.
- Self-contained `win-x64` publish with an offline WebView2 and PowerShell installer bundle.

Cloud sync, accounts, mobile-native packaging, rewards, and reminders are not part of the
current local-first release.

## Requirements

- Development: .NET SDK `8.0.204`, pinned by `global.json`; Node.js/npm for OpenSpec and
  Factory gates; Python 3 for the repository Factory runner.
- Installed app: supported 64-bit Windows. The release bundle includes the .NET runtime.

## Develop

On Linux, run `bash scripts/start-browser.sh` to start browser mode at
`http://127.0.0.1:5016`. The script installs the pinned SDK if needed and
keeps development data in `.tmp/browser-data`.

```powershell
dotnet restore FunHabitTracker.sln
dotnet test FunHabitTracker.sln
$env:ASPNETCORE_URLS='http://127.0.0.1:5016'
dotnet run --project src\FunHabitTracker\FunHabitTracker.csproj
dotnet run --project src\FunHabitTracker.Desktop\FunHabitTracker.Desktop.csproj
```

Use an isolated data directory when developing or testing manually:

```powershell
$env:FunHabitTracker__DataDirectory="$PWD\.tmp\local-data"
```

The default data root is `%LOCALAPPDATA%\FunHabitTracker`. See
`docs/backup-restore.md` before moving or restoring database files.

## Verify

```powershell
npm ci
npm run openspec:validate
npm run factory:all
```

Material changes follow `AGENTS.md`, `docs/factory-process.md`, and the OpenSpec artifacts
under `openspec/`.

## Package

```powershell
.\installer\package.ps1
.\installer\verify.ps1
```

The versioned static publishing bundle, ZIP, manifest, checksum, SBOM, provenance, trust
notice, and user documents are written under `artifacts/website/`. Installation and
uninstall behavior are documented in `docs/installation.md`.

## Data safety

Backups contain personal habit descriptions and history. They are processed locally and
are never uploaded by the application. Normal uninstall preserves user data unless
`-RemoveData` is explicitly supplied.
