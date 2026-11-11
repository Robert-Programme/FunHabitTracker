# Desktop Release Operations

## Signing

`installer/package.ps1` accepts `-SignToolPath`, `-CertificateThumbprint`, and
`-TimestampUrl`. The certificate stays in the Windows certificate store; private keys,
tokens, and passwords must not be passed as files under the repository or copied into
staging. The production channel fails unless the desktop executable verifies as signed.

## Rollback

Keep the prior immutable website directory available. To roll back application files,
run the prior version's `install.ps1`; the installer stops the owner and preserves the
per-user data directory and desktop settings. Never replace or delete the SQLite database
as part of an application rollback. Take an in-app backup before a schema-changing update.

## Troubleshooting

- No window: launch the Start Menu shortcut again or use tray **Heute öffnen**.
- No tray owner: inspect `%LOCALAPPDATA%\FunHabitTracker\Logs\desktop.log` and
  `startup.log`, then run the browser fallback project if WebView initialization failed.
- Autostart mismatch: toggle **Mit Windows starten** off and on; the UI reads the actual
  current-user Run registration and does not trust stale settings.
- Port conflict: restart the owner; it selects another available loopback port.
- Broken update: reinstall the prior release. User data remains outside the install root.
- Corrupt data: follow `backup-restore.md`; do not delete files before preserving a copy.

Run `installer/verify.ps1` after every packaging change. It uses isolated install, data,
and Start Menu paths, restores any pre-existing Run registration, and validates clean
install, duplicate activation, update, preserved-data uninstall, and explicit data removal.
