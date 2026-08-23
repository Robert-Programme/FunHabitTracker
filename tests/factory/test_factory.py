import json
from pathlib import Path
import unittest

from ci import factory


ROOT = Path(__file__).resolve().parents[2]


class FactoryPolicyTests(unittest.TestCase):
    def test_policy_contains_enforceable_project_gates(self) -> None:
        policy = json.loads((ROOT / "quality-policy.yaml").read_text(encoding="utf-8"))
        mandatory = set(policy["pull_request"]["mandatory"])
        self.assertTrue(
            {
                "openspec_validation",
                "spec_traceability",
                "build",
                "format",
                "unit_tests",
                "dependency_scan",
                "secret_scan",
            }.issubset(mandatory)
        )
        self.assertFalse(policy["production"]["direct_agent_changes"])
        self.assertEqual("A7", policy["production"]["forbidden_autonomy_level"])

    def test_acceptance_requirements_have_stable_ids(self) -> None:
        found = factory.requirements()
        self.assertGreaterEqual(len(found), 4)
        for requirement in found:
            factory.verify_requirement_shape(requirement)

    def test_vulnerability_detection_walks_nested_results(self) -> None:
        clean = {"projects": [{"frameworks": [{"topLevelPackages": []}]}]}
        vulnerable = {
            "projects": [
                {"frameworks": [{"transitivePackages": [{"vulnerabilities": [{"severity": "High"}]}]}]}
            ]
        }
        self.assertFalse(factory.contains_vulnerability(clean))
        self.assertTrue(factory.contains_vulnerability(vulnerable))

    def test_workflow_uses_the_local_factory_entry_point(self) -> None:
        workflow = (ROOT / ".github/workflows/factory.yml").read_text(encoding="utf-8")
        self.assertIn("npm run openspec:validate", workflow)
        self.assertIn("npm run factory:all", workflow)
        self.assertIn("actions/setup-dotnet", workflow)


if __name__ == "__main__":
    unittest.main()
