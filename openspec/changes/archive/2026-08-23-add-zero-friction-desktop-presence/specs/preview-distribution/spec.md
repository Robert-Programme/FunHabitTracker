## ADDED Requirements

### Requirement: DIST-001 Reproducible website publishing bundle
**Risk:** Medium. The release process SHALL produce one versioned Windows preview bundle suitable for static website hosting. The publishing bundle MUST contain the offline installer payload, SHA-256 checksum, machine-readable release manifest, installation and removal instructions, privacy notice, supported Windows architecture, minimum operating-system statement, and release notes.
**Evidence:** `installer/package.ps1`, `tests/factory/test_release_bundle.py`, `artifacts/evidence/website-bundle-inventory.json`

#### Scenario: Release bundle is built
- **WHEN** the release packaging command completes successfully
- **THEN** every required file is present, the manifest version matches the application version, and the recorded checksum matches the downloadable installer bytes

#### Scenario: Bundle is copied to static hosting
- **WHEN** the complete publishing bundle is served without server-side application logic
- **THEN** a visitor can determine the version, platform, file size, checksum, installation steps, and privacy behavior before downloading

### Requirement: DIST-002 Honest signing and preview status
**Risk:** High. Release metadata and human-readable publishing guidance MUST identify the package as a preview and MUST state whether the executable and installer are signed. The pipeline SHALL support signing through externally supplied credentials but MUST fail a production-marked release when the required signature cannot be verified. Secrets and private signing keys MUST NOT be stored in the repository or publishing bundle.
**Evidence:** `tests/factory/test_release_trust.py`, `artifacts/evidence/release-signature-report.json`, `artifacts/evidence/sbom.spdx.json`

#### Scenario: Unsigned preview is packaged
- **WHEN** no signing credential is supplied for a preview build
- **THEN** packaging succeeds only with manifest and installation guidance that explicitly report the unsigned preview status

#### Scenario: Production release is requested without a valid signature
- **WHEN** packaging is configured as production and signature verification is absent or fails
- **THEN** the release gate fails before publishing artifacts are declared ready

#### Scenario: Signed package is produced
- **WHEN** approved external signing credentials are supplied
- **THEN** the pipeline verifies the resulting signatures and records signer and timestamp verification without exposing credential material

### Requirement: DIST-003 Data-preserving install and update contract
**Risk:** High. A website-distributed preview installation SHALL remain self-contained and offline after download, SHALL install per user without administrator rights, SHALL preserve `%LOCALAPPDATA%\FunHabitTracker` during normal update and removal, and SHALL make destructive data removal an explicit separate choice.
**Evidence:** `installer/verify.ps1`, `artifacts/evidence/windows-clean-install.md`, `artifacts/evidence/windows-upgrade-uninstall.md`

#### Scenario: User installs while offline
- **WHEN** the complete downloaded bundle is installed with external networking blocked
- **THEN** the application, desktop presence, and runtime dependencies are installed without an additional download

#### Scenario: User installs a newer preview
- **WHEN** a supported prior preview and local habit database exist
- **THEN** installation replaces application files, preserves user data and settings, and the upgraded application opens the existing dataset

#### Scenario: User performs a normal uninstall
- **WHEN** the user removes the application without selecting data deletion
- **THEN** application files, shortcuts, notification-area process, and start-at-sign-in registration are removed while local habit data and backups remain

### Requirement: DIST-004 Verifiable release acceptance
**Risk:** High. A publishing bundle MUST NOT be declared ready until strict OpenSpec validation, the project build and test suite, Factory gates, dependency vulnerability checks, desktop lifecycle acceptance, blocked-network acceptance, and clean install/update/removal acceptance all pass for the exact packaged bytes.
**Evidence:** `.github/workflows/factory.yml`, `tests/factory/`, `artifacts/evidence/release-provenance.json`, `artifacts/evidence/website-bundle-inventory.json`

#### Scenario: Acceptance evidence matches the package
- **WHEN** release verification completes
- **THEN** provenance links the tested application version and SHA-256 digest to the exact website publishing bundle

#### Scenario: A mandatory gate fails
- **WHEN** any required specification, build, test, security, desktop, offline, or installer gate fails
- **THEN** packaging does not mark the website publishing bundle as releasable
