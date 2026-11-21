# Static Website Publishing

Run `installer/package.ps1` only after the OpenSpec and Factory gates pass. Publish the
complete versioned directory under
`artifacts/website/fun-habit-tracker/0.2.0-preview.1/` without renaming individual files.
It contains the installer ZIP, SHA-256 checksum, machine-readable manifest, SBOM,
provenance, signature report, installation instructions, privacy notice, trust notice,
and release notes. No server-side application is required.

Before upload, run `python ci/release_bundle.py <manifest> <directory>` and
`installer/verify.ps1`. After upload, verify the downloaded ZIP against both the manifest
and checksum. Preview pages must show `unsigned-preview` before the download link. Do not
label a build production unless packaging reports a valid Authenticode signature.

Keep each version immutable. A replacement build receives a new version and directory;
never overwrite bytes behind an already published checksum.
