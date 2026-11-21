## ADDED Requirements

### Requirement: SCHED-001 Users can manage the habit lifecycle
The system SHALL allow a user to create, view, edit, and archive a habit with a required trimmed name, optional description, start date, and recurrence schedule. Archiving SHALL preserve history and SHALL remove the habit from future Today views; restoring an archived habit SHALL not rewrite historical outcomes.

**Risk:** Medium
**Evidence:** `tests/FunHabitTracker.IntegrationTests/HabitLifecycleJourneyTests.cs`

#### Scenario: Create a valid habit
- **WHEN** the user submits a non-empty name, start date, and valid recurrence
- **THEN** the system persists the habit and makes it available on dates where its schedule is due

#### Scenario: Reject invalid input
- **WHEN** the user submits an empty name or an invalid recurrence
- **THEN** no habit is persisted and field-specific validation is displayed without requiring internet access

#### Scenario: Archive while preserving history
- **WHEN** the user archives a habit with prior outcomes
- **THEN** it is absent from future Today views and its prior outcomes remain visible in history

### Requirement: SCHED-002 Recurrence is evaluated using explicit local-date rules
The system SHALL support Daily and Selected Weekdays recurrence types beginning on a configured local start date. The due calculation MUST use the application's local-calendar provider and MUST NOT depend on free-form frequency text.

**Risk:** High
**Evidence:** `tests/FunHabitTracker.Tests/RecurrenceEvaluatorTests.cs`, `tests/FunHabitTracker.Tests/LocalDateProviderTests.cs`

#### Scenario: Daily schedule begins on start date
- **WHEN** a Daily habit has a start date in the future
- **THEN** it is not due before that date and is due on and after that date while active

#### Scenario: Selected weekdays schedule
- **WHEN** a habit is configured for Monday, Wednesday, and Friday
- **THEN** it is due only on those weekdays on or after its start date

#### Scenario: Local day changes at midnight
- **WHEN** the injected local-calendar provider advances across local midnight
- **THEN** Today and due calculations use the new local date consistently
