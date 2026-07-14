# Architecture index

Navigation hub for agents. Read this before broad code search. Open the relevant [system map](systems/) for entry points and key files.

Authoring conventions: [Documents/SKILL.md](../SKILL.md). Agent rules: [AGENTS.md](../../AGENTS.md).

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
| Entities | [entities](systems/entities.md) | stub | Humanoids, minds, entity spawning |
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
| [2026-07_machine-interface-phase3-smes-generalization](2026-07_machine-interface-phase3-smes-generalization.md) | shipped (minor cleanup remaining) |
| [2026-07_diegetic-screen-ui-framework](2026-07_diegetic-screen-ui-framework.md) | shipped |
| [2026-07_interaction-system-hardening](2026-07_interaction-system-hardening.md) | shipped |
| [2026-07_area-foundation](2026-07_area-foundation.md) | shipped (deferred: live mutation recompute, editor merge/split) |
| [2026-07_atmos-ecs-foundation](2026-07_atmos-ecs-foundation.md) | shipped (deferred: liquid/solid phase, pipes, pumps, client VFX sync) |
| [2026-07_atmos-client-visualization-sync](2026-07_atmos-client-visualization-sync.md) | planned |

## Implementation plans

Temporary working plans in [Documents/plans/](../plans/). Update todos when work ships.

| Plan | Topic |
|------|-------|
| [examine_localization_design_5ca361a6.plan.md](../plans/examine_localization_design_5ca361a6.plan.md) | Examine localization migration |
| [radial_menu_implementation_5a83bdf9.plan.md](../plans/radial_menu_implementation_5a83bdf9.plan.md) | Three-tier radial interaction menu |
| [interaction_system_improvements_9e14ae22.plan.md](../plans/interaction_system_improvements_9e14ae22.plan.md) | Interaction system hardening |
| [areas_implementation_plan_c0639343.plan.md](../plans/areas_implementation_plan_c0639343.plan.md) | APC-seeded areas, flood-fill, power/lighting follow-ups |
| [electricity_kwh_foundation_917ccdbc.plan.md](../plans/electricity_kwh_foundation_917ccdbc.plan.md) | kWh storage, priority shedding, HV cable grid rules |
| [persistence_architecture_design_2fe61864.plan.md](../plans/persistence_architecture_design_2fe61864.plan.md) | Layered persistence framework; station templates, server meta, round snapshots |

## Design specs (read-only)

Gameplay specs in [Documents/design/](../design/) — owner-maintained. Agents link, never edit.

## Reference (non-system)

| Resource | Path | Use when |
|----------|------|----------|
| Art asset index | [art-asset-index.md](../art-asset-index.md) | Locating or importing art from SS3D-Art |
| Available for import | [art-available-for-import.json](../art-available-for-import.json) | Finding game-ready art not yet in `Assets/Art/` |
| UI icon index | [icon-index.md](../icon-index.md) | Finding external game-icons SVGs for UI work |
