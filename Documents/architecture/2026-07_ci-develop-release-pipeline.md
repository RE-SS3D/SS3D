> Implements: Documents/architecture/2026-07_multiplayer-test-harness.md (CI), Documents/architecture/2026-07_headless-dedicated-server.md
> Touches systems: networking-session
> Status: shipped

# CI develop-release pipeline (Jul 2026)

## Goal

Give this fork a path from a known commit to a downloadable **Windows** player zip
(self-host via `Builds/*.bat`) plus secondary Linux client/server artifacts. Default
dispatch is **build-only** (Linux → Windows → prerelease). EditMode and multiplayer
smoke are **opt-in** inputs — day-to-day cuts already pay ~30–45 min of Unity builder
time; re-running gates that `editmodetestrunner.yml` / `multiplayer-smoke-test.yml`
cover separately is optional. Trigger is manual (`workflow_dispatch`) only — develop
merges ~5–10 PRs/day, and every run burns rate-limited `unity_tests` Unity licenses.

## Shipped

### Workflow: `.github/workflows/develop-release.yml`

**Inputs (defaults favour a fast cut):**

| Input | Default | Effect |
|-------|---------|--------|
| `run_editmode` | `false` | When true, EditMode job runs first and must pass before Linux builds. |
| `run_smoke` | `false` | When true, after Linux builds run `basic-round` + `late-join 2`. |
| `skip_release` | `false` | When true, skip Windows build and GitHub prerelease (Linux only). |
| `tag_suffix` | _(empty)_ | Override tag suffix; full tag is `develop-<suffix>` (else short SHA). |

**Jobs:**

1. **EditMode** (opt-in) — same `game-ci/unity-test-runner@v4` / `6000.3.16f1` pattern as
   `editmodetestrunner.yml`, including secret preflight and `environment: unity_tests`.
   Skipped when `run_editmode` is false; Linux builds still proceed.
2. **Linux build** — sequential dedicated server + client via `ServerBuildScript` /
   `ClientBuildScript`, separate `buildsPath` roots (`build/GameServer`, `build/Game`) so the
   second builder cannot clobber the first. `versioning: None` (fork tags like `0.3.95j` break
   game-ci Semantic versioning). Unity 6 Linux binaries are `SS3D-Server` / `SS3D-Client`
   (no `.x86_64` suffix).
3. **Smoke** (opt-in step on the Linux job) — `Testing/multiplayer/run_smoketest.sh
   basic-round` then `late-join 2`; harness logs uploaded when smoke was requested.
4. **Windows client** — after Linux succeeds, default `unity-builder` `StandaloneWindows64`
   (`buildName: SS3D`). Not smoke-tested on Windows. Skipped when `skip_release` is set.
5. **Prerelease** — primary zip `SS3D-Windows-<tag>.zip` with layout:
   `Start_SS3D_*.bat` + `README.txt` beside `Game/SS3D.exe` (matches [`Builds/`](../../Builds/)
   locally). Secondary: Linux client/server zips. Tag `develop-<shortsha>` (or
   `tag_suffix` override). `prerelease: true`. Release notes do not claim EditMode/smoke
   unless those inputs were enabled for that run.

### Related workflow changes

- `multiplayer-smoke-test.yml` — removed `push: develop`; keep label-gated PR +
  `workflow_dispatch`; same separate build dirs / `versioning: None`. Prefer this (or
  `run_smoke: true` on develop-release) when you need a smoke gate.
- `editmodetestrunner.yml` — unchanged cheap PR/`develop` feedback. Prefer this (or
  `run_editmode: true` on develop-release) when you need an EditMode gate on a cut.
- `main.yml` (Automated Build) — **removed**; Windows prereleases go through
  `develop-release.yml`. Upstream milestone/project workflows stay deleted (see
  commit `23b75ad36`).

## Pitfalls recorded for operators

- **License secrets live on the `unity_tests` Environment.** Jobs without
  `environment: unity_tests` see empty `UNITY_LICENSE` / `UNITY_SERIAL` even when EditMode is
  green (the old `main.yml` hit this before Environment was wired).
- **Do not use `versioning: Semantic`** until tags are clean `X.Y.Z` — letter-suffix tags produce
  `Failed to parse git describe output`.
- **Do not use `ClientBuildScript` for Windows** — it hardcodes `StandaloneLinux64`; Windows
  uses the default game-ci build method.
- **Default prereleases are unproven by EditMode/smoke.** Trust PR/`develop` EditMode CI and
  opt into `run_smoke` (or run `multiplayer-smoke-test.yml`) when you need a gated cut.
- **Windows is not multiplayer-smoke-tested.** Even with `run_smoke`, trust is “same commit as
  green Linux smoke.”
- **Unity 6 Linux binary names have no `.x86_64` suffix.** Smoke/`chmod` must use `SS3D-Server`
  / `SS3D-Client` (local Editor menus still write `SS3D.x86_64` via hardcoded paths).

## Out of scope

- Auto-trigger on every `develop` push.
- Windows dedicated-server build / Windows smoke harness.
- Wiring `known_unity_noise` into the harness fail gate.

## Related docs

- [2026-07_multiplayer-test-harness.md](2026-07_multiplayer-test-harness.md)
- [2026-07_headless-dedicated-server.md](2026-07_headless-dedicated-server.md)
- [networking-session.md](systems/networking-session.md)
