# How To Contribute To This Fork

This document covers contributing to **[henkhooft/SS3D](https://github.com/henkhooft/SS3D)**. For upstream RE:SS3D
community and official releases, see [RE-SS3D/SS3D](https://github.com/RE-SS3D/SS3D).

Read [FORK_STATUS.md](FORK_STATUS.md) first if you are new here — it explains how this repo diverges from upstream.

## Documentation

This fork maintains its own docs in `Documents/`, independent of upstream's GitBook.

- [SKILL.md](SKILL.md) — conventions for all documentation layers (read before writing)
- [FORK_STATUS.md](FORK_STATUS.md) — upstream divergence log
- [milestones/](milestones/) — playable gates and current focus
- [architecture/INDEX.md](architecture/INDEX.md) — navigation hub for finding code by system
- [architecture/systems/](architecture/systems/) — per-domain system maps (entry points, key files)
- [design/](design/) — gameplay design specs (what and why; owner-maintained)
- [architecture/](architecture/) — dated implementation effort docs (how and in what order)
- [plans/](plans/) — temporary implementation plans (updated when work ships)
- [archive/](archive/) — shipped efforts and completed plans, for the *why* behind a design choice
- [ROADMAP.md](../ROADMAP.md) — player-facing progress, generated from `milestones/` by
  `python3 Tools/generate_roadmap.py` (never edit by hand; CI checks it)
- [AGENTS.md](../AGENTS.md) — instructions for AI agents (docs-first navigation)

If a design doc exists for the system you are changing, align with it or state the deviation in your PR.
After shipping, run the `update-system-docs` skill to sync system maps and plans.

## Setup

Clone the repo and check out **`develop`**:

```bash
git clone https://github.com/henkhooft/SS3D.git
cd SS3D
git checkout develop
```

Open the project in Unity Hub with editor **`6000.3.28f1`**. The game is written in C# and uses
[FishNet](https://fish-networking.com/) for networking.

Upstream's [dev guide](https://ss3d.gitbook.io/dev-guide/) may still help with generic Unity and FishNet concepts,
but project direction, milestones, and gameplay specs for this fork live in `Documents/` — not GitBook.

### Editor toolbar

The fork adds **Scene Switcher** (left), and **Launcher** toggle and **Network Settings** (right) to
Unity's main toolbar. Unity hides custom toolbar elements until you enable them: if they're missing,
right-click the toolbar and enable them under **Tools**.

## Testing and conventions

- **Tests**: EditMode and PlayMode suites live under `Assets/Scripts/Tests/`; run them from
  **Window > General > Test Runner**. CI runs EditMode on every PR.
- **Validation**: run targeted checks, `Testing/ui/validate.sh --steps compile,editmode` plus focused
  steps for what you touched ([Testing/ui/README.md](../Testing/ui/README.md),
  [testing philosophy](architecture/2026-11_testing-philosophy.md)).
- **Code style**: `.editorconfig` (`_camelCase` private fields, `PascalCase` events, block-scoped
  namespaces). Match the surrounding code.
- **Versioning**: bump the save or protocol version when a change breaks old saves or clients; see
  [game versioning](architecture/2026-10_game-versioning.md).

## Running The Game

To just play, download the latest [release](https://github.com/henkhooft/SS3D/releases/latest), unzip it and
run `Game\SS3D.exe` (Linux: `Game/SS3D-Client`). The main menu hosts a local game or joins a server. CI
publishes numbered releases as stable, and the in-game **Update and restart** or the bundled updater
scripts keep an install current ([updater README](../Tools/updater/README.md)). The
[testers' guide](../Builds/TESTERS.md) covers map building and bug reports.

For development, run the game from the Unity Editor. Maintainers publish a numbered release by running
**Develop Release** manually; see [architecture/2026-07_ci-develop-release-pipeline.md](architecture/2026-07_ci-develop-release-pipeline.md).

## Questions

Open a [discussion](https://github.com/henkhooft/SS3D/discussions) or [issue](https://github.com/henkhooft/SS3D/issues)
on this repository.
