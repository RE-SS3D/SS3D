#!/usr/bin/env python3
"""Fail on references to the old henkhooft/SS3D fork that should point at this repository.

Development moved from henkhooft/SS3D into RE-SS3D/SS3D in November 2026. Links to the fork's
releases, issues page, discussions or clone URL send players and contributors to the wrong place.
Allowed anywhere: historical references to one fork PR or issue (`henkhooft/SS3D#123`,
`.../henkhooft/SS3D/pull/123`, `.../issues/123`). Allowed files are listed below.

    python3 Tools/ci/check_fork_links.py
"""
import importlib.util
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

FORK = re.compile(r"henkhooft/ss3d", re.IGNORECASE)
HISTORICAL = re.compile(r"henkhooft/ss3d(?:#\d+|/(?:pull|issues)/\d+)", re.IGNORECASE)

ALLOWED_PREFIXES = ("Documents/archive/",)
ALLOWED_FILES = {
    ".mailmap",                                       # author identities
    "CLAUDE.md",                                      # explains how to read pre-merge #N references
    "Assets/Scripts/Tests/EditMode/ServerInfoTests.cs",  # git-remote parsing inputs
    "Tools/ci/check_fork_links.py",
}


def project_repo_copies():
    """The build-facing repository copies are governed by Tools/updater/set_project_repo.py."""
    spec = importlib.util.spec_from_file_location(
        "set_project_repo", ROOT / "Tools/updater/set_project_repo.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return {name for name, _ in module.COPIES}


def main():
    allowed = ALLOWED_FILES | project_repo_copies()
    files = subprocess.run(["git", "ls-files", "-z"], cwd=ROOT, capture_output=True, check=True).stdout
    problems = []
    for name in files.decode("utf-8").split("\0"):
        if not name or name in allowed or name.startswith(ALLOWED_PREFIXES):
            continue
        path = ROOT / name
        if not path.is_file():
            continue
        data = path.read_bytes()
        if b"\0" in data[:8000] or not FORK.search(data.decode("utf-8", errors="ignore")):
            continue
        for number, line in enumerate(data.decode("utf-8", errors="ignore").splitlines(), 1):
            if FORK.search(HISTORICAL.sub("", line)):
                problems.append(f"{name}:{number}: {line.strip()[:160]}")
    if problems:
        print("Links to the old henkhooft/SS3D fork (point them at RE-SS3D/SS3D, or write a historical")
        print("reference as henkhooft/SS3D#N):")
        print("\n".join(problems))
        return 1
    print("No stray links to the old fork.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
