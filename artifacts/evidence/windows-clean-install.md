# Windows desktop installer acceptance

- Offline self-contained desktop and bundled WebView2 runtime installed.
- Start Menu shortcut targets FunHabitTracker.Desktop.exe.
- Background launch created a tray owner and loopback endpoint: http://127.0.0.1:65184.
- Compact and full application routes returned HTTP 200.
- No non-loopback ASP.NET listener was exposed.
- Duplicate activation exited and reused the owning process.
- In-place update preserved habit data and desktop settings.
- Normal removal removed application files and autostart registration while preserving data.
- Explicit destructive removal deleted the isolated test data.
- Archive SHA-256: 9ad669f46536a0c5c08da5e603ebe54fc4232551387c1759766431753fa62e89
