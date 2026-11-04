> Code paths: Assets/Scripts/SS3D/Systems/StructuralDamage/ (plus integrity stage, occupancy and blast VFX Rpc hooks in Systems/Tile/, mapped by [tile](tile.md))
> Entry points: StructuralDamageSubSystem, StructuralDamageService, BlastResolutionService, BlastVfxPresenter, StructuralIntegrityPresenter, HurtStructureCommand, BlastCommand
> Status: partial
> Verified: 319a1db1b — 2026-11-04

# Structural destruction

## Overview

Per-tile integrity for Turf walls, doors, windows, and FurnitureBase steel/wood/glass tables and opted-in metal furniture per [explosives-destruction.md](../../design/explosives-destruction.md) §3. Phase 1–3: accumulate force → stages → Destroyed clear; melee StructuralForce; hop blast BFS + crew brute. Phase 4: all structural wall variants, including windows and the girder, show a dent at Damaged and a torn seam at Cracked. Steel tables use tabletop-only dents/tears; wooden tables use chipped gouges and grain-following splits with exposed fibres; glass tables grow an impact star into branching fractures; lockers mark body/door meshes; opted-in machines mark side casings to preserve front displays; civilian and security airlocks mark each moving leaf, excluding control panels and generated floors. Bounds-based placement has stable per-tile variation. Other supported surfaces use MPB tint. Cracked walls add a hiss; steel tables/airlocks use torn-metal examine feedback, wood uses gouged/split-wood text, and glass uses chipped/fractured-glass text. Tables and airlocks do not play the hull-leak hiss. Confirmed melee and ranged contacts emit metal flecks/dust, wood chips/dust, or pale glass chips according to material. Impact RPC carries the material byte (0 metal, 1 wood, 2 glass; protocol 13). Blast detonation VFX: epicenter fireball wash (atmos-fire palette), point light, boom SFX, scorch decals, distance-scaled shake + blast screen flash — via `TileSubSystem` ObserversRpc. Explosive items and repair remaining: [station_structural_damage.plan.md](../../plans/station_structural_damage.plan.md).

Steel walls (including reinforced steel) and steel tables now leave deterministic procedural faceted folded-panel wreckage after a short accelerating fall with a small impact bounce. Breakup emits a soft mottled dust cloud followed by a wider ground-impact puff; both expand and fade within roughly 1.7 seconds and only play for live destruction, not late joins. No matching rubble models exist in the art indexes. Wreckage has no collision or occupancy, is replicated for late joiners, and is session-only (not saved or salvageable). Rebuilding on the same tile/layer removes it; map clear and disconnect clean it up. Other materials retain the existing removal behavior.

Wall removal also opens the existing atmos edges: [atmospherics](atmospherics.md) now drives wind streaks, pressure-driven knockdown and pull from the resulting gradient, without a separate destruction impulse.

## Start here

- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralDamageSubSystem.cs` — self-bootstrap; `TryApplyStructuralDamage` / `TryGetIntegrity` / `ResolveBlast` (+ VFX broadcast)
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralDamageService.cs` — server apply + Destroyed clear
- `Assets/Scripts/SS3D/Systems/StructuralDamage/SteelWreckageRecord.cs` / `SteelWreckageView.cs` — pre-clear bounds capture, procedural folded steel and collapse animation
- `Assets/Scripts/SS3D/Systems/Tile/TileSubSystem.Wreckage.cs` — session wreckage SyncList, unbuffered breakup RPC, placement cleanup (protocol 14)
- `Assets/Scripts/SS3D/Systems/StructuralDamage/BlastResolutionService.cs` — BFS blast spread
- `Assets/Scripts/SS3D/Systems/StructuralDamage/BlastVfxPresenter.cs` — client detonation FX (catalog-driven)
- `Assets/Scripts/SS3D/Systems/StructuralDamage/BlastVfxCatalog.cs` — Resources catalog + falloff tuning
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralIntegrityPresenter.cs` — stage overlays or fallback tint + optional WindLight hiss
- `Assets/Scripts/SS3D/Systems/StructuralDamage/SteelWallDamageVfx.cs` — mesh overlays and short contact particles using `Assets/Resources/StructuralDamage/SteelWallDamage.shader`
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralIntegrityRules.cs` — supported targets, layer-aware lookup, provisional HP thresholds / stage fractions
- `Assets/Scripts/SS3D/Systems/Tile/StructuralIntegrityStage.cs` — Intact / Damaged / Cracked / Destroyed
- `Assets/Scripts/SS3D/Systems/Tile/PlacedObjects/PlacedTileObject.cs` — SyncVar integrity + `ServerSetIntegrity` → presenter
- `Assets/Scripts/SS3D/Systems/Tile/TileSubSystem.cs` — `ServerNotifyBlastDetonated` / `RpcBlastDetonated`
- `Assets/Scripts/SS3D/Systems/Examine/StructuralIntegrityExaminable.cs` — Damaged/Cracked examine sections
- `Assets/Scripts/SS3D/Systems/Tile/TileOccupancyEvaluator.cs` — Cracked → not airtight
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/HurtStructureCommand.cs` — admin `hurtstructure [force]`
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/BlastCommand.cs` — admin `blast [yield] [falloff]`
- `Assets/Scripts/SS3D/Systems/StructuralDamage/Editor/StructuralDamageContentPrefabRecipes.cs` — **SS3D → Structural Damage → Run Content Prefab Recipes**
- `Assets/Scripts/SS3D/Systems/StructuralDamage/Editor/StructuralIntegrityPrefabSetup.cs` — wall/window, supported table, locker, machine and airlock presenter + examinable statics
- `Assets/Scripts/SS3D/Systems/StructuralDamage/Editor/BlastVfxSetup.cs` — blast catalog/prefab/scorch statics
- `Assets/Scripts/Tests/EditMode/StructuralDamageTests.cs` / `BlastResolutionTests.cs` / `StructuralIntegrityPresentationTests.cs` / `BlastVfxFalloffTests.cs`

## Extension points

- Call `StructuralDamageSubSystem.TryApplyStructuralDamage(coord, force, source)` from melee, ranged hitscan, blast hops, chemistry, etc. — one apply path. Lookup prefers structural Turf, then supported furniture on FurnitureBase; destruction clears only the chosen layer. Sources include `StructuralDamageSource.Melee` / `Ranged` / `Blast` / `Console`.
- Call `StructuralDamageSubSystem.ResolveBlast(epicenter, yield, falloff)` from grenades/charges (Phase 5) — also broadcasts blast VFX.
- Override max HP per SO via `TileObjectSo.structuralMaxIntegrity` (> 0).
- Wall/window, supported table, locker, machine and airlock prefabs + blast assets: **SS3D → Structural Damage → Run Content Prefab Recipes**; assign boom `AudioClip` on `BlastVfxCatalog`.

- Metal furniture is opt-in via `TileObjectSo.structuralDamageEnabled` on FurnitureBase; `StructuralIntegrityPrefabSetup` owns the explicit locker/machine asset list. Tables opt in by generic/specific material.

## Pitfalls

- **Area reflood must not run inside `OnTileCleared` synchronously:** `TileMap` notifies before the occupant is removed; immediate flood still sees the wall/door. Area queues `_pendingLiveBoundaryRecompute` and flushes next `UpdateEvent` (same class of bug as `AtmosTileObserver` defer).
- **Cracked still blocks Area expansion and blast hops:** only Destroyed (clear) opens boundaries; Cracked only flips `IsAirtight` for atmos.
- **Cracked is not a slow room↔room leak yet:** design wants a slow vent; `IsAirtight=false` only changes the wall cell's atmos state. Neighbour `CanFlow` still uses binary `BlockedEdges` — real permeability deferred (document fork until ShareGasJob / edge rate exists).
- **Do not bind TileQueryService at Awake:** `StructuralDamageSubSystem` self-bootstraps before `TileSubSystem` creates its map. Resolve query/construction lazily from the current `TileSubSystem` or `TryApplyStructuralDamage` / `ResolveBlast` always miss.
- **Structural melee ray length ≠ hand range:** camera aim rays must cast ~8m (like living zones), then check `RangeLimit` from the **entity root** (not the swinging hand bone) to the hit/closest point. Hand-bone reach during windup often fails adjacent walls; cardinal-ahead is the last fallback.
- **Blast BFS keeps max force per tile:** weaker revisit paths are skipped; a stronger cascade path must still enqueue.
- **Blast hop checks mirror atmos `CanFlow` locally:** do not call `AtmosNeighbourBuilder` (`internal`); keep the BlockedEdges bit test in `BlastResolutionService`.
- **Open blast hops require `HasPlenum`:** empty chunk cells still return occupancy with `BlockedEdges=0`; without a plenum gate the BFS floods the void and flanks doors/walls from the side (EditMode: door took 30+10, thin wall 65+35→Destroyed).
- **Never assign integrity SyncVars without a spawned NetworkObject:** FishNet `SyncBase.IsNetworkInitialized` NREs when `_networkObjectCache` is null (common for door/floor prefabs that get `PlacedTileObject` via `AddComponent` at place time). `ServerSetIntegrity` falls back to local fields when the NB cache is missing or not spawned (same guard pattern as `SetDirection`). Server damage/clear still works; clients will not see stage SyncVars on those tiles until prefabs bake `PlacedTileObject`.
- **Host SyncVar OnChange may skip:** `ServerSetIntegrity` always calls `StructuralIntegrityPresenter.Apply` so host visuals and hiss update without relying on FishNet OnChange.
- **Intact must clear MaterialPropertyBlocks:** writing a white tint on Intact breaks SRP Batcher on every undamaged wall. `Apply(Intact)` uses `SetPropertyBlock(null)`. Hit 2026-07-28.
- **Walls and tables swap meshes with adjacency:** `TileAdjacencyView` refreshes the overlay after applying a new mesh. Table projection clips legs and undersides; airlock overlays must remain children of the moving leaves. Overlay renderers follow their source renderers’ visibility, including FishNet host visibility and girder construction meshes. Contact bursts are unbuffered and require a measured surface point and normal.
- **Blast VFX Rpc must RunLocally and must not BufferLast:** one-shot boom; host needs `RunLocally = true`. Scorch decal materials stay **Opaque** (same URP Decal pitfall as blood).

- **Destroying storage must release contents before clearing the tile:** `AttachedContainer.ReleaseForDestruction` bypasses removal locks; skip containers belonging to stored items so backpacks retain their contents. Damage overlays likewise exclude item descendants.

## Tests

- `StructuralDamageTests`: steel/wood-table stages and destruction preserve flooring/plenum; table material eligibility includes glass.
- `StructuralIntegrityPresentationTests`: tabletop/leaf selection, moving surface inheritance, stage changes, repair clearing, and wall adjacency regression.
- Run `Testing/ui/validate.sh --steps compile,editmode,visual`; inspect object renders separately from the lobby visual smoke.

## Depends on / Used by

- **Depends on:** [inventory](inventory.md) (release storage contents), [tile](tile.md), [area](area.md), [atmospherics](atmospherics.md) (observer refresh), [health](health.md) (blast crew brute), [examine](examine.md), [screen-effects](screen-effects.md) (blast flash), [core-subsystems](core-subsystems.md)
- **Used by:** [combat](combat.md) melee connect; Phase 5+ explosive items

## Related docs

- Design: [Documents/design/explosives-destruction.md](../../design/explosives-destruction.md)
- Effort: [2026-07_structural-destruction.md](../2026-07_structural-destruction.md)
- Related: [2026-07_srp-batcher-gpu-instancing.md](../2026-07_srp-batcher-gpu-instancing.md) (Intact MPB clear)
- Plan: [station_structural_damage.plan.md](../../plans/station_structural_damage.plan.md)
- [INDEX.md](../INDEX.md)
