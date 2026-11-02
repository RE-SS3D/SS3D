"""In-place Linux client updater. Standard library only; no Unity or system installation."""

import argparse
import datetime
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import stat
import sys
import subprocess
import tempfile
import time
import urllib.request
import uuid
import zipfile


API = "https://api.github.com/repos/henkhooft/SS3D/releases"
TAG = re.compile(r"v([0-9]+)\.([0-9]+)\.([0-9]+)\Z")
# update_ss3d.py replaces itself so updater fixes reach existing installs; Python has already
# read it. update_ss3d.sh is only a launcher and stays as is.
MANAGED = ("Game", "README.txt", "TESTERS.md", "update_ss3d.py", ".ss3d-release.json")
# Launch scripts from releases before the main menu replaced them; moved into the backup, not reinstalled.
RETIRED = ("start_ss3d_host.sh", "start_ss3d_client.sh")


def version(tag):
    match = TAG.fullmatch(tag)
    return tuple(map(int, match.groups())) if match else None


def request(url):
    return urllib.request.Request(url, headers={
        "User-Agent": "SS3D-Updater", "Accept": "application/vnd.github+json"})


def latest_release():
    releases = []
    page = 1
    while True:
        with urllib.request.urlopen(request(f"{API}?per_page=100&page={page}"), timeout=30) as response:
            batch = json.load(response)
        releases.extend(r for r in batch if not r["draft"] and not r["prerelease"]
                        and version(r["tag_name"]) is not None)
        if len(batch) < 100:
            break
        page += 1
    if not releases:
        raise RuntimeError("No stable numbered release is available. Try again after a release is promoted.")
    return max(releases, key=lambda r: version(r["tag_name"]))


def assert_closed():
    # Linux comm names fit these names in full. Check twice; never signal a process.
    for comm in Path("/proc").glob("[0-9]*/comm"):
        try:
            name = comm.read_text().strip()
        except (FileNotFoundError, ProcessLookupError, PermissionError):
            continue
        if name in ("SS3D-Client", "SS3D.x86_64"):
            raise RuntimeError("Close all SS3D Linux client windows before updating. No process was stopped.")


def wait_for_exit(pid, timeout=60.0, poll=0.25):
    """The in-game Update button quits the game, then this waits for it before the closed check."""
    deadline = time.monotonic() + timeout
    while Path(f"/proc/{pid}").exists():
        if time.monotonic() >= deadline:
            raise RuntimeError("The game did not close within a minute. Close it and run the updater again.")
        time.sleep(poll)


def restart(root):
    """Relaunch the client with no arguments, which opens the main menu. Never waits for it."""
    game = root / "Game"
    for name in ("SS3D-Client", "SS3D.x86_64"):
        if (game / name).is_file():
            subprocess.Popen([str(game / name)], cwd=game, start_new_session=True,
                             stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            return
    print("Could not find the game to restart it.", file=sys.stderr)


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def download(url, target):
    with urllib.request.urlopen(request(url), timeout=600) as response, target.open("wb") as stream:
        shutil.copyfileobj(response, stream)


def expand_release(archive_path, destination, tag):
    prefix = f"SS3D-Linux-Client-{tag}/"
    seen = set()
    with zipfile.ZipFile(archive_path) as archive:
        # Validate every entry BEFORE extracting anything, including links and special files.
        for entry in archive.infolist():
            name = entry.filename
            parts = name.rstrip("/").split("/")
            mode = entry.external_attr >> 16
            if (not name.startswith(prefix) or "\\" in name or ":" in name
                    or any(p in ("", ".", "..") for p in parts)
                    or any(ord(c) < 32 for c in name)
                    or stat.S_IFMT(mode) not in (0, stat.S_IFREG, stat.S_IFDIR)
                    or name.rstrip("/") in seen):
                raise RuntimeError(f"Unsafe or unexpected archive path: {name}")
            seen.add(name.rstrip("/"))
        for entry in archive.infolist():
            target = destination / entry.filename
            if entry.is_dir():
                target.mkdir(parents=True, exist_ok=True)
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                with archive.open(entry) as source, target.open("wb") as out:
                    shutil.copyfileobj(source, out)
                # Keep executable native helpers; never restore setuid/setgid/sticky bits.
                target.chmod(((entry.external_attr >> 16) & 0o777) or 0o644)
    root = destination / prefix.rstrip("/")
    required = ("Game/SS3D-Client", "Game/UnityPlayer.so", "Game/SS3D-Client_Data/globalgamemanagers",
                "Game/Config/permissions.txt", "Game/Config/network.json",
                "Game/Data/StationTemplates/ministation.json", "README.txt", "TESTERS.md", "update_ss3d.py")
    for name in required:
        if not (root / name).is_file():
            raise RuntimeError(f"Incomplete Linux client release: missing {name}")
    client = root / "Game/SS3D-Client"
    client.chmod(client.stat().st_mode | stat.S_IXUSR)
    return root


def metadata(root):
    path = root / ".ss3d-release.json"
    if not path.exists():
        return {"tag": "unknown (older download)", "defaults": {}}
    # utf-8-sig also accepts metadata produced by Windows PowerShell.
    value = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(value.get("tag"), str) or not isinstance(value.get("defaults"), dict):
        raise RuntimeError("Invalid .ss3d-release.json; refusing to update.")
    return value


def write_metadata(root, tag):
    defaults = {}
    for folder in ("StationTemplates", "Tilemaps"):
        for path in (root / "Game/Data" / folder).rglob("*"):
            if path.is_file():
                defaults[path.relative_to(root).as_posix()] = sha256(path)
    (root / ".ss3d-release.json").write_text(
        json.dumps({"tag": tag, "defaults": defaults}), encoding="utf-8")


def copy_local_data(old, new):
    defaults = metadata(old)["defaults"]
    for name in ("Game/Config", "Game/Data", "Game/Logs"):
        source = old / name
        if source.is_symlink():
            raise RuntimeError(f"Remove links from {name} before updating.")
        if not source.exists():
            continue
        for directory, dirs, files in os.walk(source, followlinks=False):
            for child in dirs + files:
                path = Path(directory) / child
                if path.is_symlink() or (not path.is_dir() and not path.is_file()):
                    raise RuntimeError(f"Remove links/special files from {name} before updating.")
            for child in files:
                path = Path(directory) / child
                relative = path.relative_to(old).as_posix()
                if relative in defaults and sha256(path).lower() == defaults[relative].lower():
                    continue  # Refresh unmodified bundled maps; keep edited/custom maps.
                target = new / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(path, target)


def install_release(root, new, backup):
    backup.mkdir(parents=True)
    saved, installed = [], []
    try:
        for name in RETIRED:
            old = root / name
            if old.exists() or old.is_symlink():
                old.rename(backup / name)
                saved.append(name)
        for name in MANAGED:
            old = root / name
            if old.exists():
                old.rename(backup / name)
                saved.append(name)
            (new / name).rename(old)
            installed.append(name)
    except BaseException:
        # Catch Ctrl+C too. If restoration fails, leave staging and backup for recovery.
        for name in reversed(installed):
            (root / name).rename(new / name)
        for name in reversed(saved):
            (backup / name).rename(root / name)
        raise


def prune_backups(backups, keep):
    """Keep only the backup just made (the previous version); older ones add up to GBs."""
    removed = 0
    for path in backups.iterdir():
        if path != keep and path.is_dir() and not path.is_symlink():
            shutil.rmtree(path, ignore_errors=True)
            removed += not path.exists()
    return removed


def update(root, check_only=False):
    root = root.resolve()
    if not any((root / "Game" / name).is_file() for name in ("SS3D-Client", "SS3D.x86_64")):
        raise RuntimeError("Keep the updater beside the Game folder in an extracted Linux client release.")
    if (root / "Game").is_symlink():
        raise RuntimeError("The Game directory must not be a symbolic link.")
    assert_closed()
    stage = None
    swap_started = complete = False
    with (root / ".ss3d-update.lock").open("a") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError as error:
            raise RuntimeError("Another updater is already using this installation.") from error
        try:
            release = latest_release()
            tag = release["tag_name"]
            local = metadata(root)["tag"]
            print(f"Installed: {local}   Latest stable release: {tag}")
            if version(local) is not None and version(local) >= version(tag):
                print("Already up to date (or newer).")
                return
            if check_only:
                return
            name = f"SS3D-Linux-Client-{tag}.zip"
            assets = [a for a in release["assets"] if a["name"] == name]
            if len(assets) != 1:
                raise RuntimeError(f"Release {tag} does not have exactly one Linux client download. Try again later.")
            asset = assets[0]
            digest = asset.get("digest", "") or ""
            if not re.fullmatch(r"sha256:[a-fA-F0-9]{64}", digest):
                raise RuntimeError("The release has no valid SHA-256 digest. Refusing to update.")
            url = f"https://github.com/henkhooft/SS3D/releases/download/{tag}/{name}"
            if asset["browser_download_url"] != url:
                raise RuntimeError("Unexpected release download URL.")
            stage = Path(tempfile.mkdtemp(prefix=".ss3d-update-", dir=root))
            archive = stage / "release.zip"
            print(f"Downloading {tag}... Current installation stays in place until verification finishes.")
            download(url, archive)
            if sha256(archive) != digest[7:].lower():
                raise RuntimeError("Download SHA-256 mismatch. Your installation has not been changed.")
            new = expand_release(archive, stage / "extract", tag)
            write_metadata(new, tag)  # Capture shipped hashes before copying local edits.
            copy_local_data(root, new)
            assert_closed()
            backup = root / ".ss3d-backups" / (
                datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + "-" + uuid.uuid4().hex[:8])
            swap_started = True
            install_release(root, new, backup)
            complete = True
            removed = prune_backups(backup.parent, backup)
            print(f"Updated to {tag}. Start Game/SS3D-Client as usual.")
            print(f"Previous installation: {backup}")
            if removed:
                print(f"Removed {removed} older backup(s); only the previous installation is kept.")
            print("To roll back, close SS3D and run Game/SS3D-Client inside that backup folder.")
        finally:
            if stage is not None and (not swap_started or complete):
                shutil.rmtree(stage, ignore_errors=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check-only", action="store_true", help="Check the stable release without installing")
    parser.add_argument("--wait-pid", type=int, default=0, help="Wait for this game process to exit first")
    parser.add_argument("--restart", action="store_true",
                        help="Relaunch the game afterwards, also when the update fails (the old version still runs)")
    args = parser.parse_args()
    root = Path(__file__).parent
    result = 0
    try:
        if args.wait_pid:
            wait_for_exit(args.wait_pid)
        update(root, args.check_only)
    except (Exception, KeyboardInterrupt) as error:
        print(f"Update failed: {error}", file=sys.stderr)
        result = 1
    if args.restart:
        restart(root.resolve())
    return result


if __name__ == "__main__":
    sys.exit(main())
