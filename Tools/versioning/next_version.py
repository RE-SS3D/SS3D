#!/usr/bin/env python3
"""Work out SS3D's version from git. Rules: Documents/architecture/2026-10_game-versioning.md.

  next_version.py next [--bump auto|patch|minor|major]   next release version + why
  next_version.py describe                                version of HEAD between releases

Output is key=value lines (append to $GITHUB_OUTPUT in CI). Release tags are vMAJOR.MINOR.PATCH;
upstream RE:SS3D's unprefixed tags (0.0.7.6, 0.3.95o) are ignored.
"""
import argparse
import re
import subprocess
import sys

TAG_GLOB = "v[0-9]*"
TAG_RE = re.compile(r"^v(\d+)\.(\d+)\.(\d+)$")
FIRST_RELEASE = (0, 1, 0)
LEVELS = ["patch", "minor", "major"]

# Integer constants whose increase means old saves / old clients no longer work.
COMPAT_CONSTANTS = [
    ("Assets/Scripts/SS3D/Data/Persistence/PersistenceEnvelope.cs",
     r"CurrentSchemaVersion\s*=\s*(\d+)", "save schema"),
    ("Assets/Scripts/SS3D/Networking/ProtocolVersion.cs",
     r"\bCurrent\s*=\s*(\d+)", "network protocol"),
]
MILESTONE_DIR = "Documents/milestones"
SLICE_ROW = re.compile(r"^\|\s*([A-Za-z0-9]+)\s*\|[^|]*\|\s*([^|]*?)\s*\|")
GATE_STATUS = re.compile(r"^>\s*Status:\s*(\S+)", re.MULTILINE)


def git(*args, check=True):
    result = subprocess.run(["git", *args], capture_output=True, text=True)
    if check and result.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)}: {result.stderr.strip()}")
    return result.stdout.strip() if result.returncode == 0 else None


def last_release_tag():
    tag = git("describe", "--tags", "--abbrev=0", "--match", TAG_GLOB, "HEAD", check=False)
    return tag if tag and TAG_RE.match(tag) else None


def show(rev, path):
    return git("show", f"{rev}:{path}", check=False)


def compat_signals(tag):
    for path, pattern, label in COMPAT_CONSTANTS:
        old, new = show(tag, path), show("HEAD", path)
        old_m = re.search(pattern, old or "")
        new_m = re.search(pattern, new or "")
        if old_m and new_m and int(new_m.group(1)) > int(old_m.group(1)):
            yield "minor", f"{label} version {old_m.group(1)} -> {new_m.group(1)}"


def slice_statuses(text):
    statuses = {}
    for line in (text or "").splitlines():
        m = SLICE_ROW.match(line)
        if m and m.group(1) not in ("Id",) and not set(m.group(1)) <= set("-"):
            statuses[m.group(1)] = m.group(2).lower()
    return statuses


def milestone_signals(tag):
    files = git("ls-tree", "--name-only", "HEAD", f"{MILESTONE_DIR}/", check=False) or ""
    for path in files.splitlines():
        if not path.endswith(".md"):
            continue
        old, new = show(tag, path), show("HEAD", path)
        name = path.rsplit("/", 1)[-1][:-3]
        old_gate = GATE_STATUS.search(old or "")
        new_gate = GATE_STATUS.search(new or "")
        if new_gate and new_gate.group(1).lower().startswith("shipped") and not (
                old_gate and old_gate.group(1).lower().startswith("shipped")):
            yield "minor", f"milestone {name} reached"
        old_slices = slice_statuses(old)
        for sid, status in slice_statuses(new).items():
            if status.startswith("shipped") and not old_slices.get(sid, "").startswith("shipped"):
                yield "minor", f"milestone slice {name} {sid} shipped"


def trailer_signals(tag):
    out = git("log", f"{tag}..HEAD", "--format=%h%x09%(trailers:key=Version-Bump,valueonly,separator=%x2C)")
    for line in out.splitlines():
        sha, _, values = line.partition("\t")
        for value in filter(None, (v.strip().lower() for v in values.split(","))):
            if value in LEVELS:
                yield value, f"Version-Bump: {value} trailer on {sha}"
            else:
                print(f"warning: ignoring unknown Version-Bump '{value}' on {sha}", file=sys.stderr)


def bump(version, level):
    major, minor, patch = version
    if level == "major":
        return major + 1, 0, 0
    if level == "minor":
        return major, minor + 1, 0
    return major, minor, patch + 1


def fmt(version):
    return ".".join(map(str, version))


def emit(**values):
    for key, value in values.items():
        print(f"{key}={value}")


def cmd_next(requested):
    tag = last_release_tag()
    if tag is None:
        version = FIRST_RELEASE if requested in ("auto", "patch", "minor") else (1, 0, 0)
        emit(version=fmt(version), previous="", bump="initial", reasons="first release of the numbered scheme")
        return 0

    commits = int(git("rev-list", "--count", f"{tag}..HEAD"))
    if commits == 0:
        print(f"error: HEAD is already released as {tag}; nothing to release", file=sys.stderr)
        return 2

    signals = list(compat_signals(tag)) + list(milestone_signals(tag)) + list(trailer_signals(tag))
    if requested == "auto":
        level = max((LEVELS.index(lvl) for lvl, _ in signals), default=0)
        level = LEVELS[level]
        reasons = [reason for lvl, reason in signals if lvl == level] or [f"{commits} commit(s), no compatibility or milestone signal"]
    else:
        level = requested
        reasons = [f"bump '{requested}' requested on dispatch"]

    version = bump(tuple(map(int, TAG_RE.match(tag).groups())), level)
    emit(version=fmt(version), previous=tag[1:], bump=level, reasons="; ".join(reasons))
    return 0


def cmd_describe():
    """0.1.0 on a tag, 0.1.1-dev.12+5da58b9 after it (.dirty with local changes). Mirrors GameVersion.cs."""
    short = git("rev-parse", "--short=7", "HEAD")
    dirty = bool(git("status", "--porcelain", "--untracked-files=no"))
    tag = last_release_tag()
    if tag is None:
        # Pre-releases of the first release.
        version = f"{fmt(FIRST_RELEASE)}-dev.{git('rev-list', '--count', 'HEAD')}+{short}" + (".dirty" if dirty else "")
        emit(version=version)
        return 0
    base, commits = tuple(map(int, TAG_RE.match(tag).groups())), int(git("rev-list", "--count", f"{tag}..HEAD"))
    if commits == 0:
        version = fmt(base) + (f"+{short}.dirty" if dirty else "")
    else:
        version = f"{fmt(bump(base, 'patch'))}-dev.{commits}+{short}" + (".dirty" if dirty else "")
    emit(version=version)
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    nxt = sub.add_parser("next")
    nxt.add_argument("--bump", choices=["auto", *LEVELS], default="auto")
    sub.add_parser("describe")
    args = parser.parse_args()
    return cmd_next(args.bump) if args.command == "next" else cmd_describe()


if __name__ == "__main__":
    sys.exit(main())
