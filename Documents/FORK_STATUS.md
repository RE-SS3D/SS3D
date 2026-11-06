# Fork status

This fork ([henkhooft/SS3D](https://github.com/henkhooft/SS3D)) has diverged meaningfully from
[RE-SS3D/SS3D](https://github.com/RE-SS3D/SS3D) upstream. It is an active experiment in
redesigning core gameplay systems and development foundations at a faster pace than upstream's
current review capacity supports.

This document is the plain-language divergence log. It is updated periodically — not per-commit.
For doc authoring conventions see [SKILL.md](SKILL.md).

**Last updated:** 2026-10-31

---

## At a glance

| | Upstream (`RE-SS3D/SS3D`) | This fork (`henkhooft/SS3D`) |
|---|---|---|
| Default branch | `develop` | `develop` |
| Unity version | 2021.3.15f1 | **6000.3.16f1** (Unity 6) |
| Render pipeline | Built-in | **URP 17** |
| Release channel | Tagged releases on GitHub | **Manual + nightly prereleases** via `develop-release.yml` (Windows zip + bats by default; nightly Windows+Linux → `develop-nightly`; Linux / EditMode / smoke opt-in on dispatch) — day-to-day still build-from-source |
| Documentation | GitBook ([ss3d.gitbook.io](https://ss3d.gitbook.io/dev-guide/)) | `Documents/design/` + `Documents/architecture/` + system maps + [milestones](milestones/INDEX.md) |
| Commits ahead of upstream | — | **~1056** (1 behind as of 2026-10-31: upstream #1491 "Port Vision System") |
| Files changed vs upstream | — | ~16.1k files, +1.65M / −322k lines |

### Shipped gameplay

Player-facing systems on this fork that upstream does not have (or has only in a thinner form).
Engine, CI, and agent/docs tooling stay out of this table — see sections below for those.
Coverage means how much of the design intent is playable today, not “code exists.”

| Feature | Coverage | What shipped |
|---|---|---|
| [Selection](#selection-api-1387) | shipped | Shader mesh picking; hover outlines exclude pick shells |
| [Examine](#detailed-examine-1394) | partial | UITK hover/Shift detail; character paperdoll Search + hold-to-take; health Tier 0/1 lines; obscured-slot filter / restrained loot open |
| [Interactions + radial menu](#interactions-hardening-and-radial-menu) | shipped | Hardened multiplayer interactions; three-tier UITK radial; armed Tier-2; Discover contract; `InteractionController` decomposed (combat on sibling network) |
| [Main HUD](#main-hud-ui-toolkit) | partial | Gear/hands/intent UITK overlay; health+atmos alert stack; storage panels; melee/ranged reticle; vitals cluster still pending |
| [Inventory & storage](#inventory-and-storage-redesign) | partial | Container model + on-demand panel; clothing folded world presentation; bright full-toon ObjectIcon HUD icons; Play Mode verification pending |
| [Comms](#comms-local-speech-feed-and-crowd-cap) | partial | Local speech chips + crowd cap; non-diegetic radio feed + Tab/slash compose + announce SFX; headset/PDA/radial channel pick deferred |
| [Disposal](#disposal-item-network) | partial | BFS pipe routing, chute/outlet transit, SizeClass gate; pipe crafting, Cargo hook, player transit deferred |
| [Health](#health-rewrite) | partial | Two-tier damage, organs, bleeding, critical/defib, treatments, severing; env exposure + feel SFX; critical ragdoll; alert stack; vitals UITK pending |
| [Combat](#combat-melee-ranged-armor-stamina) | partial | Melee + ranged hitscan (M4) + armor absorption + swing/fire stamina drains; M1p feel (muzzle/holes/audio/two-hand); disarm/block/seal deferred |
| [Structural destruction](#structural-destruction) | partial | Per-tile integrity, melee force, blast BFS, tint/examine VFX; debris spawn / Area local remesh deferred |
| [Body animation](#player-body-animation-foundation) | partial | Peaceful / melee / ranged stance; injured limp; Ragdoll owns collapsed/dead/critical presentation; ranged aim IK deferred |
| [Screen effects](#screen-space-effects) | partial | Health overlays + hit/blast flash; turf temp/fire from `HealthSnapshot.Environment` |
| [Audio](#audio-foundation) | partial | SFX occlusion, impact and machine sounds, department room tone, wind and sound needs air, personal heartbeat/breathing, alert cues, footsteps; music/volume categories pending |
| [Vision / FOV](#vision-fov) | shipped | Hard black fog-of-war from viewpoint raycasts (hot-path GC cut) |
| [Station lighting (URP)](#urp-lighting-phase-1) | partial | Fixture-driven half-toon Forward+ look; client SyncVar fixture visuals; visual plate polish still open |
| [Areas + station power](#area-foundation) | partial | APC-seeded areas, kWh cells, channel shedding, area lights/switches; [solar panels + tracker](#solar-generation); live remesh pending |
| [Machine interfaces](#machine-interfaces) | shipped | Diegetic UITK APC/SMES/atmos ports/vending; engineering ID swipe; host-echo TargetRpc gate |
| [Atmospherics](#atmospherics-ecs-foundation) | partial | Turf gas ECS + fire + per-edge bulk flow + equal-P composition diffusion; pipe networks + ports; Phase 1 dirty-chunk client VFX sync (AOI / late-join deferred) |
| [ID / access](#id-access-foundation) | partial | Crew records, door + machine gates, ID console; auth logs / broader design pending |
| [Tilemap / construction](#tilemap-and-adjacency-engine) | shipped | Adjacency engine, construction service, AOI tile sync, layer visibility |
| [Map Editor](#map-editor-replacement) | partial | Full-screen UITK Map Editor + spawn-point authoring/persistence; Builder / instant place / round-config pool / runtime spawn pick deferred |
| [Rounds](#game-lifecycle-hardening) | shipped | Single-flight round state machine (join/start/end races hardened) |
| [Lobby](#lobby-ui-toolkit-redesign) | partial | UITK lobby shell + Character Creator (Human; saved slots), job/antag prefs with one-pass resolution, networked OOC/LOOC/Admin chat, Server Settings + live Server Info; legacy uGUI lobby purged |
| [Session / world lifecycle](#session-world-lifecycle) | shipped | Session FSM + world readiness graph + `NetworkSystemsHub`; Boot disconnect storm fixed; host loopback / client spawn polish |

Details and deferred work for each row live under [Shipped on `develop`](#shipped-on-develop).

---

## Repository and documentation

### Presentation

- [README.md](../README.md) and [CONTRIBUTING.md](CONTRIBUTING.md) describe this fork, not upstream.
- Issue templates and the PR checklist point contributors at `Documents/` rather than GitBook.
- [ISSUE_TEMPLATE/Config.yml](../.github/ISSUE_TEMPLATE/Config.yml) links discussions and this file
  instead of upstream milestones.

### Documentation structure

Independent of upstream's GitBook. The layers (design specs, architecture efforts, system maps,
plans, milestones, and the `Documents/archive/` for shipped efforts and completed plans) and their
rules live in [SKILL.md](SKILL.md); agents start at [AGENTS.md](../AGENTS.md). Upstream's dev guide
still helps with generic Unity and FishNet concepts.

### GitHub automation

Upstream-only workflows (milestones, roadmap, Discord notifications, old packaging helpers)
were **removed** from this fork. Remaining workflows:

- [develop-release.yml](../.github/workflows/develop-release.yml) — **manual** (`workflow_dispatch`)
  + **nightly** (`cron` 05:00 UTC → floating `develop-nightly` Windows+Linux client/server).
  Manual default: Windows client → GitHub **prerelease** (zip + launch bats; Config/Tilemaps
  seeded). Linux / EditMode / smoke are opt-in; runs on TomNAS (dispatch `runner: github` for GitHub-hosted) — see
  [2026-07_ci-develop-release-pipeline.md](architecture/2026-07_ci-develop-release-pipeline.md)
- [editmodetestrunner.yml](../.github/workflows/editmodetestrunner.yml) — EditMode tests on push to
  `develop`, PRs (`pull_request_target`), and manual dispatch; forces Node 24 for JS actions;
  validates `UNITY_LICENSE` / `UNITY_SERIAL` secrets before running; TomNAS-prefer with GitHub
  fallback ([2026-07_multiplayer-testing-self-hosted-ci.md](architecture/2026-07_multiplayer-testing-self-hosted-ci.md))
- [multiplayer-smoke-test.yml](../.github/workflows/multiplayer-smoke-test.yml) — nightly PlayMode
  scenarios plus the headless smoke set (`late-join`, `reconnect`, `observer-sync`;
  `sandbox-reset` and `nukeops-round` nightly only) against real dedicated-server + client builds

Doc layer redesign (coverage table, strip prototyping prompts from design docs) merged via PR #2.
Agent-first composition policy and UI path-catalog conventions live under
[architecture/](architecture/) (see shipped sections below). A design/implementation alignment
audit (PR #18) fixed code that had drifted from its design doc. Design-docs integration (PR #21)
added antagonist / audio / networking / player-accounts specs. Cross-cutting structural debt lives
in [TECH_DEBT.md](architecture/TECH_DEBT.md) (PR #25). Playable focus sequencing lives in
[milestones/](milestones/) (PR #43; test-server gate + Nuke Ops retarget via PRs #45 / #53). Asset
placement taxonomy + EditMode enforcement:
[2026-07_asset-file-structure-taxonomy.md](architecture/2026-07_asset-file-structure-taxonomy.md)
(PR #30); Phase 1 icon consolidation under `Assets/Art/Icons/` (PR #40). Editor menu hygiene
(A/B/C tiers): [2026-07_editor-tooling-tiers.md](architecture/2026-07_editor-tooling-tiers.md)
(PR #57).

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
- **Palette emission fix** (PR #7) — Simple Toon samples `_EmissionMap` so shared
  `PaletteEmission` materials get UV swatch colors instead of a flat white `_EmissionColor`

Upstream remains on the Built-in render pipeline with no equivalent URP assets or selection-pick
render features.

### URP lighting Phase 1

**Paths:** `Assets/Content/Resources/Simple Toon/`, fixture / `LightPower` behaviours,
`Assets/Settings/URP/`

Fixture-driven half-toon station lighting on URP 17 Forward+ (merged via PR #24,
`feature/urp-lighting-phase1-v2`):

- Zero ambient / no main directional light; gameplay Volume + SSAO on the Forward+ renderer
- Half-toon lighting in `STLighting.hlsl` with soft-near attenuation for fill hotspots
- Dual fixture lights (Spot + PointFill) on tubes/bulbs; emergency mode is short-range spot-only
  (fill off) so unpowered rooms read as power-starved
- Floor derivative normals; inventory icon previews get temporary lights so icons stay readable
  under fixture-only scene lighting

Visual plate matching upstream Built-in reference screenshots is **not** finished — handoff in
[2026-07_urp-lighting-look-polish.md](architecture/2026-07_urp-lighting-look-polish.md).
Plan: [urp_lighting_look_plan_d42c32f5.plan.md](plans/urp_lighting_look_plan_d42c32f5.plan.md).
System map: [rendering.md](architecture/systems/rendering.md).

**Client fixture sync** (PR #36): `LightPower` SyncVar drives fixture visuals on pure clients so
APC / wall-switch toggles are visible off-host.

### Analyzer tooling

StyleCop analyzers disabled in the project (2026-07-14) to reduce friction on AI-assisted edits.
EditMode test CI still runs on push and PR.

### Technical debt tracker

**Path:** [TECH_DEBT.md](architecture/TECH_DEBT.md)

Project-wide register of structural risks (mega-prefab, collapse authority, interaction Discover
contract, condemned crafting, etc.) ranked by blast radius — not a fourth doc layer; indexes
system-map Pitfalls and agent-first composition debt. Merged via PR #25.

Resolved since earlier refresh (see TECH_DEBT §6): Discover contract (PR #44 / 1.3), crafting purge
(PR #39 / 1.6), body-presentation authority (PR #42), session/world lifecycle + Boot disconnect storm
(PRs #36–#38 / 1.7), Human.prefab Phase 0 hygiene + recipe tools (PR #31 / 1.1 partial),
`InteractionController` decomposition (PR #69 / 1.9 interactions slice; Main HUD god-class still
open), Editor menu proliferation paydown (PR #57 / 1.5 partial).

### Addressables Phases 1–3

**Paths:** `SS3D.Data` (`AssetHandle` / `AssetProvider` / `Assets.GetAsync`),
`Assets/Content/Addressables/`

Turns the previously inert Addressables setup into a dual-path loader (eager sync still default;
AddressablesAsync opt-in per database). Merged via PR #41 (`cursor/addressables-phases-1-3`);
effort scoped in PR #33:

- Phase 1 — orphan `Assets/AddressableAssetsData/` removed; single settings root under Content
- Phase 2 — pin `com.unity.addressables` **2.9.1**; `AssetHandle<T>` / `AssetProvider` dual path
- Phase 3 — InteractionIcons pilot on AddressablesAsync warm preload

Phases 4–6 (broader database migration, UI catalog off `Resources.Load`, network preload barrier)
still open — see
[2026-07_addressables-expansion-migration.md](architecture/2026-07_addressables-expansion-migration.md).
Follow-on polish on the same PR: radial petal clicks after first close; Pacman `MachineVibrate`
facing snap.

### Profiler / hot-path tooling

**Paths:** Editor `SS3D/Perf` export → `Logs/perf/*.md`, `.cursor/skills/analyze-unity-perf/`

PR #28 adds ranked Profiler markdown export + the analyze-unity-perf skill, and cuts Vision FOV /
interaction-outline discovery GC (batched raycasts, no per-frame `Color[]` churn; `ProfilerMarker`s
on outline/discover and atmos tick). Architecture:
[2026-07_unity-perf-ai-tooling.md](architecture/2026-07_unity-perf-ai-tooling.md).

### Testing and agent validation loop

Upstream runs EditMode tests only. This fork adds (PRs #73–#84):

- **Multiplayer smoke harness** (`Testing/multiplayer/`): real dedicated server + client processes
  driven by a script DSL; scheduled nightly on a self-hosted runner and on labelled PRs (no
  passing scheduled run since 2026-10-01, see [TECH_DEBT.md](architecture/TECH_DEBT.md) §1.10)
- **PlayMode suite** repaired and extended, with server-side helpers and passing known-issue repros
  promoted into the regular suite
- **One-command validation** (`Testing/ui/validate.sh`): compile check, EditMode, rendered UI pilots
  through Unity MCP with real OS input; `--nightly` adds PlayMode, client and server builds and smoke.
  See [Testing/ui/README.md](../Testing/ui/README.md)
- **Multiplayer Play Mode checks** (`--steps mppm:<scenario>`): the same smoke scenario scripts in an
  editor host + virtual-player pair, no build needed; the built smoke stays the nightly gate.
  See [networking-session-testing.md](architecture/systems/networking-session-testing.md)

Architecture: [2026-07_multiplayer-test-harness.md](archive/architecture/2026-07_multiplayer-test-harness.md),
[2026-07_multiplayer-testing-self-hosted-ci.md](architecture/2026-07_multiplayer-testing-self-hosted-ci.md).

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

**Post-merge polish** (PR #5): hover flicker and outline bleed into item icons fixed by excluding
outline shells from the pick pass via `SelectionRenderingLayers` (lives in `Rendering.URP` to avoid
assembly cycles); green outline cleared on item pickup.

**Post-merge polish** (PR #17): `ClosestPoint` no longer called on non-convex `MeshCollider`s
(was throwing/misbehaving on some selectable meshes).

### Detailed examine (#1394)

**Paths:** `SS3D.Systems.Examine`, `SS3D.UI.Examine`, `SS3D.Localization`

Extends hover tooltips with **shift-hold detailed examine** — text and image panel variants,
range-gated, driven off the selection system's current `IExaminable`. Posters and other content
wired as `ImageExaminable`.

Merged from `archive/feature-1394-detailed-examine`.

### Examine localization

**Paths:** `SS3D.Localization`, `SS3D.Systems.Examine`

Unified **Examine** string table for all 147 `ExamineData` assets, replacing sparse per-category
tables (Items, Tiles, Misc). Shared **`LocalizedTextService`** (caching, locale-change invalidation,
dev/release fallback) drives code-driven lookups; **`ExamineContentResolver`** separates static
table text from dynamic sections via **`IExamineContentProvider`**.

English strings migrated and wired through `LocalizedString` fields on every examine asset. Editor
menus under `SS3D/Localization/Examine/` export/import JSON for translation workflows.
**Identification cards** use dynamic owner/role lines (`IdentificationCardExaminable`).

Non-English locales (`fr`, `pt-BR`, `ru-RU`) have empty Examine tables for now — they fall back to
English until a translation import lands.

Merged from `archive/feature-examine-localization`. Design plan:
[examine_localization_design_5ca361a6.plan.md](archive/plans/examine_localization_design_5ca361a6.plan.md).

**Examine UX:** hover + Shift-hold detailed panels via `ExamineSubSystem` / UITK overlay off the
current selection — **not** an `IInteraction` petal. A radial Tier 1 Examine petal was planned but
is not shipped as `ExamineInteraction` on `develop`.

### Examine UI Toolkit — character paperdoll + health lines

**Paths:** `SS3D.UI.Examine`, `SS3D.Systems.Examine`, `SS3D.Systems.Inventory` (Search / take)

PR #68 (`claude/examine-system-ui-toolkit-1ztb52`) replaces condemned uGUI examine with UITK and
ships design [examine.md](design/examine.md) §7 character examine:

- **Hover** → name tooltip (self + others); **Shift-hold** → details + health Tier 0 (public
  appearance) / Tier 1 (self feel); Shift with no target → self detailed examine
- **Search** interaction (radial or Shift+Click) → persistent paperdoll on **others** only
- **Hold-to-take** (~1.5s) from dead/unconscious characters; restrained loot deferred
- Default input scheme resolves Shift/C/E conflicts ([2026-07_default-input-scheme.md](archive/architecture/2026-07_default-input-scheme.md))
- Phase 0 purge of uGUI `ExamineUI` trio (not migrated)

Deferred: obscured-slot filtering, restrained loot, StoragePanel foreign-loot, medical scanner /
vitals UITK. System map: [examine.md](architecture/systems/examine.md).

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
Architecture: [2026-07_interaction-system-hardening.md](archive/architecture/2026-07_interaction-system-hardening.md).
Plans: [interaction_system_improvements_9e14ae22.plan.md](archive/plans/interaction_system_improvements_9e14ae22.plan.md),
[radial_menu_implementation_5a83bdf9.plan.md](plans/radial_menu_implementation_5a83bdf9.plan.md).

**Post-merge polish** (2026-07-12): stuck outline cleanup, drop-on-click duplicate-spawn fix, instant-interaction
loading-bar flash, toggle-interaction Power icon fallback, radial menu polish, crafting outline log spam.
Removed FastScriptReload and repaired Game scene missing scripts.

Merged from `archive/feature-interactions`.

**Post-merge polish** (PR #17): outline now shows only for objects with a target-bound interaction;
range enforced when an interaction point is missing; Craft discovery gated behind a check, with the
underlying interaction-architecture smells documented rather than silently worked around.

**Discover contract** (PR #44, TECH_DEBT 1.3): locks source-only vs target-bound candidates at the
type level (`InteractionEntry.IsSourceOnly` / `SourceOnly` factory); `InteractionEvent.HasPoint`
replaces the `Vector3.zero` unset-point sentinel; outline discovery filters source-only. Effort:
[2026-07_interaction-discover-contract.md](archive/architecture/2026-07_interaction-discover-contract.md).

**Crafting purge** (PR #39, TECH_DEBT 1.6): obsolete crafting runtime / recipes / Addressables group
removed; system map is `stub` awaiting redesign
([crafting.md](architecture/systems/crafting.md)). Freeform recipe / Tier 3 combine rebuild still
design-only.

**InteractionController decomposition** (PR #69, TECH_DEBT 1.9 interactions slice): thin player
router + `InteractionDiscovery` / `InteractionDispatch` / `InteractionOutlineDriver` /
`DelayedInteractionTracker`; Harm combat RPCs move to sibling `CombatInteractionNetwork` on Human
(recipe-wired). Effort:
[2026-07_interaction-controller-decomposition.md](archive/architecture/2026-07_interaction-controller-decomposition.md).
System map: [interactions-runtime.md](architecture/systems/interactions-runtime.md).

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
- **Build-menu layer visibility** — client-only dim/restore of non-selected tile layer groups in the
  construction build tab (`TileLayerVisibilityService`); no network sync

Merged from `archive/feature-tilemap-system-refactor` and
`archive/tilemap-layer-visibility` (build-menu layer toggles).

The in-game **Map Editor** (below) replaces the legacy TileMap Creator uGUI for admin authoring;
layer visibility is shared with the editor toolbar.

### Map Editor replacement

**Paths:** `SS3D.Systems.Tile.MapEditor`, `Assets/Content/Systems/UI/MapEditor/`

Full-screen **UI Toolkit** Map Editor replacing the DynamicPanels TileMap Creator tab
(merged via PR #26, `claude/map-editor-creative-mode-8pp6qj`):

- Tools: Select / Edit / Move / Delete / Dropper; undoable compound place/delete/decal commands
- Admin-gated via `IMapEditorAuthorizer` (default `AdminMapEditorAuthorizer`); catalog regenerated
  from Editor menu
- Layer visibility, overlays subcategory (sparse floor decals), save/load local station templates
- Orphan TileMap Creator UI prefabs purged so AssetAudit no longer sees missing scripts
- Creative-mode extension hooks reserved (`MapEditorPlacementMode`, round-config save target,
  Area tool stub) — see [map-editor-creative-hooks.md](architecture/systems/map-editor-creative-hooks.md)

Deferred: creative-mode Builder role / instant placement, area merge/split tool, round-config map
pool save, Scripting & Placements backends beyond spawn points, runtime job-aware spawn pick.

**Spawn point authoring** (PR #29): Map Editor Scripting → Spawn Placements places job- and
antagonist-tagged markers (`spawn:job:*` / `spawn:antag:*`); `SpawnPointPersistenceContributor`
persists them in station templates. Runtime `EntitySubSystem` role→spawn resolution still deferred.

Architecture: [2026-07_map-editor-replacement.md](archive/architecture/2026-07_map-editor-replacement.md),
[2026-07_spawn-point-authoring.md](archive/architecture/2026-07_spawn-point-authoring.md).
System map: [tile.md](architecture/systems/tile.md). Design:
[creative-mode.md](design/creative-mode.md).

### Game lifecycle hardening

**Paths:** `SS3D.Systems.Rounds`

Round loop refactored to a **single-flight state machine** with generation-tracked
`CancellationTokenSource` (prevents double start/stop and embark-during-ending races). States:
`Stopped → Preparing → WarmingUp → Ongoing → Ending → Ended`. Join and round action ordering
hardened across `EntitySubSystem`, `PlayerSubSystem`, and `GamemodeSubSystem`.

Regression tests: `Assets/Scripts/Tests/PlayMode/RegressionTests/RoundLifecycle_*` (with
`Issue0990_PropagatingPocketProblem_HostPerspective`), promoted from `KnownIssueReproduction/`
on 2026-10-31 once the PlayMode suite ran again and they passed. `KnownIssueReproduction/`
is kept, empty, for new repros.

Merged from `archive/feature-game-lifecycle-hardening`.

### Machine interfaces

**Paths:** `SS3D.UI.MachineInterface`, `Assets/Content/Systems/UI/MachineInterface/`

Diegetic **UI Toolkit** panels for station machines, networked via FishNet snapshots:

| Phase | Status | What shipped |
|---|---|---|
| [Phase 1](archive/architecture/2026-07_machine-interface-phase1-foundation.md) | Shipped | UI foundation, local APC panel preview |
| [Phase 2](archive/architecture/2026-07_machine-interface-phase2-apc-networking.md) | Shipped | Networked APC with power channel gating |
| [Phase 3](archive/architecture/2026-07_machine-interface-phase3-smes-generalization.md) | Shipped | SMES interface (exterior + engineer views), registry-driven plumbing, shared binder flow |
| [Diegetic screen UI](archive/architecture/2026-07_diegetic-screen-ui-framework.md) | Shipped | Reusable diegetic device shell + component library; vending machine as first consumer |

**APC and SMES** now use `DiegeticDeviceShell` (chassis/bezel/screen) with engineering
`AccessGatePanel` ID swipe gates, glanceable status chips, channel rows, and steel-button controls.
Legacy modal `MachineWindow` remains available for simple panels but is no longer used by APC/SMES.

**Atmospheric devices** ship diegetic panels with the same shell pattern:

- **Air alarm** — live turf sampling (tile in front of wall mount), area vent/scrubber discovery,
  preset mode dispatch, plasma/temperature readouts, gas bar widgets
- **Scrubber** — per-gas filter toggles and flow-rate stepper wired into `ScrubberController`
- **Vent** — target pressure control enforced in `VentController` (stops filling at target)
- **Pump** — inlet/outlet pressure readouts, target outlet pressure, power toggle; operates on its
  own tile and pipe network only (not area-linked)

Power toggles on atmos port UIs require engineering ID access. Diegetic panels support vertical
scrolling for tall content. `MachineInterfaceShellKind` registers modal vs diegetic layouts on the
host.

**Vending machines** open a networked diegetic panel (`VendingMachineController`) with tray-based
dispense (vend to tray, then take). Legacy per-product `DispenseProductInteraction` and
`VendingMachine` behaviour removed. Vending stays **ungated** (no engineering ID swipe) even after
the ID/access foundation shipped for APC/SMES/atmos panels.

**Post-merge polish** (PR #4): refresh-driven UI teardown no longer drops Take/vend click handlers;
`MachinePowerConsumer` no longer sticks at “in use” wattage after vend.

**Backdrop blur** (PR #14): diegetic panels blur the world behind them via Dual Kawase blur plus a
dark dim, replacing an earlier depth-of-field approach.

**Host-echo gate** (PR #36): machine-interface TargetRpc UI gated on `conn.IsLocalClient` so a remote
client's open/refresh/close no longer paints on the listen-server host.

`MachineInterfaceHost` disables `UIDocument` when closed to avoid interfering with the selection
pick pass. Diegetic panels mount the full cloned UXML `TemplateContainer` so attached style sheets
apply at runtime. UI Toolkit masking rule: never combine `border-radius` and `overflow: hidden` on
the same element — split painted and clipping layers (`DiegeticDeviceShell`, `PanelSection`).

Merged from `archive/machine-ui` (APC/SMES networking),
`archive/feature-diegetic-screen-ui-framework` (diegetic shell + vending), and
`atmos-pipes` (APC/SMES diegetic redesign + atmos device panels). Backdrop blur merged via PR #14
(`cursor/diegetic-ui-backdrop-blur`). Plan:
[diegetic_screen_ui_framework_643c2e6f.plan.md](archive/plans/diegetic_screen_ui_framework_643c2e6f.plan.md).
System map: [machine-interface.md](architecture/systems/machine-interface.md).

### Area foundation

**Paths:** `SS3D.Systems.Area`, `SS3D.Systems.Electricity`

APC-seeded per-tile area partition with flood-fill, save/load, and area-scoped power and lighting:

- **`AreaSubSystem`** + **`AreaFloodFillService`** — first-wins APC order, door-tile post-pass, wall-mount
  seeding from `FacingDirection`; per-chunk `ushort[]` area ids; overlap diagnostic in APC machine UI
- **Area-scoped power** — `AreaApcPowerDistribution` draws grid headroom to each APC and drains its cell
  per area; consumers (vendors, jukebox, air alarms, airlocks) do not join the HV cable graph
- **kWh storage model** — tick-integrated charge/discharge; priority channel shedding (Equipment →
  Environment → Lighting)
- **Lighting state** — `AreaLightingState` derivation + client sync; `LightPower` fixture visuals;
  `LightSwitchController` toggles player lighting preference (distinct from APC lighting breaker)
- **Consumer visuals** — `ConsumerPowerVisual` dims emissive/panel materials; airlocks power-gated via
  `AirLockOpener` with delayed close on power loss
- **Atmos port power** — vents, scrubbers, pumps, and air alarms draw from the **Environment**
  power channel via `BasicPowerConsumer`; port simulation skips when unpowered

EditMode tests under `Assets/Scripts/Tests/EditMode/AreaTests/` and
`Assets/Scripts/Tests/EditMode/ElectricityTests/`.

Merged from `archive/areas-foundation` and extended by `atmos-pipes` (environment channel for atmos
ports). Architecture:
[2026-07_area-foundation.md](archive/architecture/2026-07_area-foundation.md). Plans:
[areas_implementation_plan_c0639343.plan.md](archive/plans/areas_implementation_plan_c0639343.plan.md),
[electricity_kwh_foundation_917ccdbc.plan.md](archive/plans/electricity_kwh_foundation_917ccdbc.plan.md).

Deferred: live tile-mutation recompute; editor merge/split UI.

**Post-merge polish** (PR #17): `PowerStatus` flicker fixed so airlocks can actually close;
APC flood-fill now deferred until station template load finishes, instead of racing it.

### Atmospherics ECS foundation

**Paths:** `SS3D.Systems.Atmospherics`, `SS3D.Rendering.URP.AtmosRendererFeature`

Server-authoritative open-tile gas simulation on the turf grid:

- **ECS world** — `AtmosSimulation` with native cell buffers, active-cell sleep/wake, Burst jobs
  (`ShareGasJob`, `ConductHeatJob`, `ReactAtmosJob` for plasma combustion)
- **Tile bridge** — `AtmosTileObserver` on `ITileMutationObserver`; dynamic airlock occupancy via
  `IDynamicTileOccupant` reopens/closes gas paths when doors move
- **GPU visualization** — `AtmosGpuUploader` → pressure/temperature/composition/fire textures;
  `AtmosRendererFeature` scatter + plasma glow + heat distortion passes
- **Client VFX sync Phase 1** (PR #34) — `AtmosDirtyChunkTracker` + `AtmosChunkPatchBuilder`
  broadcast per-chunk patches to pure clients; `AtmosClientVisualizationBridge` /
  `AtmosClientAtlas` rebuild the atlas client-side (AOI / bandwidth caps / late-join bootstrap
  deferred to Phase 2 —
  [2026-07_atmos-client-visualization-sync.md](architecture/2026-07_atmos-client-visualization-sync.md);
  harness regression coverage on the same PR)
- **Equal-pressure composition diffusion** (PR #61) — turf gas sharing mixes composition at equal
  pressure (closes a silent M8 exposure gap); see atmospherics system map
- **Debug** — `AtmosDebugController` overlay; shader debug views on renderer feature

EditMode tests under `Assets/Scripts/Tests/EditMode/Atmospherics/`.

Merged from `archive/feature-atmos-ecs`. Architecture:
[2026-07_atmos-ecs-foundation.md](archive/architecture/2026-07_atmos-ecs-foundation.md).

Deferred: liquid/solid phase buffers, valves, liquid pipe networks, pipe failures, chemistry
integration, atmos client VFX sync AOI + late-join bootstrap (Phase 2).

### Atmospherics pipe machinery

**Paths:** `SS3D.Systems.Atmospherics.Pipes`, `SS3D.UI.MachineInterface` (atmos panels)

Gas pipe layer on top of the turf ECS simulation — bulk pipe networks exchange gas with turf cells
via registered port devices:

- **Pipe networks** — `GasPipeNetworkRegistry`, `AtmosPipeSimulation`, tile-driven connectivity via
  `AtmosPipeObserver`; bulk pressure/mole sharing across connected segments
- **Vents** — `VentController` pushes network gas into turf until target pressure reached
- **Scrubbers** — `ScrubberController` pulls filtered gases from turf into the network; UI flow rate
  scales rated throughput
- **Pumps** — `AtmosPumpController` moves turf gas into the pipe network with target outlet pressure
  and max differential stall; not area-linked
- **Air alarms** — `AirAlarmController` samples the turf cell in front of the wall mount, discovers
  area vents/scrubbers via `AtmosAreaDeviceQuery`, and dispatches preset modes; port commands
  validated against the alarm's resolved area
- **Power gating** — port devices require Environment channel power (`AtmosPortPower`)
- **VFX polish** — fire clipping, wall occlusion, and heat distortion placement fixes on the atmos
  render passes
- **Debug** — `AtmosDebugController` extended with pipe network overlay

EditMode tests under `Assets/Scripts/Tests/EditMode/Atmospherics/` (port flow, pump stall/target,
scrubber flow rate, air-alarm sampler). Gas pump examine strings added to the unified Examine table.

Merged from `atmos-pipes`. System map: [atmospherics.md](architecture/systems/atmospherics.md).

Deferred: valves, liquid pipes, pipe failures (clog/rupture), junction chemistry.

### ID / access foundation

**Paths:** `SS3D.Systems.IdAccess`, `SS3D.UI.MachineInterface`, `SS3D.Systems.Furniture`

Server-side crew identity rail and shared access checks:

- **`IdAccessSubSystem`** + **`CrewRecord`** — authoritative access bitmask per ckey; physical `IDCard`
  tokens bind to records (no independent card state)
- **`AccessCredentialResolver`** — finds bound ID on hands and inventory containers (direct card or
  PDA-inserted)
- **Machine UI gates** — `AccessGatedMachineInterfaceBehaviour` server ID scan sessions; granted,
  scanning, and denied states on diegetic ID reader variants
- **Door access** — `AirLockAccessGate` checks holder credential against area default or override
- **ID console** — `IdConsoleController` for Change ID gated editing; dev console commands
  (`accesscheck`, `accessgrant`, `accessrevoke`, `accesspreset`)

EditMode tests under `Assets/Scripts/Tests/EditMode/IdAccessTests/`.

Merged from `archive/feature-id-access-foundation`. System map:
[id-access.md](architecture/systems/id-access.md).

Deferred: full design-spec coverage (auth logs, comms integration, playtime/job unlocks).

### Persistence foundation

**Paths:** `SS3D.Data.Persistence`, `SS3D.Systems.Persistence`

Layered contributor-based disk persistence replacing the monolithic tilemap JSON pipeline:

- **`PersistenceSubSystem`** + **`IPersistenceContributor`** — ordered save/load orchestrator with
  `OnBeforeCapture` / `OnAfterRestore` lifecycle events
- **`PersistenceEnvelope`** — versioned wrapper with per-domain JSON chunks via
  `EnvelopePersistenceStore`
- **Station templates** — `TileMapPersistenceContributor` + `AreaPersistenceContributor`; legacy flat
  tilemap JSON migration; template restore preserves area metadata (including `lightingSwitchOn`) when
  APCs are already placed
- **Server meta** — `PermissionsPersistenceContributor` with legacy `permissions.txt` fallback;
  append-only round history JSONL on round end
- **Wiring** — `TileSubSystem` save/load backend; server boot `LoadServerMeta`; permissions
  auto-save on `UserPermissionsChangedEvent`

EditMode tests: `PersistenceFrameworkTests`, `ServerMetaPersistenceTests`; updated
`AreaFloodFillTests` for template restore.

Merged from `archive/feature-persistence-framework`. Design:
[persistence-save.md](design/persistence-save.md). System map:
[persistence.md](architecture/systems/persistence.md). Plan:
[persistence_architecture_design_2fe61864.plan.md](plans/persistence_architecture_design_2fe61864.plan.md).

Deferred: round-config map pool contributor, Phase 2 round snapshots (items, electricity kWh,
atmospherics, substances, entities), player meta.

### Structured logging

**Paths:** `SS3D.Logging`

Serilog-based structured logging with mandatory sender + context enum, namespace-level filtering
via `LogSettings` ScriptableObject, Unity console + file sinks, and client-ID enrichment for
multiplayer. Recent work replaced remaining `Debug.Log` calls and reduced startup noise.

### Player body / animation foundation

**Paths:** `SS3D.Systems.Entities.Humanoid.Body`, `Assets/Art/Animations/`

Body-state-driven humanoid animation replacing thin Speed-blend + ad-hoc Animator calls:

- **`HumanoidBodyStateMachine`** + packed **`BodyAnimationSnapshot`** SyncVar — authoritative body /
  combat presentation state
- **`AnimationOrchestrator`** — snapshot → Animator parameters (`CombatStance`, `VelX`/`VelZ`,
  triggers); walk/run blend easing
- **Combat stance packs** — Peaceful (Locomotion Pack), Melee (Pro Melee Axe), Ranged (Basic Shooter)
  FreeformCartesian2D blends; rebuild via **SS3D → Animation → Rebuild Combat Stance Blend Trees**
- **`HumanoidIkController`** — combat look-at IK for aim yaw/pitch; melee swing on upper-body layer
- **`HumanoidCombatController`** / inventory bridge — Melee/Ranged stance from held-item traits;
  `C` toggles Peaceful ↔ combat stance

This is **stance and locomotion presentation** — hit resolution, windup, and damage live under
[Combat](#combat-phase-0-1-clean-slate-melee). Blend/timing polish remains animator-owned where
possible.

Architecture: [2026-07_player-body-animation.md](archive/architecture/2026-07_player-body-animation.md).
Plan: [animation_system_design_250de599.plan.md](archive/plans/animation_system_design_250de599.plan.md).
System map: [entities.md](architecture/systems/entities.md).

Merged from `feature/animation-system` onto `develop`.

**Post-merge polish** (PR #17): removed a duplicate `_bodyStateMachine` component on the ghost
controller.

**Animation polish** (PR #22, `cursor/male-injured-pack-mixamo-clips`): melee Upper Body torso
authority + Mixamo swing variants; injured limp gait from Male Injured Pack (all stances);
left-hand Upper Body mirror; arm-injury additive overlay; combat gait sync with world speed.
Shelved Misc / Probably Not clip packs for future collapse/presentation work.
Architecture: [2026-07_animation-polish.md](archive/architecture/2026-07_animation-polish.md).

**Body presentation authority** (PR #42): `Ragdoll` is the sole writer of replicated
`BodyPresentationState` (Locomotion / Collapsed / Dead); Health emits intent only via
`BodyPresentationIntent.FromSnapshot`. Fixes dual death/unconscious RPCs, upright walk-cycle
corpses, and cardiac-without-ragdoll. Critical collapse extended in PR #62
(`HealthState.Critical`). Same PR #42 banks an AssetProvider UniTask `LogAssert` pitfall.
Architecture: [2026-07_body-presentation-authority.md](archive/architecture/2026-07_body-presentation-authority.md).
System map: [entities.md](architecture/systems/entities.md).

### Screen-space effects

**Paths:** `SS3D.Systems.ScreenEffects`

Client-only URP Volume overlays for diegetic feedback from [main-hud.md](design/main-hud.md) §5:

- Sustained intensities (`ScreenEffectType`): heat/cold, fire/freezing particles, low oxygen,
  dying/critical, blood-loss tunnel vision, concussion, unconscious
- Momentary melee **hit flash** via `TriggerHitFlash`
- F2 debug menu (lazy UI) and console commands (`screeneffect`, hit-flash)
- Self-bootstraps at runtime (not Boot.unity) so it can land without scene YAML edits

**Health wiring shipped** (with the health rewrite): local-owner `HealthScreenEffectMapper` drives
dying/blood-loss/oxy/concussion/unconscious from `HealthSnapshot`; hit flash on `ApplyDamage`.
**Turf temp/fire/frost** wired from synced `HealthSnapshot.Environment` via `AtmosScreenEffectMapper`
(PR #62 / health-env-feel). Blast flash fired by structural-destruction VFX (distance-gated).

Merged via PR #6; health wiring via PR #12; env/turf via PR #62. Architecture:
[2026-07_screen-space-effects.md](archive/architecture/2026-07_screen-space-effects.md).
System map: [screen-effects.md](architecture/systems/screen-effects.md).

### Health rewrite

**Paths:** `SS3D.Systems.Health`

Clean-slate rewrite per [health.md](design/health.md) / [health_implementation_plan.md](plans/health_implementation_plan.md):

- **Phases 1–5b shipped** — bleeding + bandage + VFX, asset-backed organs / pools / cardiac arrest,
  multi-threshold critical + defibrillation window, BodyParts zone resolution, field treatments
  (burn dressing, splint, O2, CPR, transfusion, antitoxin), limb severing (anatomy hide, world drops,
  head mind-swap)
- **`HumanHealthController`** + packed **`HealthSnapshot`** SyncVar; organ tick via
  `HealthSimulation` / `OrganSimulation`
- **Bleed VFX** — bone-anchored particles + URP Decal floor/body marks scaled from synced bleed rates
- **Collapse / death presentation** — `Ragdoll` owns replicated body presentation (PR #42; interim
  multi-writer RPCs removed — see
  [2026-07_body-presentation-authority.md](archive/architecture/2026-07_body-presentation-authority.md))
- **Phase 0d** strips legacy health components from `Human.prefab` (do not dual-stack)
- **Alert stack wiring** (PR #27) — `HealthAlertStackMapper` drives Bleeding / Dying / CardiacArrest /
  LowOxygen icons on the Main HUD from `HealthSnapshot`; atmos alerts via `AtmosAlertStackMapper`
- **Env + feel** (PR #62) — critical ragdoll on `HealthState.Critical`; turf→body O₂/CO₂ exchange,
  PO₂ breathability, hot/cold/fire/pressure exposure; blood impact spray; personal heartbeat/
  breathing + alert beep SFX — see [2026-07_health-env-feel.md](archive/architecture/2026-07_health-env-feel.md)
- **Examine health lines** (PR #68) — Tier 0/1 text on Shift examine; vitals UITK cluster still open

Deferred: vitals HUD / examine-self (Phase 6 remainder), stamina bridge (Phase 7a partially via
inventory), armor environmental seal / internals, audible air-alarm cues, virology/chemistry
modifiers.

Merged via PR #12 (`health-rewrite`); alert stack via PR #27; body presentation via PR #42;
env-feel via PR #62; examine health via PR #68.
System map: [health.md](architecture/systems/health.md).

### Combat — melee, ranged, armor, stamina

**Paths:** `SS3D.Systems.Combat`

Clean-slate rewrite per [combat.md](design/combat.md) /
[combat_implementation_plan.md](plans/combat_implementation_plan.md) (melee via PR #23;
ranged/docs via PR #47; armor via PR #50; stamina drains via PR #51; M1p feel via PR #63).
Marks the earlier thin melee vertical slice condemned and replaces the hit path:

- **Harm primary** routes through `CombatInteractionNetwork` (PR #69 sibling of
  `InteractionController`): held `RangedWeaponItemExtension` → hitscan fire; else melee swing
- **Melee (Phase 0–1):** always swings (windup → connect → recovery + stamina); zone damage at
  connect from synced camera aim; fists / improvised / crowbar·hatchet·knife; structural Turf
  via `MeleeStructuralHitResolver` when no living target
- **Ranged (Phase 3 + M1p):** M4 hitscan inside accuracy cone (base + recoil + movement bloom +
  range + exertion); mag/cooldown/reload; muzzle flash + bullet-hole decals; SS14 fire/surface
  audio; Fire/Reload oneshots over Rifle Aim Idle; two-hand right-grip + reserved off-hand;
  `rangeddebug` toggle for gold/grey spheres (off by default). Aim IK deferred
- **Armor (Phase 5):** per-zone flat brute/burn absorption + integrity on worn pieces
  (`ArmorItemExtension` / `ArmorSimulation` inside `HumanHealthController.ApplyDamage`);
  environmental seal/breach deferred
- **Stamina drains (Phase 4):** swing + fire deplete stamina; exertion widens accuracy cone and
  slows melee windup/recovery (block drain deferred)
- **Intent ↔ stance:** `F` / HUD intent chip toggles Help/Harm; Harm enters combat stance;
  Harm is combat-exclusive (no fall-through to Drop / MI mid-fight)
- **HUD feedback:** zone reticle lock-on/flash; ranged bloom tracks spread; connect flash
- Admin test tool: console `spawndummy` (mindless Human ahead; equips `JumpsuitSecurity` for armor)

Deferred: disarm/grab, blocking, projectile/thrown, armor wear visuals, environmental seal.

Plan: [combat_implementation_plan.md](plans/combat_implementation_plan.md).
System map: [combat.md](architecture/systems/combat.md).

### Structural destruction

**Paths:** `SS3D.Systems.StructuralDestruction` (and tile / health / examine / screen-effects touch)

MVP1 M2 station integrity + blast (PR #46, `cursor/structural-damage-plan`; plenum-hop fix PR #48):

- Per-tile integrity SyncVars; Cracked opens airtightness; Destroyed clears tile
- Melee structural force + blast BFS (`BlastResolutionService`) with crew chest brute on visited tiles
- Presentation: integrity tint + optional WindLight; examine Damaged/Cracked sections; blast VFX /
  screen flash
- Consoles: `hurtstructure`, `blast`
- Explosives content: timed C4 and X4 charges and the Syndicate minibomb; blast damage to flooring
  and hull plating; bodies thrown away from blasts

Deferred: debris spawn, true local-region Area remesh.
Architecture: [2026-07_structural-destruction.md](architecture/2026-07_structural-destruction.md).
System map: [structural-destruction.md](architecture/systems/structural-destruction.md).

### Vision / FOV

**Paths:** `SS3D.Systems.Vision`, `SS3D.Rendering.URP.VisionRendererFeature`

Client grid-based FOV / fog-of-war as a **hard black mask** (not soft fog):

- **`VisionSubSystem`** — `RaycastCommand` batch from `Entity.ViewPoint` → `_VisionMap`
- **`VisionRendererFeature`** composites the mask; unseen areas are fully opaque black
- Rays skip furniture/props until the nearest wall/door; triggers and inventory preview cameras
  ignored so open airlocks and dense props do not leak or stripe vision

Lives under the rendering system map ([rendering.md](architecture/systems/rendering.md)).
Merged via PR #11 (supersedes stalled `feature/vision-urp-tilemap`). Hot-path GC/CPU cut in PR #28.

### Main HUD (UI Toolkit)

**Paths:** `SS3D.UI.MainHud`, `Assets/Content/Systems/UI/MainHud/`

Partial player HUD overlay from [main-hud.md](design/main-hud.md):

- Worn equipment (incl. gloves), gear strip, hands, intent Help/Harm (**F** key)
- Hand-well clicks → `HumanInventory.ActivateHand`; shown only after local spawn
- Legacy inventory / stamina-bar uGUI disabled (condemned); hold-to-self-examine deferred
  (Shift self-examine via examine UITK shipped instead)
- **`MainHudAssetCatalog`** via `Resources.Load` so standalone builds resolve UITK assets
  (same path-catalog pattern as machine UI — shared helper deferred under UiShell; Addressables
  migration still open)
- Registers with `InputInterface` so pointer-over-HUD blocks world examine/selection
- **Alert icon stack** (PR #27) — top-right hazard icons (14 types) with warning/critical styling;
  health-backed Bleeding / Dying / CardiacArrest / LowOxygen via `HealthAlertStackMapper`;
  atmos-backed alerts via `AtmosAlertStackMapper`; F4 debug + `alertstack` console
- **Zone reticle** — melee lock-on/connect flash + ranged bloom (exertion-aware)

Merged via PR #10; alert stack via PR #27. System map:
[inventory.md](architecture/systems/inventory.md) (player HUD slice).

### Inventory and storage redesign

**Paths:** `SS3D.Systems.Inventory`, `Assets/Content/Systems/UI/MainHud/StoragePanel/`

Clean-slate rewrite per [inventory-storage.md](design/inventory-storage.md), replacing the
placeholder inventory data model:

- **Container primitive** — real weight (recursive through nested containers), a five-tier
  size-class fit-check, stackable identical items, minimal ID-locked containers
- **On-demand storage panel** — opens per-container from the gear strip or a world object; drag-drop
  between panels and hands reuses the Tier 3 combine convention; locker access gated on the door
- **Main HUD wiring** — worn item names on HUD slots, drag-to-world drop, backpack/toolbelt/locker
  hookup, gear-strip PDA well renamed to Pocket
- **Old UI purge** — legacy inventory uGUI fully removed, not just disabled
- **Stamina bridge (Phase 7a)** — `CarriedWeight` feeds the stamina regen function

Merged via PR #16 (`claude/inventory-architecture-redesign-6q9527`). Architecture:
[2026-07_inventory-storage-redesign.md](archive/architecture/2026-07_inventory-storage-redesign.md).
System map: [inventory.md](architecture/systems/inventory.md).

**Clothing world presentation** (PR #50): single NetworkObject jumpsuit with
`ClothingItemPresentation` folded child for world/hand vs worn body mesh; orphan folded prefabs
removed. Plan: [clothing_world_presentation.plan.md](archive/plans/clothing_world_presentation.plan.md).

**Bright ObjectIcon HUD icons** (PR #65): full-toon inventory previews with white outlines via
`IconPreviewGenerator` / ObjectIcon; item world rest orientation preserved (PR #64).

Deferred: Play Mode verification, uniform-specific pocket variance, standalone "quick loot everything."

### Comms — local speech, feed, and crowd cap

**Paths:** `SS3D.Systems.Chat` / `CommsSubSystem`, Main HUD + UiShell comms UI

Vertical slice of [comms.md](design/comms.md):

- **Local speech chips** — world-anchored subtitle bubbles at the speaker's head, not a chat-log line
- **Distance/occlusion tiers** and **crowd cap** — nearest speakers get bubbles, the rest compress
  into a "+N more talking nearby" chip
- **T-compose** — on-demand input box; Tab cycles Local + Eng/Sec; slash prefixes `/eng` `/sec`
  `/announce` (interim vs design channel radial)
- **Non-diegetic feed** (PRs #59 / #60) — left radio cards + top ALL-STATION banner; announce SFX;
  legacy `ChatSubSystem` purged — see
  [2026-07_comms-non-diegetic-feed.md](archive/architecture/2026-07_comms-non-diegetic-feed.md)

Merged via PR #20 (`claude/crowd-cap-chat-plan-o8v03z`); feed via PRs #59 / #60. System map:
[chat-audio-screens.md](architecture/systems/chat-audio-screens.md).

Deferred: headset/ID channel gating, whisper/shout distance tiers, PDA history log, voice
compatibility, design-faithful channel radial.

### Audio foundation

**Paths:** `SS3D.Systems.Audio`

PR #54 (`claude/sound-effects-plan-w8m93y`) ships Phases 1–4 of
[2026-07_audio-foundation.md](architecture/2026-07_audio-foundation.md):

- Client-local **SFX occlusion** via shared `LineOfSight` (server still triggers playback)
- **Area ambience** crossfade (replaces legacy knobbed `AmbienceHandler`)
- **Personal** heartbeat / breathing (health + stamina max-merge) and alert cues
- Footsteps and diegetic pool consumers gain occlusion for free

[ambience-and-impact-audio](archive/architecture/2026-11_ambience-and-impact-audio.md) adds material
impacts, blasts, item collisions, machine hums, department room tone, wind and sound that thins with
the air. Protocol 18.

Deferred: Phase 0 content audit leftovers, Phase 5 music / volume-category polish.
System map: [audio.md](architecture/systems/audio.md) (split from chat-audio-screens).

### Solar generation

**Paths:** `SS3D.Systems.Electricity` (`SolarPanel`, `SolarTrackingBeacon`, `SolarCycle`)

PR #67 ships design [electricity.md](design/electricity.md) §2 solar as HV-backbone producers:

- Stub `SolarCycle` (azimuth / day-night / eclipse); panels aim via Rotate interaction; tracking
  beacon auto-aims nearby panels
- Prefab recipes under `SS3D/Electricity/Run Content Prefab Recipes`

Deferred: real celestial / station rotation, reactor, map-authored farms, MI panels.
Architecture: [2026-07_solar-generation.md](archive/architecture/2026-07_solar-generation.md).
System map: [electricity.md](architecture/systems/electricity.md).

### Disposal — item network

**Paths:** `SS3D.Systems.Disposal`

Physical BFS-routed pipe network per [disposal.md](design/disposal.md), replacing BYOND's
instant off-map transit:

- **Pipe network routing** — BFS connectivity across placed pipe segments, same technique class as
  Area's flood fill and electricity's backbone
- **Chute entry** — SizeClass gate (shares inventory-storage's five-tier field, not a bespoke check)
- **Transit and outlets** — item travels the routed path at a real pace; outlet eject animation,
  spit-delay timing, and main-outlet arrival fixed for items with no wired space-ejection point
- **Interaction polish** — Dispose click priority, recycle icon registered

Merged via PR #19 (`claude/disposal-implementation-plan-4wh4hr`). Architecture:
[2026-07_disposal-item-network.md](archive/architecture/2026-07_disposal-item-network.md).
System map: [disposal.md](architecture/systems/disposal.md).

Deferred: pipe crafting/placement UI (now designed in
[creative-mode.md](design/creative-mode.md) §4), the Cargo export hook, Phase 2 player transit.

### Input arbitration

**Paths:** `SS3D.Systems.Inputs`

Handle-based input ownership replacing the global action refcount:

- **`InputArbiter`** — contexts + suppressions return `IInputHandle`; dispose removes exactly that
  request; single writer of `InputAction.enabled`
- **`InputInterface`** — unified pointer-over-UI spanning uGUI EventSystem and UI Toolkit panels
- Gameplay and UI callers migrated off legacy `UnityEngine.Input` polling and unbalanced
  Toggle* counters

EditMode tests for the pure arbiter. Merged via PRs #8 / #9. Architecture:
[2026-07_input-arbitration.md](archive/architecture/2026-07_input-arbitration.md).
System map: [inputs.md](architecture/systems/inputs.md).

**Default input scheme** ([2026-07_default-input-scheme.md](archive/architecture/2026-07_default-input-scheme.md),
with examine PR #68): Help/Harm intent = **F** (frees C); resolves Shift/C/E conflicts with
character examine / cancel / reload.

### Headless dedicated server

**Paths:** Editor build scripts, `UNITY_SERVER` runtime guards, Docker / launch scripts

Genuine Server-subtarget Linux build (not a client launched with `-serveronly`):

- Editor menus / CI (`SS3D/Build/Dedicated Server`, `Client`) and
  `develop-release.yml` Linux build / nightly / opt-in smoke (also `multiplayer-smoke-test.yml`)
- Runtime skips Intro/Launcher, defaults `NetworkType.DedicatedServer`, disables cameras/audio and
  spawned renderers/lights under `#if UNITY_SERVER`
- Docker Compose + start scripts under `Builds/`
- Windows release zips seed **Config/Tilemaps** so packaged playtests load maps (PR #56)

Known gaps: selection outline and drop interaction against a real client still broken (not
root-caused). An automated multiplayer harness now exists — see below.
Host loopback / interaction outline player-build fix: PR #58.

Architecture: [2026-07_headless-dedicated-server.md](archive/architecture/2026-07_headless-dedicated-server.md).

### Multiplayer test harness

**Paths:** Headless multiplayer test scripts, dedicated-server + client launch tooling

Replaces the brittle PlayMode-based multiplayer test with a real headless harness that boots a
dedicated server and one or more real clients:

- Automated round-start auth and permission seeding for smoke runs
- Log-signal detection for round lifecycle (replacing timing-based waits) plus a combined build menu
- **Reconnect scenario** (PR #35) — disconnect ownership cleanup, reconnecting a player to their own
  body (`claude/player-join-leave-arch-z5zzmm`)
- Atmospherics client-visualization sync regression coverage (PR #34)

Merged via PR #15 (`claude/multiplayer-test-harness-l90540`); reconnect via PR #35. Architecture:
[2026-07_multiplayer-test-harness.md](archive/architecture/2026-07_multiplayer-test-harness.md).

Self-hosted CI effort (PR #49 docs; TomNAS prefer in PRs #55 / editmode workflow):
[2026-07_multiplayer-testing-self-hosted-ci.md](architecture/2026-07_multiplayer-testing-self-hosted-ci.md)
— Phase 0 online; warm-run smoke proof and Phases 1–2 still open.

Known gaps: mouse/screen-space interaction and pocket/container round-trip regressions not yet
covered.

### Session / world lifecycle

**Paths:** `SS3D.Networking`, `SS3D.Core`, `SystemsBootstrap`, `NetworkSystemsHub`

Replaces ad-hoc init/reconnect bandages with explicit contracts (PRs #37–#38; Boot disconnect storm
fix in PR #36):

- **Session lifecycle FSM** — named `SessionState` for connect / disconnect / recovery
- **World readiness graph** — prerequisites for round start modeled instead of timing hopes
- **`NetworkSystemsHub`** — Resources prefab spawned Online for world/session `NetworkSubSystem`s;
  process-wide services live in `SystemsBootstrap` (DDOL); Boot Persistent Systems / Game Systems
  SubSystem dumps emptied (Phase 3h)
- **Disconnect storm fix** — `ClientConnectionRecovery` arms `Empty.unity` as FishNet offline after
  first successful connect; `CanStartNetworkSession` guards re-entrant starts; Intro Retry rewired

Residual: some content prefabs still carry `SubSystem`s that register when Game loads before the hub
is Online (cameras, radial/armed overlays, Map Editor) — consumers use `TryGet` / lazy resolve.
Architecture: [2026-07_session-world-lifecycle.md](archive/architecture/2026-07_session-world-lifecycle.md).
System map: [networking-session.md](architecture/systems/networking-session.md).

**Client spawn / HUD polish** (PR #66): Mix_Floating AOI race, NetworkTransform drop gravity,
Main HUD SyncList icon refresh, airlock ClosestPoint, hand-grip facing, clothing re-equip /
rejoin lobby fixes.

### Lobby (UI Toolkit redesign)

**Paths:** `SS3D.UI.Lobby`, `SS3D.Systems.Rounds`

Replaces upstream's uGUI lobby and character customizer with a UI Toolkit shell on the shared
`UiShellSubSystem` Modal layer (PR #84, Phases A–F):

- **Character Creator**: guided steps with a live preview booth; name, body, hair/beard/brow colours,
  skin and eyes for Human (other species disabled); local character slots persist across restarts
  and are applied to the spawned body
- **Jobs and antagonists**: per-job preferences resolved in one pass at round start, plus latejoin;
  antagonist opt-ins are stored but no gamemode consumes them yet
- **Lobby chat**: networked OOC / LOOC / Admin channels with server-side identity and rate limits
- **Server Settings / Server Info**: admin map/mode selection and round controls; live server name,
  MOTD, build, uptime and git change log
- **Legacy purge**: the uGUI lobby, old customizer canvas and `GameScreens` system are deleted

Architecture: [2026-08_lobby-uitk-redesign.md](archive/architecture/2026-08_lobby-uitk-redesign.md).
System map: [rounds-lobby.md](architecture/systems/rounds-lobby.md).

### Agent-first composition, UiShell, and UI path catalogs

Policy + first catalog wedges so agents can ship UI without scene/prefab YAML edits:

- **[Agent-first composition](architecture/2026-07_agent-first-composition.md)** — no Boot/Game
  registration for new features; no hand-edits to mega-prefabs; UITK + catalog/path pattern only;
  condemned uGUI must be replaced, not migrated
- **[Editor tooling tiers](architecture/2026-07_editor-tooling-tiers.md)** (PR #57) — A repeatable
  menus / B recipe aggregators (no one-shot MenuItems) / C time-boxed migrations
- **Machine UI path catalog** ([2026-07_mi-path-catalog.md](archive/architecture/2026-07_mi-path-catalog.md))
  — `MachineUiAssetCatalog` loads templates by path for builds
- **UiShell Phase 0–1** (PR #32) — `SS3D.UI.Shell` (`UiShellSubSystem`, layer stack, `PanelAnimator`,
  `UiAssetCatalogBase`); radial menu + armed overlay migrated onto shared `UIDocument` overlay layer.
  Main HUD / Storage / MI migration still open —
  [2026-07_ui-shell-consolidation.md](architecture/2026-07_ui-shell-consolidation.md),
  [ui-shell.md](architecture/systems/ui-shell.md)

### Human.prefab decomposition (Phase 0 + recipe tools)

**Paths:** `Human.prefab` Editor recipes under `Assets/Scripts/SS3D/.../Editor/`

Pays down TECH_DEBT §1.1 (PR #31): remove dev-only hack components; establish `PrefabUtility` recipe
convention (`HumanPrefabHygiene`, `BodyPartContainerInteractiveStrip`, hands wiring recipe);
integrity tests prevent regression. Phase 1 organ-extraction deprioritized; remaining Phase 3
domains strip-and-rewire as their own redesigns touch entity wiring. Effort:
[2026-07_human-prefab-decomposition.md](archive/architecture/2026-07_human-prefab-decomposition.md).

### Hot-path performance

**Paths:** atmospherics GPU upload, pipe networks, substances, tile coords, Vision, interactions

PR #13 cut per-tick GC on hot sim paths: `TileCoord` equality for dictionary keys, atmos upload
allocation avoidance, pipe `GetAllPlacedObject` list churn, `SubstanceContainer.AsReadOnly` on bleed
ticks. PR #28 cut Vision FOV LateUpdate CPU/GC and interaction outline discovery GC. Pitfalls
recorded on the atmospherics / tile / substances / vision system maps.

---

## Design specs (future direction)

[design/](design/) holds the owner-authored gameplay direction. Which specs are built, partly built
or still unbuilt is tracked in one place: the coverage table in
[architecture/INDEX.md](architecture/INDEX.md). What to build next is in
[milestones/INDEX.md](milestones/INDEX.md).

Feature branches not yet on `develop`: `git branch -r --no-merged origin/develop`.

---

## What upstream still has that we don't automatically inherit

- Official releases and the [ss3d.space](https://ss3d.space/) download channel (this fork's
  prereleases are manual/nightly GitHub Actions cuts, not the same channel)
- GitBook documentation and devblogs
- Active milestone/project board automation
- Discord CI notifications
- Whatever lands on `upstream/develop` after our fork point (currently **1** commit behind as of
  2026-10-31)

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

This is a divergence log, not a changelog: describe how the fork differs from upstream, per system.
Update when:

- A system ships on `develop` that upstream lacks (add or update its section and its row under
  **Shipped gameplay**)
- Unity, toolchain, CI or repo presentation changes
- Upstream moves: `git fetch upstream`, then refresh the counts in **At a glance**

Agents may update this file. Bump **Last updated** at the top.
