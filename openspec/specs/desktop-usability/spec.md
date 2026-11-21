## Purpose

Defines predictable Windows window behavior and low-friction, accessible interactions for completing daily habits and returning from habit history.

## Requirements

### Requirement: USABILITY-001 Compact window remains recoverable
The desktop application SHALL keep a minimized compact window discoverable through standard Windows task switching and SHALL restore the existing compact window through tray activation or repeated application launch. Closing the compact window SHALL continue to hide it in the notification area, while explicit Quit SHALL remain the only window command that stops the owner process. Risk: high. Evidence SHALL include desktop lifecycle tests and manual Windows acceptance.

#### Scenario: Minimized compact window remains discoverable
- **WHEN** the user minimizes the compact window
- **THEN** the same window remains available through the Windows taskbar and Alt-Tab and can be restored without starting another owner process

#### Scenario: Closed compact window remains tray-owned
- **WHEN** the user closes the compact window with the title-bar Close command
- **THEN** the window is hidden, the tray owner and local host remain running, and tray activation or repeated launch restores that window in a normal state

#### Scenario: Explicit Quit retains its distinct meaning
- **WHEN** the user selects Quit from the tray menu
- **THEN** the desktop owner, windows, tray icon, endpoint file, and local host shut down cleanly

### Requirement: USABILITY-002 Compact window uses appropriate native chrome
The compact desktop window SHALL retain accessible native Windows chrome, identify itself with the product icon and a clear title, permit resizing within supported bounds, and SHALL NOT offer a maximize operation that expands compact content into a full-screen shell. Risk: medium. Evidence SHALL include window configuration tests and a desktop screenshot.

#### Scenario: Compact chrome presents supported commands
- **WHEN** the compact window is displayed
- **THEN** native Move, Resize, Minimize, and Close behavior is available, Maximize is unavailable, and the product icon and compact title are visible

#### Scenario: Full window remains independently resizable
- **WHEN** the full application window is displayed
- **THEN** it retains native minimize, maximize, resize, and close behavior with the product icon and full application title

### Requirement: USABILITY-003 Today status marker is the primary completion toggle
Each status marker in the full Today view SHALL be an accessible button that toggles its habit between Pending and Done using anti-forgery-protected server persistence. Its accessible name and pressed state SHALL identify the habit and current action without relying on color or JavaScript. Risk: high. Evidence SHALL include controller or integration tests, no-JavaScript form tests, and keyboard browser acceptance.

#### Scenario: Pending marker marks a habit Done
- **WHEN** a user activates the status marker for a Pending habit
- **THEN** the habit is persisted as Done for the local current date and the refreshed marker communicates the Done state

#### Scenario: Done marker resets a habit to Pending
- **WHEN** a user activates the status marker for a Done habit
- **THEN** the stored outcome is removed and the refreshed marker communicates the Pending state

#### Scenario: Marker works without a pointing device or JavaScript
- **WHEN** a keyboard-only user activates the marker while JavaScript is unavailable
- **THEN** the server-rendered form completes the same authenticated toggle without horizontal layout breakage

### Requirement: USABILITY-004 Active outcomes toggle back to Pending
The full and compact Today experiences SHALL treat Done and Skipped as reversible choices: selecting an already active stored outcome SHALL remove it and return the habit to Pending. Selecting a different stored outcome SHALL replace the prior outcome. Pending SHALL remain the absence of a stored outcome and SHALL NOT introduce a fourth domain state. Risk: high. Evidence SHALL include real-SQLite service or controller tests and compact/full rendering journeys.

#### Scenario: Active Done is selected again
- **WHEN** a habit is Done and the user selects Done again
- **THEN** its completion row for the local current date is removed and the habit becomes Pending

#### Scenario: Active Skipped is selected again
- **WHEN** a habit is Skipped and the user selects Skipped again
- **THEN** its completion row for the local current date is removed and the habit becomes Pending

#### Scenario: Different outcome replaces current outcome
- **WHEN** a habit is Done and the user selects Skipped, or is Skipped and the user selects Done
- **THEN** the existing date-specific outcome is replaced without creating a duplicate completion row

#### Scenario: Pending reset remains idempotent
- **WHEN** the user invokes the explicit Pending reset for an already Pending habit
- **THEN** the habit remains Pending and no completion row is created

### Requirement: USABILITY-005 Habit history offers direct return navigation
Habit history SHALL provide a prominent, keyboard-accessible Back to Today action in addition to global navigation, while preserving the existing Edit action and six-week history. Risk: low. Evidence SHALL include Razor route rendering and browser navigation tests.

#### Scenario: User returns from history to Today
- **WHEN** the user activates Back to Today from a habit history page
- **THEN** the full Today view opens directly

#### Scenario: History actions remain usable on narrow layouts
- **WHEN** the history heading is rendered at a constrained desktop or mobile width
- **THEN** Back to Today, Edit, the title, and history content remain visible without overlap or horizontal overflow

### Requirement: USABILITY-006 Full navigation identifies the current section
The full application navigation SHALL identify exactly one current top-level section among Today, Habits, and Data using semantic current-page markup and a subtle visual indicator consistent with the existing color scheme. The indicator SHALL remain understandable without relying on color alone and SHALL NOT cause overlap or horizontal overflow. Risk: medium. Evidence SHALL include Razor rendering tests plus keyboard, forced-colors, and narrow-layout browser acceptance.

#### Scenario: Today is identified as current
- **WHEN** the full Today view is displayed
- **THEN** Today is the only top-level navigation item marked as the current page

#### Scenario: Habit pages remain associated with Habits
- **WHEN** the habit list, create, edit, or history view is displayed
- **THEN** Habits is the only top-level navigation item marked as the current page

#### Scenario: Data pages are identified as current
- **WHEN** a backup, export, or import page is displayed
- **THEN** Data is the only top-level navigation item marked as the current page

#### Scenario: Current navigation remains accessible and stable
- **WHEN** the full navigation is used with a keyboard, forced colors, or a constrained width
- **THEN** the current section remains perceivable without color alone and all navigation items remain operable without overlap or horizontal overflow

### Requirement: USABILITY-007 Desktop surfaces are mutually exclusive
The desktop application SHALL present compact and full views as mutually exclusive surfaces under one owner process. Activating either surface SHALL hide the other before presenting a refreshed destination view, so both windows are never intentionally visible at the same time. The full view SHALL provide a direct, keyboard-accessible action for opening the compact view, and browser fallback navigation SHALL remain functional. Risk: high. Evidence SHALL include desktop coordinator and lifecycle tests plus compact-to-full-to-compact Windows acceptance.

#### Scenario: Compact view opens the full application
- **WHEN** the user activates Ganze App öffnen from the compact view
- **THEN** the compact window is hidden and a refreshed full Today window is brought forward without starting another owner process

#### Scenario: Full application opens the compact view
- **WHEN** the user activates Miniaturansicht öffnen from the full view
- **THEN** the full window is hidden and a refreshed compact Today window is brought forward without starting another owner process

#### Scenario: Surface switching reflects persisted changes
- **WHEN** a user changes a habit outcome and then switches to the other desktop surface
- **THEN** the destination surface displays the persisted outcome and the previous surface is not visible

#### Scenario: Repeated activation preserves one visible surface
- **WHEN** tray activation, repeated application launch, or repeated surface-switch commands occur
- **THEN** one owner process remains active and no more than one application window is visible after the command completes

#### Scenario: Browser fallback retains local navigation
- **WHEN** the same full-to-compact or compact-to-full action is used outside the Windows desktop shell
- **THEN** it follows the corresponding loopback route in the current browser context without requiring JavaScript or external network access
