> Code paths: Assets/Scripts/SS3D/Systems/Construction/ (editor recipes in Editor/ and Editor/Content/)
> Entry points: ConstructionRecipes, ConstructionStepInteraction, ConstructionPlacementSubSystem, ConstructionApply, ConstructionFloorTiles
> Status: partial
> Verified: 6c49dab49 — 2026-11-11

# Construction

## Overview

Crew repair and rebuild with a welder, a crowbar and stacks of steel sheets, glass sheets and floor tiles ([construction.md](../../design/construction.md) §2–§11). Every step is one row of the static rule table `ConstructionRecipes`: input, target, cost, seconds, outcome, result and returns. Almost every step is an ordinary target-bound interaction (`ConstructionStepInteraction`, a `DelayedInteraction`) offered by the held item's `ConstructionTool` or `ConstructionMaterial` marker:

- **On an object:** weld or repair, plate or glaze a girder, weld seams, cut seams, pull a plate, lift a pane, dismantle a girder, pry a floor tile.
- **On the cell under the hovered floor or plating** (`ConstructionRule.ActsOnCell`): "Construct girder", "Lay floor tile", "Clear wreckage".
- **Hull holes only** (`IsCellAction`): an empty cell has nothing to hover, so `ConstructionPlacementSubSystem` shows a plating ghost and runs the step through its own ServerRpc and timer.

Wall stages are tile identity, not per-object state: `SteelGirder` → `SteelGirderPlated` → `SteelWall`, or girder → `SteelWindow`. A step replaces the tile, so stages sync, save and reach late joiners. Materials are checked at start and spent only at completion; a cancelled step costs time, never sheets. Each laid floor has its own tile item (`ConstructionFloorTiles`): prying returns the floor's kind, and that kind lays the same floor back. Floors with no tile item refuse the crowbar.

Feedback: a UI Toolkit progress ring on every delayed interaction (`DelayedProgressRingSubSystem`, [interactions-runtime](interactions-runtime.md)) with a cancel reason; the hover outline, green only for what the held tool or stack can do ([interactions-runtime](interactions-runtime.md)); floor work kneels and wall work holds a two-handed loop for the step's timer ([entities](entities.md)); stack counts on the HUD hand and gear slots ([inventory](inventory.md)). Design deviations: [construction-mvp1](../2026-11_construction-mvp1.md) "Deviations from design".

## Start here

- `ConstructionRecipes.cs`: the rule table; `NotDeconstructible` lists plain walls with no ladder
- `ConstructionRule.cs`: steps, target kinds, outcomes, `ActsOnCell`, `NoReturnWhenCracked`
- `ConstructionObjectRules.cs`: which placed object a step acts on, and `CellFailure` for blocked cell steps
- `ConstructionCellRules.cs`: cell shape and validity (hull hole, bare plating, clear floor, wreckage), shared client/server
- `ConstructionStepInteraction.cs` / `ConstructionInteractions.cs`: the interaction and its discovery and outline probes
- `ConstructionApply.cs`: server outcome, spending and returns (`ResultNameFor`, `ReturnNameFor`)
- `ConstructionPlacementSubSystem.cs`: hull-hole ghost, cell ServerRpc and timer, work cues (`.Cues.cs`)
- `ConstructionFloorTiles.cs`: floor ↔ tile item kinds and `NotPryable`
- `ConstructionTool.cs`, `ConstructionMaterial.cs`, `ConstructionInput.cs`: held-item markers
- Editor: `ConstructionContentPrefabRecipes` (menu **SS3D → Construction → Run Content Prefab Recipes**) runs `ConstructionItemRecipe`, `ConstructionHubRecipe`, `GirderStageRecipe`, `FloorTileItemRecipe`, `ConstructionReinforcedRecipe`

## Extension points

- **New step:** add a `ConstructionRule` row and, if it needs a new target shape, a `ConstructionTargetKind` handled in `ConstructionObjectRules` / `ConstructionCellRules`. Map its generic name to a clip in `InteractionAnimation.ChooseClip` (floor work `Kneel`, wall work `WallWork`); `ConstructionRecipesTests.EveryRule_HasABodyAnimation` enforces it.
- **New floor tile:** add a `Kinds` row (and run the recipes menu to build the item) or a `NotPryable` row with a reason. **New wall tile:** a deconstruction rule, `TileObjectSo.reinforced`, or a `NotDeconstructible` row. `ConstructionCoverageTests` fails until one is done.
- **New tool or material:** a `ConstructionInput` value plus the marker's kind; markers are added by `ConstructionItemRecipe`, never by hand on the prefab.
- **Stage prefabs:** built by `GirderStageRecipe` from the girder, never edited as YAML.

## Pitfalls

- **Catalog names are reference names, not asset file names.** `GenericObjectSo.NameString` is the prefab's `ObjectAssetReference` name: the carpet floor is `FancyCarpetRed`, dirt is `TileDirt1`, the grey floor's asset is `TIleGrey` but its name is `TileGrey`. A table keyed on file names silently matches nothing; `ConstructionCoverageTests` catches floors and walls.
- **A copied networked prefab keeps the source's FishNet prefab id.** Recipes that copy a prefab (`GirderStageRecipe`, `FloorTileItemRecipe`) call `ManagedObjects.InitializePrefab(networkObject, -1)` on the copy, or the client spawns the wrong prefab.
- **Hidden child renderers come back.** `PlacedTileObject.EnableAllChildRenderers` re-enables every child renderer on the in-AOI host path, so stage prefabs contain only the meshes they show.
- **A non-airtight wall must also clear its edges.** Atmos flow reads `BlockedEdges`, so a girder gets `BlockedEdges = 0`, not only `IsAirtight = false`; `TileUnderfloorVisibility` treats only airtight walls as covering, or plating disappears under an open girder.
- **Wreckage is not a target.** It has no collider or `Selectable`; "Clear wreckage" is offered on the plating or floor under it, and placement goes through `TileSubSystem.KeepWreckage()` so construction does not delete it (the Map Editor still does).
- **Area ids do not replicate.** The hull-hole ghost reads green over open space on a client; the server refuses "Outside the station" and the label shows it briefly.

## Tests

- EditMode: `ConstructionRecipesTests` (every name resolves, prefabs match recipes, every rule has a clip), `ConstructionCoverageTests` (floors, tile items, walls), `ConstructionFloorHullTests`, `ConstructionLadderTests`, `ConstructionDeconstructionTests`
- PlayMode: `scenarios:Construction` (`ConstructionScenarios`): weld cracked plating and re-tile, patch a hull hole in magboots and hold air, clear wreckage and rebuild a wall that blocks air
- Outstanding: a separate-client multiplayer check of the ghost, ring, cancel reasons, stage swaps, wreckage and work poses

## Depends on / Used by

Depends on [tile](tile.md) (placement, `BuildChecker`, `TileObjectSo.airtight` / `blocksVision` / `reinforced`, wreckage), [structural-destruction](structural-destruction.md) (integrity stages, repair to Intact), [interactions-framework](interactions-framework.md) and [interactions-runtime](interactions-runtime.md) (delayed interactions, cancel reasons, outline), [inventory](inventory.md) (stacks, `Item.TryConsume`), [atmospherics](atmospherics.md) (re-seal through the tile observer), [entities](entities.md) (held work poses). Used by the M8 hull patch and M10 in [mvp1-nuke-ops](../../milestones/mvp1-nuke-ops.md).

## Related docs

- Design: [construction.md](../../design/construction.md), [explosives-destruction.md](../../design/explosives-destruction.md) §3
- Effort: [construction-mvp1](../2026-11_construction-mvp1.md)
