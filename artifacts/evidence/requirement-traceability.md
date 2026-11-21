# Local-first requirement traceability

| Requirement | Primary verification |
| --- | --- |
| TRACK-001 | `DailyTrackingServiceTests`, `OfflineCoreJourneyTests`, desktop/mobile screenshots |
| TRACK-002 | `StreakCalculatorTests`, history browser journey |
| SCHED-001 | `HabitService`, `OfflineCoreJourneyTests` |
| SCHED-002 | `RecurrenceEvaluatorTests`, `LocalDateProviderTests` |
| DATA-001 | `AppPathsTests`, `LegacyDatabaseMigrationTests` |
| DATA-002 | real-SQLite service and backup tests |
| BACKUP-001 | JSON round-trip and browser export |
| BACKUP-002 | invalid input test, preview/confirm journey, transactional import |
| BACKUP-003 | SQLite backup/restore integrity test |
| OFFLINE-001 | packaged install, loopback, duplicate launch, uninstall smoke test |
| OFFLINE-002 | local-resource scan and no-JavaScript core journey |
| OFFLINE-003 | semantic DOM inspection and 360-pixel screenshot |
| PRESENCE-001 | desktop lifecycle, settings, and installer acceptance |
| PRESENCE-002 | compact ASP.NET journey and desktop screenshot |
| PRESENCE-003 | named-pipe single instance and launch timing evidence |
| PRESENCE-004 | Playwright keyboard, overflow, and loopback-only network log |
| PRESENCE-005 | invalid-root preservation and startup recovery evidence |
| DIST-001 | release manifest validator and website inventory |
| DIST-002 | signature report, trust disclosure, and production rejection tests |
| DIST-003 | clean install, update, normal removal, and explicit data removal |
| DIST-004 | Factory gates and exact package provenance |
| USABILITY-001 | desktop window acceptance, activation lifecycle tests, chrome screenshot |
| USABILITY-002 | native style/profile tests, executable icon test, chrome screenshot |
| USABILITY-003 | no-JavaScript outcome journey, keyboard Playwright journey, Today rendering |
| USABILITY-004 | real-SQLite toggle tests, compact/full HTTP journeys |
| USABILITY-005 | history route assertion, responsive desktop/narrow screenshots |
