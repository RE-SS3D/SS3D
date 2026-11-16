> Implements: infrastructure — the machines and accounts the project depends on beyond GitHub itself
> Touches systems: networking-session (CI, releases, test servers), rounds-lobby (sandbox and Nuke Ops servers)
> Status: shipped

# Project infrastructure (Nov 2026)

What the project runs outside this repository's code: one line per role, who runs it, and what
stops working without it. Docs elsewhere name the role ("the self-hosted runner"), never a
machine, so this page is the one place to update when hosting changes hands. How each role is
set up lives in the linked doc.

## Roles

| Role | What it does | Run by | Without it |
|---|---|---|---|
| **Self-hosted runner** (labels `self-hosted, linux, x64, unity`; Intel N100, Docker, user systemd) | Nightly EditMode and EditMode on PRs from branches of this repository (never fork PRs), `develop-release.yml` builds with a warm `Library`, the multiplayer smoke set, the weekly graphical pilots. Setup: [2026-07_multiplayer-testing-self-hosted-ci](2026-07_multiplayer-testing-self-hosted-ci.md) § Ops | Maintainer (BroodjeKipcorn) | EditMode and releases still run GitHub-hosted with `runner: github` (cold `Library`, slower). Smoke (`multiplayer-smoke-test.yml`) and `weekly-graphical.yml` do not run at all: both need the runner's host editor and real input. |
| **Nightly release dispatcher** | Dispatches `develop-release.yml` with `tag_suffix=nightly` at 05:17 UTC, skipping a day that already has one ([2026-07_ci-develop-release-pipeline](2026-07_ci-develop-release-pipeline.md) § Triggers). Runs on the runner host with a GitHub token that can dispatch workflows on this repository | Maintainer | No `develop-nightly` prerelease; run `gh workflow run develop-release.yml --ref develop -f tag_suffix=nightly` by hand. |
| **Project test servers** | The sandbox server ([2026-11_sandbox-server](2026-11_sandbox-server.md)) and the 24/7 Nuke Ops server ([test-server](../milestones/test-server.md) T7, T8): Docker containers next to the runner, deployed from release builds outside this repository | Maintainer | No public place to play between playtests; the test-server milestone gates T7 and T8 regress. |
| **Unity license secrets** (`UNITY_LICENSE`, `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `unity_tests` environment) | Activate Unity in every CI job that opens the editor | Owner recorded in the repository's secret descriptions | Every Unity job fails at activation; the preflight step says which secret is missing. |
| **GitHub Releases on this repository** | Where players' updaters and the in-game release banner look (`SS3D.Utils.ProjectRepository`, [Tools/updater/README.md](../../Tools/updater/README.md)) | Maintainers, via `develop-release.yml` | Players stay on their installed build; the updater reports no newer release. |
| **ss3d.space** | Project website; its download link points at this repository's releases page | Central team | Players find releases through GitHub instead. |
| **Discord** | Community, playtest announcements ([milestones](../milestones/INDEX.md)) | Central team | Playtests have no announcement channel. |

## When hosting changes hands

1. Register a new runner with the same labels and work through the § Ops setup in
   [2026-07_multiplayer-testing-self-hosted-ci](2026-07_multiplayer-testing-self-hosted-ci.md).
2. Move the nightly dispatcher and its token, and redeploy the test servers from the latest release.
3. Update the **Run by** column here. No other doc names the host.
