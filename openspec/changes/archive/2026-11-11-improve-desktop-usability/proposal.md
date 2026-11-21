## Why

Hands-on preview testing exposed several interactions that look actionable but do not behave as users expect: the compact window becomes difficult to recover after minimizing, its status marker is not clickable, selected outcomes cannot be toggled off directly, habit history lacks an obvious return path, the full view does not clearly identify the current section, and compact and full windows can remain visible at the same time. These issues add friction or ambiguity to the app's most frequent daily workflow and should be corrected before expanding the product scope.

## What Changes

- Keep the compact window recoverable through the Windows taskbar, Alt-Tab, tray activation, and repeated application launch after it is minimized.
- Disable maximizing the compact window while preserving resize within useful compact bounds.
- Retain native Windows chrome for this iteration while applying the product icon and clear window titles.
- Make each full Today status marker an accessible primary control that toggles the habit between Pending and Done.
- Make active Done and Skipped actions toggle back to Pending when selected again; keep Pending as the absence of a stored outcome rather than inventing a fourth state.
- Add an obvious Back to Today action to habit history.
- Add a subtle, accessible active indicator to the full view navigation for Today, Habits, and Data.
- Treat compact and full views as mutually exclusive desktop surfaces: opening one hides the other, and the full view provides a direct action for returning to the compact view.
- Add focused desktop, controller, rendering, keyboard, and regression coverage for the changed interactions.

Non-goals:

- Custom-drawn window chrome or a redesign of the application visual system.
- Moving export/backup out of the main navigation.
- Changes to compact-view information architecture beyond the same outcome semantics.
- Supporting compact and full desktop windows as simultaneously visible workspaces.
- Sync, mobile clients, accounts, gamification, reminders, or other post-preview features.

## Capabilities

### New Capabilities

- `desktop-usability`: Recoverable and mutually exclusive desktop surfaces plus low-friction, accessible daily outcome and navigation interactions.

### Modified Capabilities

None.

## Impact

- Affected areas: `FunHabitTracker.Desktop` window lifecycle, surface switching, and icon metadata; shared full-view navigation; full Today Razor rendering; `HomeController` outcome actions; habit history navigation; CSS; and automated tests.
- Database schema: no change. Existing Done, Skipped, and absent/Pending persistence semantics remain authoritative.
- Security and privacy: no new network access, protocol command, stored data, or external dependency. Existing anti-forgery and loopback-origin controls remain required.
- Operations: release packaging must continue to include the correct desktop icon; installer and lifecycle smoke checks must still pass.
- Compatibility: no breaking data or route contract is intended. Existing server-rendered forms and no-JavaScript operation remain supported.
