## 1. Reversible daily outcomes

- [x] 1.1 Add authoritative Done/Skipped toggle behavior to `DailyTrackingService` and verify real-SQLite tests cover Pending-to-active, same-active-to-Pending, cross-outcome replacement, duplicate prevention, invalid habit, and unscheduled habit cases.
- [x] 1.2 Route full and compact outcome posts through the shared toggle behavior while preserving anti-forgery validation, local-date selection, explicit Pending reset, error feedback, and return routing; verify controller or application integration tests cover each branch.
- [x] 1.3 Replace the full Today decorative status symbol with a stable-size semantic server form button whose visible symbol, accessible label, `aria-pressed` state, focus treatment, and non-color state remain correct; verify Razor integration assertions and no-JavaScript submission tests pass.
- [x] 1.4 Verify active Done and Skipped controls toggle to Pending in both compact and full Today, different outcomes replace each other, and an explicit Pending reset remains idempotent through application journey tests.

## 2. History navigation and responsive polish

- [x] 2.1 Add a prominent Back to Today command alongside Edit in habit history and verify route rendering and direct navigation tests reach the full Today view.
- [x] 2.2 Add responsive heading-action styles and verify browser acceptance at narrow and desktop widths has no overlap or horizontal overflow and remains keyboard operable.

## 3. Recoverable native desktop windows

- [x] 3.1 Add deterministic multi-size Windows icon artwork derived from the approved product PNG, assign it to the executable and both WPF windows, and verify build/publish outputs and window metadata reference the committed icon.
- [x] 3.2 Make the compact window visible in taskbar and Alt-Tab when minimized, preserve close-to-tray, and remove only its native Maximize command while retaining constrained resize; verify focused window-policy tests cover compact and full configurations.
- [x] 3.3 Preserve tray, named-pipe, and repeated-launch restoration through `BringForward`, including normalization of minimized state without creating a second owner; verify desktop lifecycle tests and endpoint cleanup tests pass.
- [x] 3.4 Perform Windows acceptance for resize, minimize/taskbar restore, close/tray restore, repeated launch, full-window maximize, explicit Quit, title, and icon; record a screenshot and lifecycle evidence.

## 4. Documentation and release gates

- [x] 4.1 Update README/operations notes and requirement traceability for USABILITY-001 through USABILITY-005, including the distinction between Minimize, Close, and Quit, and verify every referenced evidence path resolves.
- [x] 4.2 Run keyboard, forced-colors, no-JavaScript, narrow-layout, blocked-network, compact, full, and history browser acceptance and verify no non-loopback requests, console errors, clipping, or inaccessible status names occur.
- [x] 4.3 Run `dotnet build FunHabitTracker.sln --configuration Release`, `dotnet test FunHabitTracker.sln`, `npm run openspec:validate`, and `npm run factory:all`; resolve every warning or failure before marking the change complete.
- [x] 4.4 Rebuild the unsigned preview website bundle, run installer verification against its exact digest, and confirm icon, lifecycle, toggle, navigation, manifest, signature disclosure, update, and uninstall acceptance evidence describe the tested package.

## 5. Active full-view navigation

- [x] 5.1 Derive exactly one active Today, Habits, or Data item from the current full-view route, render semantic `aria-current="page"` markup with a restrained non-color-only green indicator, and verify Razor rendering tests cover every top-level section and current child route.
- [x] 5.2 Verify the active indicator and all navigation items remain keyboard operable, perceivable in forced colors, and free of overlap or horizontal overflow at narrow and desktop widths through browser acceptance.

## 6. Exclusive compact and full surfaces

- [x] 6.1 Move compact/full visibility transitions behind coordinator operations that hide the previous surface, refresh the destination route, and bring forward only the requested surface; verify lifecycle tests cover compact-to-full, full-to-compact, tray activation, repeated launch, repeated commands, single ownership, and final window visibility.
- [x] 6.2 Add a bottom-left, keyboard-accessible `Miniaturansicht öffnen` action to the full layout and intercept only its exact loopback route in the desktop shell while preserving normal no-JavaScript browser fallback navigation; verify route, origin, and callback tests.
- [x] 6.3 Verify persisted outcome freshness and absence of simultaneous windows through compact-to-full-to-compact application and Windows acceptance, including virtual-desktop/taskbar behavior, Close, Minimize, and explicit Quit.
- [x] 6.4 Update operational documentation and traceability for USABILITY-006 and USABILITY-007, rerun the solution tests and all OpenSpec/Factory gates, then rebuild and verify the exact unsigned preview bundle and its acceptance evidence.
