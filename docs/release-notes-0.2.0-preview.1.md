# Fun Habit Tracker 0.2.0-preview.1

- Adds a compact Today desktop window and Windows notification-area access.
- Adds optional, reversible start at sign-in without foreground interruption.
- Reuses the existing local SQLite data, history, backup, and restore workflows.
- Keeps all core runtime traffic on loopback and bundles the WebView2 runtime installer.
- Adds a static website publishing manifest, checksums, trust status, and release evidence.
- Keeps minimized compact windows discoverable and distinguishes Minimize, close-to-tray,
  and explicit Quit.
- Makes the Today status marker clickable, lets active Done or Skipped actions return to
  Pending, and adds direct Back to Today navigation from habit history.

This preview does not include mobile clients, accounts, cloud sync, reminders, or
gamification.
