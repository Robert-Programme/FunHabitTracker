# FunHabitTracker Factory Process

This repository uses OpenSpec for material changes and a deterministic gate
runner for completion evidence. The process is intentionally project-native: it
checks the ASP.NET Core solution rather than requiring contracts or deployment
artifacts that the application does not own.

## Change Lifecycle

1. Inspect the repository and existing specs without changing application code.
2. Create a change with `$openspec-propose "description"`.
3. Review proposal, design, requirement scenarios, risk, and tasks.
4. Apply the approved change with `$openspec-apply-change`.
5. Run OpenSpec validation and all factory gates.
6. Archive the completed change with `$openspec-archive-change`.

## Local Verification

```powershell
npm ci
npm run openspec:validate
npm run factory:all
```

`npm run factory:all` executes:

- OpenSpec strict validation.
- NuGet restore and Release build with compiler warnings treated as errors.
- `dotnet format` verification.
- MSTest and Python standard-library tests for the factory validators.
- NuGet vulnerability and repository secret scans.
- Requirement-to-evidence traceability checks.
- SARIF, CycloneDX-style file inventory, provenance, and release evidence generation.

Generated evidence is written below `reports/` and `artifacts/`. It is ignored by
Git and uploaded by CI. It must not contain local databases, build outputs,
Data Protection keys, IDE state, or secrets.

## Requirement Evidence

Machine-readable acceptance packages live in `specs/*/acceptance.yaml`. Each
requirement records:

- A stable ID and statement.
- Risk and resolution status.
- Acceptance criteria.
- Verification evidence.
- Documentation and code references.

The traceability gate rejects missing references. High-risk changes should add
integration or independent acceptance evidence; critical changes also need an
explicit threat model and rollback plan.

## Current Limits

- Repository files cannot enable GitHub branch protection by themselves. Configure
  the `factory-gates` workflow as a required check in repository settings.
- The application targets unsupported .NET 6; modernization should be a separate
  OpenSpec change with compatibility tests.
- The bootstrap secret scanner complements rather than replaces GitHub secret
  scanning or an organization-grade scanner.
- Generated provenance is unsigned local evidence, not a hosted attestation.
