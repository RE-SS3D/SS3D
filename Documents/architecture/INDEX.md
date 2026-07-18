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
| main-hud | [main-hud.md](../design/main-hud.md) — active | [phase1-foundation](2026-07_machine-interface-phase1-foundation.md), [phase2-apc-networking](2026-07_machine-interface-phase2-apc-networking.md), [phase3-smes-generalization](2026-07_machine-interface-phase3-smes-generalization.md), [diegetic-screen-ui-framework](2026-07_diegetic-screen-ui-framework.md), [mi-area-electricity-debt](2026-07_mi-area-electricity-debt.md), [screen-space-effects](2026-07_screen-space-effects.md), [mi-path-catalog](2026-07_mi-path-catalog.md) — all shipped (screen-effects health wired; atmos wiring deferred; UiShell still deferred); player HUD overlay is a partial in-branch slice (no dated effort yet) | [inventory](systems/inventory.md) — partial (player HUD); also [machine-interface](systems/machine-interface.md), [screen-effects](systems/screen-effects.md) |
| comms | [comms.md](../design/comms.md) — active | [phase1-foundation](2026-07_machine-interface-phase1-foundation.md) — shipped (operator feedback conventions only) | [chat-audio-screens](systems/chat-audio-screens.md) — stub |
| area | [area.md](../design/area.md) — active | [area-foundation](2026-07_area-foundation.md) — shipped (partial: APC-seeded variant; live mutation recompute and editor merge/split deferred) | [area](systems/area.md) — partial |
| hacking-interface | [hacking-interface.md](../design/hacking-interface.md) — active | none yet | none yet |
| combat | [combat.md](../design/combat.md) — active | [player-body-animation](2026-07_player-body-animation.md) — shipped (stance/locomotion foundation only; combat.md not implemented) | [combat](systems/combat.md) — stub |
| stamina | [stamina.md](../design/stamina.md) — active | [2026-07_inventory-storage-redesign](2026-07_inventory-storage-redesign.md) — Phase 7a core shipped with inventory clean-slate (combat drains deferred) | [stamina](systems/stamina.md) — partial |
| health | [health.md](../design/health.md) — active | rewrite in flight: [health_implementation_plan](../plans/health_implementation_plan.md); [body-presentation-authority](2026-07_body-presentation-authority.md) — planned; screen overlays in [screen-space-effects](2026-07_screen-space-effects.md) (health wired) | [health](systems/health.md) — partial |
| armor | [armor.md](../design/armor.md) — active | none yet | none yet |
| inventory-storage | [inventory-storage.md](../design/inventory-storage.md) — active | [2026-07_inventory-storage-redesign](2026-07_inventory-storage-redesign.md) — in-progress (clean-slate: data model + panel + Main HUD equip/drag + stamina 7a + old UI purge shipped; Play Mode verification pending) | [inventory](systems/inventory.md) — partial |
| examine | [examine.md](../design/examine.md) — active | none yet | [examine](systems/examine.md) — shipped |
| crafting | [crafting.md](../design/crafting.md) — active | none yet | [crafting](systems/crafting.md) — stub |
| death-cloning-respawn | [death-cloning-respawn.md](../design/death-cloning-respawn.md) — active | none yet | none yet |
| surgery | [surgery.md](../design/surgery.md) — active | none yet | none yet |
| lobby | [lobby.md](../design/lobby.md) — active | none yet | [rounds-lobby](systems/rounds-lobby.md) — shipped |
| round-config | [round-config.md](../design/round-config.md) — active | none yet | [rounds-lobby](systems/rounds-lobby.md) — shipped |
| round-end | [round-end.md](../design/round-end.md) — draft | none yet | none yet |
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
| rendering-lighting | [rendering-lighting.md](../design/rendering-lighting.md) — active | none yet (look pass planned: [urp_lighting_look_plan](../plans/urp_lighting_look_plan_d42c32f5.plan.md); palette emission sample fix shipped on Simple Toon) | [rendering](systems/rendering.md) — partial |
| shuttles | [shuttles.md](../design/shuttles.md) — active | none yet | none yet |
| ai-cyborgs | [ai-cyborgs.md](../design/ai-cyborgs.md) — active | none yet | none yet |
| persistence-save | none yet | none yet | [persistence](systems/persistence.md) — partial (station templates, server meta only, not a design spec) |
| networking | none yet | [headless-dedicated-server](2026-07_headless-dedicated-server.md) — shipped (partial: selection outline and drop interaction against a real client still broken, not root-caused), [multiplayer-test-harness](2026-07_multiplayer-test-harness.md) — shipped (partial: mouse/screen-space interaction and pocket/container regressions not covered), [ci-develop-release-pipeline](2026-07_ci-develop-release-pipeline.md) — shipped (manual Windows+bats prerelease by default; Linux/EditMode/smoke opt-in) | [networking-session](systems/networking-session.md) — partial |
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

`design/round-end.md` (round-end/transition) now exists as a **draft** — it builds its summary
screen and transition on the spectator framework `observer.md` §7 supplies and spends the secret
gamemode identity `round-config.md` §4 protects. Authored in the house style (no prototyping
section, `Status: draft`, design-docs-cite-design-docs only). Owner review to promote it to
`active`.

## Infrastructure

| System | Map | Status | Summary |
|--------|-----|--------|---------|
| Core / SubSystems | [core-subsystems](systems/core-subsystems.md) | shipped | `SubSystem` / `NetworkSubSystem` base types and `SubSystems` service locator; scene registration legacy — target code bootstrap |
| Application | [application](systems/application.md) | stub | App bootstrap and startup; Boot/Game as thin launch pads |
| Networking (session) | [networking-session](systems/networking-session.md) | partial | FishNet host/join session management; headless dedicated-server build; real multi-process test harness |
| Scene management | [scene-management](systems/scene-management.md) | stub | Scene loading and switching; not a system composition root |
| UI shell | [ui-shell](systems/ui-shell.md) | stub | Target UITK composition root; MI + Main HUD path catalogs shipped (duplicated); shared catalog helper + full shell deferred |
| Interactions (framework) | [interactions-framework](systems/interactions-framework.md) | shipped | Shared `IInteraction` contracts, pipeline, and wire identifiers |
| Data / codegen | [data-codegen](systems/data-codegen.md) | stub | Asset databases and generated references |
| Persistence | [persistence](systems/persistence.md) | partial | Contributor-based station templates and server meta (permissions, round history) |
| Localization | [localization](systems/localization.md) | partial | `LocalizedTextService` and examine string tables |
| Logging | [logging](systems/logging.md) | shipped | Serilog structured logging |
| Permissions | [permissions](systems/permissions.md) | partial | Admin permission checks; persisted via [persistence](systems/persistence.md) envelope with legacy txt fallback |
| Rendering | [rendering](systems/rendering.md) | partial | URP features: selection pick pass (+ exclude layers), atmospherics scatter/glow/distortion; Simple Toon palette emission; client FOV hard mask (`VisionRendererFeature` + raycast `_VisionMap`) |

## Gameplay

| System | Map | Status | Summary |
|--------|-----|--------|---------|
| Interactions (runtime) | [interactions-runtime](systems/interactions-runtime.md) | shipped | `InteractionController`, radial menu, armed interactions, outlines |
| Selection | [selection](systems/selection.md) | shipped | Shader-ID mesh picking; outline shells excluded from pick pass |
| Examine | [examine](systems/examine.md) | shipped | Hover/detailed examine; uGUI views condemned pending UITK redesign |
| Tile / construction | [tile](systems/tile.md) | shipped | Tilemap, adjacency, construction; TileMap Creator uGUI condemned; `TileCoord` must be `IEquatable` for dict keys |
| Atmospherics | [atmospherics](systems/atmospherics.md) | partial | ECS turf gas sim; GPU fog/fire on server/host only — client VFX sync planned; tick GC pitfalls documented (upload/pipes) |
| Area | [area](systems/area.md) | partial | APC-seeded flood-fill, area power, lighting state, wall light switches |
| Electricity | [electricity](systems/electricity.md) | partial | kWh storage, HV cable grid, APC/SMES/generators, consumer visuals |
| Substances | [substances](systems/substances.md) | partial | Containers, transfer interactions, Tier 2 armed proof-of-concept; container `AsReadOnly` GC pitfall |
| Inventory | [inventory](systems/inventory.md) | partial | Items/containers/hands + weight/size-class/stacking/locks; Main HUD sole equip/storage UI + StoragePanel; HUD suppressed while MI open; old uGUI purged; `CarriedWeight` → stamina; Human hands wiring remains prefab debt |
| Stamina | [stamina](systems/stamina.md) | partial | Phase 7a core: health-modulated regen, encumbrance, sprint drain, overdraw→oxy; no permanent bar; combat drains deferred |
| Entities | [entities](systems/entities.md) | partial | Humanoids, minds, spawning; body-state animation + combat stances; `Human.prefab` composition debt; collapse/death presentation debt ([body-presentation-authority](2026-07_body-presentation-authority.md)) |
| Health | [health](systems/health.md) | partial | Phases 1–5b shipped; screen-effects wired from snapshot; Phase 0d strips/rewires `Human.prefab`; vitals HUD / examine-self Phase 6 remainder; interim collapse RPCs — see [body-presentation-authority](2026-07_body-presentation-authority.md) |
| Combat | [combat](systems/combat.md) | partial | Phase 4 melee vertical slice (fists + crowbar); stance/aim presentation in [entities](systems/entities.md) |
| Crafting | [crafting](systems/crafting.md) | stub | Recipe crafting; crafting menu uGUI condemned |
| Furniture / world objects | [furniture](systems/furniture.md) | partial | Airlocks, vendors, jukebox; power-gated behaviors; vending via diegetic machine-interface |
| Rounds / lobby | [rounds-lobby](systems/rounds-lobby.md) | shipped | Round state machine; lobby UI condemned pending lobby.md redesign |
| Gamemodes / roles / traits | [gamemodes-roles-traits](systems/gamemodes-roles-traits.md) | stub | Objectives, job roles, character traits |
| Player control | [player-control](systems/player-control.md) | stub | Player subsystem and input routing |
| Chat / audio / screens | [chat-audio-screens](systems/chat-audio-screens.md) | stub | Chat UI condemned per comms.md; audio/camera controllers |
| Machine interface UI | [machine-interface](systems/machine-interface.md) | shipped | Diegetic APC/SMES/atmos/vending; path catalog; Dual Kawase blur + dim; DOTween bring-up/dismiss |
| Screen-space effects | [screen-effects](systems/screen-effects.md) | partial | URP Volume overlays; health drives dying/blood/oxy/concussion/unconscious + hit flash; `SetUiBackdropBlur` for machine UI; atmos temp/fire deferred; F2 debug Canvas condemned |
| ID / access | [id-access](systems/id-access.md) | partial | Crew records, credential checks, doors, machine UI gates, dev console helpers |
| Inputs | [inputs](systems/inputs.md) | partial | Arbitration + `InputInterface` UITK/uGUI pointer authority (Main HUD / MI / radial register documents) |
| In-game console | [ingame-console](systems/ingame-console.md) | partial | Command dispatch; console panel uGUI condemned pending UITK debug layer |

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
| [2026-07_screen-space-effects](2026-07_screen-space-effects.md) | shipped (foundation + health wiring; atmos deferred) |
| [2026-07_headless-dedicated-server](2026-07_headless-dedicated-server.md) | shipped (partial: selection outline, drop interaction against a real client still broken, not root-caused) |
| [2026-07_agent-first-composition](2026-07_agent-first-composition.md) | shipped (policy); code deferred — bootstrap, UiShell + shared path-catalog helper, prefab tooling; main-HUD UITK slice partial ([inventory](systems/inventory.md)) |
| [2026-07_mi-path-catalog](2026-07_mi-path-catalog.md) | shipped (MI path catalog wedge of composition follow-on b; Main HUD later copied the pattern — unify under [ui-shell](systems/ui-shell.md)) |
| [2026-07_body-presentation-authority](2026-07_body-presentation-authority.md) | planned |
| [2026-07_inventory-storage-redesign](2026-07_inventory-storage-redesign.md) | in-progress (clean-slate + stamina 7a code shipped; Play Mode verification pending) |
| [2026-07_multiplayer-test-harness](2026-07_multiplayer-test-harness.md) | shipped (partial: mouse/screen-space interaction and pocket/container round-trip regressions not covered; not yet verified against a real Unity build) |
| [2026-07_ci-develop-release-pipeline](2026-07_ci-develop-release-pipeline.md) | shipped (manual workflow_dispatch; default Windows+bats prerelease; Linux/EditMode/smoke opt-in) |

## Implementation plans

Temporary working plans in [Documents/plans/](../plans/). Update todos when work ships.

| Plan | Topic |
|------|-------|
| [examine_localization_design_5ca361a6.plan.md](../plans/examine_localization_design_5ca361a6.plan.md) | Examine localization migration |
| [radial_menu_implementation_5a83bdf9.plan.md](../plans/radial_menu_implementation_5a83bdf9.plan.md) | Three-tier radial interaction menu (Phases 4–5 pending) |
| [interaction_system_improvements_9e14ae22.plan.md](../plans/interaction_system_improvements_9e14ae22.plan.md) | Interaction system hardening |
| [diegetic_screen_ui_framework_643c2e6f.plan.md](../plans/diegetic_screen_ui_framework_643c2e6f.plan.md) | Diegetic shell + vending (shipped) |
| [areas_implementation_plan_c0639343.plan.md](../plans/areas_implementation_plan_c0639343.plan.md) | APC-seeded areas, flood-fill, power/lighting follow-ups |
| [electricity_kwh_foundation_917ccdbc.plan.md](../plans/electricity_kwh_foundation_917ccdbc.plan.md) | kWh storage, priority shedding, HV cable grid rules |
| [persistence_architecture_design_2fe61864.plan.md](../plans/persistence_architecture_design_2fe61864.plan.md) | Layered persistence framework; Phase 1a/1b shipped, Phase 2 round snapshots pending |
| [animation_system_design_250de599.plan.md](../plans/animation_system_design_250de599.plan.md) | Player body / layered animation foundation |
| [health_implementation_plan.md](../plans/health_implementation_plan.md) | Clean-slate health rewrite (Phases 0–5b shipped; 6–9 pending) |
| [combat_implementation_plan.md](../plans/combat_implementation_plan.md) | Combat build-out on health/animation/screen-fx/atmos foundations (pending) |
| [urp_lighting_look_plan_d42c32f5.plan.md](../plans/urp_lighting_look_plan_d42c32f5.plan.md) | URP half-toon look pass (pending) |
| [shuttle_system_design_a3dd2e04.plan.md](../plans/shuttle_system_design_a3dd2e04.plan.md) | Shuttle tile blueprints / multi-map (pending) |

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
