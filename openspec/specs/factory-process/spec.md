# factory-process Specification

## Purpose
TBD - created by archiving change bootstrap-factory-process. Update Purpose after archive.

## Requirements

### Requirement: FACTORY-001 Spec-First Change Control

Every feature and material behavior change SHALL have versioned OpenSpec
artifacts with explicit acceptance scenarios before implementation is considered
complete.

#### Scenario: Material change reaches completion review

- **WHEN** a feature or material change is evaluated for completion
- **THEN** an OpenSpec proposal, design, requirement delta, and task list SHALL
  describe the implemented behavior and verification

### Requirement: FACTORY-002 Project-Native Quality Gates

The repository SHALL expose deterministic gates that invoke the actual .NET
restore, build, format, and test toolchain.

#### Scenario: Factory command runs locally or in CI

- **WHEN** `npm run factory:all` is executed from the repository root
- **THEN** all mandatory gates SHALL pass or the command SHALL return a non-zero
  exit code with an actionable failure

### Requirement: FACTORY-003 Traceability And Security

Each factory requirement SHALL identify risk, acceptance criteria, verification
evidence, documentation, and implementation references, and the repository SHALL
scan dependencies and committed text for known secret forms.

#### Scenario: Traceability or security evidence is missing

- **WHEN** a requirement references a missing file or a mandatory security scan fails
- **THEN** the factory command SHALL reject the candidate

### Requirement: FACTORY-004 CI Evidence

Pull requests and pushes to main SHALL run OpenSpec and factory gates and retain
machine-readable security and release evidence.

#### Scenario: GitHub Actions evaluates a change

- **WHEN** the factory workflow runs
- **THEN** it SHALL execute the same gate entry point used locally and upload
  SARIF, SBOM, provenance, and release-evidence outputs
