## ADDED Requirements

### Requirement: TRACK-001 Today view presents actionable due habits
The system SHALL present habits due on the user's current local date in a responsive Today view and SHALL represent each due habit as Pending, Done, or Skipped. The user MUST be able to change an outcome without creating duplicate records, and the persisted result SHALL remain visible after restart.

**Risk:** High
**Evidence:** `tests/FunHabitTracker.IntegrationTests/TodayJourneyTests.cs`, `artifacts/evidence/local-restart-today.md`

#### Scenario: Complete a due habit
- **WHEN** the user marks a Pending habit as Done
- **THEN** exactly one Done outcome is persisted for that habit and local date and the Today view displays it as Done

#### Scenario: Skip a due habit without JavaScript
- **WHEN** JavaScript is unavailable and the user submits the Skip action for a due habit
- **THEN** the outcome is persisted as Skipped and the refreshed Today view displays it as Skipped

#### Scenario: Reject an unknown habit
- **WHEN** an outcome command references a habit that does not exist or is archived
- **THEN** the system rejects the command without creating an orphaned outcome and displays an actionable response

### Requirement: TRACK-002 History and streak summaries follow schedule semantics
The system SHALL provide per-habit history and summary values derived from persisted schedules and outcomes. A streak SHALL count consecutive scheduled dates marked Done; a scheduled Skipped date SHALL neither extend nor break the streak, an unscheduled date SHALL be ignored, and a scheduled missed date SHALL break the streak.

**Risk:** High
**Evidence:** `tests/FunHabitTracker.Tests/StreakCalculatorTests.cs`, `tests/FunHabitTracker.IntegrationTests/HistoryJourneyTests.cs`

#### Scenario: Calculate a streak across an unscheduled day
- **WHEN** a weekday-only habit is Done on two consecutive scheduled dates separated by an unscheduled weekend
- **THEN** the system reports a two-occurrence streak

#### Scenario: Preserve but do not extend a streak on skip
- **WHEN** a scheduled date between two Done outcomes is marked Skipped
- **THEN** the skipped date does not add to the streak and does not reset it

#### Scenario: Break a streak on a missed scheduled date
- **WHEN** a scheduled past date has no Done or Skipped outcome
- **THEN** the current streak does not continue across that date
