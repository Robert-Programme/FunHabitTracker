# Installer Decision

The first Windows distribution uses a self-contained `win-x64` publish bundled with
PowerShell install and uninstall scripts in an offline ZIP archive.

## Decision drivers

- Offline installation with no separate .NET runtime or package feed.
- No additional installer compiler on developer or CI machines.
- Per-user install without administrator rights.
- Start Menu integration, in-place upgrades, and user-data preservation by default.
- A release artifact whose desktop executable can be code-signed externally without
  exposing certificate credentials in staging or provenance.
- A signed WebView2 offline bootstrapper with browser security servicing retained by
  Microsoft instead of shipping an application-owned fixed browser runtime.

MSIX was not selected because unsigned local deployment adds certificate management and
upgrade identity constraints. WiX and Inno Setup were not selected because neither tool
is present in the project toolchain and both add a separate compiler dependency. The
PowerShell bundle is less polished than a signed MSI, but it is deterministic, auditable,
and fully offline. A signed MSI/MSIX can replace it later without changing application
capabilities or data paths.
