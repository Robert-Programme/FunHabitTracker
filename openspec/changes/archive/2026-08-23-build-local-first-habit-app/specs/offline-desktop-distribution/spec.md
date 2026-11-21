## ADDED Requirements

### Requirement: OFFLINE-001 Windows installation and launch are self-contained
The system SHALL provide an offline-capable Windows installer for the supported `win-x64` target. Installation SHALL include the application runtime and required assets, create a normal launch entry, preserve user data during upgrades and uninstall unless the user explicitly requests removal, and bind the local host only to loopback.

**Risk:** High
**Evidence:** `artifacts/evidence/windows-clean-install.md`, `artifacts/evidence/windows-upgrade-uninstall.md`

#### Scenario: Install and launch without internet
- **WHEN** a supported Windows machine with no internet connection runs the installer and launches the application
- **THEN** the application opens to a usable Today view without downloading a runtime or asset

#### Scenario: Upgrade preserves local data
- **WHEN** a user installs a newer compatible version over an existing installation
- **THEN** the application package is upgraded and the user's habits, outcomes, and backups remain available

#### Scenario: Local host is not exposed to the network
- **WHEN** the installed application is running
- **THEN** its HTTP listener accepts connections only through loopback interfaces

### Requirement: OFFLINE-002 Core journeys have no network dependency
The system MUST package every script, style, font, icon, and validation resource required by core journeys. Creating, editing, completing, skipping, reviewing, exporting, and importing habits SHALL work with external network access disabled and SHALL make no non-loopback request.

**Risk:** High
**Evidence:** `tests/FunHabitTracker.BrowserTests/OfflineCoreJourneyTests.cs`, `artifacts/evidence/offline-network-log.json`

#### Scenario: Complete the core journey offline
- **WHEN** external network access is blocked and the user creates, completes, reviews, exports, and imports a habit
- **THEN** every action succeeds and captured network traffic contains no non-loopback request

#### Scenario: JavaScript is unavailable
- **WHEN** JavaScript is disabled
- **THEN** create, edit, complete, skip, archive, export, and confirmed import remain operable through server-rendered forms

### Requirement: OFFLINE-003 Application shell is responsive and accessible
The system SHALL provide a today-centered shell usable at desktop and 360 CSS-pixel-wide viewports. Interactive controls MUST remain visible, non-overlapping, keyboard operable, and programmatically labelled; status SHALL not be conveyed by color alone.

**Risk:** Medium
**Evidence:** `tests/FunHabitTracker.BrowserTests/ResponsiveAccessibilityTests.cs`, `artifacts/evidence/today-desktop.png`, `artifacts/evidence/today-mobile.png`

#### Scenario: Use Today view at narrow width
- **WHEN** the Today view is rendered at 360 CSS pixels wide with representative long habit names
- **THEN** text and controls remain readable and usable without horizontal page scrolling or incoherent overlap

#### Scenario: Operate with keyboard
- **WHEN** the user navigates core screens using only a keyboard
- **THEN** focus is visible, controls have meaningful accessible names, and all core commands can be completed
