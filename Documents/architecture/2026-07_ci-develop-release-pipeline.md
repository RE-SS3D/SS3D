> Implements: Documents/architecture/2026-07_multiplayer-test-harness.md (CI), Documents/architecture/2026-07_headless-dedicated-server.md
> Touches systems: networking-session
> Status: shipped

# CI develop-release pipeline (Jul 2026)

## Goal

Give this fork a gated path from a known commit to downloadable Linux client + dedicated-server
binaries: EditMode → full player builds → multiplayer smoke → GitHub **prerelease**. Trigger is
manual (`workflow_dispatch`) only — develop merges ~5–10 PRs/day, and every run burns
rate-limited `unity_tests` Unity licenses plus long builder time.

## Shipped

### Workflow: `.github/workflows/develop-release.yml`

1. **EditMode** — same `game-ci/unity-test-runner@v4` / `6000.3.16f1` pattern as
   `editmodetestrunner.yml`, including secret preflight and `environment: unity_tests`.
2. **Build** — sequential Linux dedicated server + client via `ServerBuildScript` /
   `ClientBuildScript`, separate `buildsPath` roots (`build/GameServer`, `build/Game`) so the
   second builder cannot clobber the first. `versioning: None` (fork tags like `0.3.95j` break
   game-ci Semantic versioning).
3. **Smoke** — `Testing/multiplayer/run_smoketest.sh basic-round` then `late-join 2` against
   those same binaries; harness logs uploaded always.
4. **Prerelease** — zip the smoke-proven trees; `softprops/action-gh-release` with
   `prerelease: true`, tag `develop-<shortsha>` (or dispatch `tag_suffix` override). Optional
   `skip_release` input runs EditMode+build+smoke only.

### Related workflow changes

- `multiplayer-smoke-test.yml` — removed `push: develop`; keep label-gated PR +
  `workflow_dispatch`; same separate build dirs / `versioning: None`.
- `main.yml` — marked deprecated for day-to-day releases; fixed `environment: unity_tests`,
  secret preflight, `versioning: None`; Windows/legacy path uploads artifacts only (no longer
  creates a non-prerelease GitHub Release). Prefer `develop-release.yml`.
- `editmodetestrunner.yml` — unchanged cheap PR/`develop` feedback; the release workflow
  re-runs EditMode so a manual cut is never based on a stale green check.

## Pitfalls recorded for operators

- **License secrets live on the `unity_tests` Environment.** Jobs without
  `environment: unity_tests` see empty `UNITY_LICENSE` / `UNITY_SERIAL` even when EditMode is
  green (`main.yml` hit this before the fix).
- **Do not use `versioning: Semantic`** until tags are clean `X.Y.Z` — letter-suffix tags produce
  `Failed to parse git describe output`.

## Out of scope

- Auto-trigger on every `develop` push.
- Windows/macOS in the gated release.
- Wiring `known_unity_noise` into the harness fail gate.

## Related docs

- [2026-07_multiplayer-test-harness.md](2026-07_multiplayer-test-harness.md)
- [2026-07_headless-dedicated-server.md](2026-07_headless-dedicated-server.md)
- [networking-session.md](systems/networking-session.md)
