# Agent guide — SS3D fork

Instructions for AI agents working in this repository.

This file inlines the **tripwires** — the mistakes that are high-frequency, high-cost, or silent, and that you must not make even on a first read. Everything else — doc layers, header blocks, section order, authoring rules — lives in [Documents/SKILL.md](Documents/SKILL.md), which is canonical. Read it before writing anything under `Documents/`. When in doubt about *where* something goes, SKILL.md decides; this file only tells you what to never do and where to start.

## Navigate docs before searching code

1. Read [Documents/architecture/INDEX.md](Documents/architecture/INDEX.md) to find the relevant domain.
2. Open the linked **system map** under `Documents/architecture/systems/` for entry points and key files.
3. Read the map's **Pitfalls** section before any UI Toolkit, FishNet, or prefab work — it records failures that compile and run but misbehave with no error or log. These cost hours precisely because nothing throws; the map is where that knowledge is banked.
4. Only then read specific source files or run targeted search — not full-tree exploration.

**Trust the maps, but verify.** Each system map header carries a `> Verified: <commit>` stamp — the commit the map was last checked against code. If that commit is far behind current `HEAD`, treat the map as possibly stale: confirm the files under **Start here** still exist and still do what the map says before relying on them. If you find a map wrong or out of date, **fix it via `update-system-docs` — do not silently route around it.** A stale map the next agent trusts is worse than no map at all.

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

## What you may and may not touch

Canonical layer table (what each folder answers, who updates it): [Documents/SKILL.md](Documents/SKILL.md). The hard rules, inlined so you hit them before you act:

- **Never edit `Documents/design/*` or `Documents/FORK_STATUS.md`** unless the owner explicitly asks. If code diverges from a design spec, record the divergence in the system map, plan, or architecture effort doc — **do not change the design file to match code.**
- **Design docs are WHAT/WHY only** — no prototyping prompts, no build-status field. Need a Cursor or Claude Design prompt for a system? Generate it fresh from the design doc plus the current system map; don't expect one written into the design doc. Need to know what's built vs. only designed? Read the coverage table in [INDEX.md](Documents/architecture/INDEX.md), not the design doc.
- **Plans and system maps are yours to update; design specs are not.** When your feature ships, sync the maps (below) — an unsynced map is the drift this whole system exists to prevent.

## When code search is still appropriate

- The system map is `stub` and lacks the detail you need.
- The task spans cross-cutting concerns not covered by any map.
- You are verifying a specific symbol, or checking whether a map is still accurate (see "Trust the maps, but verify" above).

## After implementing a feature

Run the **`update-system-docs`** skill (`.cursor/skills/update-system-docs/SKILL.md`) to sync:

- Affected system maps (including bumping their `Verified` stamp and recording any new **Pitfalls** you hit)
- INDEX status and coverage table
- Linked plans in `Documents/plans/`
- Architecture effort doc status, if applicable

## Composition, prefabs, and UI

Before adding a SubSystem, entity behaviour, or UI surface, read [Documents/architecture/2026-07_agent-first-composition.md](Documents/architecture/2026-07_agent-first-composition.md).

- **Do not** edit `Boot.unity` / `Game.unity` to register systems or UI hosts unless the task *is* the bootstrap effort.
- **Do not** hand-edit mega-prefabs (especially `Human.prefab`) to add features — write Editor setup scripts or wait for the owning redesign’s Phase 0 rewire; never grow the component dump “just this once.”
- **Do not** add or extend uGUI / TMP gameplay UI; **do not** “migrate” condemned views to UI Toolkit as a bridge.
- **Do not** “fix” or feature-extend system maps marked **condemned** — replace per the linked design doc with a Phase 0 purge.
- New UI: UI Toolkit (UXML/USS) + catalog/path pattern. Interim reference until UiShell exists: [machine-interface](Documents/architecture/systems/machine-interface.md). Target shell: [ui-shell](Documents/architecture/systems/ui-shell.md).

## Authoring conventions

See [Documents/SKILL.md](Documents/SKILL.md) for header blocks, linking rules, system map template, and how a brand-new domain goes from design-only to mapped.

## Project context

- Unity 6 / URP multiplayer game using FishNet.
- Gameplay code under `Assets/Scripts/SS3D/`.
- Subsystem pattern: `SubSystem` / `NetworkSubSystem` via `SubSystems.Get<T>()`.
- Upstream GitBook may help with generic Unity/FishNet concepts; fork direction lives in `Documents/`.
