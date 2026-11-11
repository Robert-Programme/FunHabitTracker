## Context

FunHabitTracker is now a reliable .NET 8 ASP.NET Core MVC application with server-rendered Razor views, local-only static assets, application services, EF Core, and a stable per-user SQLite database. Desktop mode starts a loopback host on an available port, coordinates one instance with a mutex, and opens the system browser. The offline ZIP installer is deterministic and data-preserving, but it offers neither notification-area presence nor a compact application window, and its unsigned PowerShell installation flow is suitable only for an explicitly labeled preview.

The immediate stakeholder is a Windows user who wants the daily check-in to be continuously reachable without being interrupted. The design must improve that experience without turning this release into the later mobile/sync migration. Existing data, backup behavior, browser fallback, no-JavaScript behavior, and Factory controls remain constraints.

## Goals / Non-Goals

**Goals:**

- Keep one local application owner available through the Windows notification area.
- Open a focused compact Today experience quickly and allow a transition to the complete application.
- Make start-at-sign-in optional, reversible, observable, and quiet.
- Reuse existing tracking services, SQLite storage, Razor rendering, validation, and offline assets.
- Preserve browser fallback and existing data through install, update, and removal.
- Generate a static-hosting-ready preview bundle with integrity, trust, privacy, and provenance metadata.
- Establish measurable lifecycle, accessibility, offline, and launch-performance evidence.

**Non-Goals:**

- Selecting or implementing the future Android/iOS client framework.
- Adding accounts, cloud services, sync metadata, conflict resolution, or background network activity.
- Rewriting all MVC views as Blazor, SPA, MAUI, or native views.
- Scheduled reminders, gamification, auto-update downloads, or website CMS deployment.
- Claiming store-grade trust for unsigned preview bytes.

## Decisions

### Add a separate Windows desktop shell around the existing local web host

Create a `FunHabitTracker.Desktop` Windows-targeted executable that owns process activation, the notification-area icon, compact/full windows, and the ASP.NET Core host lifecycle. The existing web project remains the application and browser fallback. Refactor host construction behind a callable composition boundary so tests and the desktop shell can start it without duplicating service registration.

Use WPF for window ownership and `System.Windows.Forms.NotifyIcon` for notification-area integration. Host the compact and full local Razor surfaces in WebView2 windows pointed only at the selected loopback origin. Package a pinned WebView2 fixed-version runtime in the offline preview bundle unless release verification proves that an accepted target environment already provides a compatible runtime; installation MUST never download it.

This path reuses the working UI and domain logic while keeping desktop lifecycle code isolated. A .NET MAUI Blazor Hybrid rewrite was rejected for this change because current pages are MVC views rather than reusable Razor components and the rewrite would combine presence, UI migration, and future mobile decisions. Opening the system browser alone was rejected because it cannot provide a stable compact window or notification-area lifecycle. A fully native Today UI was rejected because it would duplicate presentation and form behavior immediately.

### Use a per-user mutex plus current-user-only named-pipe activation

The owning desktop process retains a per-user mutex. A later process connects through a current-user-only named pipe and sends a bounded activation command such as `ShowCompact`, `ShowFull`, or `Quit`; it then exits. The owner validates the command and dispatches window work to the UI thread. Endpoint files may remain diagnostic but are not the activation authority.

Named-pipe activation is deterministic and avoids trying to manipulate another browser process. Raw TCP activation was rejected because it expands the local attack surface and requires another authentication mechanism. Multiple independent local hosts were rejected because they create database-writer and lifecycle ambiguity.

### Add a dedicated compact Razor route and shell layout

Add a compact Today route that calls `DailyTrackingService`, uses the same anti-forgery and outcome actions as the full Today page, and renders through a constrained compact layout with no primary navigation chrome. The route exposes only current-day status changes and links to the full surface for habit management, history, and data operations. Progressive enhancement may update a row in place, but normal form posts remain the persistence fallback.

The desktop shell owns fixed initial dimensions with responsive minimums and scrolling constraints. It restores a valid prior compact position when still visible on a connected display, otherwise it uses a deterministic work-area corner. Personal habit names are never copied into tray labels or native notifications.

### Make close, background start, and quit explicit state transitions

The desktop lifecycle is:

```text
Cold launch ──foreground──> Compact visible
     │
     └──--background──────> Tray only

Tray only ──activate──────> Compact visible
Compact ──open full───────> Full visible
Any window ──close────────> Tray only
Tray menu ──quit──────────> Host stopped, process exits
```

Start-at-sign-in writes a per-user Windows registration only after explicit confirmation and starts with `--background`. The shell reads the actual registration state rather than trusting a cached flag. Uninstall removes registration regardless of its last known setting. Settings unrelated to habit records live in a versioned JSON file under the existing application data root, written atomically; no database migration is required.

### Keep local web hosting loopback-only and preserve browser fallback

The shell starts the existing application on an available IPv4 loopback port before creating WebView2 content. Navigation is restricted to the selected loopback origin; external navigation is denied or handed to the system browser only after an explicit user command. Existing CSP and anti-forgery controls remain. If WebView2 cannot initialize, the shell reports the failure and offers to open the full interface in the system browser rather than changing or resetting data.

### Produce a release manifest before archive creation

Packaging derives one canonical semantic version and emits a machine-readable manifest containing product, channel (`preview` or `production`), architecture, minimum OS, installer filename, size, SHA-256 digest, signature status, privacy statement path, release-notes path, SBOM path, and provenance path. The outer website publishing directory contains the installer archive and all referenced files; verification rejects missing or mismatched entries.

Preview packaging permits an unsigned result only when every human- and machine-readable surface says `unsigned preview`. Production packaging requires externally injected signing material, Authenticode verification, and a trusted timestamp. No signing secret is copied into staging. The repository prepares a publishable directory but does not upload it to an external website.

### Verify native lifecycle separately from server journeys

Keep domain and ASP.NET integration tests in the existing suite. Add Windows-only desktop tests for mutex/pipe activation, background startup, tray commands, close-versus-quit behavior, registration rollback, WebView navigation restrictions, and failure presentation. Measure cold and warm activation against a documented acceptance machine and store structured timings. Installer verification launches the exact packaged desktop executable with networking blocked and checks process, data, registration, and runtime cleanup.

## Risks / Trade-offs

- **[High] WebView2 fixed-version packaging materially increases archive size and security-update responsibility.** -> Pin the runtime, include it in vulnerability/release inventory, document its size, and revisit a native/shared UI migration in the later cross-platform change.
- **[High] Combining WPF, ASP.NET hosting, and WebView2 introduces UI-thread and shutdown complexity.** -> Give the desktop shell sole lifecycle ownership, isolate activation behind an interface, use explicit cancellation, and test every close/quit/failure transition.
- **[High] Start-at-sign-in can feel intrusive or become stale after an update.** -> Keep it opt-in, launch background-only, resolve the installed executable dynamically during install, expose actual state, and remove registration on uninstall.
- **[High] A local web surface is reachable by other processes running as the same user.** -> Bind only to loopback, retain anti-forgery protection, restrict WebView navigation, avoid sensitive native IPC payloads, and use current-user-only named pipes.
- **[Medium] The compact MVC page is not directly reusable by future mobile clients.** -> Keep business behavior in existing application services and treat the compact page as a disposable Windows presentation adapter.
- **[Medium] Unsigned preview installation may trigger Windows reputation warnings.** -> State the limitation before download, publish hashes and provenance, support external signing, and forbid production labeling without verified signatures.
- **[Medium] Hard timing thresholds vary by hardware and security software.** -> Record acceptance hardware and cold/warm methodology, retain raw timing evidence, and diagnose regressions rather than weakening thresholds silently.

## Migration Plan

1. Record baseline cold/warm launch measurements and capture the existing installer, data-path, and browser-fallback behavior.
2. Extract reusable web-host composition without changing routes, SQLite schema, or browser launch behavior; run all existing gates.
3. Add the desktop shell, named-pipe activation, tray lifecycle, background mode, and browser recovery fallback behind development-only packaging.
4. Add the compact Today route and constrained desktop window, then verify no-JavaScript, keyboard, long-content, and offline behavior.
5. Add atomic desktop settings and reversible per-user start-at-sign-in registration.
6. Extend installer and uninstall flows for the shell, WebView2 runtime, start-at-sign-in cleanup, and data-preserving upgrade.
7. Generate the website publishing manifest, privacy/trust documents, checksums, provenance, and optional signing verification.
8. Run all project, OpenSpec, Factory, desktop, blocked-network, install, update, removal, and package-integrity gates against the exact release archive.

Rollback distributes the previous self-contained preview and leaves `%LOCALAPPDATA%\FunHabitTracker` untouched. Removing the new desktop shell must also remove its start-at-sign-in registration. Because no database migration is introduced, the prior application can reopen the same data after rollback.

## Open Questions

- Which product name, icon set, version, and website download path should be used for the first public preview?
- Which Windows versions and acceptance hardware define the initial support and performance baseline?
- Will an Authenticode certificate and timestamp service be available for the preview, or must the first website package be explicitly unsigned?
