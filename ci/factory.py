"""Deterministic local quality gates for FunHabitTracker."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time
from typing import Any, Iterable


ROOT = Path(__file__).resolve().parents[1]
SOLUTION = ROOT / "FunHabitTracker.sln"
POLICY_PATH = ROOT / "quality-policy.yaml"
ACCEPTANCE_ROOT = ROOT / "specs"
IGNORED_PARTS = {
    ".git",
    ".idea",
    ".ai",
    ".aspnet-data-protection-keys",
    "artifacts",
    "bin",
    "node_modules",
    "obj",
    "reports",
    "TestResults",
}
TEXT_SUFFIXES = {
    ".cs",
    ".cshtml",
    ".css",
    ".html",
    ".json",
    ".js",
    ".md",
    ".ps1",
    ".py",
    ".sln",
    ".toml",
    ".xml",
    ".yaml",
    ".yml",
}
SECRET_PATTERNS = {
    "GitHub token": re.compile(r"\b(?:ghp|github_pat)_[A-Za-z0-9_]{20,}\b"),
    "AWS access key": re.compile(r"\bAKIA[0-9A-Z]{16}\b"),
    "private key": re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----"),
    "OpenAI API key": re.compile(r"\bsk-[A-Za-z0-9_-]{20,}\b"),
}
REQUIREMENT_ID = re.compile(r"^[A-Z][A-Z0-9]+-[0-9]{3}$")


class GateFailure(RuntimeError):
    pass


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise GateFailure(f"Cannot read {relative(path)}: {error}") from error


def command_path(name: str) -> str:
    resolved = shutil.which(name)
    if resolved:
        return resolved
    if os.name == "nt":
        resolved = shutil.which(f"{name}.cmd")
        if resolved:
            return resolved
    raise GateFailure(f"Required command is not installed: {name}")


def run(command: list[str], *, capture: bool = False) -> str:
    actual = [command_path(command[0]), *command[1:]]
    print(f"RUN {' '.join(command)}")
    result = subprocess.run(
        actual,
        cwd=ROOT,
        text=True,
        stdout=subprocess.PIPE if capture else None,
        stderr=subprocess.STDOUT if capture else None,
        env={**os.environ, "OPENSPEC_TELEMETRY": "0"},
    )
    output = result.stdout or ""
    if capture and output:
        print(output.rstrip())
    if result.returncode != 0:
        raise GateFailure(f"Command failed with exit code {result.returncode}: {' '.join(command)}")
    return output


def pass_gate(name: str, detail: str) -> None:
    print(f"PASS {name}: {detail}")


def repository_files() -> Iterable[Path]:
    for path in ROOT.rglob("*"):
        if not path.is_file() or any(part in IGNORED_PARTS for part in path.parts):
            continue
        yield path


def acceptance_files() -> list[Path]:
    return sorted(ACCEPTANCE_ROOT.glob("*/acceptance.yaml"))


def requirements() -> list[dict[str, Any]]:
    result: list[dict[str, Any]] = []
    for path in acceptance_files():
        data = load_json(path)
        if not isinstance(data, dict) or not isinstance(data.get("requirements"), list):
            raise GateFailure(f"{relative(path)} must contain a requirements array")
        for requirement in data["requirements"]:
            if not isinstance(requirement, dict):
                raise GateFailure(f"{relative(path)} contains a non-object requirement")
            result.append(requirement)
    return result


def verify_requirement_shape(requirement: dict[str, Any]) -> None:
    required = {"id", "statement", "risk", "status", "acceptance", "verification"}
    missing = sorted(required - requirement.keys())
    if missing:
        raise GateFailure(f"Requirement is missing fields {missing}: {requirement}")
    requirement_id = str(requirement["id"])
    if not REQUIREMENT_ID.fullmatch(requirement_id):
        raise GateFailure(f"Invalid requirement ID: {requirement_id}")
    if requirement["risk"] not in {"low", "medium", "high", "critical"}:
        raise GateFailure(f"Invalid risk for {requirement_id}: {requirement['risk']}")
    if requirement["status"] not in {"resolved", "unresolved"}:
        raise GateFailure(f"Invalid status for {requirement_id}: {requirement['status']}")
    if not isinstance(requirement["acceptance"], list) or not requirement["acceptance"]:
        raise GateFailure(f"{requirement_id} needs at least one acceptance criterion")
    if not isinstance(requirement["verification"], dict):
        raise GateFailure(f"{requirement_id} needs a verification map")


def verify_spec() -> None:
    for path in (POLICY_PATH, ROOT / "openspec/config.yaml", ROOT / "package.json"):
        if not path.is_file():
            raise GateFailure(f"Required file is missing: {relative(path)}")
    load_json(POLICY_PATH)
    found = requirements()
    if not found:
        raise GateFailure("At least one specs/*/acceptance.yaml package is required")
    for requirement in found:
        verify_requirement_shape(requirement)
    run(["npx", "openspec", "validate", "--all", "--strict", "--no-interactive"])
    pass_gate("verify-spec", f"OpenSpec and {len(found)} traceable requirements validated")


def verify_build() -> None:
    if not SOLUTION.is_file():
        raise GateFailure("FunHabitTracker.sln is missing")
    run(["dotnet", "restore", str(SOLUTION)])
    run(["dotnet", "build", str(SOLUTION), "--configuration", "Release", "--no-restore", "/warnaserror"])
    pass_gate("verify-build", "restore, compiler type checks, and Release build passed")


def verify_format() -> None:
    run([
        "dotnet",
        "format",
        str(SOLUTION),
        "--no-restore",
        "--verify-no-changes",
        "--severity",
        "warn",
    ])
    pass_gate("verify-format", "dotnet format reported no required changes")


def verify_tests() -> None:
    run(["dotnet", "test", str(SOLUTION), "--configuration", "Release", "--no-restore"])
    run([sys.executable, "-m", "unittest", "discover", "-s", "tests/factory", "-p", "test_*.py"])
    pass_gate("verify-tests", "MSTest and factory validator tests passed")


def contains_vulnerability(value: Any) -> bool:
    if isinstance(value, dict):
        for key, child in value.items():
            if key.lower() == "vulnerabilities" and isinstance(child, list) and child:
                return True
            if contains_vulnerability(child):
                return True
    elif isinstance(value, list):
        return any(contains_vulnerability(item) for item in value)
    return False


def dependency_scan() -> None:
    output = run([
        "dotnet",
        "list",
        str(SOLUTION),
        "package",
        "--vulnerable",
        "--include-transitive",
        "--format",
        "json",
    ], capture=True)
    try:
        data = json.loads(output)
    except json.JSONDecodeError as error:
        raise GateFailure(f"Dependency scan did not return JSON: {error}") from error
    if contains_vulnerability(data):
        raise GateFailure("The dependency scan found vulnerable NuGet packages")


def secret_findings() -> list[dict[str, str]]:
    findings: list[dict[str, str]] = []
    for path in repository_files():
        if path.suffix.lower() not in TEXT_SUFFIXES and path.name not in {"AGENTS.md", ".gitignore"}:
            continue
        try:
            content = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        for name, pattern in SECRET_PATTERNS.items():
            if pattern.search(content):
                findings.append({"rule": name, "path": relative(path)})
    return findings


def write_sarif(findings: list[dict[str, str]]) -> None:
    output = ROOT / "reports/security.sarif"
    output.parent.mkdir(exist_ok=True)
    results = [
        {
            "ruleId": finding["rule"],
            "level": "error",
            "message": {"text": f"Potential secret detected: {finding['rule']}"},
            "locations": [{"physicalLocation": {"artifactLocation": {"uri": finding["path"]}}}],
        }
        for finding in findings
    ]
    sarif = {
        "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
        "version": "2.1.0",
        "runs": [{"tool": {"driver": {"name": "funhabittracker-factory-security"}}, "results": results}],
    }
    output.write_text(json.dumps(sarif, indent=2) + "\n", encoding="utf-8")


def verify_security() -> None:
    dependency_scan()
    findings = secret_findings()
    write_sarif(findings)
    agents = (ROOT / "AGENTS.md").read_text(encoding="utf-8")
    policy = load_json(POLICY_PATH)
    if "A7" not in agents or policy.get("production", {}).get("direct_agent_changes") is not False:
        raise GateFailure("Agent policy must forbid A7 and direct production changes")
    if findings:
        raise GateFailure(f"Potential secrets found: {findings}")
    pass_gate("verify-security", "NuGet vulnerability scan, secret scan, and autonomy policy passed")


def referenced_paths(requirement: dict[str, Any]) -> Iterable[str]:
    verification = requirement.get("verification", {})
    for value in verification.values():
        if isinstance(value, list):
            yield from (str(item) for item in value)
    for key in ("documentation", "code_refs"):
        value = requirement.get(key, [])
        if isinstance(value, list):
            yield from (str(item) for item in value)


def verify_traceability() -> None:
    found = requirements()
    for requirement in found:
        verify_requirement_shape(requirement)
        requirement_id = str(requirement["id"])
        references = list(referenced_paths(requirement))
        if not references:
            raise GateFailure(f"{requirement_id} has no evidence references")
        for reference in references:
            if not (ROOT / reference).is_file():
                raise GateFailure(f"{requirement_id} references missing file: {reference}")
    pass_gate("verify-traceability", f"{len(found)} requirement evidence chains resolved")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def inventory() -> list[dict[str, str]]:
    return sorted(
        ({"path": relative(path), "sha256": sha256(path)} for path in repository_files()),
        key=lambda item: item["path"],
    )


def verify_release() -> None:
    files = inventory()
    output = ROOT / "artifacts"
    output.mkdir(exist_ok=True)
    timestamp = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
    sbom = {
        "bomFormat": "CycloneDX",
        "specVersion": "1.5",
        "version": 1,
        "metadata": {"timestamp": timestamp, "component": {"type": "application", "name": "FunHabitTracker"}},
        "components": [
            {"type": "file", "name": item["path"], "hashes": [{"alg": "SHA-256", "content": item["sha256"]}]}
            for item in files
        ],
    }
    (output / "sbom.cdx.json").write_text(json.dumps(sbom, indent=2) + "\n", encoding="utf-8")
    provenance = {
        "_type": "https://in-toto.io/Statement/v1",
        "subject": [{"name": item["path"], "digest": {"sha256": item["sha256"]}} for item in files],
        "predicateType": "https://slsa.dev/provenance/v1",
        "predicate": {"buildDefinition": {"buildType": "funhabittracker-local-factory"}},
    }
    (output / "provenance.intoto.jsonl").write_text(json.dumps(provenance, sort_keys=True) + "\n", encoding="utf-8")
    evidence = {
        "generated_at": timestamp,
        "policy": "quality-policy.yaml",
        "requirements": [{"id": req["id"], "risk": req["risk"], "status": req["status"]} for req in requirements()],
        "gates": ["verify-spec", "verify-build", "verify-format", "verify-tests", "verify-security", "verify-traceability"],
        "file_count": len(files),
    }
    (output / "release-evidence.json").write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    required = load_json(POLICY_PATH)["evidence"]["required_files"]
    missing = [path for path in required if not (ROOT / path).is_file()]
    if missing:
        raise GateFailure(f"Required release evidence was not generated: {missing}")
    pass_gate("verify-release", "SBOM, provenance, SARIF, and release evidence generated")


def run_all() -> None:
    verify_spec()
    verify_build()
    verify_format()
    verify_tests()
    verify_security()
    verify_traceability()
    verify_release()


COMMANDS = {
    "all": run_all,
    "verify-build": verify_build,
    "verify-format": verify_format,
    "verify-release": verify_release,
    "verify-security": verify_security,
    "verify-spec": verify_spec,
    "verify-tests": verify_tests,
    "verify-traceability": verify_traceability,
}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=sorted(COMMANDS))
    args = parser.parse_args(argv)
    try:
        COMMANDS[args.command]()
    except GateFailure as error:
        print(f"ERROR {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
