# JSON schema v1 evidence

The versioned contract is documented in `docs/json-backup-v1.md`. `BackupServiceTests`
round-trips a habit, schedule, lifecycle state, and outcome through format version 1.
`OfflineCoreJourneyTests` performs export, upload preview, explicit confirmation, and
replacement through anti-forgery-protected HTTP forms.
