> Implements: Documents/design/explosives-destruction.md §2 (blast BFS), §3 (structural stages), §4 (atmos on Destroyed; areas unchanged since the 2026-10 review), §5 (crew brute), §1/§7 (world presentation)
> Touches systems: structural-destruction, tile, area, atmospherics, health, examine, screen-effects
> Status: in-progress

# Structural destruction — integrity + blast

## Goal

Ship per-tile structural integrity for Turf walls/doors/windows and hop-based blast resolution that terminates in the same Apply path.

## Phase 1 shipped surface

- `StructuralDamageSubSystem` (self-bootstrap) + `StructuralDamageService.TryApplyStructuralDamage`
- Integrity SyncVars on `PlacedTileObject`; optional `TileObjectSo.structuralMaxIntegrity`
- Cracked → `TileOccupancy.IsAirtight = false` (wall still blocks Area expansion)
- Destroyed → `ConstructionService.TryClearTile` (steel walls/tables leave procedural wreckage)
- Area `OnTileCleared`/`OnTilePlaced` queues deferred `RefloodAllAreaTilesPreservingMetadata` (not yet true local-region flood)
- Console: `hurtstructure [force]`
- EditMode: `StructuralDamageTests`; Area door-clear live recompute harness test

## Phase 2 shipped surface

- `MeleeWeaponProfile.StructuralForce` + `ResolveStructuralForce()`
- `MeleeHitInteraction` connect: living first, else `MeleeStructuralHitResolver` → `TryApplyStructuralDamage`
- Crowbar 35 / hatchet 28 / fists 5 / knife 4 provisional force

## Phase 3 shipped surface

- `BlastResolutionService.Resolve(epicenter, yield, falloff)` — BFS, subtractive falloff, max-force revisit
- Blocked-edge apply to structural turf; Destroyed mid-pass cascades in the same resolve
- Crew chest brute on visited tiles (`EntitySubSystem.SpawnedPlayers` + `FindObjectsByType`)
- Console: `blast [yield] [falloff]` (defaults 120 / 25)
- EditMode: `BlastResolutionTests` (corridor, sealed door cascade, cracked wall blocks)

## Phase 4 shipped surface

- `StructuralIntegrityPresenter` — steel wall overlays, fallback tint, and optional `WindLight` loop at Cracked
- `PlacedTileObject` integrity SyncVar OnChange + explicit host `Apply` from `ServerSetIntegrity`
- `StructuralIntegrityExaminable` — Damaged/Cracked examine sections (fallback English; table keys via Examine menu)
- Editor: **SS3D → Structural Damage → Setup Wall Integrity Presentation** (walls/windows with baked `PlacedTileObject`)
- Atmos slow-leak **deferred** — keep binary `IsAirtight`; BlockedEdges still seal room↔room flow (documented fork)

## Blast detonation VFX (post Phase 4)

- `ResolveBlast` → `TileSubSystem.ServerNotifyBlastDetonated` → `RpcBlastDetonated` (`RunLocally`, no BufferLast) → `BlastVfxPresenter`
- Epicenter fireball wash (atmos fire-core palette `(1, 0.55, 0.15)`), point light, boom clip (catalog), floor scorch DecalProjector
- Distance-scaled `CameraFollow.AddImpulse` + `ScreenEffectsSubSystem.TriggerBlastFlash`
- Catalog: `Assets/Resources/BlastVfxCatalog.asset`; content under `Assets/Content/WorldObjects/World/VFX/Structural/`
- Editor: **SS3D → Structural Damage → Setup Blast VFX Assets**

## Deviations / notes

- Phase 1 Area recompute is **full reflood preserving metadata**, deferred one Update tick because `TileMap` notifies clear **before** occupant removal (same pitfall as atmos). True local flood fill is follow-up.
- Steel wall/table wreckage uses procedural folded steel, synchronized per session with an unbuffered breakup animation. Other materials await wreckage; salvage and save persistence remain deferred.
- Blast hop rules duplicate atmos `CanFlow` bit checks locally (avoid `AtmosNeighbourBuilder` internal coupling).
- Steel walls use shader overlays on their current adjacency meshes. See the [system map](systems/structural-destruction.md#overview) for coverage.
- Blast VFX copies atmos fire *look* only — does not write `_AtmosFire` atlas or fake combustion.
- Boom `AudioClip` on catalog left unassigned until SFX exists.
- Doors: presentation/examine wait on baking `PlacedTileObject` onto airlock prefabs (Phase 1 SyncVar pitfall).
- Items / repair: later phases in [station_structural_damage.plan.md](../plans/station_structural_damage.plan.md).

## Related

- Plan: [Documents/plans/station_structural_damage.plan.md](../plans/station_structural_damage.plan.md)
- Design: [Documents/design/explosives-destruction.md](../design/explosives-destruction.md)
- System map: [systems/structural-destruction.md](systems/structural-destruction.md)
