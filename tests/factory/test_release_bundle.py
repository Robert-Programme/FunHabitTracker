import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest

from ci.release_bundle import BundleValidationError, REQUIRED_DOCUMENTS, REQUIRED_EVIDENCE, validate


class ReleaseBundleTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        installer = self.root / "setup.zip"
        installer.write_bytes(b"installer")
        for filename in REQUIRED_DOCUMENTS | REQUIRED_EVIDENCE:
            (self.root / filename).write_text("safe release content\n", encoding="utf-8")
        self.manifest = {
            "product": "Fun Habit Tracker Preview",
            "version": "0.2.0-preview.1",
            "channel": "preview",
            "architecture": "win-x64",
            "minimumWindows": "10.0.17763.0",
            "installer": {"file": "setup.zip", "bytes": 9, "sha256": hashlib.sha256(b"installer").hexdigest()},
            "signature": {"status": "unsigned-preview", "signer": None},
            "runtime": {"bundled": True, "signature": "valid-microsoft"},
            "documents": sorted(REQUIRED_DOCUMENTS),
            "evidence": sorted(REQUIRED_EVIDENCE),
        }

    def tearDown(self) -> None:
        self.temp.cleanup()

    def test_valid_preview_passes(self) -> None:
        validate(self.manifest, self.root)

    def test_missing_file_and_digest_mismatch_are_rejected(self) -> None:
        (self.root / "privacy.md").unlink()
        with self.assertRaises(BundleValidationError):
            validate(self.manifest, self.root)
        (self.root / "privacy.md").write_text("safe", encoding="utf-8")
        changed = copy.deepcopy(self.manifest)
        changed["installer"]["sha256"] = "0" * 64
        with self.assertRaises(BundleValidationError):
            validate(changed, self.root)

    def test_false_signature_claim_and_unsigned_production_are_rejected(self) -> None:
        false_claim = copy.deepcopy(self.manifest)
        false_claim["signature"] = {"status": "valid", "signer": None}
        with self.assertRaises(BundleValidationError):
            validate(false_claim, self.root)
        production = copy.deepcopy(self.manifest)
        production["channel"] = "production"
        with self.assertRaises(BundleValidationError):
            validate(production, self.root)

    def test_secret_material_is_rejected(self) -> None:
        (self.root / "privacy.md").write_text("client_secret=do-not-publish", encoding="utf-8")
        with self.assertRaises(BundleValidationError):
            validate(self.manifest, self.root)


if __name__ == "__main__":
    unittest.main()
