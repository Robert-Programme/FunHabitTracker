# Windows Installation

1. Extract `FunHabitTracker-Setup-win-x64.zip` to a local folder.
2. Run `install.ps1` in PowerShell.
3. Start **Fun Habit Tracker** from the Start Menu. The compact Today window opens and
   the app remains available from the notification area after the window is closed.

The bundle includes .NET, local assets, and Microsoft's signed WebView2 Evergreen
Standalone Installer. Setup installs WebView2 only if no compatible runtime exists and
does not download runtime bytes. The application listens only on a random loopback port.
Launching the shortcut again reopens the existing local instance.

Use the tray menu to open compact Today, open the full application, opt in or out of
**Mit Windows starten**, or explicitly exit. Closing a window hides it; **Beenden** stops
the tray owner and local host.

To upgrade, extract the newer bundle and run its `install.ps1`. Application files are
replaced and `%LOCALAPPDATA%\FunHabitTracker` remains unchanged.

Run the installed `uninstall.ps1` from another working directory to remove application
files. User data is preserved unless `-RemoveData` is explicitly supplied.

Version `0.2.0-preview.1` is an unsigned preview. Compare the ZIP with the published
SHA-256 file before installation and read `preview-trust.md` for the resulting Windows
trust limitations.
