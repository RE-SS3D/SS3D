# Fork status

This fork ([henkhooft/SS3D](https://github.com/henkhooft/SS3D)) has diverged meaningfully from
[RE-SS3D/SS3D](https://github.com/RE-SS3D/SS3D) upstream. It is an active experiment in
redesigning core gameplay systems and development foundations at a faster pace than upstream's
current review capacity supports.

This document is the plain-language divergence log. It is updated periodically — not per-commit.
For doc authoring conventions see [SKILL.md](SKILL.md).

**Last updated:** 2026-07-09

---

## At a glance

| | Upstream (`RE-SS3D/SS3D`) | This fork (`henkhooft/SS3D`) |
|---|---|---|
| Default branch | `develop` | `develop` |
| Unity version | 2021.3.15f1 | **6000.3.16f1** (Unity 6) |
| Render pipeline | Built-in | **URP 17** |
| Release channel | Tagged releases on GitHub | **No releases** — build from source |
| Documentation | GitBook ([ss3d.gitbook.io](https://ss3d.gitbook.io/dev-guide/)) | `Documents/design/` + `Documents/architecture/` + system maps |
| Commits ahead of upstream | — | **~119** (0 behind as of last fetch) |
| Files changed vs upstream | — | ~2,672 files, +164k / −44k lines |

---

## Repository and documentation

### Presentation

- [README.md](../README.md) and [CONTRIBUTING.md](CONTRIBUTING.md) describe this fork, not upstream.
- Issue templates and the PR checklist point contributors at `Documents/` rather than GitBook.
- [ISSUE_TEMPLATE/Config.yml](../.github/ISSUE_TEMPLATE/Config.yml) links discussions and this file
  instead of upstream milestones.

### Documentation structure

Independent of upstream's GitBook:

| Path | Purpose |
|---|---|
| [SKILL.md](SKILL.md) | Conventions for all documentation layers |
| [architecture/INDEX.md](architecture/INDEX.md) | Navigation hub — find code by system |
| [architecture/systems/](architecture/systems/) | Per-domain system maps (entry points, key files) |
| [design/](design/) | Gameplay design specs — **what** and **why** (owner-maintained) |
| [architecture/](architecture/) | Dated implementation efforts — **how** and **in what order** |
| [plans/](plans/) | Temporary implementation plans (updated when work ships) |
| [AGENTS.md](../AGENTS.md) | AI agent instructions (docs-first navigation) |
| [FORK_STATUS.md](FORK_STATUS.md) | This divergence log |

Upstream's dev guide may still help with generic Unity and FishNet concepts, but project direction,
milestones, and gameplay specs for this fork live in `Documents/`.

### GitHub automation

Upstream milestone/roadmap/release workflows are **disabled for automatic triggers** — they run on
`workflow_dispatch` only. The automated release build ([main.yml](../.github/workflows/main.yml))
is likewise manual-only; this fork does not publish releases. Discord webhook notifications were
removed from CI.

EditMode test CI ([editmodetestrunner.yml](../.github/workflows/editmodetestrunner.yml)) still runs
on push and PR.

---

## Engine and toolchain

These changes touch nearly every asset and are the largest structural divergence from upstream.

### Unity 6 upgrade

Merged via `archive/develop-unity6`. Includes:

- Editor upgrade to **6000.3.16f1**
- Asset and ProjectSettings reserialization to Unity 6 format
- ugui 2.0 / TMP Essential Resources import
- Addressables 2.x cleanup (legacy Built In Data entries removed)
- Scriptable Build Pipeline settings
- Input System Controls.cs regeneration (inputsystem 1.19.0)
- FishNet DefaultPrefabObjects registry rebuild
- Editor toolbar tools migrated from UnityToolbarExtender to Unity 6 **MainToolbar API**
  (Scene Switcher, Launcher, Network Settings — see README)

### URP migration

Merged via `archive/feature-urp-migration` (through `develop-unity6`). Includes:

- URP 17 foundation (`com.unity.render-pipelines.universal` 17.3.0)
- Pipeline asset and Forward+ renderer in `Assets/Settings/URP/`
- Simple Toon shader port to URP HLSL
- Game content materials converted from Built-in Standard to URP Lit
- Post Processing Stack v2 removed; post-processing migrated to URP volumes
- Pipeline MSAA disabled so camera TAA can run without warnings
- Editor migration tooling in `Assets/Editor/URPMigration/`

Upstream remains on the Built-in render pipeline with no equivalent URP assets or selection-pick
render features.

---

## Shipped on `develop`

These systems are implemented and merged. They represent the bulk of code divergence from upstream.

### Selection API (#1387)

**Paths:** `SS3D.Systems.Selection`, `SS3D.Rendering.URP.SelectionPick*`

Shader-ID mesh picking replaces naive screen raycasts for interaction targeting. Each `Selectable`
gets a unique render color; a URP offscreen pick pass plus `SelectionCamera` readback identifies
the hover target. `InteractionController` routes client interaction targeting through this system;
the server validates using `NetworkObject` and interaction point.

Merged from `archive/feature-1387-selection-api`. Cursor picking misalignment fix merged from
`archive/fix-selection-camera-picking`.

### Detailed examine (#1394)

**Paths:** `SS3D.Systems.Examine`

Extends hover tooltips with **shift-hold detailed examine** — text and image panel variants,
range-gated, driven off the selection system's current `IExaminable`. Posters and other content
wired as `ImageExaminable`.

Merged from `archive/feature-1394-detailed-examine`.

### Examine localization

**Paths:** `SS3D.Localization`, `SS3D.Systems.Examine`

Unified **Examine** string table for all 146 `ExamineData` assets, replacing sparse per-category
tables (Items, Tiles, Misc). Shared **`LocalizedTextService`** (caching, locale-change invalidation,
dev/release fallback) drives code-driven lookups; **`ExamineContentResolver`** separates static
table text from dynamic sections via **`IExamineContentProvider`**.

English strings migrated and wired through `LocalizedString` fields on every examine asset. Editor
menus under `SS3D/Localization/Examine/` export/import JSON for translation workflows.
**Identification cards** use dynamic owner/role lines (`IdentificationCardExaminable`).

Non-English locales (`fr`, `pt-BR`, `ru-RU`) have empty Examine tables for now — they fall back to
English until a translation import lands.

Merged from `archive/feature-examine-localization`. Design plan:
[examine_localization_design_5ca361a6.plan.md](plans/examine_localization_design_5ca361a6.plan.md).

**Examine interaction (radial Tier 1):** `ExamineInteraction` added as a shift-hold detailed examine
action reachable from the radial menu (merged with interaction work below).

### Interactions — hardening and radial menu

**Paths:** `SS3D.Interactions`, `SS3D.Systems.Interactions`

Production multiplayer hardening of the source/target interaction model:

- **`InteractionIdentifier`** wire protocol (`genericName` + `targetComponentIndex`) — RPCs no longer
  match display names
- **`InteractionPipeline`** — shared discover → filter → sort on client and server; **`Priority`**
  for deterministic primary-click order
- Gameplay gates: intent sync, stamina, inventory ownership, locker **`InteractionPermission`**
- Cancellation (**C** key + movement auto-cancel on delayed interactions)
- Optimistic client feedback (loading bars + pending hover outlines); **`TargetRejectInteraction`**
  rollback on server reject
- Hover availability outlines (green/yellow/blue) via **`InteractionOutlineView`** + URP outline shader

**Three-tier radial menu** (UI Toolkit, shared tokens at `Assets/Content/Systems/UI/Tokens/`):

- Dynamic petals with labels; instant (Tier 1), armed targeted (Tier 2), combine (Tier 3 — drag route
  still pending)
- **`ArmedInteractionSubSystem`** + overlay for second-click targeting (`TransferSubstanceInteraction`
  proof-of-concept)
- Radial HUD lives on scene overlay (`Game.unity`), not `PlayerCanvas`

EditMode **`InteractionPipelineTests`**; PlayMode pickup regression via **`InteractionPlayModeTests`**.

Merged from `archive/feature-interaction-system-hardening` (includes `archive/feature-radial-menu-redesign`).
Architecture: [2026-07_interaction-system-hardening.md](architecture/2026-07_interaction-system-hardening.md).
Plans: [interaction_system_improvements_9e14ae22.plan.md](plans/interaction_system_improvements_9e14ae22.plan.md),
[radial_menu_implementation_5a83bdf9.plan.md](plans/radial_menu_implementation_5a83bdf9.plan.md).

### Tilemap and adjacency engine

**Paths:** `SS3D.Systems.Tile`, `SS3D.Systems.Tile.Connections`

Major refactor of the construction tilemap:

- **AdjacencyEngine** — queued adjacency recompute replacing recursive neighbour ping-pong;
  connectors migrated for walls, doors, pipes, cables, disposal pipes, furniture, and more
- **TileAdjacencyView** — local mesh/direction visuals synced from adjacency state
- **ITileQueryService** / **TileQueryService** — read-only tile queries
- **ConstructionService** — server-authoritative placement (Phase 3)
- **Tile identity sync** — compact ushort asset catalog (Phase 1)
- **FishNet HashGrid AOI** — tile replication scoped by area-of-interest; fixes for tile pop-in
  when re-entering AOI
- TileMap Creator RPCs gated behind server-side Administrator checks

Merged from `archive/feature-tilemap-system-refactor`.

### Game lifecycle hardening

**Paths:** `SS3D.Systems.Rounds`

Round loop refactored to a **single-flight state machine** with generation-tracked
`CancellationTokenSource` (prevents double start/stop and embark-during-ending races). States:
`Stopped → Preparing → WarmingUp → Ongoing → Ending → Ended`. Join and round action ordering
hardened across `EntitySubSystem`, `PlayerSubSystem`, and `GamemodeSubSystem`.

Regression tests added under `Assets/Scripts/Tests/KnownIssueReproduction/RoundLifecycle_*`.

Merged from `archive/feature-game-lifecycle-hardening`.

### Machine interfaces

**Paths:** `SS3D.UI.MachineInterface`, `Assets/Content/Systems/UI/MachineInterface/`

Diegetic **UI Toolkit** panels for station machines, networked via FishNet snapshots:

| Phase | Status | What shipped |
|---|---|---|
| [Phase 1](architecture/2026-07_machine-interface-phase1-foundation.md) | Shipped | UI foundation, local APC panel preview |
| [Phase 2](architecture/2026-07_machine-interface-phase2-apc-networking.md) | Shipped | Networked APC with power channel gating |
| [Phase 3](architecture/2026-07_machine-interface-phase3-smes-generalization.md) | Mostly shipped | SMES interface (exterior + engineer views), registry-driven plumbing, shared binder flow |

`MachineInterfaceHost` disables `UIDocument` when closed to avoid interfering with the selection
pick pass. Pattern documented for adding new machine types on the host.

Merged from `archive/machine-ui`. Phase 3 architecture doc still lists minor cleanup (shared
diagnostics across binders, multi-viewer integration tests).

### Structured logging

**Paths:** `SS3D.Logging`

Serilog-based structured logging with mandatory sender + context enum, namespace-level filtering
via `LogSettings` ScriptableObject, Unity console + file sinks, and client-ID enrichment for
multiplayer. Recent work replaced remaining `Debug.Log` calls and reduced startup noise.

---

## Design specs (not yet implemented)

These files in [design/](design/) define **future gameplay direction**. They are marked
`Status: active` but the systems they describe are largely **not implemented** on `develop` yet.
Implementation should follow the specs or document explicit deviations.

| Doc | Summary |
|---|---|
| [health.md](design/health.md) | Two-tier damage: per-limb physical + organ-regulated systemic (toxin/oxy/blood volume) |
| [combat.md](design/combat.md) | Deterministic melee + weapon-intrinsic ranged accuracy; windup/recovery, blocking |
| [stamina.md](design/stamina.md) | Fast stamina pool as front-end of oxy-debt; pushing past empty draws real debt |
| [armor.md](design/armor.md) | Per-zone flat absorption; binary environmental seals hooking into organ model |
| [area.md](design/area.md) | Per-tile area partition (flood-fill + manual override) for power, access, cameras, comms |
| [comms.md](design/comms.md) | Diegetic speech subtitles with distance/occlusion; visual radio channel selector |
| [main-hud.md](design/main-hud.md) | Minimal chrome HUD — 3D body vitals, cut targeting doll, intent chording |
| [hacking-interface.md](design/hacking-interface.md) | Field diagnostic unit (FDU) — diegetic 7-panel tool; discovery by physical access |

Machine interfaces and the radial interaction menu partially implement
[main-hud.md](design/main-hud.md) (tiered interactions, intent chording — drag-combine Tier 3 still
pending). Machine interfaces also touch [area.md](design/area.md) power assumptions; the rest of
these specs remain design-only.

---

## In progress on feature branches

Work that has **not** merged to `develop` yet. Branches drift from `develop` quickly — rebase
before assuming commit counts.

| Branch | Ahead / behind `develop` | System | Notes |
|---|---|---|---|
| `feature/atmos-ecs` | 26 / 21 | ECS atmospherics + GPU gas/fire visuals | `SS3D.Systems.Atmospherics`, `AtmosRendererFeature`; EditMode tests on branch |
| `feature/vision-urp-tilemap` | 9 / 21 | Grid-based FOV / fog-of-war on URP + tilemap | `SS3D.Systems.Vision`, `VisionRendererFeature`; supersedes older vision work |
| `vision-system-wip` | 6 / 100 | Older vision prototype | Stale — use `feature/vision-urp-tilemap` instead |
| `feature/animation-system` | 0 / 75 | Animation system | No unique commits; stale relative to `develop` |

---

## Archived branches merged into `develop`

These `archive/*` branches were feature-complete enough to merge and are kept for history.
Do not develop on them — use `develop` or a new feature branch.

| Branch | Merged | Primary deliverable |
|---|---|---|
| `archive/develop-unity6` | 2026-07 | Unity 6 upgrade umbrella |
| `archive/feature-urp-migration` | (via develop-unity6) | URP 17 pipeline |
| `archive/feature-1387-selection-api` | (via develop-unity6) | Shader selection API |
| `archive/feature-1394-detailed-examine` | (via develop-unity6) | Detailed examine UI |
| `archive/feature-tilemap-system-refactor` | (via develop-unity6) | Adjacency engine + construction |
| `archive/feature-game-lifecycle-hardening` | (via develop-unity6) | Round state machine |
| `archive/fix-selection-camera-picking` | (via develop-unity6) | Cursor picking alignment |
| `archive/machine-ui` | 2026-07 | Machine interface UI (APC/SMES) |
| `archive/feature-examine-localization` | 2026-07-09 | Unified Examine localization + `LocalizedTextService` |
| `archive/feature-interaction-system-hardening` | 2026-07-09 | Interaction RPC hardening, pipeline, outlines, radial menu |
| `archive/feature-radial-menu-redesign` | (via interaction-system-hardening) | UI Toolkit three-tier radial menu + armed overlay |

---

## What upstream still has that we don't automatically inherit

- Official releases and the [ss3d.space](https://ss3d.space/) download channel
- GitBook documentation and devblogs
- Active milestone/project board automation
- Discord CI notifications
- Whatever lands on `upstream/develop` after our fork point (currently 0 commits behind, but
  this will change as upstream moves)

When pulling from upstream, expect conflicts in: ProjectSettings, materials/shaders, render
pipeline config, tilemap/construction code, interaction/selection code, and any system listed
above as shipped.

---

## Standing offer

Anything here can be proposed upstream on request. Open an
[issue](https://github.com/henkhooft/SS3D/issues) or
[discussion](https://github.com/henkhooft/SS3D/discussions) and a PR-shaped version of whatever
is relevant can be prepared. Large divergences (Unity 6, URP, tilemap refactor) are best proposed
as incremental slices rather than one mega-PR.

---

## Maintaining this document

Update when:

- A feature branch merges to `develop` (move it from "In progress" to "Shipped")
- A new design spec or architecture doc lands
- Unity/toolchain versions change
- GitHub automation or repo presentation changes
- Significant divergence from upstream develops (re-fetch `upstream` and update commit counts)

After updating, bump the **Last updated** date at the top.
