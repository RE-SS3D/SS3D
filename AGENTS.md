# Agent guide — SS3D fork

Instructions for AI agents working in this repository.

## Navigate docs before searching code

1. Read [Documents/architecture/INDEX.md](Documents/architecture/INDEX.md) to find the relevant domain.
2. Open the linked **system map** under `Documents/architecture/systems/` for entry points and key files.
3. Only then read specific source files or run targeted search — not full-tree exploration.

## Doc layers

| Layer | Path | Agent role |
|-------|------|------------|
| Design specs | `Documents/design/` | **Read-only** — owner-maintained gameplay rules |
| Implementation plans | `Documents/plans/` | Update todos when work ships |
| Architecture efforts | `Documents/architecture/YYYY-MM_*.md` | Set `Status` when effort ships |
| System maps | `Documents/architecture/systems/` | Update after feature work |
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
