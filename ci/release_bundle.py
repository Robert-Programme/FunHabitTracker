"""Validation for website-ready Fun Habit Tracker release bundles."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any


class BundleValidationError(RuntimeError):
    pass


REQUIRED_DOCUMENTS = {
    "installation.md",
    "privacy.md",
    "preview-trust.md",
    "release-notes-0.2.0-preview.1.md",
}
REQUIRED_EVIDENCE = {"sbom.cdx.json", "release-provenance.json", "release-signature-report.json"}
SECRET_MARKERS = ("-----BEGIN " + "PRIVATE KEY-----", "password=", "client_secret", "certificatepassword")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def validate(manifest: dict[str, Any], root: Path) -> None:
    required = {"product", "version", "channel", "architecture", "minimumWindows", "installer", "signature", "runtime", "documents", "evidence"}
    missing = required - manifest.keys()
    if missing:
        raise BundleValidationError(f"Manifest fields missing: {sorted(missing)}")
    if manifest["channel"] not in {"preview", "production"}:
        raise BundleValidationError("Unknown release channel")
    if manifest["architecture"] != "win-x64":
        raise BundleValidationError("Unexpected release architecture")

    installer = manifest["installer"]
    installer_path = root / str(installer.get("file", ""))
    if not installer_path.is_file():
        raise BundleValidationError("Installer file is missing")
    if installer_path.stat().st_size != installer.get("bytes"):
        raise BundleValidationError("Installer byte count does not match")
    if sha256(installer_path) != str(installer.get("sha256", "")).lower():
        raise BundleValidationError("Installer digest does not match")

    documents = set(map(str, manifest["documents"]))
    evidence = set(map(str, manifest["evidence"]))
    if documents != REQUIRED_DOCUMENTS or evidence != REQUIRED_EVIDENCE:
        raise BundleValidationError("Publishing inventory is incomplete or unexpected")
    for filename in documents | evidence:
        if not (root / filename).is_file():
            raise BundleValidationError(f"Publishing file is missing: {filename}")

    signature = manifest["signature"]
    status = signature.get("status")
    if manifest["channel"] == "production" and status != "valid":
        raise BundleValidationError("Production release is not signed")
    if status == "valid" and not signature.get("signer"):
        raise BundleValidationError("Valid signature claim has no signer")
    if status == "unsigned-preview" and manifest["channel"] != "preview":
        raise BundleValidationError("Unsigned status is only valid for preview releases")

    for path in root.iterdir():
        if not path.is_file() or path.suffix.lower() not in {".json", ".md", ".txt"}:
            continue
        text = path.read_text(encoding="utf-8").lower()
        if any(marker.lower() in text for marker in SECRET_MARKERS):
            raise BundleValidationError(f"Potential secret material in {path.name}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("manifest", type=Path)
    parser.add_argument("root", type=Path)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text(encoding="utf-8-sig"))
    validate(manifest, args.root)
    print("Release bundle validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
