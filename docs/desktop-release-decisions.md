# Desktop Preview Decisions

## Product and support

- Product: **Fun Habit Tracker Preview**
- Version: `0.2.0-preview.1`
- Channel: `preview`
- Target: self-contained `win-x64`
- Minimum supported operating system: Windows 10 version 1809
- Website publishing directory: `artifacts/website/fun-habit-tracker/0.2.0-preview.1/`
- Product icon: `src/FunHabitTracker.Desktop/Assets/app-icon.png`

The preview remains local-first. Its Today, habit management, history, backup, and
restore workflows do not require internet access.

## Desktop runtime

The desktop shell uses WPF, `System.Windows.Forms.NotifyIcon`, and the pinned
`Microsoft.Web.WebView2` SDK. The release package includes the WebView2 Evergreen
Standalone Installer and invokes it only when no compatible runtime is installed.
Installation never fetches runtime bytes from the network. A fixed-version runtime was
not selected because it would place browser security servicing entirely on each app
release and materially increase every application payload.

## Signing and trust

No Authenticode certificate or timestamp credential is currently configured. Therefore
`0.2.0-preview.1` is an **unsigned preview**. The website manifest and instructions must
state this before download. SHA-256 hashes, SBOM, provenance, and release evidence help
verify exact bytes but do not replace publisher identity or Windows reputation.

The packaging script accepts externally supplied signing configuration. A production
channel build fails unless signatures verify. Private keys and credentials are never
stored in the repository, staging directory, or logs.

## Acceptance hardware

The initial performance baseline is measured on `LAPTOP-JHIG9EOJ`, Windows
`10.0.26200.0`, Intel64 Family 6 Model 154, with 15.7 GB RAM. The prior MVC development
host reached its loopback Today page in 3246 ms from `dotnet run`; a warm page request
took 58 ms and the process working set was 84.4 MB. Release acceptance measures the
self-contained desktop executable separately and retains raw timings.
