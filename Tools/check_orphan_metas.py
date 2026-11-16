#!/usr/bin/env python3
"""Fail when a tracked .meta file under Assets/ has no tracked asset or non-empty folder beside it.

Git doesn't track empty folders, so a folder .meta whose folder holds no tracked files is
orphaned in every clean checkout. Unity deletes it on import and the deletion turns up as
noise in an unrelated PR. A folder that holds only other orphaned folder .meta files counts
as empty too, so the whole chain is reported at once.

Usage:
    python3 Tools/check_orphan_metas.py           # list orphans, exit 1 if any
    python3 Tools/check_orphan_metas.py --delete  # git rm them
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent


def tracked_assets() -> list[str]:
    out = subprocess.run(
        ["git", "ls-files", "-z", "--", "Assets"],
        cwd=REPO_ROOT,
        check=True,
        capture_output=True,
    ).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


def find_orphans(files: list[str]) -> list[str]:
    tracked = set(files)
    folders_with_content: set[str] = set()
    for path in files:
        if path.endswith(".meta"):
            continue
        parts = path.split("/")
        for i in range(1, len(parts)):
            folders_with_content.add("/".join(parts[:i]))

    orphans = []
    for path in files:
        if not path.endswith(".meta"):
            continue
        asset = path[: -len(".meta")]
        if asset in tracked or asset in folders_with_content:
            continue
        orphans.append(path)
    return sorted(orphans)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--delete", action="store_true", help="git rm every orphaned .meta file")
    args = parser.parse_args()

    orphans = find_orphans(tracked_assets())
    if not orphans:
        print("No orphaned .meta files.")
        return 0

    if args.delete:
        subprocess.run(["git", "rm", "-q", "--", *orphans], cwd=REPO_ROOT, check=True)
        print(f"Removed {len(orphans)} orphaned .meta file(s).")
        return 0

    print(f"{len(orphans)} orphaned .meta file(s) (no tracked asset or files in the folder):")
    for path in orphans:
        print(f"  {path}")
    print("Fix with: python3 Tools/check_orphan_metas.py --delete")
    return 1


if __name__ == "__main__":
    sys.exit(main())
