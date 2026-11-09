> Implements: Documents/design/construction.md §2 (tools and materials), §3 (use-on-target, placement ghost, timed interruptible steps), §4 (floors and hull), §5 (air after a patch), §6 (wall ladder), §7 (windows), §8 (wreckage and salvage), §9 (repair and deconstruction), §10 (feel targets), §11 (HUD touchpoints); Documents/design/explosives-destruction.md §3 (repair to Intact, floor layers)
> Touches systems: tile, structural-destruction, interactions-framework, interactions-runtime, inventory, atmospherics, ui-shell, gamemodes-roles-traits
> Status: planned

**Progress:** Grilled with the owner on 2026-11-09, after the gameplay grill that revised [construction.md](../design/construction.md). Serves [mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M10, and S2 closes M8's "an engineer seals a hull hole". Takes over [station structural damage](../plans/station_structural_damage.plan.md) Phase 6 (repair). No phase started.

# Construction for MVP1: floors, hull, walls, windows (Nov 2026)

The crew needs to answer the operatives' C4: patch hull holes, re-lay floors, weld damaged walls back to Intact, rebuild a destroyed wall or window, barricade, and take a barricade down. No door rebuilding and no fabrication (design §14).

## Starting state (2026-11-09, `4e8c5f34`)

Checked by three read-only explorations against code; the [tile](systems/tile.md) map is stamped `3190b2f1c`, [structural-destruction](systems/structural-destruction.md) `7785df8dc`, interactions `e2f86cf20`.

- **No in-round tile placement.** `ConstructionService.TryPlaceTile` is reached only from admin-gated `TileSubSystem.RpcPlaceObject` and Map Editor commands. Nothing checks or consumes materials. `BuildChecker.Evaluate` holds the rules and `BuildFailReason` the toastable reasons.
- **Layers.** Walls, girders, windows, doors and floor tiles are all `TileLayer.Turf`; plating is `Plenum`. A hull hole is an empty Plenum location; clearing a plenum cascades its occupants.
- **Tile identity is the only saved per-object state.** `SavedPlacedTileObject` is id, SO name, origin, dir. Integrity is a `PlacedTileObject` SyncVar pair (`_syncIntegrityRemaining`, `_syncIntegrityStage`), replicated, not persisted; `-1` means "first query sets Intact". There is no repair API; `ServerSetIntegrity(max, Intact)` with `StructuralIntegrityRules.ResolveMaxIntegrity` is the only path.
- **Girder.** `SteelGirder.asset` is a Turf Wall/Steel `TileObjectSo`, placeable in the Map Editor. Its prefab carries eight child layers (`GirderShape`, `SteelWallSheets`, `SteelWindowSheets`, three reinforced sheet layers, `Supports`, `Struts`), each with its own adjacency connector driven by `MultiAdjacencyConnector`; only `GirderShape`'s renderer is on. No code toggles the others. **Hazard:** `PlacedTileObject.EnableAllChildRenderers` re-enables every child renderer on the in-AOI host path and in Map Editor authoring, so hidden layers would show. `SteelGirderUnbolted` is a draggable `ItemObjectSo` prop, not a pick-up.
- **Occupancy.** `TileOccupancyEvaluator` makes any non-window Wall turf `HasWall`, `BlocksVision` and airtight unless Cracked/Destroyed, so a girder is a full wall to atmos today. `BlocksVision` has no consumer outside the evaluator and one atmos test; light and sight come from meshes.
- **Interactions.** Discovery collects target-side `CreateTargetInteractions` and tool-side `IInteractionSourceExtension`s on the held item (`TimedCharge` does both). Dispatch needs a spawned `NetworkObject` with a `Selectable`, so an empty cell or wreckage can't be targeted. Tool checks are ad hoc (`TimedCharge.HasCutters` compares item asset). Welder and Crowbar have no interactions; Crowbar has a melee profile.
- **Timed steps.** `DelayedInteraction` runs its effect at the end; cancels on move (`CharacterMoveCheck`), on `!CanInteract` or `!Source.CanContinueInteraction()` each `CheckInterval` (covers knockdown and arm loss via `Hand`), and on Backspace. No cancel on damage. Progress is the uGUI `LoadingBar` slider spawned by `ClientDelayedInteraction`.
- **Stacks.** `Item._maxStackSize` (all 56 prefabs at 1, including `SteelSheet`, `GlassSheet`, `FloorTile`) and SyncVar `_stackCount`; `[Server] SetStackCount` clamps to ≥1. No consume API. `RoleLoadout` is asset lists with no counts; `SavedPlacedItemObject` saves no count.
- **Wreckage.** `TileSubSystem.Wreckage` is a `SyncList<SteelWreckageRecord>`, at most one per coord+layer, only for non-window steel walls and steel tables. No collider, does not block placement; `WreckageMutationObserver.OnTilePlaced` deletes it when anything is placed there. `RemoveWreckage` is private.
- **Reinforced.** `SteelWallReinforced.asset` differs from `SteelWall.asset` only by name and prefab: same 100 HP, no flag.
- **Atmos.** Placement fires `OnTilePlaced` → `AtmosTileObserver.UpdateCell` synchronously; a new airtight wall blocks edges. A plenum placed over a vacuum cell goes through `InitCell`, which seeds full station O2/N2 (intended, atmospherics map Pitfall). `VentController` pushes to `_targetPressureKpa` (101) at up to 100 mol/s when powered and on a Supply/Mixed network. The `Fill` air alarm preset only disables scrubbers. `testroom.json` has vents but no pipes or air alarm.
- **Outline.** `InteractionOutlineDriver` is hover-only, and `TryEvaluateOutlineInteractability` checks only target-side interactions.
- **Hologram.** `ConstructionHologram` has no Map Editor dependency; `ConstructionHologramManager` is bound to `MapEditorSubSystem`.

## Decisions

| # | Decision | Why |
|---|---|---|
| D1 | **One `TileObjectSo` per wall stage:** `SteelGirder` → new `SteelGirderPlated` → `SteelWall`; a step replaces the tile. Glass on a girder replaces it with `SteelWindow`. Stage prefabs contain only the meshes they show: the girder prefab loses its unused child layers instead of hiding them. Prefabs come from an editor recipe (`PrefabUtility`), not YAML edits | Identity already syncs, saves and reaches late joiners; no per-object state has a save precedent. Removing children sidesteps the `EnableAllChildRenderers` hazard |
| D2 | **`TileObjectSo.airtight`** (default true), read by `TileOccupancyEvaluator`. False on girder and plated. No `blocksVision` field | Design §6 needs air through girder and plated. Light and sight already follow meshes |
| D3 | **`ConstructionPlacementSubSystem`** (client + server) for held-item **cell actions**: sheets on a hull hole or clear floor, floor tiles on bare plating, crowbar on wreckage. The client shows a ghost through `ConstructionHologram`; one ServerRpc carries coord + action; the server checks rule, range, materials and `BuildChecker`. Actions on existing objects (weld, plate a girder, glaze, pry a tile, repair, deconstruct) are ordinary `IInteractionSourceExtension` interactions | Interactions need spawned targets; cell placement is "click where the ghost is", not a radial verb |
| D4 | **`ConstructionRecipes`**: one static C# table of rules (input SO, tool or material, cost, duration, result SO, returns), keyed by catalog names. An EditMode test proves every rule resolves to real assets | ~15 rules, reviewable as text; move to SO assets if balancing needs the Inspector |
| D5 | **`ConstructionTool` component** with a kind (`Welder`, `Crowbar`), added by an editor recipe. No welder fuel or on/off. **`[Server] Item.TryConsume(int n)`** deletes the item at 0. Stack limits: steel and glass 50, floor tiles 20. Cost is checked at start and spent at completion | Shared "is this a crowbar" check for later doors and cables; design §3 says cancels cost nothing |
| D6 | **UI Toolkit progress ring** for every `DelayedInteraction`, anchored to the target in the `UiShellSubSystem` overlay layer. `LoadingBar` and its uGUI prefab are purged. Cancels carry a `DelayedCancelReason` (Moved, KnockedDown, Hurt, MissingMaterials, Manual) sent to the owning client and shown briefly | Design §11; composition policy forbids new uGUI and prefers replace-and-purge |
| D7 | **`DelayedInteraction.CancelOnDamage`** (default true), fired from the health damage event. Applies to defuse too | Design §3; SS13 precedent for defuse |
| D8 | **Keep the air behaviour.** Patched cells keep `InitCell`'s station-air seed; `Fill` stays scrubbers-off. The M0 test station gets a supply network and an air alarm so vents can refill | One cell of free air is negligible against a room; without a network nothing refills |
| D9 | **Wreckage blocks construction.** New `BuildFailReason.WreckageInTheWay` in `BuildChecker`; the observer no longer deletes wreckage on construction placement (the Map Editor keeps delete-on-place); `RemoveWreckage` goes public; crowbar "Clear wreckage" is a cell action that removes it and spawns steel sheets via `TryPlaceItem` | Design §8 |
| D10 | **`TileObjectSo.reinforced`**, set on `SteelWallReinforced` and `SteelWindowReinforced`. Deconstruction rules refuse it; repair allows it. Reinforced walls get a higher `structuralMaxIntegrity` (value is balancing) | Design §9; today the vault wall has plain-wall HP |
| D11 | **Hover outline includes tool-side interactions**: `TryEvaluateOutlineInteractability` also asks the held item's source extensions. No candidate scan | Same signal where the player is looking, no per-frame scan |
| D12 | **`Item._initialStackCount`** (default 1), applied at server spawn. Steel and glass sheets 20, floor tiles 10. Salvage calls `SetStackCount` explicitly | Loadouts and map-placed closet stock arrive as stacks with no loadout or save format change |
| D13 | **Versioning.** New tile types and RPCs bump `ProtocolVersion.Current`. No `PersistenceEnvelope` bump: stages are tile identity and integrity stays unsaved. The commit that completes S4 carries `Version-Bump: minor` | [game-versioning](2026-10_game-versioning.md) |

## Deviations from design

- **construction.md §3, valid targets light up:** MVP1 outlines only the hovered target (D11). Full candidate highlighting while a tool is held is post-playtest.
- **construction.md §5, a patched room refills from its vents:** the patched cell itself starts with station air (D8), and the air alarm's refill preset doesn't make vents faster, it only stops scrubbers.
- **Girder prefab:** its reinforced, window-sheet, support and strut layers move out (D1). They stay in `SteelWalls.fbx` for reinforced building later.

## Phases

### S1: foundations

- [ ] `Item.TryConsume`, `_initialStackCount`, stack limits and initial counts on `SteelSheet`, `GlassSheet`, `FloorTile` (editor recipe). EditMode: consume to zero deletes; merge respects the limit
- [ ] `ConstructionTool` component + recipe on `Welder` and `Crowbar`
- [ ] `DelayedCancelReason`, `CancelOnDamage`; owner-client cancel RPC with reason
- [ ] UITK progress ring presenter (running, done, cancelled + reason) in the overlay layer; purge `LoadingBar` and its prefab; `InteractionOptimisticFeedback` adopts the ring. Defuse uses it unchanged
- [ ] `ConstructionRecipes` table + resolve-all EditMode test
- [ ] `ConstructionPlacementSubSystem`: active-hand watch, cell pick (ground plane where there is no plenum), ghost via `ConstructionHologram` with fail reason and "n / m" cost tag, ServerRpc with range, rule, `BuildChecker` and material checks, timed completion via the same cancel rules
- [ ] D11 outline change
- Validation: `compile,editmode`

### S2: floor and hull (closes M8's patch)

- [ ] Cell action: steel sheets on an empty Plenum location → plating (~3 s). Atmos re-seals through the existing observer
- [ ] Cell action: floor tiles on bare plating → floor turf (~1 s)
- [ ] Interaction: crowbar on a floor tile → tile removed, a floor tile item to the hand or the floor
- [ ] Interaction: welder on Damaged/Cracked plating → Intact (~4 s)
- [ ] Examine stage and next-step text for floor and plating (`StructuralIntegrityExaminable`)
- [ ] EditMode: rules, consumption, cancel without spend; scenario: blast a hole, patch it, the cell turns airtight
- Validation: `compile,editmode` + `scenarios:Explosives`

### S3: weld repair (structural plan Phase 6)

- [ ] Interaction: welder on a Damaged/Cracked wall, window or reinforced wall → `ServerSetIntegrity(max, Intact)` (~4 s); presenter clears the stage visuals; atmos refreshes a Cracked wall
- [ ] Examine next-step text for damaged structures
- Validation: `compile,editmode`

### S4: girder ladder and windows

- [ ] `TileObjectSo.airtight`, evaluator reads it; girder and plated set false
- [ ] Editor recipe: strip `SteelGirder` to `GirderShape`; build `SteelGirderPlated` (`GirderShape` + `SteelWallSheets`, both adjacency-driven); new `SteelGirderPlated.asset`; Map Editor catalog entry
- [ ] Cell action: steel sheets on a clear floor turf → girder (blocks movement; air and light pass)
- [ ] Interactions: steel sheets on girder → plated; welder on plated → `SteelWall`; glass sheets on girder → `SteelWindow` (longer timer)
- [ ] D9 wreckage: `BuildFailReason.WreckageInTheWay`, observer change, public `RemoveWreckage`, crowbar clear cell action with salvage
- [ ] Owner check in editor: does plated read as unfinished next to sealed? If not, commission seam art (design §6)
- [ ] Welding sparks/hiss and crowbar scrape cues
- [ ] EditMode: ladder transitions, occupancy per stage, wreckage blocks girder
- Validation: `compile,editmode` + `visual` (stage meshes)

### S5: deconstruction and reinforced

- [ ] `TileObjectSo.reinforced` on reinforced wall and window; `structuralMaxIntegrity` override on the reinforced wall
- [ ] Interactions from Intact only: welder cuts seams (wall → plated), crowbar pulls plate (plated → girder, sheets back), crowbar lifts pane (window → girder, glass back), welder cuts girder (→ open, sheets back); about twice the build time; refused on reinforced and on damaged
- [ ] EditMode: reverse ladder, refusals, returns
- Validation: `compile,editmode`

### S6: test station and acceptance

- [ ] M0 test station: supply network to its vents and an air alarm per breach-prone room; engineering closet with sheets, glass and floor tiles; Engineer loadout with welder, crowbar and stacks (lands with M0's Engineer)
- [ ] Scenario `scenarios:Construction`: blast a hull hole, patch it, pressure climbs back; destroy a wall, clear wreckage, rebuild to sealed, rooms separate
- [ ] Separate-client multiplayer check: ghost, ring, cancel reasons, stage swaps and wreckage visible to a remote client
- [ ] `update-system-docs`: tile, structural-destruction, interactions, inventory maps; M8/M10 slices
- Validation: `compile,editmode` + the new scenario + multiplayer smoke

## Out of scope

Door rebuilding, fabrication, new deck into space, reinforced building, grilles, wrench/screwdriver/rod steps, welder fuel, full candidate highlighting, PDA guide, the paper quick reference, saving integrity or stack counts (design §14).

## Open questions for the owner

- Stack limits and initial counts (D5, D12) and the reinforced-wall integrity value (D10) are first guesses for the balancing pass.
