> Parent map: [structural-destruction.md](structural-destruction.md)
> Code paths: Assets/Scripts/SS3D/Systems/StructuralDamage/, Assets/Scripts/SS3D/Systems/Tile/TileMap.cs
> Status: partial
> Verified: 7785df8dc — 2026-11-06

# Floor and hull damage

## Overview

Implements [explosives-destruction.md §3 Floors](../../design/explosives-destruction.md#3-structural-failure--multi-stage-per-tile). Blast force damages the removable Turf floor and the Plenum hull independently, using each object's existing integrity SyncVars. Default integrity is 60 for flooring and 200 for plenum; `TileObjectSo.structuralMaxIntegrity` overrides either. Damage accumulates across explosions. Direct melee/ranged structural hits retain their existing wall/furniture selection.

A destroyed floor reveals the existing underfloor plating, cables and pipes. Damaged or cracked plating remains a seal; only removing the plenum makes the tile vacuum. A hull hole clears unsupported structures and infrastructure through the existing tilemap rules, releases world-storage contents first, and notifies mutation observers for every cleared layer. Atmos queues the refresh, then the hole vents adjacent gas through the existing simulation; the tile keeps its area. Damage stages use the existing tint presenter and localized examine text, without a false leak/hiss warning for sealed plating.

Floor removal happens after blast traversal. The traversal records the strongest visit at each tile, so its new holes cannot cut off its own remaining hops. X4 resolves both its radial and directional passes before applying their combined force to the floor/hull. One C4 strips a wider ring and cracks the plating at its epicenter, but a second blast on that tile is needed to space it. X4 breaches fresh hull at its target and two tiles further through it, while the room side keeps its plating. The minibomb only dents plating. Values remain provisional balance tuning.

**Divergence from design:** [explosives-destruction.md §8 example E](../../design/explosives-destruction.md#8-worked-examples) has a charge set on a corridor floor breaching the plating. X4 can only be armed on a wall or door, so the only charge that can sit on a floor is C4, and that no longer breaches undamaged hull. This was the owner's balance call on 2026-11-06, in line with C4 in SS13 and SS14.

Hull repair/rebuilding remains the construction milestone's separate work. Damage uses the existing replicated integrity fields and tile despawns; no new protocol or persistence fields were added.

## Start here

- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralDamageService.Floors.cs` — layer damage, hull clear and storage release
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralIntegrityRules.cs` — floor eligibility and integrity defaults
- `Assets/Scripts/SS3D/Systems/StructuralDamage/BlastResolutionService.cs` — deferred floor callback and combined `ResolveShaped` traversal
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralDamageSubSystem.cs` — binds the floor callback on the live server
- `Assets/Scripts/SS3D/Systems/StructuralDamage/Editor/StructuralIntegrityPrefabSetup.cs` — `SetupFloors`, registered on the structural damage recipe aggregator
- `Assets/Scripts/SS3D/Systems/Tile/TileMap.cs` — cascade clear notifications
- `Assets/Scripts/SS3D/Systems/Examine/StructuralIntegrityExaminable.cs` — flooring/plating damage text

## Extension points

- Run **SS3D → Structural Damage → Run Content Prefab Recipes** to add presenters/examinables to floor/plenum prefabs. `SetupFloors()` is available for a focused rebuild.
- Custom blast resolvers should pass `StructuralDamageService.ApplyBlastFloorDamage` as the `BlastResolutionService` floor callback, after the crew callback. Call `ResolveShaped` when combining radial and cone forces.
- Override an individual floor/hull definition with `structuralMaxIntegrity` rather than changing the common thresholds.

## Pitfalls

- **No eager hull clearing during traversal:** losing `HasPlenum` mid-pass stops later hops. Defer floor damage until all passes finish, including X4's extra cone.
- **Cascade clears must notify each occupant:** clearing the hull previously deleted its pipes/cables without their observers seeing removal, leaving stale networks. `TileMap.ClearTileObject` now emits their clear notifications before destruction.
- **Cracked hull plating is still airtight:** do not give it the wall's leak/hiss examine text or refresh gas as though it were a breached wall.
- **Storage contents must survive loss of support:** release world-container contents before clearing the plenum; preserve contents of nested Item containers.

## Tests

- `BlastFloorDamageTests`: stage/accumulation, exposed plating stays sealed, full breaches clear and notify infrastructure, deferred propagation, X4 directional hull damage and actual atmosphere loss through a hull hole.
- `ExplosivesScenarios` (`scenarios:Explosives`): a real C4 in the test room cracks the plating and holds the air, surrounding plenums draw in the same frame, and a second blast breaches and vents.
- Related focused suites: `BlastResolutionTests`, `StructuralDamageTests`, `TileMapIntegrationTests`.
- Host acceptance checked real floor removal, a wider exposed-plating ring, a vacuum epicenter and pressure loss, before plenum integrity rose from 160 to 200. It has not been repeated since. Separate-client acceptance remains pending.

## Depends on / Used by

- **Depends on:** [tile](tile.md), [atmospherics](atmospherics.md), [inventory](inventory.md), [examine](examine.md)
- **Used by:** [explosive items](structural-destruction-explosives.md), [structural-destruction](structural-destruction.md)

## Related docs

- [Station structural damage plan](../../plans/station_structural_damage.plan.md)
- [MVP1](../../milestones/mvp1-nuke-ops.md) M3/M8
