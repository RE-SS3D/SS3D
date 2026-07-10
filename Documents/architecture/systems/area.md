> Code paths: Assets/Scripts/SS3D/Systems/Area/
> Entry points: AreaSubSystem, AreaFloodFillService, AreaBoundaryEvaluator
> Status: partial

# Area

## Overview

APC-seeded area flood-fill: each APC owns one `AreaRecord` and claims reachable floor tiles with a per-chunk `ushort[]` area-id layer. Walls and doors block expansion; unclaimed tiles stay `AreaId.None`. Live boundary recompute on tile mutation is deferred — rebuild runs on map load and APC place/remove only.

**Fork deviations from** [area.md](../../design/area.md): areas are APC-seeded (not generic auto-detection); unclaimed tiles have no fallback area; all doors block expansion regardless of open/closed state. Wall-mounted APCs seed flood fill from the walkable tile **in front of** `FacingDirection`, not from every cardinal neighbor.

## Start here

- `Assets/Scripts/SS3D/Systems/Area/AreaSubSystem.cs` — registry, APC lifecycle, rebuild orchestration
- `Assets/Scripts/SS3D/Systems/Area/AreaFloodFillService.cs` — BFS from APC seeds, door-tile post-pass
- `Assets/Scripts/SS3D/Systems/Area/AreaBoundaryEvaluator.cs` — walkability and expansion blocking rules
- `Assets/Scripts/SS3D/Systems/Area/AreaRegistry.cs` — `AreaRecord` storage and APC reverse lookup
- `Assets/Scripts/SS3D/Systems/Area/IAreaApcOrigin.cs` — APC contract (`OriginTile`, `FacingDirection`)
- `Assets/Scripts/SS3D/Systems/Area/AreaDevSettings.cs` — dev toggle (`SS3D → Dev → Areas → Show Area Gizmos`)
- `Assets/Scripts/SS3D/Systems/Area/AreaDebugGizmoDrawer.cs` — Scene-view tile overlay (host/server map only)
- `Assets/Scripts/Tests/EditMode/AreaFloodFillTests.cs` — flood-fill and boundary edit-mode tests

## Extension points

- Resolve area for a tile: `AreaSubSystem.TryGetAreaForTile` / `ITileQueryService.TryGetAreaId`.
- Register APC origins: implement `IAreaApcOrigin` (see `ApcController`).
- Server rename/tag API: `AreaSubSystem.RenameArea`, `SetParentTag` (no editor UI yet).
- **Not yet wired:** per-consumer APC channel gating ([electricity](electricity.md)), lighting state derivation.

## Depends on / Used by

- **Depends on:** [tile](tile.md) (`TileMap` area-id storage, `ITileQueryService`, save/load)
- **Used by:** [machine-interface](machine-interface.md) (APC overlap diagnostic); [electricity](electricity.md) (planned area→APC resolver)

## Related docs

- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md)
- Architecture effort: [2026-07_area-foundation](../2026-07_area-foundation.md)
- Design (read-only): [Documents/design/area.md](../../design/area.md)
