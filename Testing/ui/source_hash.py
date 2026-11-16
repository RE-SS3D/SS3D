"""Print a content hash of the player-build inputs in this checkout.

Run with python3 Testing/ui/source_hash.py. The client build stamps it into
Builds/Game/ss3d-build-hash.txt; a different hash means the build is stale.

Content-based through git blob ids: staging or committing the same files keeps the
hash, so does editing docs. Inputs are Assets/, Packages/ and ProjectSettings/,
minus Editor folders (never in a player) and the Addressables output a build writes.
"""
import hashlib
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = [
    "Assets", "Packages", "ProjectSettings",
    ":(exclude,glob)**/Editor/**",
    ":(exclude,glob)Assets/AddressableAssetsData*",
    ":(exclude,glob)Assets/AddressableAssetsData/**",
    ":(exclude,glob)Assets/Content/Addressables/Linux*",
    ":(exclude,glob)Assets/Content/Addressables/Linux/**",
    # Unity's Performance Testing package writes these on every editor test run; their timings
    # change the hash without changing anything a player build uses.
    ":(exclude,glob)Assets/Resources/PerformanceTestRun*",
]


def git(*args, stdin=None):
    return subprocess.run(["git", *args], cwd=ROOT, check=True, capture_output=True, input=stdin).stdout


def paths(output):
    return {p for p in output.split(b"\0") if p}


def main():
    blobs = {}
    for entry in paths(git("ls-files", "-s", "-z", "--", *SPEC)):
        meta, path = entry.split(b"\t", 1)
        blobs[path] = meta.split()[1]
    changed = paths(git("diff", "--name-only", "-z", "--", *SPEC))
    changed |= paths(git("ls-files", "--others", "--exclude-standard", "-z", "--", *SPEC))
    present = sorted(p for p in changed if (ROOT / p.decode()).is_file())
    for path in changed.difference(present):
        blobs.pop(path, None)
    if present:
        ids = git("hash-object", "--stdin-paths", stdin=b"\n".join(present) + b"\n").split()
        blobs.update(zip(present, ids))
    digest = hashlib.sha256()
    for path in sorted(blobs):
        digest.update(path + b" " + blobs[path] + b"\n")
    print(digest.hexdigest()[:16])


if __name__ == "__main__":
    main()
