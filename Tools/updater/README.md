# SS3D launcher / updater

## Update an extracted Windows release

Release zips ship `Builds/Update_SS3D.bat` and `Builds/Update_SS3D.ps1` beside the launch
scripts. Double-click `Update_SS3D.bat` with the game closed, or pass `-CheckOnly` to check
without installing. This updates the existing folder, preserving `Game/Config`, custom and
modified maps, exports, server state and logs. Player settings outside the install are untouched.
Unmodified bundled maps refresh using hashes recorded in `.ss3d-release.json`; older downloads
without metadata conservatively keep all existing maps, including collisions with bundled maps.

Only **published, non-prerelease `vMAJOR.MINOR.PATCH` releases** are selected, by highest version.
Drafts, prereleases and nightlies never reach this channel. CI publishes numbered releases as
stable (0.x included), so the updater offers them as soon as the release job finishes; nightlies
and `develop-<suffix>` cuts stay prereleases. A newer installed version is never automatically downgraded.

The script locks out concurrent updates, refuses while any SS3D process is running, verifies
GitHub's SHA-256 asset digest, validates archive paths and required player files, and stages
on the same filesystem before swapping files. A caught swap failure restores the original.
If restoration itself fails or power is lost, staging and backup directories remain for recovery.
There is no dependency on Git, Unity, a hosted server, or an administrator account.

Each successful update keeps the previous complete installation in `.ss3d-backups/<id>/`.
To roll back, close the game and run `Game/SS3D.exe` in that backup; it contains the
local data from before the update. Later changes remain in the newer installation. Only that
previous installation (n-1) is kept: older backups are deleted after a successful update.
`Update_SS3D.ps1` is replaced along with the game (PowerShell has already read it), so updater
fixes reach existing installs. `Update_SS3D.bat` is never replaced: cmd reads a running batch
file line by line.

Regression coverage: `pwsh -NoProfile -File Tools/updater/test-update.ps1` (or `powershell`
on Windows). All fixtures and simulated downloads are temporary; no real game is launched.
`.github/workflows/updater-tests.yml` runs it on Windows PowerShell 5.1 for relevant PRs;
the release workflow also gates publication on these tests.

Real-Windows end to end: `.github/workflows/updater-e2e.yml` runs `e2e-windows.ps1` on GitHub's
`windows-latest` (Windows PowerShell 5.1). It updates the previous stable release to the latest
with that install's own `Update_SS3D.bat`, checks data preservation, byte-identical game files,
n-1 backups, self-update and a no-op repeat, then launches the updated game and the backup
headless (`e2e-host.txt`). PRs test the branch's updater (`-UseRepoUpdater`); runs after manual
Develop Release dispatches test the updater players actually have.

## Update an extracted Linux client release

Linux client zips ship `update_ss3d.sh` and `update_ss3d.py` beside the `Game/` folder.
Close all Linux clients, then run `./update_ss3d.sh`; use `--check-only` to check without
installing. Requires Python 3 and its standard library only. No sudo or system changes.
The updater shares the Windows contract above: stable numbered releases only, verified
downloads, preserved local data, refreshed unchanged bundled maps and runnable backups.
It also restores executable permissions on the player and native helpers.
`update_ss3d.py` updates itself like the PowerShell script; the `update_ss3d.sh` launcher stays.

Use `python3 -B Tools/updater/test_update_linux.py` for isolated regression tests. Fixtures
and mocked downloads live in temporary directories; no real game runs. Both the PR updater
workflow and the release publication gate run this suite on Linux. Release packaging is also
required to ship the helper and map-hash manifest only in the client zip, not the server zip.

For manual release acceptance, update a disposable older Linux client extraction with a custom
map, export and config change; confirm those survive, the new client launches, a repeat update
does nothing, and the backup's `Game/SS3D-Client` starts the old build. Native game launch remains
a manual check. Numbered releases always build Linux (the release workflow forces
`build_linux`), because the updater refuses a stable release without its matching Linux
client zip rather than silently falling back to a different version.

## Separate bootstrap launchers

Bootstrap launchers that download and run the newest **stable `v*`** release. They do not
require a git checkout and do not update an install in place: every release is extracted
under a versioned directory, and the current plus one previous version are kept.

Only tags matching `vMAJOR.MINOR.PATCH` are considered. `develop-*`, `develop-nightly` and
other development cuts are ignored. Downloads are verified against GitHub's SHA-256 asset
digest; if the digest is missing or does not match, the launcher refuses to install.

The launcher itself is a small, stable bootstrap. It is not replaced by game updates.

## Linux

Requirements: `curl`, `jq`, `unzip`, `sha256sum`.

```bash
./ss3d-launcher.sh --server <host> --port 1151 --ckey <ckey>
```

Installs under `~/.local/share/ss3d/versions/`. Useful flags:

- `--server HOST`, `--port PORT`, `--ckey CKEY` — passed to the game.
- `--install-dir DIR` — override the install root.
- `--tag TAG` — testing only; follow a specific release instead of the stable channel.
- `--no-launch` — download/install only.
- `--` — remaining arguments are passed to the game.

## Windows

Run `SS3D-Launcher.bat` (it invokes PowerShell with `-ExecutionPolicy Bypass`). Windows
PowerShell 5.1 or newer is required, and `SS3D-Launcher.ps1` must stay next to the `.bat`.

```bat
SS3D-Launcher.bat -Server <host> -Port 1151 -Ckey <ckey>
```

Installs under `%LOCALAPPDATA%\SS3D\versions\`. Parameters: `-InstallDir`, `-Tag`,
`-NoLaunch`.

## Notes

- The default server is `127.0.0.1`; pass the hosted address when one is configured.
- If GitHub is unreachable, the launcher starts the already-installed version when one
  exists; otherwise it exits with an error.
- The launchers check GitHub's releases API on each start. The API is public and
  rate-limited; one check per launch is well within the unauthenticated limit.
- `--tag` / `-Tag` is for testing a specific release. Normal use follows the stable channel.
