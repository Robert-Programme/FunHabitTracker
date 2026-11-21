# Desktop lifecycle acceptance

- The WPF owner starts one ASP.NET Core host on an available IPv4 loopback port.
- A per-user mutex prevents a second owning process.
- A current-user-only named pipe accepts only `ShowCompact`, `ShowFull`, and `Quit`.
- Foreground launch opens Compact Today; `--background` creates tray presence without a foreground window.
- Closing a window hides it to the tray; explicit Quit closes WebView windows, removes the endpoint file, stops the web host, and exits.
- The browser fallback rejects non-loopback targets.
- Start-at-sign-in uses `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, reflects actual registration state, and rolls back failed writes.

Automated coverage: `DesktopLifecycleTests`, `DesktopSettingsTests`, and `installer/verify.ps1`.
