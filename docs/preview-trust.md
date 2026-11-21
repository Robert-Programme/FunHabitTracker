# Preview Trust Status

Version `0.2.0-preview.1` is an unsigned Windows preview unless the accompanying
`release-manifest.json` reports a valid signer. Windows may display a reputation warning.
Verify the downloaded installer archive against `FunHabitTracker-Setup-win-x64.zip.sha256`.
Checksums detect changed bytes but do not provide publisher identity. Production packaging
is blocked unless Authenticode verification succeeds.
