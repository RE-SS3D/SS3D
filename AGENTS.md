# Agent guide — SS3D fork

Instructions for AI agents working in this repository.

## Navigate docs before searching code

1. Read [Documents/architecture/INDEX.md](Documents/architecture/INDEX.md) to find the relevant domain.
2. Open the linked **system map** under `Documents/architecture/systems/` for entry points and key files.
3. Only then read specific source files or run targeted search — not full-tree exploration.

## Finding art assets

When a feature needs models, textures, sounds, or UI art:

1. Read [Documents/art-asset-index.md](Documents/art-asset-index.md).
2. Search [Documents/art-available-for-import.json](Documents/art-available-for-import.json) for assets not yet in-game.
3. Use [Documents/art-asset-index.json](Documents/art-asset-index.json) to check whether art is already imported.

Most game-ready source files live in [RE-SS3D/SS3D-Art](https://github.com/RE-SS3D/SS3D-Art). The index maps each source file to its expected `Assets/Art/` import path. Regenerate with `python3 Tools/generate_art_index.py` after importing new art.

## Finding UI icons

When building UI that needs icon sprites (buttons, HUD, panels, machine interfaces):

1. Read [Documents/icon-index.md](Documents/icon-index.md).
2. Search [Documents/icon-index.json](Documents/icon-index.json) by name, tag, or pack.
3. Icons live in `Assets/Art/Icons/external icons/` — game-icons.net SVGs grouped by contributor.

Regenerate with `python3 Tools/generate_icon_index.py` after adding icons.

## Doc layers

| Layer | Path | Agent role |
|-------|------|------------|
| Design specs | `Documents/design/` | **Read-only** — owner-maintained gameplay rules |
| Implementation plans | `Documents/plans/` | Update todos when work ships |
| Architecture efforts | `Documents/architecture/YYYY-MM_*.md` | Set `Status` when effort ships |
| System maps | `Documents/architecture/systems/` | Update after feature work |
| Art asset index | `Documents/art-asset-index.md` | **Read** when importing or locating art |
| UI icon index | `Documents/icon-index.md` | **Read** when building UI that needs game-icons |
| Fork status | `Documents/FORK_STATUS.md` | **Read-only** unless owner asks |

**Do not edit `Documents/design/` or `Documents/FORK_STATUS.md`.** If code diverges from a design spec, document the divergence in the system map, plan, or architecture effort doc — do not change the design file.

## When code search is still appropriate

- The system map is `stub` and lacks the detail you need.
- The task spans cross-cutting concerns not covered by any map.
- You are verifying a specific symbol or checking whether a map is still accurate.

## After implementing a feature

Run the **`update-system-docs`** skill (`.cursor/skills/update-system-docs/SKILL.md`) to sync:

- Affected system maps and INDEX status
- Linked plans in `Documents/plans/`
- Architecture effort doc status, if applicable

## Authoring conventions

See [Documents/SKILL.md](Documents/SKILL.md) for header blocks, linking rules, and system map template.

## Project context

- Unity 6 / URP multiplayer game using FishNet.
- Gameplay code under `Assets/Scripts/SS3D/`.
- Subsystem pattern: `SubSystem` / `NetworkSubSystem` via `SubSystems.Get<T>()`.
- Upstream GitBook may help with generic Unity/FishNet concepts; fork direction lives in `Documents/`.
