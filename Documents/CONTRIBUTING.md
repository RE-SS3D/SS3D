# How To Contribute To This Fork

This document covers contributing to **[henkhooft/SS3D](https://github.com/henkhooft/SS3D)**. For upstream RE:SS3D
community and official releases, see [RE-SS3D/SS3D](https://github.com/RE-SS3D/SS3D).

Read [FORK_STATUS.md](FORK_STATUS.md) first if you are new here — it explains how this repo diverges from upstream.

## Documentation

This fork maintains its own docs in `Documents/`, independent of upstream's GitBook.

- [SKILL.md](SKILL.md) — conventions for all documentation layers (read before writing)
- [architecture/INDEX.md](architecture/INDEX.md) — navigation hub for finding code by system
- [architecture/systems/](architecture/systems/) — per-domain system maps (entry points, key files)
- [design/](design/) — gameplay design specs (what and why; owner-maintained)
- [architecture/](architecture/) — dated implementation effort docs (how and in what order)
- [plans/](plans/) — temporary implementation plans (updated when work ships)
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

Open the project in Unity Hub. The game is written in C# and uses [FishNet](https://fish-networking.com/) for
networking.

Upstream's [dev guide](https://ss3d.gitbook.io/dev-guide/) may still help with generic Unity and FishNet concepts,
but project direction, milestones, and gameplay specs for this fork live in `Documents/` — not GitBook.

## Running The Game

This fork does not publish releases. Build from source in Unity.

Running the game is still more involved than it should be. Until fork-specific run instructions land here, upstream's
[running the project](https://ss3d.gitbook.io/dev-guide/guides/running-the-project) guide covers the basic host/join
flow — steps may drift as this fork changes.

## Questions

Open a [discussion](https://github.com/henkhooft/SS3D/discussions) or [issue](https://github.com/henkhooft/SS3D/issues)
on this repository.
