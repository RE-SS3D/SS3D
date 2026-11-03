# Architecture index

Navigation hub for agents. Read this before broad code search, then open the relevant [system map](systems/) for entry points, key files and Pitfalls.

- Authoring conventions: [Documents/SKILL.md](../SKILL.md). Agent rules: [AGENTS.md](../../AGENTS.md).
- What to build next: [milestones/INDEX.md](../milestones/INDEX.md). Structural risk: [TECH_DEBT.md](TECH_DEBT.md).
- Shipped efforts and completed plans: [Documents/archive/](../archive/). The root `.ignore` hides that folder from searches.

Statuses below are one word. Phase-level detail lives in the linked effort or plan, never here. Update this file as part of `update-system-docs`.

## Coverage table

One row per gameplay domain that has, or should eventually have, a design doc. A design with no architecture entry is designed but unbuilt. A domain with no design entry hasn't been designed. Gaps within a designed system stay in that design doc's `§Out of scope for this pass`.

| Domain | Design | Architecture | System map |
|---|---|---|---|
| admin-tools | [admin-tools](../design/admin-tools.md) | none yet | [ingame-console](systems/ingame-console.md) — partial |
| ai-cyborgs | [ai-cyborgs](../design/ai-cyborgs.md) | none yet | none yet |
| antagonist-content | [antagonist-content](../design/antagonist-content.md) | none yet | [gamemodes-roles-traits](systems/gamemodes-roles-traits.md) — partial |
| area | [area](../design/area.md) | [area-foundation](../archive/architecture/2026-07_area-foundation.md) — shipped | [area](systems/area.md) — partial |
| armor | [armor](../design/armor.md) | [combat plan](../plans/combat_implementation_plan.md) Phase 5 — in-progress | [combat](systems/combat.md) — partial |
| atmospherics | [atmospherics](../design/atmospherics.md) | [atmos-ecs-foundation](../archive/architecture/2026-07_atmos-ecs-foundation.md) — shipped; [atmos-client-visualization-sync](2026-07_atmos-client-visualization-sync.md) — shipped; [metastation-scale-perf](../archive/architecture/2026-07_metastation-scale-perf.md) — shipped | [atmospherics](systems/atmospherics.md) — partial |
| audio | [audio](../design/audio.md) | [audio-foundation](2026-07_audio-foundation.md) — in-progress | [audio](systems/audio.md) — partial |
| cargo | [cargo](../design/cargo.md) | none yet | none yet |
| chemistry | [chemistry](../design/chemistry.md) | [substances-foundation](../archive/architecture/2026-07_substances-foundation.md) — shipped | [substances](systems/substances.md) — partial |
| combat | [combat](../design/combat.md) | [combat plan](../plans/combat_implementation_plan.md) — in-progress; [player-body-animation](../archive/architecture/2026-07_player-body-animation.md), [animation-polish](../archive/architecture/2026-07_animation-polish.md) — shipped | [combat](systems/combat.md) — partial |
| comms | [comms](../design/comms.md) | [comms-non-diegetic-feed](../archive/architecture/2026-07_comms-non-diegetic-feed.md) — shipped | [chat-audio-screens](systems/chat-audio-screens.md) — partial |
| construction | [construction](../design/construction.md) | none yet | [tile](systems/tile.md) — partial |
| crafting | [crafting](../design/crafting.md) | none yet | [crafting](systems/crafting.md) — stub |
| creative-mode | [creative-mode](../design/creative-mode.md) | [map-editor-replacement](../archive/architecture/2026-07_map-editor-replacement.md), [spawn-point-authoring](../archive/architecture/2026-07_spawn-point-authoring.md), [ss13-map-import](../archive/architecture/2026-07_ss13-map-import.md), [ss13-map-import-infrastructure](../archive/architecture/2026-07_ss13-map-import-infrastructure.md) — shipped | [tile](systems/tile.md) — partial; [map-editor-creative-hooks](systems/map-editor-creative-hooks.md) |
| cryogenics | [cryogenics](../design/cryogenics.md) | none yet | none yet |
| death-cloning-respawn | [death-cloning-respawn](../design/death-cloning-respawn.md) | none yet | none yet |
| disposal | [disposal](../design/disposal.md) | [disposal-item-network](../archive/architecture/2026-07_disposal-item-network.md) — shipped | [disposal](systems/disposal.md) — partial |
| electricity | [electricity](../design/electricity.md) | [solar-generation](../archive/architecture/2026-07_solar-generation.md), [mi-area-electricity-debt](../archive/architecture/2026-07_mi-area-electricity-debt.md) — shipped | [electricity](systems/electricity.md) — partial |
| examine | [examine](../design/examine.md) | none yet | [examine](systems/examine.md) — partial |
| explosives-destruction | [explosives-destruction](../design/explosives-destruction.md) | [structural-destruction](2026-07_structural-destruction.md) — in-progress | [structural-destruction](systems/structural-destruction.md) — partial |
| hacking-interface | [hacking-interface](../design/hacking-interface.md) | none yet | none yet |
| health | [health](../design/health.md) | [health plan](../plans/health_implementation_plan.md) — in-progress; [body-presentation-authority](../archive/architecture/2026-07_body-presentation-authority.md), [health-env-feel](../archive/architecture/2026-07_health-env-feel.md) — shipped | [health](systems/health.md) — partial |
| id-access | [id-access](../design/id-access.md) | none yet | [id-access](systems/id-access.md) — partial |
| inventory-storage | [inventory-storage](../design/inventory-storage.md) | [inventory-storage-redesign](../archive/architecture/2026-07_inventory-storage-redesign.md) — shipped | [inventory](systems/inventory.md) — partial |
| lobby | [lobby](../design/lobby.md) | [lobby-uitk-redesign](../archive/architecture/2026-08_lobby-uitk-redesign.md) — shipped | [rounds-lobby](systems/rounds-lobby.md) — partial |
| main-menu | [main-menu](../design/main-menu.md) | none yet | none yet (dev Launcher scene: [networking-session](systems/networking-session.md)) |
| main-hud | [main-hud](../design/main-hud.md) | [machine-interface phase 1](../archive/architecture/2026-07_machine-interface-phase1-foundation.md), [2](../archive/architecture/2026-07_machine-interface-phase2-apc-networking.md), [3](../archive/architecture/2026-07_machine-interface-phase3-smes-generalization.md), [diegetic-screen-ui-framework](../archive/architecture/2026-07_diegetic-screen-ui-framework.md), [mi-path-catalog](../archive/architecture/2026-07_mi-path-catalog.md), [screen-space-effects](../archive/architecture/2026-07_screen-space-effects.md) — shipped; [ui-shell-consolidation](2026-07_ui-shell-consolidation.md) — shipped | [inventory](systems/inventory.md), [machine-interface](systems/machine-interface.md), [screen-effects](systems/screen-effects.md), [ui-shell](systems/ui-shell.md) |
| networking | [networking](../design/networking.md) | [headless-dedicated-server](../archive/architecture/2026-07_headless-dedicated-server.md), [session-world-lifecycle](../archive/architecture/2026-07_session-world-lifecycle.md), [multiplayer-test-harness](../archive/architecture/2026-07_multiplayer-test-harness.md), [ci-develop-release-pipeline](2026-07_ci-develop-release-pipeline.md), [game-versioning](2026-10_game-versioning.md), [test-harness-rework](../archive/architecture/2026-11_test-harness-rework.md) — shipped; [multiplayer-testing-self-hosted-ci](2026-07_multiplayer-testing-self-hosted-ci.md) — in-progress | [networking-session](systems/networking-session.md) — partial |
| objectives | [objectives](../design/objectives.md) | none yet | none yet |
| observer | [observer](../design/observer.md) | none yet | none yet |
| onboarding-tutorial | none yet | none yet | none yet |
| pda | [pda](../design/pda.md) | none yet | [inventory](systems/inventory.md) — partial |
| persistence-save | [persistence-save](../design/persistence-save.md) | [persistence plan](../plans/persistence_architecture_design_2fe61864.plan.md) — in-progress | [persistence](systems/persistence.md) — partial |
| player-accounts | [player-accounts](../design/player-accounts.md) | none yet | none yet |
| rd-material-economy | none yet | none yet | none yet |
| rendering-lighting | [rendering-lighting](../design/rendering-lighting.md) | [srp-batcher-gpu-instancing](2026-07_srp-batcher-gpu-instancing.md) — shipped; [urp-lighting-look-polish](2026-07_urp-lighting-look-polish.md) — planned | [rendering](systems/rendering.md) — partial |
| round-config | [round-config](../design/round-config.md) | [lobby-uitk-redesign](../archive/architecture/2026-08_lobby-uitk-redesign.md) — shipped | [rounds-lobby](systems/rounds-lobby.md) — partial |
| round-end | [round-end](../design/round-end.md) | none yet | none yet |
| sandbox-server | [sandbox-server](../design/sandbox-server.md) | [sandbox-server](2026-11_sandbox-server.md) — in-progress | [rounds-lobby](systems/rounds-lobby.md), [map-editor-creative-hooks](systems/map-editor-creative-hooks.md), [entities](systems/entities.md) — partial |
| shuttles | [shuttles](../design/shuttles.md) | [shuttle plan](../plans/shuttle_system_design_a3dd2e04.plan.md) — planned | none yet |
| stamina | [stamina](../design/stamina.md) | [inventory-storage-redesign](../archive/architecture/2026-07_inventory-storage-redesign.md) — shipped | [stamina](systems/stamina.md) — partial |
| surgery | [surgery](../design/surgery.md) | none yet | none yet |
| virology | [virology](../design/virology.md) | none yet | none yet |

Every design doc is `Status: active` unless its own header says otherwise. Deferred (design stands, no milestone up to MVP2 needs it): ai-cyborgs, construction, crafting, cryogenics, hacking-interface, pda, surgery, virology. The 2026-10 design review is recorded in [REVIEW_2026-10](../design/REVIEW_2026-10.md).

## System maps

Infrastructure and gameplay maps, including the ones with no design doc (interactions, selection, entities, inputs and others).

| Map | Status | Covers |
|---|---|---|
| [application](systems/application.md) | stub | `ApplicationInitializerSubSystem`; Boot as launch pad |
| [area](systems/area.md) | partial | APC default areas, authored painting ([area-authoring](systems/area-authoring.md)), lighting snapshot |
| [asset-organization](systems/asset-organization.md) | partial | Art/Content/Data folder taxonomy, icon placement |
| [atmospherics](systems/atmospherics.md) | partial | ECS turf gas sim, client VFX sync, host AOI |
| [audio](systems/audio.md) | partial | SFX pool, occlusion, ambience, personal audio cues; child map [audio-content](systems/audio-content.md) for clips |
| [chat-audio-screens](systems/chat-audio-screens.md) | partial | Comms hub: local speech, radio/announce feed, compose |
| [combat](systems/combat.md) | partial | Melee, ranged hitscan, armor absorption, combat feel |
| [core-subsystems](systems/core-subsystems.md) | partial | `SubSystems.Get<T>()`, bootstrap, world-readiness graph |
| [crafting](systems/crafting.md) | stub | Nothing live; awaits redesign |
| [data-codegen](systems/data-codegen.md) | partial | Asset databases, generated refs, Addressables |
| [disposal](systems/disposal.md) | partial | Item disposal network |
| [electricity](systems/electricity.md) | partial | kWh grid, APCs, SMES, solar |
| [entities](systems/entities.md) | partial | Humanoids, minds, spawning, body animation; [Entity prefab recipes](systems/entities-prefab-recipes.md) |
| [examine](systems/examine.md) | partial | Hover/detailed examine, Search paperdoll |
| [furniture](systems/furniture.md) | partial | Airlocks, lockers, vendors, jukebox |
| [gamemodes-roles-traits](systems/gamemodes-roles-traits.md) | partial | Gamemodes, objectives, jobs, traits |
| [health](systems/health.md) | partial | Body parts, damage, vitals, collapse; child map [health-anatomy-map](systems/health-anatomy-map.md) |
| [id-access](systems/id-access.md) | partial | Crew records, credentials, access gates |
| [ingame-console](systems/ingame-console.md) | partial | Dev/admin console commands |
| [inputs](systems/inputs.md) | partial | Input arbitration, default scheme, pointer authority |
| [interactions-framework](systems/interactions-framework.md) | shipped | `IInteraction` contracts, Discover, pipeline |
| [interactions-runtime](systems/interactions-runtime.md) | shipped | `InteractionController`, radial/armed menus, outlines |
| [inventory](systems/inventory.md) | partial | Items, containers, hands, Main HUD, StoragePanel |
| [localization](systems/localization.md) | partial | `LocalizedTextService`, string tables |
| [logging](systems/logging.md) | shipped | Serilog structured logging |
| [machine-interface](systems/machine-interface.md) | shipped | Diegetic APC/SMES/atmos/vending UI |
| [map-editor-creative-hooks](systems/map-editor-creative-hooks.md) | partial | Map editor extension points for creative mode |
| [networking-session](systems/networking-session.md) | partial | Session FSM, protocol gate, headless server, Windows/Linux client stable-release updaters; [Multiplayer testing](systems/networking-session-testing.md) |
| [permissions](systems/permissions.md) | partial | Admin permission checks |
| [persistence](systems/persistence.md) | partial | Station templates, server meta |
| [player-control](systems/player-control.md) | partial | Connect/authorize/disconnect, reconnect-to-body |
| [rendering](systems/rendering.md) | partial | URP features, selection pick, living-body Vision FOV, decals, world-only health blackout |
| [rounds-lobby](systems/rounds-lobby.md) | partial | Round state machine, UITK lobby, Server Settings, pre-countdown map validation |
| [scene-management](systems/scene-management.md) | stub | `SceneSubSystem`, scene transitions |
| [screen-effects](systems/screen-effects.md) | partial | URP Volume overlays driven by health/atmos |
| [selection](systems/selection.md) | shipped | Shader-ID mesh picking, pick-grid index |
| [stamina](systems/stamina.md) | partial | Regen, encumbrance, sprint, exertion |
| [structural-destruction](systems/structural-destruction.md) | partial | Turf integrity, structural force, blasts |
| [substances](systems/substances.md) | partial | Networked containers, reactions, heat |
| [tile](systems/tile.md) | partial | Tilemap, adjacency, Map Editor with persistent Map Check, SS13 DMM import |
| [ui-shell](systems/ui-shell.md) | partial | UITK composition root, shared catalog/binder, bug report dialog (F8) on the Modal layer; [main menu](systems/ui-shell-main-menu.md) (join/host/status/update); F1 debug host still separate |

## Active efforts and plans

Only work that is in flight, planned, or kept as standing reference. Everything else is in [the archive](../archive/).

| Doc | Status |
|---|---|
| [2026-07_addressables-expansion-migration](2026-07_addressables-expansion-migration.md) | in-progress |
| [2026-07_agent-first-composition](2026-07_agent-first-composition.md) | shipped (standing policy) |
| [2026-07_asset-file-structure-taxonomy](2026-07_asset-file-structure-taxonomy.md) | in-progress |
| [2026-07_atmos-client-visualization-sync](2026-07_atmos-client-visualization-sync.md) | shipped (cited by code) |
| [2026-07_audio-foundation](2026-07_audio-foundation.md) | in-progress |
| [2026-07_camera-ownership](2026-07_camera-ownership.md) | planned |
| [2026-07_ci-develop-release-pipeline](2026-07_ci-develop-release-pipeline.md) | shipped (reference) |
| [2026-07_editor-tooling-tiers](2026-07_editor-tooling-tiers.md) | shipped (standing policy) |
| [2026-07_multiplayer-testing-self-hosted-ci](2026-07_multiplayer-testing-self-hosted-ci.md) | in-progress |
| [2026-07_srp-batcher-gpu-instancing](2026-07_srp-batcher-gpu-instancing.md) | shipped (reference) |
| [2026-07_structural-destruction](2026-07_structural-destruction.md) | in-progress |
| [2026-07_ui-shell-consolidation](2026-07_ui-shell-consolidation.md) | shipped (cited by code) |
| [2026-07_unity-framedebug-ai-tooling](2026-07_unity-framedebug-ai-tooling.md) | shipped (reference) |
| [2026-07_unity-perf-ai-tooling](2026-07_unity-perf-ai-tooling.md) | shipped (reference) |
| [2026-07_urp-lighting-look-polish](2026-07_urp-lighting-look-polish.md) | planned |
| [2026-10_game-versioning](2026-10_game-versioning.md) | shipped (reference) |
| [2026-10_player-settings](2026-10_player-settings.md) | in-progress |
| [2026-11_sandbox-server](2026-11_sandbox-server.md) | in-progress |
| [2026-11_testing-philosophy](2026-11_testing-philosophy.md) | shipped (standing policy) |
| [agent_validation_loop.plan](../plans/agent_validation_loop.plan.md) | shipped (reference) |
| [combat_implementation_plan](../plans/combat_implementation_plan.md) | in-progress |
| [health_implementation_plan](../plans/health_implementation_plan.md) | in-progress |
| [persistence_architecture_design_2fe61864.plan](../plans/persistence_architecture_design_2fe61864.plan.md) | in-progress |
| [radial_menu_implementation_5a83bdf9.plan](../plans/radial_menu_implementation_5a83bdf9.plan.md) | in-progress |
| [shuttle_system_design_a3dd2e04.plan](../plans/shuttle_system_design_a3dd2e04.plan.md) | planned |
| [station_structural_damage.plan](../plans/station_structural_damage.plan.md) | in-progress |
| [urp_lighting_look_plan_d42c32f5.plan](../plans/urp_lighting_look_plan_d42c32f5.plan.md) | planned |

## Player settings and developer tools

- [Player settings](2026-10_player-settings.md) — in-progress; code under `Assets/Scripts/SS3D/Systems/Settings/`, controls in [inputs](systems/inputs.md), settings UI in [rounds-lobby](systems/rounds-lobby.md).
- [Debug tools](systems/ui-shell.md#extension-points) — F1 named-tool registry; remaining host debt is [TECH_DEBT.md §1.13](TECH_DEBT.md#113-debug-tools-centralized-ui-host-consolidation-remains).
- [Multiplayer testing](systems/networking-session-testing.md#tests) — local smoke and load/soak entry points, with coverage limits.

## Reference

| Resource | Use when |
|---|---|
| [milestones/INDEX.md](../milestones/INDEX.md) | Deciding what to work on next |
| [TECH_DEBT.md](TECH_DEBT.md) | Prioritizing structural cleanup |
| [art-asset-index.md](../art-asset-index.md), [art-available-for-import.json](../art-available-for-import.json) | Finding or importing art from SS3D-Art |
| [icon-index.md](../icon-index.md) | Finding UI icons |
