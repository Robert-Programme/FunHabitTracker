## Context

The existing application is a .NET 6 MVC solution with MSTest and SQLite. The
factory starter is Python-oriented and validates example contracts that do not
belong to this application. The integration must preserve the starter's control
principles while invoking the real FunHabitTracker toolchain.

## Goals / Non-Goals

**Goals:**

- Make OpenSpec and Codex workflow instructions visible inside the repository.
- Provide one local command that runs all mandatory project gates.
- Keep requirement, risk, verification, documentation, and code references
  machine-readable.
- Produce CI-uploadable security and release evidence.

**Non-goals:**

- Introduce application APIs or operational contracts solely to satisfy a template.
- Reformat or modify unrelated application code during bootstrap.
- Replace GitHub branch protection with repository files.

## Decisions

- Pin `@fission-ai/openspec` in `package-lock.json` for reproducible validation.
- Keep machine-readable `.yaml` policy and acceptance files in JSON-compatible
  syntax so `ci/factory.py` needs no Python packages.
- Treat compiler warnings as build failures and `dotnet format` as the style gate.
- Run NuGet vulnerability and repository secret scans as mandatory PR gates.
- Remove unused provider packages when the vulnerability gate proves they expand
  the attack surface; keep required EF Core packages on the final 6.0 patch line.
- Generate evidence into ignored `reports/` and `artifacts/` paths, then upload
  those paths from CI.

## Risks / Trade-offs

- .NET 6 is out of support and remains a separate modernization risk.
- The local secret scanner is intentionally narrow; hosted secret scanning can
  strengthen it later.
- File-inventory SBOM and provenance are bootstrap evidence, not signed release
  attestations.
- Controller and browser-level coverage remain limited until separate feature
  proposals add integration and end-to-end tests.
