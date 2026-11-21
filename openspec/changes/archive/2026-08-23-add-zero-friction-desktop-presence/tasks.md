## 1. Baseline and release decisions

- [x] 1.1 Record current cold launch, warm relaunch, memory, process, loopback binding, installer size, and browser-fallback baselines on documented acceptance hardware.
- [x] 1.2 Select and document the first preview product name, semantic version, icon assets, supported Windows versions, architecture, and website publishing path.
- [x] 1.3 Evaluate the installed versus fixed-version WebView2 runtime against offline installation, package size, security servicing, CI, and supported Windows constraints; pin and document the selected runtime.
- [x] 1.4 Decide the first preview signing mode based on available Authenticode and timestamp credentials and record the trust limitations visible before download.

## 2. Reusable local host

- [x] 2.1 Extract ASP.NET Core host and service composition from top-level startup into a callable application boundary without changing routes, data paths, migrations, or browser mode.
- [x] 2.2 Preserve the current loopback-only browser launcher as a supported fallback and add regression tests for startup success, startup failure, endpoint selection, and shutdown.
- [x] 2.3 Add bounded lifecycle logging and cancellation so a desktop owner can start and stop the local host deterministically without leaving an endpoint file or database connection behind.
- [x] 2.4 Run the existing application, migration, backup, offline journey, build, and format tests after the host refactor and resolve regressions before adding the desktop shell.

## 3. Windows desktop shell and activation

- [x] 3.1 Add the Windows-targeted `FunHabitTracker.Desktop` project with pinned WPF, NotifyIcon, WebView2, logging, and testable lifecycle abstractions.
- [x] 3.2 Implement one owning process per Windows user with a mutex and a current-user-only named-pipe protocol restricted to validated `ShowCompact`, `ShowFull`, and `Quit` commands.
- [x] 3.3 Implement notification-area icon creation, accessible label, compact/full commands, explicit Quit, disposal, and clean host shutdown without exposing habit content.
- [x] 3.4 Implement foreground launch, `--background` launch, close-to-tray, minimize, reopen, and multi-monitor window-position recovery according to the lifecycle state machine.
- [x] 3.5 Restrict embedded navigation to the selected loopback origin and add a user-visible browser fallback for WebView or shell initialization failures.
- [x] 3.6 Add native lifecycle tests for duplicate launch, named-pipe authorization and command bounds, tray commands, close versus Quit, endpoint conflicts, display changes, and failure cleanup.
- [x] 3.7 Measure cold and warm activation on the acceptance hardware and resolve regressions until the three-second cold and one-second warm interaction budgets pass.

## 4. Compact Today experience

- [x] 4.1 Add a compact Today route and layout that reuse `DailyTrackingService`, anti-forgery protection, local date handling, and existing Done, Skipped, and Pending semantics.
- [x] 4.2 Design and implement the constrained compact list, progress summary, free-day state, long-content scrolling, and explicit full-application command without full navigation chrome.
- [x] 4.3 Preserve functional server-rendered forms without JavaScript and add guarded progressive enhancement only where it shortens outcome interaction without owning persistence.
- [x] 4.4 Add ASP.NET integration tests for compact rendering, every outcome transition, no-due state, archived and unscheduled exclusions, invalid requests, and full-application navigation.
- [x] 4.5 Add desktop accessibility tests for keyboard-only operation, focus restoration, accessible names, non-color state communication, scaling, high contrast, and constrained-window overflow.
- [x] 4.6 Capture verified desktop screenshots for normal, long-content, free-day, error, and narrow compact states with no overlapping or clipped controls.

## 5. Settings and start at sign-in

- [x] 5.1 Add a versioned desktop settings document under the existing per-user data root with validated defaults and atomic, recoverable writes.
- [x] 5.2 Implement an abstraction over the per-user Windows start-at-sign-in registration that reads actual state, writes the installed executable with `--background`, and rolls back failed changes.
- [x] 5.3 Add an explicit application setting for enabling and disabling start-at-sign-in and surface registration failures without reporting a false state.
- [x] 5.4 Add tests for default opt-out, enable, disable, stale executable paths, denied registration, atomic settings interruption, quiet sign-in launch, and preservation across update.

## 6. Installer and website publishing bundle

- [x] 6.1 Extend self-contained packaging to include the desktop executable, selected WebView runtime, icons, local assets, browser fallback, and no runtime download requirement.
- [x] 6.2 Update install, in-place update, and uninstall flows to manage shortcuts, running desktop processes, start-at-sign-in registration, application files, settings, and preserved habit data correctly.
- [x] 6.3 Define and generate a machine-readable release manifest with canonical version, channel, platform, minimum OS, architecture, filenames, sizes, SHA-256 digests, signature status, and evidence references.
- [x] 6.4 Add versioned installation, removal, privacy, preview-trust, release-note, and checksum documents suitable for direct static website hosting.
- [x] 6.5 Add optional external Authenticode signing and timestamp verification without copying credentials into staging, logs, provenance, or repository files.
- [x] 6.6 Enforce explicit unsigned-preview labeling when credentials are absent and fail production packaging unless every required executable and installer signature verifies.
- [x] 6.7 Add Factory tests that reject incomplete manifests, digest or version mismatches, secret leakage, false signing claims, missing website files, and unverifiable production artifacts.

## 7. Acceptance, evidence, and documentation

- [x] 7.1 Run blocked-network compact and full daily journeys and record that no non-loopback request occurs during launch, activation, outcome updates, or shutdown.
- [x] 7.2 Extend installer verification for clean offline installation, quiet background start, duplicate activation, port conflict, update, normal removal, explicit data removal, and stale registration cleanup.
- [x] 7.3 Link the exact tested publishing-bundle digest to SBOM, vulnerability results, signature report, launch timings, screenshots, lifecycle logs, and installer evidence in release provenance.
- [x] 7.4 Update README, AGENTS.md, desktop lifecycle operations, user privacy, website publishing, signing, rollback, and troubleshooting documentation.
- [x] 7.5 Update acceptance metadata and requirement traceability for every PRESENCE and DIST requirement and verify all evidence paths resolve.
- [x] 7.6 Run `dotnet build FunHabitTracker.sln --configuration Release`, `dotnet test FunHabitTracker.sln`, desktop tests, `npm run openspec:validate`, and `npm run factory:all`; resolve every warning and failure.
- [x] 7.7 Perform a final acceptance pass against every OpenSpec scenario using the exact website publishing bundle and record whether it is an unsigned preview or verified signed release.
