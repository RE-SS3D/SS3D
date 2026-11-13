> Parent map: [combat.md](combat.md)
> Code paths: Assets/Scripts/SS3D/Systems/Combat/
> Status: partial
> Verified: 442be459e — 2026-11-13

# Flamethrower and surface napalm

## Overview

The spawnable `Flamethrower` uses its SS3D-Art model, rifle hold and `RangedVisualStyle.Flame`. Harm-primary repeats server-validated fuel ticks at 0.16 s (60 ticks, 4 s timed refill). The server samples nine rays across a 50° horizontal / 12° vertical fan, four metres from its own muzzle. A body takes at most one direct hit per fuel tick (4 burn), with existing armor and invincibility rules. Client aim supplies direction only; the server owns origins, reach and contacts. Fan rays ignore trigger volumes. Spraying a surface closer than 1.1 m splashes fuel back and coats the shooter at the nearest limb; walking into any burning pool coats the shooter like anyone else.

Actual non-living contacts deposit fuel for six seconds. Nearby contacts on the same collider/normal merge and refresh. Up to 64 deposits can burn, with 128 total records retaining cold black scorches after fuel burns out. Collider-local anchors follow moving surfaces; removed surfaces discard their marks. Cold records are evicted oldest first at the bound. `TileSubSystem.ClearMarkers` clears surface marks and body coatings on map reset; hub shutdown clears client views.

A 4 Hz SimulationClock channel tests actual enabled limb colliders against the visible flame volume and shared LOS. Touching a pool, or taking a direct spray hit, coats the person for six seconds; leaving the pool does not remove the coating. Contact refreshes one timer per body, never stacking DPS across overlapping pools. Coatings deal 6 burn/s as one hit each second through ordinary health/armor/invincibility rules. The one-second packet avoids flat armor absorption erasing every tiny simulation-tick hit. Up to 64 bodies can be coated. Their visual flames attach to the hit-zone bone and follow it as the person moves.

`NapalmSubSystem` sends capped buffered snapshots containing surface positions/normals, remaining fuel, cold scorches and body object ids/zones. Body ids are resolved only after that object is present in the observer's spawn table, avoiding unspawned references during join/AOI changes. Snapshots go out only on change (deposit, refresh, expiry, surface move, coating start or end); clients count remaining fuel down locally. Protocol 51 gates the extended snapshot contract. No client sends deposits or damage.

The jet emits continuously from the live muzzle (420 particles/s, 12 m/s) across the same fan, with world collision, slight sag and growth. Fuel ticks refresh a short lease independently of emission. There is no cursor-end emitter. `NapalmSurfaceVfx` presents surface stains, rising flames, smoke and light, or a bone-attached coating without a surface stain. Scorches are URP `DecalProjector`s (blast scorch material, `Resources/Combat/NapalmScorch.png` mask, world-decal rendering layer). They fade in over the last 2.5 s of fuel and are complete once the flames die; re-spraying never un-chars a surface. Cold views disable their `Update`. One pooled occluded crackle follows the nearest burning patch/person; cold scorches are silent.

No atmos ignition, body-to-body contagion, automatic burning-object spread, removable tanks, extinguishing interaction or save-file persistence yet. Scorches last until surface removal, map reset or bounded eviction. Balance remains provisional.

## Start here

- `CombatInteractionNetwork.Napalm.cs` — server fan, deduplicated direct hits, real surface contacts
- `NapalmSprayRules.cs` — fan dimensions, limb-volume contact and shared cover checks
- `NapalmPatchSet.cs` — bounded burning/cold surface state, collider anchors and extended snapshot payload
- `NapalmSubSystem.cs` — surface contact, body fuel timers, coalesced damage and buffered snapshots
- `FlamethrowerVfx.cs`, `NapalmSurfaceVfx.cs`, `Assets/Resources/Combat/Flame.shader` — continuous spray and surface flames; scorch decals in `NapalmSurfaceVfx`
- `Editor/FlamethrowerPrefabSetup.cs`, `Editor/NapalmHubRecipe.cs` — weapon recipe and appended hub behaviour; both under the combat aggregator

## Extension points

- Keep fuel lifetime and damage authoritative; particle collisions are presentation only.
- Append hub behaviours through the recipe and rebuild FishNet indices. Keep the full hub rebuild type list in sync.
- Extend patch snapshot/state for future extinguishing; never infer damage from client particles. Resolve body ids locally after spawn rather than sending buffered object references.

## Pitfalls

- **Walking through visible flames caused no damage:** sampling only `health.transform.position + up * 0.5` misses flame contacts at limbs and chest-height wall fires. Use actual enabled zone-collider volumes, including the visual edge of the fire.
- **Clothes erased continuous burn:** 0.25 s micro-hits were smaller than flat per-hit armor absorption. Accumulate exposure into one-second burn packets; keep armor and invincibility in the health chokepoint.

- A cursor endpoint can be empty air or a living hit. Emitting an impact flare there creates a floating ball at the mouse. Surface fire must come from real server geometry contacts, and must not be emitted on every damage tick.
- **Floating fires and marks off the surface:** physics queries hit triggers by default (`m_QueriesHitTriggers: 1`), so fuel landed on invisible interaction volumes. Pass `QueryTriggerInteraction.Ignore` to every napalm ray. Mesh discs also overhung wall and tile edges; scorches are decals so only real receivers get marked.
- Overlapping rays/pools must not multiply damage: deduplicate direct hits within a fuel tick, and ongoing exposure across all pools per world tick.

## Tests

- `NapalmTests` (EditMode): merge/refresh/expiry into cold scorch, moving/removed surfaces, independent burning/scorch bounds, limb contact, cover, fan geometry and FishNet hub registration.
- `NapalmScenarios` (focused PlayMode category `Napalm`): real host contact damage, moving away while coated, body-attached presentation, burn expiry and a fully revealed scorch decal.
- Validate with `Testing/ui/validate.sh --steps compile,editmode,scenarios:Napalm`; then run `--steps visual` separately after Test Runner scene restoration. Inspect a fan/surface-fire capture in addition to the general UI pilot.
- Separate-client observation and join-in-progress acceptance remain manual checks.

## Depends on / Used by

- Depends on [health](health.md), [audio](audio.md), [core-subsystems](core-subsystems.md), [tile](tile.md).
- Used by [combat](combat.md) flamethrower content.

## Related docs

- [Combat implementation plan](../../plans/combat_implementation_plan.md)
- [Composition policy](../2026-07_agent-first-composition.md)
