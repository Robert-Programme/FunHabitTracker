## Context

See `proposal.md` for motivation and `specs/desktop-usability/spec.md` for observable behavior. The compact WPF window currently opts out of the taskbar while still exposing Minimize and Maximize. Both desktop windows use native chrome but do not assign a WPF/assembly icon. Full Today renders a decorative status symbol beside three independent server forms; `SetOutcome` always sets the requested value and `ClearOutcome` removes it. The shared full-view navigation does not identify the active section, and the desktop coordinator can leave compact and full windows visible simultaneously.

The application must preserve server-rendered, anti-forgery-protected operation, loopback-only navigation, the current SQLite schema, and the existing single desktop owner.

## Goals / Non-Goals

**Goals:**

- Make Windows lifecycle behavior match the commands visibly offered by each window.
- Centralize reversible outcome semantics so full and compact forms cannot diverge.
- Turn the visually dominant full-view status marker into the fastest completion control.
- Add direct local navigation from history without changing history data or routing contracts.
- Make the current full-view section apparent without adding visual noise.
- Make compact and full views two modes of one desktop experience rather than parallel workspaces.
- Keep all changes testable below the level of fragile OS UI automation where possible.

**Non-Goals:**

- Replacing native Windows chrome with custom-drawn controls.
- Adding another completion state or changing completion storage.
- Merging compact and full windows into one WPF instance.
- Restructuring main navigation or backup/export behavior.
- Preserving both desktop surfaces as simultaneously visible windows.

## Decisions

### Use native chrome with per-window capabilities

The compact window will participate in the taskbar and Alt-Tab and use a non-maximizable native resize mode. Close interception remains close-to-tray. `BringForward` already normalizes a minimized window before activation and remains the shared recovery path for tray and named-pipe activation. The full window retains standard maximization.

Alternative considered: hide the compact window immediately when Minimize is pressed. This would still make it disappear unexpectedly and would overload Minimize with tray semantics. Alternative considered: custom chrome. It would require recreating drag, resize, keyboard system menus, DPI handling, accessibility, and Windows snap behavior for minimal product value.

### Generate and assign a Windows ICO from the approved product artwork

The existing PNG remains the source artwork for web and tray use. A multi-size ICO will be produced as a repository asset and assigned to the desktop executable and WPF windows so native title bars, taskbar entries, shortcuts, and Alt-Tab use the same identity. Packaging continues to copy the existing assets.

Alternative considered: leave the default executable icon and style only the web content. Preview feedback shows that native shell identity is part of the perceived quality and discoverability.

### Centralize toggle semantics in the daily tracking service

The daily tracking service will add one toggle operation that loads the current date-specific completion and removes it when the requested stored outcome is already active, otherwise creating or replacing it. The controller remains responsible only for validating the request, selecting the local date, and redirecting. Both full and compact forms call the same endpoint, preserving anti-forgery checks and no-JavaScript behavior. Existing explicit Set and Clear service operations remain available, and the explicit Clear endpoint remains an idempotent Pending reset.

Alternative considered: let the controller infer current state from a dashboard query or hidden form field. Either value can be stale and would split persistence decisions from the database-owning service. Alternative considered: implement toggling in JavaScript. This would split persistence ownership and violate the no-JavaScript requirement.

### Render the full-view marker as a semantic server form

The status marker becomes a button inside a form posting Done to the shared toggle endpoint. `aria-pressed`, a habit-specific accessible label, visible symbol, focus styling, and a stable square hit target communicate state without color. The labeled action controls remain available for explicit Done, Skipped, and Pending selection; active Done and Skipped use shared toggle behavior.

Alternative considered: make the entire habit row clickable. The row contains a history link and multiple forms, producing ambiguous keyboard and pointer targets.

### Add an explicit route link in history heading actions

History will render Back to Today and Edit as separate commands in a responsive action group. Back navigation uses the known Today route rather than browser history, so it behaves consistently when history was opened from a bookmark or external activation.

### Derive the active full-view navigation item on the server

The shared full-view layout will map the current controller or route to exactly one top-level section: Today, Habits, or Data. The matching link will receive `aria-current="page"` and a restrained green visual treatment that remains distinguishable by shape or weight in addition to color. Child routes such as habit creation, editing, and history remain associated with Habits. The compact layout does not render this navigation and therefore does not need an active item.

Alternative considered: set the active item with client-side JavaScript. Server-side routing already has the authoritative context, works without JavaScript, avoids a flash of incorrect state, and is straightforward to test.

### Let the desktop coordinator own exclusive surface transitions

Compact and full remain separate WPF/WebView instances, but their public activation paths become mutually exclusive. Showing the full surface first hides compact, navigates or refreshes the full WebView to the requested full route, and then brings it forward. Showing compact first hides full, refreshes compact Today, and then brings it forward. Tray and repeated-launch activation continue to use compact as the default surface, while explicit Quit still shuts down the single owner process and both windows.

The full layout will expose a bottom-left `Miniaturansicht öffnen` link to the compact route. In the desktop shell, exact loopback navigation to that route is intercepted and delegated to the coordinator; in browser fallback mode, it remains a normal local link. The existing `Ganze App öffnen` transition uses the inverse coordinator operation. Destination refresh prevents a hidden WebView from showing an outcome state that changed in the previously active surface.

Alternative considered: close and recreate a WebView window on every switch. Keeping the two owned windows alive avoids repeated WebView startup cost and preserves the current lifecycle architecture while still guaranteeing that only one is visible. Alternative considered: allow both windows and rely on live refresh. That preserves the confusing parallel-workspace model and does not solve visibility across virtual desktops.

## Risks / Trade-offs

- [Compact taskbar participation adds another visible taskbar item while running] -> This is intentional when a window is minimized; close-to-tray remains available for users who want no taskbar item.
- [Toggle requests can race under rapid repeated activation] -> Keep database uniqueness and make each request evaluate current persisted state transactionally enough for the existing single-user loopback model; add repeated-toggle tests.
- [Done exists as both marker and labeled action] -> Treat the marker as the fast path and the text controls as explicit, discoverable status choices; ensure both share identical persistence semantics.
- [ICO generation can cause non-deterministic metadata churn] -> Commit one generated multi-size ICO and test its inclusion rather than generating it during every build.
- [Title-bar appearance still follows Windows theme] -> Accept native styling in this iteration; product identity improves through correct icon and title without taking ownership of system chrome.
- [Route-to-navigation mapping can drift as pages are added] -> Centralize the mapping in the shared layout or a small helper and cover all current top-level and child routes with rendering tests.
- [A hidden WebView can retain stale rendered state] -> Navigate or refresh the destination before it becomes active and verify compact-to-full-to-compact journeys after outcome changes.
- [A surface switch can briefly expose both windows or neither window] -> Keep hide, refresh, show, and activation ordering inside one coordinator operation and assert final visibility in lifecycle tests.

## Migration Plan

No data migration is required. Build and test the web behavior first, then desktop window configuration, exclusive surface switching, and icon packaging. Run the full OpenSpec and Factory gates, rebuild the preview bundle, and execute installer/lifecycle and browser acceptance. Rollback consists of restoring the prior application files; the unchanged SQLite database remains compatible.
