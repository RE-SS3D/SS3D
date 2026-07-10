> Implements: Documents/design/area.md §data model, §flood-fill authoring (partial — APC-seeded variant)
> Touches systems: area, tile, machine-interface (APC), electricity (hooks only)
> Status: in-progress

# Area Foundation (Phases 0–2)

## Goal

Ship APC-seeded area flood-fill with per-tile storage, save/load, overlap diagnostics, and edit-mode tests — without power gating or lighting visuals.

## Shipped (Phases 0–2)

1. **Boundary rules** — `AreaBoundaryEvaluator`: plenum walkability, turf walls/doors block expansion.
2. **Storage** — per-chunk `ushort[]` area ids on `TileChunk`; `SavedAreaRecord` / `SavedTileChunk.areaIds`; `ITileQueryService.TryGetAreaId`.
3. **Subsystem** — `AreaSubSystem` on `Game.unity` `AreaSystem`; APC register/unregister; rebuild on map load.
4. **Flood fill** — `AreaFloodFillService`: first-wins APC order, door-tile post-pass, wall-mount seeding from `FacingDirection`.
5. **APC UI** — overlap warning in APC machine interface when multiple APCs share a flood-filled region.
6. **Dev tooling** — Scene-view area gizmos (`SS3D → Dev → Areas → Show Area Gizmos`).
7. **Power gating** — per-consumer APC channel resolution in `Circuit`.
8. **Lighting data** — `AreaLightingState` derivation + transition event (no fixture wiring).
9. **Tests** — `AreaFloodFillTests`, `AreaBoundaryEvaluatorTests`, `CircuitAreaChannelTests`, `AreaLightingStateDeriverTests`.

## Deferred

- Phase 4 visuals: `LightPower` tri-state, fixture subsets
- Phase 5: live tile-mutation boundary recompute; editor merge/split UI
- Area-scoped APC cell backup (battery drain remains circuit-wide equal split)

## Documented fork deviations

- APC-seeded flood fill instead of generic unnamed regions ([area.md](../design/area.md) §3)
- Tiles outside APC reach remain `AreaId.None`
- All doors block expansion (open/closed irrelevant)
- Wall-mounted APCs seed from the tile the mount faces, not all adjacent walkable tiles

## Validation

- APC in enclosed room → interior floor tiles get that APC's area id (host/server).
- Two APCs in separate walled rooms → distinct areas, no overlap warning.
- Two APCs in open space → first-wins tiles; overlap diagnostic on both APC panels.
- Dev gizmos show per-tile colors and APC facing line in Scene view during play.
