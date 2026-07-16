# Architecture index

Navigation hub for agents. Read this before broad code search. Open the relevant [system map](systems/) for entry points and key files.

Authoring conventions: [Documents/SKILL.md](../SKILL.md). Agent rules: [AGENTS.md](../../AGENTS.md).

## Coverage table

One row per gameplay domain that has (or should eventually have) a design doc. **Design**
links to [`Documents/design/`](../design/) (owner-authored spec). **Architecture** links to
the effort doc(s) in this directory that implement it, or "none yet." **System map** links
to [`systems/`](systems/), or "none yet."

This is the answer to "what's left" at the domain level: a design with no architecture
entry is designed but unbuilt; a domain with no design entry hasn't been designed at all.
Feature-level gaps *within* an already-designed system stay in that design doc's own
`§Out of scope for this pass` — this table doesn't duplicate those. Update as part of
`update-system-docs`.

| Domain | Design | Architecture | System map |
|---|---|---|---|
| main-hud | [main-hud.md](../design/main-hud.md) — active | [phase1-foundation](2026-07_machine-interface-phase1-foundation.md), [phase2-apc-networking](2026-07_machine-interface-phase2-apc-networking.md), [phase3-smes-generalization](2026-07_machine-interface-phase3-smes-generalization.md), [diegetic-screen-ui-framework](2026-07_diegetic-screen-ui-framework.md), [mi-area-electricity-debt](2026-07_mi-area-electricity-debt.md) — all shipped | none yet (see [machine-interface](systems/machine-interface.md) for the built surface) |
| comms | [comms.md](../design/comms.md) — active | [phase1-foundation](2026-07_machine-interface-phase1-foundation.md) — shipped (operator feedback conventions only) | [chat-audio-screens](systems/chat-audio-screens.md) — stub |
| area | [area.md](../design/area.md) — active | [area-foundation](2026-07_area-foundation.md) — shipped (partial: APC-seeded variant; live mutation recompute and editor merge/split deferred) | [area](systems/area.md) — partial |
| hacking-interface | [hacking-interface.md](../design/hacking-interface.md) — active | none yet | none yet |
| combat | [combat.md](../design/combat.md) — active | [player-body-animation](2026-07_player-body-animation.md) — shipped (stance/locomotion foundation only; combat.md not implemented) | [combat](systems/combat.md) — stub |
| stamina | [stamina.md](../design/stamina.md) — active | none yet | none yet |
| health | [health.md](../design/health.md) — active | none yet | [health](systems/health.md) — partial |
| armor | [armor.md](../design/armor.md) — active | none yet | none yet |
| inventory-storage | [inventory-storage.md](../design/inventory-storage.md) — active | none yet | [inventory](systems/inventory.md) — partial |
| examine | [examine.md](../design/examine.md) — active | none yet | [examine](systems/examine.md) — shipped |
| crafting | [crafting.md](../design/crafting.md) — active | none yet | [crafting](systems/crafting.md) — stub |
| death-cloning-respawn | [death-cloning-respawn.md](../design/death-cloning-respawn.md) — active | none yet | none yet |
| surgery | [surgery.md](../design/surgery.md) — active | none yet | none yet |
| lobby | [lobby.md](../design/lobby.md) — active | none yet | [rounds-lobby](systems/rounds-lobby.md) — shipped |
| round-config | [round-config.md](../design/round-config.md) — active | none yet | [rounds-lobby](systems/rounds-lobby.md) — shipped |
| round-end | none yet — see migration note below | none yet | none yet |
| observer | [observer.md](../design/observer.md) — active | none yet | none yet |
| electricity | [electricity.md](../design/electricity.md) — active | none yet (electricity system is touched by [mi-area-electricity-debt](2026-07_mi-area-electricity-debt.md), but that effort implements area.md/main-hud.md, not electricity.md) | [electricity](systems/electricity.md) — partial |
| pda | [pda.md](../design/pda.md) — active | none yet | [inventory](systems/inventory.md) — partial |
| cargo | [cargo.md](../design/cargo.md) — active | none yet | none yet |
| disposal | [disposal.md](../design/disposal.md) — active | none yet | none yet |
| id-access | [id-access.md](../design/id-access.md) — active | none yet | [id-access](systems/id-access.md) — partial |
| virology | [virology.md](../design/virology.md) — active | none yet | none yet |
| atmospherics | [atmospherics.md](../design/atmospherics.md) — active | [atmos-ecs-foundation](2026-07_atmos-ecs-foundation.md) — shipped (partial); [atmos-client-visualization-sync](2026-07_atmos-client-visualization-sync.md) — planned | [atmospherics](systems/atmospherics.md) — partial |
| chemistry | [chemistry.md](../design/chemistry.md) — active | none yet | [substances](systems/substances.md) — partial |
| explosives-destruction | [explosives-destruction.md](../design/explosives-destruction.md) — active | none yet | none yet |
| construction | [construction.md](../design/construction.md) — active | none yet | [tile](systems/tile.md) — shipped |
| creative-mode | [creative-mode.md](../design/creative-mode.md) — active | none yet | none yet |
| rendering-lighting | [rendering-lighting.md](../design/rendering-lighting.md) — active | none yet | [rendering](systems/rendering.md) — partial |
| shuttles | [shuttles.md](../design/shuttles.md) — active | none yet | none yet |
| ai-cyborgs | [ai-cyborgs.md](../design/ai-cyborgs.md) — active | none yet | none yet |
| persistence-save | none yet | none yet | [persistence](systems/persistence.md) — partial (station templates, server meta only, not a design spec) |
| networking | none yet | none yet | [networking-session](systems/networking-session.md) — stub |
| audio | none yet | none yet | none yet |
| onboarding-tutorial | none yet | none yet | none yet |
| antagonist-content | none yet | none yet | none yet |
| rd-material-economy | none yet | none yet | none yet |
| cryogenics | none yet | none yet | none yet |
| admin-tools | none yet | none yet | [ingame-console](systems/ingame-console.md) — partial (dev/admin console, not a design spec) |
| player-accounts | none yet | none yet | none yet |

[2026-07_interaction-system-hardening](2026-07_interaction-system-hardening.md) (shipped) and the infrastructure systems below (core
subsystems, rendering pipeline internals, data/codegen, etc.) aren't gameplay domains with
their own design docs — they support the domains above rather than being one themselves,
so they stay out of this table and live only in the Infrastructure section below.

### Migration note

`design/round-end.md` (round-end/transition) does not exist yet — it's referenced by
`observer.md` §6/§10 but was never authored. It's the one gap surfaced by the design-doc
cleanup pass (stripping `§Prototyping this` and the build-status field from all 29 existing
docs, per `Documents/SKILL.md`); needs the same treatment applied once it's written: no
prototyping section, `Status` line for draft/active/superseded only, cross-refs verified.

## Infrastructure

| System | Map | Status | Summary |
|--------|-----|--------|---------|
| Core / SubSystems | [core-subsystems](systems/core-subsystems.md) | shipped | `SubSystem` / `NetworkSubSystem` base types and `SubSystems` service locator |
| Application | [application](systems/application.md) | stub | App bootstrap and startup |
| Networking (session) | [networking-session](systems/networking-session.md) | stub | FishNet host/join session management |
| Scene management | [scene-management](systems/scene-management.md) | stub | Scene loading and switching |
| Interactions (framework) | [interactions-framework](systems/interactions-framework.md) | shipped | Shared `IInteraction` contracts, pipeline, and wire identifiers |
| Data / codegen | [data-codegen](systems/data-codegen.md) | stub | Asset databases and generated references |
| Persistence | [persistence](systems/persistence.md) | partial | Contributor-based station templates and server meta (permissions, round history) |
| Localization | [localization](systems/localization.md) | partial | `LocalizedTextService` and examine string tables |
| Logging | [logging](systems/logging.md) | shipped | Serilog structured logging |
| Permissions | [permissions](systems/permissions.md) | partial | Admin permission checks; persisted via [persistence](systems/persistence.md) envelope with legacy txt fallback |
| Rendering | [rendering](systems/rendering.md) | partial | URP features: selection pick pass, atmospherics scatter/glow/distortion |

## Gameplay

| System | Map | Status | Summary |
|--------|-----|--------|---------|
| Interactions (runtime) | [interactions-runtime](systems/interactions-runtime.md) | shipped | `InteractionController`, radial menu, armed interactions, outlines |
| Selection | [selection](systems/selection.md) | shipped | Shader-ID mesh picking for interaction targeting |
| Examine | [examine](systems/examine.md) | shipped | Hover tooltips and shift-hold detailed examine |
| Tile / construction | [tile](systems/tile.md) | shipped | Tilemap, adjacency engine, construction, dynamic tile occupancy; build-menu client layer visibility |
| Atmospherics | [atmospherics](systems/atmospherics.md) | partial | ECS turf gas sim; GPU fog/fire on server/host only — client VFX sync planned |
| Area | [area](systems/area.md) | partial | APC-seeded flood-fill, area power, lighting state, wall light switches |
| Electricity | [electricity](systems/electricity.md) | partial | kWh storage, HV cable grid, APC/SMES/generators, consumer visuals |
| Substances | [substances](systems/substances.md) | partial | Containers, transfer interactions, Tier 2 armed proof-of-concept |
| Inventory | [inventory](systems/inventory.md) | partial | Items, containers, hands, ID cards and PDAs |
| Entities | [entities](systems/entities.md) | partial | Humanoids, minds, entity spawning; body-state animation + combat stances |
| Health | [health](systems/health.md) | partial | Body parts, oxygen consumer (design spec not fully implemented) |
| Combat | [combat](systems/combat.md) | stub | Hit interactions (design spec not implemented) |
| Crafting | [crafting](systems/crafting.md) | stub | Recipe crafting |
| Furniture / world objects | [furniture](systems/furniture.md) | partial | Airlocks, vendors, jukebox; power-gated behaviors; vending via machine-interface |
| Rounds / lobby | [rounds-lobby](systems/rounds-lobby.md) | shipped | Round state machine and pre-round lobby UI |
| Gamemodes / roles / traits | [gamemodes-roles-traits](systems/gamemodes-roles-traits.md) | stub | Objectives, job roles, character traits |
| Player control | [player-control](systems/player-control.md) | stub | Player subsystem and input routing |
| Chat / audio / screens | [chat-audio-screens](systems/chat-audio-screens.md) | stub | Chat, audio, camera controllers |
| Machine interface UI | [machine-interface](systems/machine-interface.md) | shipped | Diegetic APC/SMES/atmos panels with server-side ID access gates |
| ID / access | [id-access](systems/id-access.md) | partial | Crew records, credential checks, doors, machine UI gates, dev console helpers |
| Inputs | [inputs](systems/inputs.md) | stub | Input subsystem |
| In-game console | [ingame-console](systems/ingame-console.md) | partial | Dev/admin console; includes ID access test commands |

## Architecture efforts (dated)

Implementation history — not navigation maps. Update `Status` in the header when an effort ships.

| Effort | Status |
|--------|--------|
| [2026-07_machine-interface-phase1-foundation](2026-07_machine-interface-phase1-foundation.md) | shipped |
| [2026-07_machine-interface-phase2-apc-networking](2026-07_machine-interface-phase2-apc-networking.md) | shipped |
| [2026-07_machine-interface-phase3-smes-generalization](2026-07_machine-interface-phase3-smes-generalization.md) | shipped |
| [2026-07_diegetic-screen-ui-framework](2026-07_diegetic-screen-ui-framework.md) | shipped |
| [2026-07_interaction-system-hardening](2026-07_interaction-system-hardening.md) | shipped |
| [2026-07_area-foundation](2026-07_area-foundation.md) | shipped (deferred: live mutation recompute, editor merge/split) |
| [2026-07_atmos-ecs-foundation](2026-07_atmos-ecs-foundation.md) | shipped (deferred: liquid/solid phase, pipes, pumps, client VFX sync) |
| [2026-07_atmos-client-visualization-sync](2026-07_atmos-client-visualization-sync.md) | planned |
| [2026-07_mi-area-electricity-debt](2026-07_mi-area-electricity-debt.md) | shipped |
| [2026-07_player-body-animation](2026-07_player-body-animation.md) | shipped (foundation; blend/timing polish remains) |

## Implementation plans

Temporary working plans in [Documents/plans/](../plans/). Update todos when work ships.

| Plan | Topic |
|------|-------|
| [examine_localization_design_5ca361a6.plan.md](../plans/examine_localization_design_5ca361a6.plan.md) | Examine localization migration |
| [radial_menu_implementation_5a83bdf9.plan.md](../plans/radial_menu_implementation_5a83bdf9.plan.md) | Three-tier radial interaction menu |
| [interaction_system_improvements_9e14ae22.plan.md](../plans/interaction_system_improvements_9e14ae22.plan.md) | Interaction system hardening |
| [areas_implementation_plan_c0639343.plan.md](../plans/areas_implementation_plan_c0639343.plan.md) | APC-seeded areas, flood-fill, power/lighting follow-ups |
| [electricity_kwh_foundation_917ccdbc.plan.md](../plans/electricity_kwh_foundation_917ccdbc.plan.md) | kWh storage, priority shedding, HV cable grid rules |
| [persistence_architecture_design_2fe61864.plan.md](../plans/persistence_architecture_design_2fe61864.plan.md) | Layered persistence framework; Phase 1a/1b shipped, Phase 2 round snapshots pending |
| [animation_system_design_250de599.plan.md](../plans/animation_system_design_250de599.plan.md) | Player body / layered animation foundation |

## Design specs (read-only)

Gameplay specs in [Documents/design/](../design/) — owner-maintained. Agents link, never edit.
See the [coverage table](#coverage-table) above for design/architecture/system-map status
per domain.

## Reference (non-system)

| Resource | Path | Use when |
|----------|------|----------|
| Art asset index | [art-asset-index.md](../art-asset-index.md) | Locating or importing art from SS3D-Art |
| Available for import | [art-available-for-import.json](../art-available-for-import.json) | Finding game-ready art not yet in `Assets/Art/` |
| UI icon index | [icon-index.md](../icon-index.md) | Finding external game-icons SVGs for UI work |
