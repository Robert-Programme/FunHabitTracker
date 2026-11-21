# Local Operations

## Startup

Startup resolves the per-user data root. When the default data root is empty, it first
imports a populated legacy `habits.db` from the application content root or launch working
directory without deleting the source file. Explicitly configured data roots stay isolated
unless `FunHabitTracker__ImportLegacyDatabaseLocation=true` is set. Startup then adopts
supported legacy `EnsureCreated` databases, creates safety backups, and applies EF Core
migrations before serving normal pages. A migration failure serves recovery guidance and appends details to
`Logs\startup.log` instead of silently continuing.

## Desktop lifecycle

`FunHabitTracker.Desktop.exe` owns the local ASP.NET host, a current-user mutex, a
current-user named pipe, the embedded WebView2 windows, and the notification-area icon.
Normal launch opens compact Today. `--full` opens the full application, `--background`
starts quietly for sign-in, and `--quit` asks the existing owner to shut down. Closing a
window hides it in the tray. Minimizing the compact window keeps it visible in the Windows
taskbar and Alt-Tab; repeated launch or tray activation restores that same owner window.
The compact window remains resizable but does not expose Maximize. Only tray **Beenden**
or `--quit` stops the host.

Compact and full are two mutually exclusive surfaces of the same owner. **Ganze App
öffnen** hides compact before refreshing and showing full Today. **Miniaturansicht öffnen**
hides full before refreshing and showing compact Today. A normal repeated launch and a
tray double-click select compact; `--full` selects full. The inactive window remains owned
but hidden, so persisted habit outcomes are refreshed when the other surface is shown.

The owner writes `desktop-endpoint.txt` under the data root after startup and deletes it
during shutdown. Only the exact selected `http://127.0.0.1:<port>` origin is permitted in
the embedded browser. The plain ASP.NET project remains the browser fallback. In browser
fallback mode, the same surface controls are ordinary local links between `/Home/Compact`
and `/Home/Today` and require neither JavaScript nor internet access.

Desktop diagnostics are best-effort under `Logs\desktop.log`; launch timings are appended
to `Logs\desktop-performance.jsonl`. Startup and database recovery details remain in
`Logs\startup.log`. A logging failure must not interrupt activation or shutdown.

## Release

Run the normal build, test, OpenSpec, and Factory gates before packaging. Then run
`installer/package.ps1` and `installer/verify.ps1`. Follow `release-operations.md` for
signing, rollback, troubleshooting, and exact-bundle acceptance.
