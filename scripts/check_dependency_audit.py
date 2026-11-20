"""Fail closed on NuGet audit findings, problems, or invalid reports."""
import json
import sys


def verify(report):
    if not isinstance(report, dict) or report.get("version") != 1:
        raise ValueError("Unsupported NuGet audit report")
    projects = report.get("projects")
    if not isinstance(projects, list) or not projects:
        raise ValueError("Audit must include projects")
    if any(not isinstance(p, dict) or not p.get("path") for p in projects):
        raise ValueError("Audit project is missing its path")

    def walk(value):
        if isinstance(value, dict):
            for key, item in value.items():
                if key in ("vulnerabilities", "problems", "errors", "error") and item:
                    raise ValueError("NuGet reported " + key)
                walk(item)
        elif isinstance(value, list):
            for item in value:
                walk(item)

    walk(report)


if __name__ == "__main__":
    try:
        with open(sys.argv[1], encoding="utf-8") as source:
            verify(json.load(source))
    except (OSError, ValueError, IndexError) as error:
        sys.exit(str(error))
    print("NuGet audit is clean")
