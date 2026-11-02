> Implements: Documents/architecture/systems/networking-session-testing.md (CI), Documents/archive/architecture/2026-07_headless-dedicated-server.md
> Touches systems: networking-session
> Status: shipped

# CI develop-release pipeline (Jul 2026)

## Goal

Give this fork a path from a known commit to a downloadable **Windows** player zip
(self-host via `Builds/*.bat`), with optional secondary Linux client/server artifacts.
Default **manual** dispatch is a numbered **stable** `vX.Y.Z` release with Windows + Linux
client/server (Linux is forced on, since both client updaters follow stable releases).
`tag_suffix` test cuts stay Windows-only prereleases unless `build_linux` is set. EditMode and
multiplayer smoke are **opt-in** — day-to-day cuts should not pay for
gates/`StandaloneLinux64` that `editmodetestrunner.yml` / `multiplayer-smoke-test.yml`
already cover separately. A **nightly schedule** (05:17 UTC) always builds Windows + Linux
client + Linux dedicated server and publishes a floating `develop-nightly` prerelease.
Not triggered on every develop merge (license cost + release spam at high PR volume).

## Shipped

### Workflow: `.github/workflows/develop-release.yml`

**Triggers:** `workflow_dispatch` + nightly `cron: "17 5 * * *"` (after smoke at 04:00 so
TomNAS `Library` is warm). `concurrency: develop-release` / `cancel-in-progress: false`.

**Nightly (schedule or manual `tag_suffix=nightly`):** forces `build_linux=true`, `skip_release=false`, EditMode/smoke off,
`tag_suffix=nightly` → tag `develop-nightly` (overwritten each night). The tag is explicitly
moved to the built commit before publication; a missing tag/release is recreated.

The schedule runs away from the busy top of the hour to reduce delayed/dropped GitHub triggers.
A cron edit on the default branch refreshes the scheduled actor registration. To recover a missing
nightly immediately, run `gh workflow run develop-release.yml --ref develop -f tag_suffix=nightly`.
This uses the same full-platform flags and nightly title as a scheduled run. It remains a
prerelease and is excluded from stable client updates. Check the run and all three release assets
afterward; a manual recovery confirms publication, not future delivery of GitHub schedule events.

**Inputs (manual; defaults favour a fast Windows cut):**

| Input | Default | Effect |
|-------|---------|--------|
| `build_linux` | `false` | When true, also build Linux dedicated server + client and attach Linux zips. |
| `run_editmode` | `false` | When true, EditMode job runs first and must pass before player builds. |
| `run_smoke` | `false` | When true, after Linux builds run smoke scenarios (needs `build_linux`). |
| `skip_release` | `false` | When true, skip Windows build and GitHub prerelease (Linux-only if `build_linux`). |
| `version_bump` | `auto` | Release bump level; `auto` derives it from signals — see [2026-10_game-versioning](2026-10_game-versioning.md). |
| `tag_suffix` | _(empty)_ | Unversioned test cut tagged `develop-<suffix>`; `nightly` forces the full nightly path. |
| `runner` | `auto` | `auto` / `tomnas` / `github` — TomNAS when online, else `ubuntu-latest`. |

**Jobs:**

1. **Prepare** — resolve schedule vs dispatch flags; compute the game version with
   `Tools/versioning/next_version.py` (full-history checkout; [2026-10_game-versioning](2026-10_game-versioning.md));
   probe whether `TomNAS-unity` is online and set `runs_on` / `self_hosted` for build jobs.
   Both builder steps pass `customParameters: -ss3dVersion <version>`.
2. **EditMode** (opt-in) — same `game-ci/unity-test-runner@v4` / `6000.3.16f1` pattern as
   `editmodetestrunner.yml`, including secret preflight and `environment: unity_tests`.
   Skipped when `run_editmode` is false (always skipped on nightly).
3. **Linux build** (nightly, or opt-in via `build_linux`) — **one** `unity-builder` step with
   `ClientAndServerBuildScript.BuildBothForCi` (avoids TomNAS PackageCache race from two
   sequential builder sessions). `versioning: None`. Unity 6 Linux binaries are
   `SS3D-Server` / `SS3D-Client` (no `.x86_64` suffix). On TomNAS: restore/save shared
   `/home/cu6e/.cache/ss3d/Library` stash + `runAsHostUser: true`.
4. **Smoke** (opt-in step on the Linux job) — harness scenarios when `run_smoke`; logs uploaded.
5. **Windows client** — default manual path and always on nightly; `StandaloneWindows64`
   (`buildName: SS3D`). Same TomNAS Library stash / `runAsHostUser` when self-hosted. Not
   smoke-tested on Windows. Skipped when `skip_release` is set.
6. **Prerelease** — primary zip `SS3D-Windows-<tag>.zip` with layout:
   `Start_SS3D_Host.bat`, `Start_SS3D_Client.bat` (prompts for name + IP), `Update_SS3D.bat`,
   `Update_SS3D.ps1`, `.ss3d-release.json`, `TESTERS.md` and `README.txt`
   beside `Game/SS3D.exe`, plus CWD fixtures
   `Game/Config/` (`permissions.txt`, `network.json`), `Game/Data/Tilemaps/`, and `Game/Data/StationTemplates/`
   copied from git-tracked [`Builds/Game/`](../../Builds/Game/) (Unity does not pack these;
   `Paths.cs` reads them from process CWD). Never ships `Data/ServerMeta/` (runtime) or
   `StationTemplates/Metastation v2.json` (broken; kept in git only for the multiplayer loadtest).
   Linux zips use the same shape when Linux was built: `SS3D-Linux-Client-<tag>/` holds
   `start_ss3d_client.sh`, `start_ss3d_host.sh`, `update_ss3d.sh`, `update_ss3d.py`,
   `.ss3d-release.json`, `TESTERS.md`, `README.txt` and `Game/` (binary `SS3D-Client`);
   `SS3D-Linux-Server-<tag>/` holds `start_ss3d_server.sh` and `GameServer/` (`SS3D-Server`).
   `*_BurstDebugInformation_DoNotShip` is stripped from every zip. Tag `vX.Y.Z` (manual release, at the built commit), `develop-nightly`
   (schedule) or `develop-<tag_suffix>`. Numbered `vX.Y.Z` releases (0.x included) are stable
and become GitHub's Latest; nightlies and `develop-*` cuts are prereleases.

### Windows release updates

`Builds/Update_SS3D.bat` invokes the adjacent PowerShell 5.1 script. It selects the highest
published **stable** `vMAJOR.MINOR.PATCH` release, ignoring drafts, prereleases and nightlies.
Numbered releases publish as stable, so they reach the update channel immediately. To hold one
back, mark it as a prerelease in GitHub; clearing the flag later promotes that exact artifact.
`-CheckOnly` checks without downloading; a newer installed version is never downgraded.

Updates require all SS3D processes closed. The script locks the installation against concurrent
updates, downloads and checks GitHub's SHA-256 digest, checks archive paths and required files,
and stages beside the installation before swapping `Game/`, launch scripts, guides and metadata.
`Game/Config`, local data and logs carry forward. `.ss3d-release.json` records tag and shipped
map hashes so unchanged bundled maps refresh while custom/edited maps survive, including name
collisions. Older downloads without metadata conservatively preserve every local map.
Player settings in Unity's persistent-data directory are untouched. `Update_SS3D.ps1` is
swapped along with the game, so updater fixes reach existing installs; `Update_SS3D.bat` is
never replaced, because cmd reads a running batch file line by line.

Each update retains a complete previous installation under `.ss3d-backups/<id>/`; launch its
Host/Client script to roll back with its pre-update local data. Later edits stay in the newer
installation. Only that previous installation is kept: after a successful update, older
backups are deleted (n-1 retention; each is a full player). A caught swap failure restores the
original; an interrupted swap or failed restore leaves staging/backup files for manual recovery.

`Tools/updater/test-update.ps1` uses temporary fixtures and mocked downloads, with no Unity or
network dependency. `.github/workflows/updater-tests.yml` checks relevant PRs on Windows
PowerShell 5.1; `develop-release.yml` also requires that test job before publishing. Native
Windows download/update and backup launch are still required in first-release dogfood.

### Linux client release updates

`Builds/update_ss3d.sh` invokes the adjacent Python 3 standard-library helper,
`update_ss3d.py`. It follows the same stable-only selection, map-hash metadata, local-data
preservation, `.ss3d-backups/` n-1 rollback and self-update contract as Windows
(`update_ss3d.py` is replaced, the `update_ss3d.sh` launcher stays). Linux uses `flock` for the installation lock and reads `/proc/*/comm`
before downloading and before swapping to refuse running `SS3D-Client` / `SS3D.x86_64`
processes; it never signals or launches them. It rejects archive symlinks/special files and
path traversal, refuses links in local data, preserves native helper execute bits and ensures
the player and launch scripts are executable. Caught swap failures (including Ctrl+C) restore
the old installation; interrupted/failed restoration retains staging and backup directories.

Only Linux **client** zips include the updater and metadata. A stable release without its
matching Linux client asset is refused safely, which is why numbered releases always build Linux. The dedicated
server remains manually deployed, as before.

`python3 -B Tools/updater/test_update_linux.py` covers stable selection/promotion and pagination,
download/hash/asset refusal, process and concurrent-update refusal, data preservation, native
permissions, rollback and unsafe archives using temporary fixtures. Both updater CI workflows
gate publication on this suite. A packaged update followed by real client/backup launch remains
a manual release check.

### Related workflow changes

- `multiplayer-smoke-test.yml` — label-gated PR + `workflow_dispatch` + nightly 04:00 UTC;
  TomNAS + `BuildBothForCi`. Prefer this (or `build_linux` + `run_smoke` on develop-release)
  when you need a smoke gate.
- `editmodetestrunner.yml` — cheap PR/`develop` EditMode; prefers TomNAS when online,
  else `ubuntu-latest`. Prefer this (or `run_editmode: true` on develop-release) when you
  need an EditMode gate on a cut.
- `main.yml` (Automated Build) — **removed**; Windows prereleases go through
  `develop-release.yml`. Upstream milestone/project YAML deleted (`23b75ad36`); ghost
  registrations for `AddToRoadmap.yml` and old `testrunner.yml` **disabled** in the repo
  Actions settings (`gh workflow disable`) so they no longer fail on pushes.

## Pitfalls recorded for operators

- **Player zips must seed `Config/` + `Data/Tilemaps/` next to the binary.** Symptom on
  `develop-f1c9476` Windows: starts, empty `Game/Data/`, no `Config/`, host log
  `No station templates found to load` — Addressables were fine. Cause: release staging
  copied only the Unity player + bats; `Paths.GetPath(GamePaths.Config|Data)` is CWD-relative
  (`Start_SS3D_*.bat` / smoke `cd` beside the exe). Fix: `seed_cwd_data` in
  `develop-release.yml` from tracked `Builds/Game/Config` + `Builds/Game/Data/Tilemaps`.
  Do not ship `Data/ServerMeta/` (gitignored runtime envelope preferred over `permissions.txt`).
- **Host.bat must give the local client a loopback address.** `ResetOnBuiltApplication` clears
  `ServerAddress`; Host without `-ip=` used to DNS-resolve empty → `fe80::…` and drop the
  session after WorldReady (Wine reproduces hard). See networking-session pitfalls; Host.bat
  now passes `-ip=127.0.0.1` and code defaults empty Host address to loopback.
- **`upload-artifact` drops the exec bit.** v0.1.0's Linux zips shipped `SS3D-Client` /
  `SS3D-Server` as `-rw-r--r--` under `build-linux/Game/StandaloneLinux64/`. Staging now
  `chmod +x`es the binary and scripts before `zip` (which keeps Unix modes); the scripts also
  `chmod +x` the binary themselves.
- **The lobby change log needs git inside the game-ci container.** v0.1.0 (GitHub-hosted) shipped
  without `BuildInfo.json`: the container runs git as root against a runner-owned checkout
  ("dubious ownership") and the build jobs cloned at depth 1. `BuildInfoGenerator` now passes
  `-c safe.directory=*`, the build checkouts use `fetch-depth: 0`, and a step after each build
  warns when the file is missing or empty.
- **License secrets live on the `unity_tests` Environment.** Jobs without
  `environment: unity_tests` see empty `UNITY_LICENSE` / `UNITY_SERIAL` even when EditMode is
  green (the old `main.yml` hit this before Environment was wired).
- **Keep game-ci `versioning: None`.** The version comes from `-ss3dVersion` + `BuildInfoGenerator`
  ([2026-10_game-versioning](2026-10_game-versioning.md)); game-ci `Semantic` also trips on
  upstream's letter-suffix tags (`Failed to parse git describe output`).
- **Do not use `ClientBuildScript` for Windows** — it hardcodes `StandaloneLinux64`; Windows
  uses the default game-ci build method.
- **Default manual prereleases are unproven by EditMode/smoke/Linux.** Trust PR/`develop`
  EditMode CI and opt into `build_linux` + `run_smoke` (or `multiplayer-smoke-test.yml`)
  when you need a gated cut. Nightly includes Linux binaries but still skips EditMode/smoke.
- **Windows is not multiplayer-smoke-tested.** Even with `run_smoke`, trust is “same commit as
  green Linux smoke.”
- **Unity 6 Linux binary names have no `.x86_64` suffix.** Smoke/`chmod` must use `SS3D-Server`
  / `SS3D-Client` (local Editor menus still write `SS3D.x86_64` via hardcoded paths).
- **Skipped optional jobs cascade unless dependents use `always()`.** EditMode and Linux are
  often skipped on manual dispatch; every downstream job (`build-windows`, `release`) must use
  `always()` and then check `needs.*.result == 'success' || 'skipped'` as appropriate.
- **Ghost workflows after rename/delete stay `active` until disabled.** Deleting YAML is not
  enough — `gh workflow disable <id|path>` or the Actions UI. `testrunner.yml` and
  `AddToRoadmap.yml` were disabled after their files were removed.
- **Offline TomNAS queues forever without a probe.** `prepare` picks GitHub-hosted when
  `TomNAS-unity` is not `online` (job `timeout-minutes` does not cover queue wait).

## Out of scope

- Auto-trigger on every `develop` push (nightly schedule only).
- Windows dedicated-server build / Windows smoke harness.
- Wiring `known_unity_noise` into the harness fail gate.

## Related docs

- [Multiplayer testing](systems/networking-session-testing.md)
- [2026-07_headless-dedicated-server.md](../archive/architecture/2026-07_headless-dedicated-server.md)
- [2026-07_multiplayer-testing-self-hosted-ci.md](2026-07_multiplayer-testing-self-hosted-ci.md)
- [networking-session.md](systems/networking-session.md)
