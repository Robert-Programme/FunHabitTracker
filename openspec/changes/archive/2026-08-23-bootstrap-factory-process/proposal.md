## Why

FunHabitTracker has project documentation and unit tests, but it has no
spec-first change workflow, deterministic repository gates, traceability model,
or CI enforcement. Agent behavior therefore depends too heavily on the active
chat instead of versioned project controls.

## What Changes

- Extend `AGENTS.md` with a factory workflow, boundaries, and Definition of Done.
- Add OpenSpec configuration and repository-local Codex skills.
- Add project-specific quality policy and deterministic .NET factory gates.
- Add requirement-to-evidence traceability and generated release evidence.
- Add a GitHub Actions workflow that runs the same gates as local development.
- Remove unused SQL Server/Newtonsoft diagnostics dependencies and update the
  required EF Core packages to the final .NET 6 patch after the new dependency
  gate exposes vulnerable transitive packages.

## Capabilities

### New Capabilities

- `factory-process`: Provides spec-first change control, project-native quality
  gates, traceability, security checks, and CI evidence for FunHabitTracker.

### Modified Capabilities

- None.

## Non-goals

- Upgrade the application from .NET 6.
- Change habit-tracking behavior or database schema.
- Add a deployment platform, production access, or hosted orchestrator.
- Claim independent testing where no separate verifier has run.

## Impact

The change adds development-process files and removes unused package references;
application source and runtime behavior remain unchanged. Node is used only for
the pinned OpenSpec CLI; Python standard library hosts the portable gate
orchestration.
