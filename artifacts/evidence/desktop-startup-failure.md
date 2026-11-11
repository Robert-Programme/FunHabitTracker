# Desktop startup failure acceptance

`DesktopFailureTests.InvalidDataRoot_FailsWithoutDeletingTheUserSelectedPath` supplies an
invalid local data root and verifies that startup fails while preserving the selected file.
Desktop exceptions are appended to `%LOCALAPPDATA%\FunHabitTracker\Logs\desktop.log` and
the WPF shell presents the log location. Database initialization failures retain the
existing startup log and backup guidance. WebView initialization failure offers the
loopback-only system-browser fallback.
