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
| Localization | [localization](systems/localization.md) | partial | `LocalizedTextService` and examine string tables |
| Logging | [logging](systems/logging.md) | shipped | Serilog structured logging |
| Permissions | [permissions](systems/permissions.md) | stub | Admin permission checks |
| Rendering | [rendering](systems/rendering.md) | partial | URP features including selection pick pass |

## Gameplay

| System | Map | Status | Summary |
|--------|-----|--------|---------|
| Interactions (runtime) | [interactions-runtime](systems/interactions-runtime.md) | shipped | `InteractionController`, radial menu, armed interactions, outlines |
| Selection | [selection](systems/selection.md) | shipped | Shader-ID mesh picking for interaction targeting |
| Examine | [examine](systems/examine.md) | shipped | Hover tooltips and shift-hold detailed examine |
| Tile / construction | [tile](systems/tile.md) | shipped | Tilemap, adjacency engine, construction |
| Area | [area](systems/area.md) | partial | APC-seeded flood-fill, per-tile area ids, save/load (power gating pending) |
| Electricity | [electricity](systems/electricity.md) | partial | Power circuits, APC, SMES, generators |
| Substances | [substances](systems/substances.md) | partial | Containers, transfer interactions, Tier 2 armed proof-of-concept |
| Inventory | [inventory](systems/inventory.md) | stub | Items, containers, ID cards |
| Entities | [entities](systems/entities.md) | stub | Humanoids, minds, entity spawning |
| Health | [health](systems/health.md) | partial | Body parts, oxygen consumer (design spec not fully implemented) |
| Combat | [combat](systems/combat.md) | stub | Hit interactions (design spec not implemented) |
| Crafting | [crafting](systems/crafting.md) | stub | Recipe crafting |
| Furniture / world objects | [furniture](systems/furniture.md) | stub | Airlocks, lockers, vending, disposal |
| Rounds / lobby | [rounds-lobby](systems/rounds-lobby.md) | shipped | Round state machine and pre-round lobby UI |
| Gamemodes / roles / traits | [gamemodes-roles-traits](systems/gamemodes-roles-traits.md) | stub | Objectives, job roles, character traits |
| Player control | [player-control](systems/player-control.md) | stub | Player subsystem and input routing |
| Chat / audio / screens | [chat-audio-screens](systems/chat-audio-screens.md) | stub | Chat, audio, camera controllers |
| Machine interface UI | [machine-interface](systems/machine-interface.md) | shipped | Diegetic UI Toolkit panels for APC/SMES |
| Inputs | [inputs](systems/inputs.md) | stub | Input subsystem |
| In-game console | [ingame-console](systems/ingame-console.md) | stub | Dev/admin console commands |

## Architecture efforts (dated)

Implementation history — not navigation maps. Update `Status` in the header when an effort ships.

| Effort | Status |
|--------|--------|
| [2026-07_machine-interface-phase1-foundation](2026-07_machine-interface-phase1-foundation.md) | shipped |
| [2026-07_machine-interface-phase2-apc-networking](2026-07_machine-interface-phase2-apc-networking.md) | shipped |
| [2026-07_machine-interface-phase3-smes-generalization](2026-07_machine-interface-phase3-smes-generalization.md) | shipped (minor cleanup remaining) |
| [2026-07_interaction-system-hardening](2026-07_interaction-system-hardening.md) | shipped |
| [2026-07_area-foundation](2026-07_area-foundation.md) | in-progress (phases 0–2 shipped) |

## Implementation plans

Temporary working plans in [Documents/plans/](../plans/). Update todos when work ships.

| Plan | Topic |
|------|-------|
| [examine_localization_design_5ca361a6.plan.md](../plans/examine_localization_design_5ca361a6.plan.md) | Examine localization migration |
| [radial_menu_implementation_5a83bdf9.plan.md](../plans/radial_menu_implementation_5a83bdf9.plan.md) | Three-tier radial interaction menu |
| [interaction_system_improvements_9e14ae22.plan.md](../plans/interaction_system_improvements_9e14ae22.plan.md) | Interaction system hardening |
| [areas_implementation_plan_c0639343.plan.md](../plans/areas_implementation_plan_c0639343.plan.md) | APC-seeded areas, flood-fill, power/lighting follow-ups |

## Design specs (read-only)

Gameplay specs in [Documents/design/](../design/) — owner-maintained. Agents link, never edit.
