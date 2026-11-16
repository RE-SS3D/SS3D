#!/usr/bin/env python3
"""Point every build-facing reference at one GitHub repository.

Players' builds reach the project's GitHub repository in two ways: the game itself (release check
banner, bug report link) and the updater scripts shipped next to it. Each names the repository
once; this script rewrites all of them together, and test_project_repo.py fails when they disagree.

    python3 Tools/updater/set_project_repo.py              # print the current value
    python3 Tools/updater/set_project_repo.py owner/name   # switch every copy

Use the exact casing GitHub returns in release download URLs: the updaters compare
browser_download_url case-sensitively and refuse to install on a mismatch.
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

# (file, pattern) -- each pattern matches exactly once and captures the slug in group 1.
COPIES = [
    ("Assets/Scripts/SS3D/Utils/ProjectRepository.cs", r'public const string Slug = "([^"]+)";'),
    ("Builds/Update_SS3D.ps1", r"^\$Repo = '([^']+)'$"),
    ("Builds/update_ss3d.py", r'^REPO = "([^"]+)"$'),
    ("Tools/updater/SS3D-Launcher.ps1", r'^\$Repo = "([^"]+)"$'),
    ("Tools/updater/ss3d-launcher.sh", r'^REPO="([^"]+)"$'),
    ("Tools/updater/e2e-windows.ps1", r"^\$repo = '([^']+)'$"),
]

SLUG = re.compile(r"[A-Za-z0-9-]+/[A-Za-z0-9._-]+")


def read_all():
    """Return {file: slug}; raises when a pattern does not match exactly once."""
    found = {}
    for name, pattern in COPIES:
        text = (ROOT / name).read_bytes().decode("utf-8")
        matches = list(re.finditer(pattern, text, re.MULTILINE))
        if len(matches) != 1:
            raise RuntimeError(f"{name}: expected one repository line, found {len(matches)}")
        found[name] = matches[0].group(1)
    return found


def write_all(slug):
    for name, pattern in COPIES:
        path = ROOT / name
        text = path.read_bytes().decode("utf-8")
        match = next(re.finditer(pattern, text, re.MULTILINE))
        start, end = match.span(1)
        path.write_bytes((text[:start] + slug + text[end:]).encode("utf-8"))


def main(argv):
    if len(argv) == 1:
        for name, slug in read_all().items():
            print(f"{slug}  {name}")
        return 0
    if len(argv) != 2 or not SLUG.fullmatch(argv[1]):
        print(__doc__.strip(), file=sys.stderr)
        return 2
    read_all()  # fail before writing anything if a file has drifted
    write_all(argv[1])
    print(f"Every copy now names {argv[1]}.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
