> Code paths: Assets/Scripts/SS3D/Systems/StructuralDamage/ (plus integrity stage, occupancy and blast VFX Rpc hooks in Systems/Tile/, mapped by [tile](tile.md))
> Entry points: StructuralDamageSubSystem, StructuralDamageService, BlastResolutionService, BlastVfxPresenter, StructuralIntegrityPresenter, HurtStructureCommand, BlastCommand
> Status: partial
> Verified: 7785df8dc — 2026-11-06

# Structural destruction

## Overview

Per-tile integrity for Turf walls, doors, windows, and FurnitureBase steel/wood/glass tables and opted-in metal furniture per [explosives-destruction.md](../../design/explosives-destruction.md) §3. Phase 1–3: accumulate force → stages → Destroyed clear; melee StructuralForce; hop blast BFS + crew brute and [body push](structural-destruction-explosives.md). Phase 4: all structural wall variants, including windows and the girder, show a dent at Damaged and a torn seam at Cracked. Steel tables use tabletop-only dents/tears; wooden tables use chipped gouges and grain-following splits with exposed fibres; glass tables grow an impact star into branching fractures; lockers mark body/door meshes; opted-in machines mark side casings to preserve front displays; civilian and security airlocks mark each moving leaf, excluding control panels and generated floors. Bounds-based placement has stable per-tile variation. Other supported surfaces use MPB tint. Cracked walls add a hiss; steel tables/airlocks use torn-metal examine feedback, wood uses gouged/split-wood text, and glass uses chipped/fractured-glass text. Tables and airlocks do not play the hull-leak hiss. Confirmed melee and ranged contacts emit metal flecks/dust, wood chips/dust, or pale glass chips according to material. Impact RPC carries the material and a sound cue (protocol 17). Material comes from `StructuralMaterials.Resolve`, which reads `impactMaterial` on `TileObjectSo` for exceptions such as windows. Every landed melee, ranged or console hit sounds like its material on all clients, reaching Cracked adds a strain, and a break plays the material's break sound. Steel wreckage thuds when it lands. A blast is one boom, or a distant rumble past 30 m, with a tail of at most three breaks nearest the listener. Blast detonation VFX: epicenter fireball wash with expanding mottled smoke lasting about three seconds (atmos-fire palette), point light, scorch decals, body-distance-scaled heavy shake (0.75 m base amplitude, 0.85 s) + full-frame exposure flash — via `TileSubSystem` ObserversRpc. C4, X4 and syndicate minibomb content are mapped in [explosive items](structural-destruction-explosives.md): replicated countdown audio, attachment, X4 directional force and a throwable five-second minibomb. Proximity alert, remote triggers and repair remain in [station_structural_damage.plan.md](../../plans/station_structural_damage.plan.md).

Steel walls (including reinforced steel) and steel tables now leave deterministic procedural faceted folded-panel wreckage after a short accelerating fall with a small impact bounce. Breakup emits a soft mottled dust cloud followed by a wider ground-impact puff; both expand and fade within roughly 1.7 seconds and only play for live destruction, not late joins. No matching rubble models exist in the art indexes. Wreckage has no collision or occupancy, is replicated for late joiners, and is session-only (not saved or salvageable). Rebuilding on the same tile/layer removes it; map clear and disconnect clean it up. Other materials retain the existing removal behavior.

Wall removal also opens the existing atmos edges: [atmospherics](atmospherics.md) now drives wind streaks, pressure-driven knockdown and pull from the resulting gradient, without a separate destruction impulse.

Blast floors and the hull beneath them now take independent accumulated damage: weaker blasts expose plating; stronger blasts open holes to space, destroy supported infrastructure and vent the room. See [floor and hull damage](structural-destruction-floors.md).

## Start here

- [Floor and hull damage](structural-destruction-floors.md) — deferred blast damage, floor/plenum integrity, vacuum consequences and floor prefab recipe

- `Assets/Scripts/SS3D/Systems/StructuralDamage/TimedCharge.cs` / `ChargeCountdownDisplay.cs` — explosive profiles, fuse/attach/throw/defuse interactions, recorded countdown and physical LED readout
- [Explosive items](structural-destruction-explosives.md) — C4/X4/minibomb behavior, countdown lifecycle, content recipe and tests

- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralDamageSubSystem.cs` — self-bootstrap; `TryApplyStructuralDamage` / `TryGetIntegrity` / `ResolveBlast` (+ VFX broadcast)
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralDamageService.cs` — server apply + Destroyed clear
- `Assets/Scripts/SS3D/Systems/StructuralDamage/SteelWreckageRecord.cs` / `SteelWreckageView.cs` — pre-clear bounds capture, procedural folded steel and collapse animation
- `Assets/Scripts/SS3D/Systems/Tile/TileSubSystem.Wreckage.cs` — session wreckage SyncList, unbuffered breakup RPC, placement cleanup
- `Assets/Scripts/SS3D/Systems/StructuralDamage/BlastResolutionService.cs` — BFS blast spread
- `Assets/Scripts/SS3D/Systems/StructuralDamage/BlastVfxPresenter.cs` — client detonation FX (catalog-driven); `Assets/Resources/StructuralDamage/BlastFire.shader` supplies the additive fireball, `CollapseDust.shader` the smoke
- `Assets/Scripts/SS3D/Systems/StructuralDamage/BlastVfxCatalog.cs` — Resources catalog + falloff tuning
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralAudioPresenter.cs` — client sounds for impacts, breaks and blasts, added by `TileSubSystem`; clip ids in `StructuralAudioTrackIds.cs`
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralSoundScheduler.cs` — pure timing: break hold, thud delay, capped blast debris
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralImpactCue.cs` — wire cue and the rule that picks it
- `Assets/Scripts/SS3D/Systems/Tile/StructuralMaterial.cs` — material enum and the one resolve function
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
- Wall/window, supported table, locker, machine and airlock prefabs + blast assets: **SS3D → Structural Damage → Run Content Prefab Recipes**.
- Give an object a different impact sound with `TileObjectSo.impactMaterial`. New clips go in `StructuralAudioTrackIds`.

- Wall-mounted machines that break rather than clear (the APC): add `BreakableDevice`. Ranged and melee find it by its collider (`BreakableDeviceResolver`) before the wall behind; `StructuralDamageService` passes blast force to the wall mounts on each tile it hits, before the wall itself (a destroyed wall still takes its mounts with it).

- Metal furniture is opt-in via `TileObjectSo.structuralDamageEnabled` on FurnitureBase; `StructuralIntegrityPrefabSetup` owns the explicit locker/machine asset list. Tables opt in by generic/specific material.

## Pitfalls

- **Opening holes during BFS erases the blast's own path:** defer floor/plenum damage until traversal finishes. X4 must finish both radial and cone passes before applying their combined floor force.

- **Orbiting camera position is not blast distance:** camera zoom can put it 15 m from a nearby explosion and suppress feedback. Use the audio listener, which follows the controlled body, before falling back to a camera.

- **Defuse must redeclare `IInteraction` on the `DelayedInteraction` subclass:** otherwise its `Priority` property is ignored by interface dispatch (default 0) and pickup wins over the tool action. `TimedChargeTests` guards the dispatch.
- **An attached charge is kinematic only while its wall stands:** `TimedCharge.Update` keeps `_attached` charges kinematic, and nothing used to clear it when another blast destroyed the wall, so the charge hung in the air (#213). The server now drops it when `_targetTile` has no wall or door on Turf. `ExplosivesScenarios.AttachedCharge_FallsWhenItsWallIsDestroyed`. The map editor's item delete (`TileMap.ClearItemObject`) matches by name within 1 m and must take the nearest match: it took the last one, so in a pile of C4 it deleted a different charge than the one clicked.

- **Unspawned charge previews have no FishNet behaviour cache:** `NetworkBehaviour.TimeManager` can throw before initialization, even in a null check. Query the root `NetworkObject.TimeManager` safely, and skip runtime Update until spawned.
- **C4 FBX has scale/axis conversion on its imported root:** baking only `model.worldToLocalMatrix * mesh.localToWorldMatrix` drops it, leaving a tiny upright mesh. Bake the mesh transform including the model root; content tests check dimensions and floor orientation.

- **Area reflood must not run inside `OnTileCleared` synchronously:** `TileMap` notifies before the occupant is removed; immediate flood still sees the wall/door. Area queues `_pendingLiveBoundaryRecompute` and flushes next `UpdateEvent`; authored maps then skip the reflood (same class of bug as `AtmosTileObserver` defer).
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
- **A blast's breakups reach clients before the blast message:** `BlastResolutionService` clears tiles first, and the scorch raycast depends on that. `StructuralSoundScheduler` holds breaks 50 ms and judges them on their own timestamps, so a frame hitch can't turn debris into full breaks.
- **Blast hops call the service directly, not `StructuralDamageSubSystem.TryApplyStructuralDamage`:** they send no per-hit impact, which is what keeps a blast one event. Break messages live in `StructuralDamageService` so blasts still reach them.
- **Blast VFX Rpc must RunLocally and must not BufferLast:** one-shot boom; host needs `RunLocally = true`. Scorch decal materials stay **Opaque** (same URP Decal pitfall as blood).

- **Destroying storage must release contents before clearing the tile:** `AttachedContainer.ReleaseForDestruction` bypasses removal locks; skip containers belonging to stored items so backpacks retain their contents. Damage overlays likewise exclude item descendants.

## Tests

- `TimedChargeTests`: physical content scale, countdown/clip wiring, wirecutter-only defuse, expiry gating. Cone propagation and barrier tests are in `BlastResolutionTests`; separate-client acceptance remains.

- `StructuralAudioTests`: material resolve, cue rules, clip registration, break hold, debris cap, frame hitch.
- `StructuralDamageTests`: direct steel/wood-table hits preserve flooring/plenum; table material eligibility includes glass. `BlastFloorDamageTests` covers the separate blast-floor path and atmosphere consequences.
- `StructuralIntegrityPresentationTests`: tabletop/leaf selection, moving surface inheritance, stage changes, repair clearing, and wall adjacency regression.
- Run `Testing/ui/validate.sh --steps compile,editmode,visual`; inspect object renders separately from the lobby visual smoke.

## Depends on / Used by

- **Depends on:** [inventory](inventory.md) (release storage contents), [tile](tile.md), [area](area.md), [atmospherics](atmospherics.md) (observer refresh), [health](health.md) (blast crew brute), [entities](entities.md) (blast push), [examine](examine.md), [screen-effects](screen-effects.md) (blast flash), [core-subsystems](core-subsystems.md)
- **Used by:** [combat](combat.md) melee connect; C4/X4/minibomb items; [furniture](furniture.md) (tables, lockers, machines, airlocks)

## Related docs

- Design: [Documents/design/explosives-destruction.md](../../design/explosives-destruction.md)
- Effort: [2026-07_structural-destruction.md](../2026-07_structural-destruction.md)
- Related: [2026-07_srp-batcher-gpu-instancing.md](../2026-07_srp-batcher-gpu-instancing.md) (Intact MPB clear)
- Plan: [station_structural_damage.plan.md](../../plans/station_structural_damage.plan.md)
- [INDEX.md](../INDEX.md)
