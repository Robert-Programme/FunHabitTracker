## ADDED Requirements

### Requirement: PRESENCE-001 User-controlled desktop availability
**Risk:** Medium. The system SHALL provide a Windows notification-area entry while the desktop process is running, SHALL offer start-at-sign-in only as an explicit opt-in setting, and SHALL allow the user to reverse that setting from the application. Start-at-sign-in MUST NOT steal keyboard focus or force a foreground window. Closing the last application window SHALL keep the process available in the notification area, and an explicit Quit command SHALL terminate it.
**Evidence:** `tests/FunHabitTracker.Desktop.Tests/DesktopLifecycleTests.cs`, `artifacts/evidence/desktop-lifecycle.md`

#### Scenario: User enables start at sign-in
- **WHEN** the user explicitly enables start-at-sign-in
- **THEN** Windows registers the current installed application for that user and the application displays the enabled state

#### Scenario: User disables start at sign-in
- **WHEN** the user disables start-at-sign-in
- **THEN** the registration is removed and a later sign-in does not launch the application

#### Scenario: Sign-in launch stays unobtrusive
- **WHEN** Windows launches the application through the enabled start-at-sign-in entry
- **THEN** the application becomes available in the notification area without opening or focusing a window

#### Scenario: User explicitly quits
- **WHEN** the user invokes Quit from the notification-area menu
- **THEN** the local application process stops and no background listener remains

### Requirement: PRESENCE-002 Compact Today interaction
**Risk:** High. The system SHALL provide a stable compact Today surface that lists every habit due on the current local date and permits each habit to be set to Done, Skipped, or Pending without opening the full application. Every state change MUST use the existing daily tracking rules and local SQLite transaction boundary.
**Evidence:** `tests/FunHabitTracker.Desktop.Tests/CompactTodayTests.cs`, `tests/FunHabitTracker.Tests/DailyTrackingServiceTests.cs`, `artifacts/evidence/compact-today-desktop.png`

#### Scenario: User opens compact Today from the notification area
- **WHEN** the user activates the notification-area entry
- **THEN** a compact, foreground Today surface appears with current due habits and their current states

#### Scenario: User completes a due habit
- **WHEN** the user marks a due habit Done in the compact surface
- **THEN** the outcome is persisted locally and the compact summary updates without navigating to another screen

#### Scenario: No habits are due
- **WHEN** the user opens compact Today on a date with no due habits
- **THEN** the surface displays a concise free-day state and offers access to the full application

#### Scenario: User needs management functions
- **WHEN** the user invokes the full-application command from the compact surface
- **THEN** the complete habit, history, and data-management interface opens using the same local dataset

### Requirement: PRESENCE-003 Fast and single-instance access
**Risk:** High. The system MUST maintain one owning desktop process per Windows user. Repeated shortcut, tray, or protocol activation SHALL reuse that process and foreground the requested surface. On documented acceptance hardware, a warm compact-surface activation SHALL become interactive within one second and a cold launch SHALL become interactive within three seconds.
**Evidence:** `tests/FunHabitTracker.Desktop.Tests/SingleInstanceTests.cs`, `artifacts/evidence/desktop-launch-performance.json`, `artifacts/evidence/desktop-lifecycle.md`

#### Scenario: User launches the shortcut twice
- **WHEN** the desktop process already owns the application instance and the user launches the shortcut again
- **THEN** no second host or database writer remains and the existing process opens the compact Today surface

#### Scenario: Warm activation meets the interaction budget
- **WHEN** the background process is healthy and the compact surface is closed
- **THEN** automated acceptance measurement records an interactive compact surface within one second on the documented acceptance hardware

#### Scenario: The preferred local endpoint is unavailable
- **WHEN** a local port or hosting resource cannot be acquired
- **THEN** the application selects another safe local endpoint or presents actionable diagnostics without exposing a non-loopback listener

### Requirement: PRESENCE-004 Offline, private, and accessible operation
**Risk:** High. Compact and full desktop workflows SHALL require no non-loopback network request, SHALL expose habit status through text or accessible names rather than color alone, SHALL support keyboard-only completion, and SHALL keep personal habit content out of notification-area labels and operating-system startup registration.
**Evidence:** `tests/FunHabitTracker.Desktop.Tests/CompactAccessibilityTests.cs`, `artifacts/evidence/desktop-offline-network-log.json`, `artifacts/evidence/compact-today-mobile-width.png`

#### Scenario: Network access is blocked
- **WHEN** all non-loopback networking is blocked during launch and daily tracking
- **THEN** notification-area activation, compact rendering, and outcome changes remain functional with no external request

#### Scenario: User operates the compact surface by keyboard
- **WHEN** the compact surface has focus and the user uses only standard keyboard navigation and activation
- **THEN** every due habit and outcome command is reachable, visibly focused, and announced with an unambiguous accessible name

#### Scenario: Window content becomes long
- **WHEN** habit names or the number of due habits exceed the initial compact viewport
- **THEN** content remains readable and reachable without horizontal overflow or overlapping controls

### Requirement: PRESENCE-005 Recoverable desktop lifecycle failures
**Risk:** High. Desktop startup, registration, activation, and host failures MUST be logged with diagnostic context and MUST provide a user-visible recovery path. A shell failure MUST NOT delete, replace, or silently reset the existing SQLite database.
**Evidence:** `tests/FunHabitTracker.Desktop.Tests/DesktopFailureTests.cs`, `artifacts/evidence/desktop-startup-failure.md`

#### Scenario: Local application initialization fails
- **WHEN** database migration or local host initialization cannot complete
- **THEN** the desktop shell displays concise recovery guidance, identifies the local log and backup locations, and does not report a successful start

#### Scenario: Start-at-sign-in registration fails
- **WHEN** Windows rejects a requested start-at-sign-in change
- **THEN** the previous registration state remains authoritative and the application reports that the setting was not changed
