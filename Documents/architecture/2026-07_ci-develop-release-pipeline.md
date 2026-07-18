> Implements: Documents/architecture/2026-07_multiplayer-test-harness.md (CI), Documents/architecture/2026-07_headless-dedicated-server.md
> Touches systems: networking-session
> Status: shipped

# CI develop-release pipeline (Jul 2026)

## Goal

Give this fork a path from a known commit to a downloadable **Windows** player zip
(self-host via `Builds/*.bat`), with optional secondary Linux client/server artifacts.
Default dispatch is **Windows → prerelease** (~one player build). Linux (~30–45 min),
EditMode, and multiplayer smoke are **opt-in** — day-to-day cuts should not pay for
gates/`StandaloneLinux64` that `editmodetestrunner.yml` / `multiplayer-smoke-test.yml`
already cover separately. Trigger is manual (`workflow_dispatch`) only — develop merges
~5–10 PRs/day, and every run burns rate-limited `unity_tests` Unity licenses.

## Shipped

### Workflow: `.github/workflows/develop-release.yml`

**Inputs (defaults favour a fast Windows cut):**

| Input | Default | Effect |
|-------|---------|--------|
| `build_linux` | `false` | When true, also build Linux dedicated server + client and attach Linux zips. |
| `run_editmode` | `false` | When true, EditMode job runs first and must pass before player builds. |
| `run_smoke` | `false` | When true, after Linux builds run `basic-round` + `late-join 2` (needs `build_linux`). |
| `skip_release` | `false` | When true, skip Windows build and GitHub prerelease (Linux-only if `build_linux`). |
| `tag_suffix` | _(empty)_ | Override tag suffix; full tag is `develop-<suffix>` (else short SHA). |

**Jobs:**

1. **EditMode** (opt-in) — same `game-ci/unity-test-runner@v4` / `6000.3.16f1` pattern as
   `editmodetestrunner.yml`, including secret preflight and `environment: unity_tests`.
   Skipped when `run_editmode` is false.
2. **Linux build** (opt-in via `build_linux`) — sequential dedicated server + client via
   `ServerBuildScript` / `ClientBuildScript`, separate `buildsPath` roots
   (`build/GameServer`, `build/Game`) so the second builder cannot clobber the first.
   `versioning: None` (fork tags like `0.3.95j` break game-ci Semantic versioning). Unity 6
   Linux binaries are `SS3D-Server` / `SS3D-Client` (no `.x86_64` suffix).
3. **Smoke** (opt-in step on the Linux job) — `Testing/multiplayer/run_smoketest.sh
   basic-round` then `late-join 2`; harness logs uploaded when smoke was requested.
4. **Windows client** — default path; `unity-builder` `StandaloneWindows64` (`buildName: SS3D`).
   Not smoke-tested on Windows. Skipped when `skip_release` is set. Does **not** require a
   Linux build (Linux may be skipped).
5. **Prerelease** — primary zip `SS3D-Windows-<tag>.zip` with layout:
   `Start_SS3D_*.bat` + `README.txt` beside `Game/SS3D.exe` (matches [`Builds/`](../../Builds/)
   locally). Linux client/server zips only when `build_linux` was enabled. Tag
   `develop-<shortsha>` (or `tag_suffix` override). `prerelease: true`.

### Related workflow changes

- `multiplayer-smoke-test.yml` — removed `push: develop`; keep label-gated PR +
  `workflow_dispatch`; same separate build dirs / `versioning: None`. Prefer this (or
  `build_linux` + `run_smoke` on develop-release) when you need a smoke gate.
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
- **Default prereleases are unproven by EditMode/smoke/Linux.** Trust PR/`develop` EditMode CI
  and opt into `build_linux` + `run_smoke` (or `multiplayer-smoke-test.yml`) when you need a
  gated cut.
- **Windows is not multiplayer-smoke-tested.** Even with `run_smoke`, trust is “same commit as
  green Linux smoke.”
- **Unity 6 Linux binary names have no `.x86_64` suffix.** Smoke/`chmod` must use `SS3D-Server`
  / `SS3D-Client` (local Editor menus still write `SS3D.x86_64` via hardcoded paths).
- **Skipped optional jobs cascade unless dependents use `always()`.** EditMode and Linux are
  often skipped; every downstream job (`build-windows`, `release`) must use `always()` and
  then check `needs.*.result == 'success' || 'skipped'` as appropriate, or Windows/prerelease
  never run after a default dispatch.

## Out of scope

- Auto-trigger on every `develop` push.
- Windows dedicated-server build / Windows smoke harness.
- Wiring `known_unity_noise` into the harness fail gate.

## Related docs

- [2026-07_multiplayer-test-harness.md](2026-07_multiplayer-test-harness.md)
- [2026-07_headless-dedicated-server.md](2026-07_headless-dedicated-server.md)
- [networking-session.md](systems/networking-session.md)
