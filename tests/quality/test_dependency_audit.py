import importlib.util
import pathlib
import unittest

spec = importlib.util.spec_from_file_location(
    "audit", pathlib.Path(__file__).resolve().parents[2] / "scripts/check_dependency_audit.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class DependencyAuditTests(unittest.TestCase):
    def test_accepts_clean_nuget_report(self):
        audit.verify({"version": 1, "projects": [{"path": "app.csproj"}]})

    def test_rejects_transitive_vulnerability(self):
        report = {"version": 1, "projects": [{"path": "app.csproj", "frameworks": [
            {"transitivePackages": [{"vulnerabilities": [{"severity": "High"}]}]}]}]}
        with self.assertRaises(ValueError):
            audit.verify(report)

    def test_rejects_reported_source_error(self):
        with self.assertRaises(ValueError):
            audit.verify({"version": 1, "projects": [{"path": "app"}], "problems": ["feed unavailable"]})

    def test_rejects_incomplete_or_unknown_reports(self):
        for report in ({}, {"version": 2, "projects": [{"path": "app"}]},
                       {"version": 1, "projects": []}, {"version": 1, "projects": [{}]}):
            with self.subTest(report=report), self.assertRaises(ValueError):
                audit.verify(report)
