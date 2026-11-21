## Why

The current Windows release is reliable offline but still behaves like a locally hosted website: the user must deliberately launch it and work in a normal browser tab. The next release should make the daily check-in immediately reachable while providing a transparent preview package that can be downloaded from the product website without pretending that the current unsigned distribution is store-grade.

## What Changes

- Add a Windows desktop companion that can remain available in the notification area and open the Today experience without starting duplicate application instances.
- Add an optional, reversible start-at-sign-in setting; it MUST be opt-in and visible to the user.
- Add a compact Today surface optimized for marking due habits Done, Skipped, or Pending, with an explicit path to the full application.
- Preserve the existing full responsive application and all offline tracking, history, backup, and restore behavior.
- Define predictable close, minimize, reopen, startup-failure, and no-due-habit behavior for the desktop companion.
- Produce a versioned website publishing bundle containing the Windows preview installer, checksum, release metadata, installation guidance, privacy information, and an explicit preview/signing status.
- Add automated and manual acceptance evidence for launch latency, single-instance behavior, compact-window layouts, keyboard access, offline operation, install/upgrade/uninstall, and website bundle integrity.

### Non-goals

- Mobile clients, cloud sync, accounts, remote APIs, or multi-device conflict resolution.
- Gamification, achievements, social features, or scheduled reminder notifications.
- Publishing to Google Play, Apple App Store, or Microsoft Store.
- Deploying or modifying an external website or CMS that is not part of this repository.
- Hiding autostart, forcing pop-ups, or interrupting the user without an explicit action or setting.
- Replacing SQLite, changing habit recurrence semantics, or redesigning backup formats.

## Capabilities

### New Capabilities

- `desktop-presence`: User-controlled Windows tray presence, optional start-at-sign-in, compact Today interaction, full-window transition, and reliable lifecycle behavior.
- `preview-distribution`: Reproducible, integrity-verifiable Windows preview artifacts and website publishing metadata with honest trust and signing disclosures.

### Modified Capabilities

None.

## Impact

- **Application areas:** Windows process lifecycle, desktop hosting shell, Today presentation, configuration, accessibility, startup diagnostics, installer scripts, release packaging, and acceptance evidence.
- **Database schema:** Not affected. Existing habits, outcomes, backups, and stable per-user data paths remain compatible.
- **Security and privacy:** Affected. Autostart registration must be user-controlled, desktop hosting must remain loopback-only or use an equally local IPC boundary, release hashes must be generated deterministically, and unsigned preview status must not be concealed.
- **Operations:** Affected. Packaging gains website-ready release metadata, optional signing hooks, and desktop lifecycle verification. External website deployment remains a separate operation.
- **Dependencies:** A Windows desktop shell or tray integration may add a pinned runtime dependency; selection must account for offline packaging, accessibility, update size, code signing, and future UI migration risk.
- **Compatibility:** Existing `%LOCALAPPDATA%\FunHabitTracker` data and installer upgrade behavior must be preserved. This release remains Windows-first and does not establish the future mobile UI framework or sync protocol.
