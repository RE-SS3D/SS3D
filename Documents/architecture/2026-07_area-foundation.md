> Implements: Documents/design/area.md §data model, §flood-fill authoring (partial — APC-seeded variant)
> Touches systems: area, tile, machine-interface (APC), electricity
> Status: shipped

# Area Foundation (Phases 0–4)

## Goal

Ship APC-seeded area flood-fill with per-tile storage, save/load, overlap diagnostics, area-scoped power and lighting, and edit-mode tests.

## Shipped (Phases 0–4)

1. **Boundary rules** — `AreaBoundaryEvaluator`: plenum walkability, turf walls/doors block expansion.
2. **Storage** — per-chunk `ushort[]` area ids on `TileChunk`; `SavedAreaRecord` / `SavedTileChunk.areaIds`; `ITileQueryService.TryGetAreaId`.
3. **Subsystem** — `AreaSubSystem` on `Game.unity` `AreaSystem`; APC register/unregister; rebuild on map load.
4. **Flood fill** — `AreaFloodFillService`: first-wins APC order, door-tile post-pass, wall-mount seeding from `FacingDirection`.
5. **APC UI** — overlap warning in APC machine interface when multiple APCs share a flood-filled region.
6. **Dev tooling** — Scene-view area gizmos (`SS3D → Dev → Areas → Show Area Gizmos`).
7. **Power gating** — per-consumer APC channel resolution in `Circuit`.
8. **Area-scoped power** — `AreaApcPowerDistribution` draws grid headroom to each APC and drains its cell per area (see [electricity.md](systems/electricity.md)).
9. **Lighting data** — `AreaLightingState` derivation + transition event + client sync; disabled lighting channel forces Dark.
10. **Fixture visuals** — `LightPower` + `AreaLightFixturePolicy` + `LightFixtureCapability`; optional departmental tint on `AreaRecord`.
11. **Electricity follow-ups on branch** — kWh storage model, priority channel shedding, HV cable grid (generators/SMES/APC only), APC grid-in UI fix, SMES UI-only controls.
12. **Tests** — `AreaFloodFillTests`, `AreaBoundaryEvaluatorTests`, `CircuitAreaChannelTests`, `AreaApcPowerDistributionTests`, `AreaLightingStateDeriverTests`, `AreaLightFixturePolicyTests`, `AreaLightingStateQueryTests`, `ElectricCableConnectivityTests`.

## Deferred

- Live tile-mutation boundary recompute; editor merge/split UI
- Fixture subset authoring on `AreaRecord` (`NormalFixtures[]`, `EmergencyFixtures[]`)

## Documented fork deviations

- APC-seeded flood fill instead of generic unnamed regions ([area.md](../design/area.md) §3)
- Tiles outside APC reach remain `AreaId.None`
- All doors block expansion (open/closed irrelevant)
- Wall-mounted APCs seed from the tile the mount faces, not all adjacent walkable tiles
- Area consumers do not join the HV cable graph; only generators, SMES, and APCs cable-link

## Validation

- APC in enclosed room → interior floor tiles get that APC's area id (host/server).
- Two APCs in separate walled rooms → distinct areas, no overlap warning.
- Two APCs in open space → first-wins tiles; overlap diagnostic on both APC panels.
- Dev gizmos show per-tile colors and APC facing line in Scene view during play.
- Lights and vending machines on cabled tiles do not cable-link to each other; backbone devices still share HV circuits.
