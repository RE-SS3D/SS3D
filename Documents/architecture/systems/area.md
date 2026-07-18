> Code paths: Assets/Scripts/SS3D/Systems/Area/
> Entry points: AreaSubSystem, AreaFloodFillService, AreaBoundaryEvaluator
> Status: partial
> Verified: add2ad2c9 — 2026-07-18

# Area

## Overview

APC-seeded area flood-fill: each APC owns one `AreaRecord` and claims reachable floor tiles with a per-chunk `ushort[]` area-id layer. Walls and doors block expansion; unclaimed tiles stay `AreaId.None`. Live boundary recompute on tile mutation is deferred — rebuild runs on map load and APC place/remove only. Area metadata persists via [persistence](persistence.md) `AreaPersistenceContributor`; template restore uses `BeginTemplateRestore` / `RestoreFromSave` / `EndTemplateRestore` so saved display names, tints, access bits, and `lightingSwitchOn` survive when APCs are already placed.

Per-consumer power gating and **area-scoped APC cell drain** via [electricity](electricity.md) `AreaApcPowerDistribution`: devices in an assigned area use their area APC's channels and cell, not circuit-wide OR or equal battery split. `AreaLightingState` (Normal/Emergency/Dark) is derived each electricity tick from the area APC's circuit stats and `AreaRecord.LightingSwitchOn`; a disabled lighting channel forces Dark regardless of cell charge. Transitions fire `OnAreaLightingStateChanged` and sync to clients via ObserversRpc. Wall `LightSwitchController` toggles `LightingSwitchOn` for its area. `LightPower` consumes area state for fixture on/off/emergency visuals; optional `DepartmentalLightTint` on `AreaRecord` tints normal-mode emission.

**Fork deviations from** [area.md](../../design/area.md): areas are APC-seeded (not generic auto-detection); unclaimed tiles have no fallback area; all doors block expansion regardless of open/closed state. Wall-mounted APCs seed flood fill from the walkable tile **in front of** `FacingDirection`, not from every cardinal neighbor.

## Start here

- `Assets/Scripts/SS3D/Systems/Area/AreaSubSystem.cs` — registry, APC lifecycle, rebuild orchestration; `BeginDeferredAreaFlood` / `EndDeferredAreaFlood` around station template load
- `Assets/Scripts/SS3D/Systems/Area/AreaFloodFillService.cs` — BFS from APC seeds, door-tile post-pass
- `Assets/Scripts/SS3D/Systems/Area/AreaBoundaryEvaluator.cs` — walkability and expansion blocking rules
- `Assets/Scripts/SS3D/Systems/Area/AreaRegistry.cs` — `AreaRecord` storage and APC reverse lookup
- `Assets/Scripts/SS3D/Systems/Area/IAreaApcOrigin.cs` — APC contract (`OriginTile`, `FacingDirection`)
- `Assets/Scripts/SS3D/Systems/Area/AreaLightingStateDeriver.cs` — Normal/Emergency/Dark from `CircuitStats` + switch state
- `Assets/Scripts/SS3D/Systems/Area/LightSwitchController.cs` — wall switch interaction, power consumer, area lighting toggle
- `Assets/Scripts/SS3D/Systems/Area/IAreaLightingStateSource.cs` — lighting state query contract
- `Assets/Scripts/SS3D/Systems/Area/AreaLightFixturePolicy.cs` — fixture emit policy (Normal/Emergency/Dark)
- `Assets/Scripts/SS3D/Systems/Area/LightFixtureCapability.cs` — `NormalOnly` / `EmergencyCapable` fixture tag
- `Assets/Scripts/SS3D/Systems/Area/AreaDevSettings.cs` — dev toggle (`SS3D → Dev → Areas → Show Area Gizmos`)
- `Assets/Scripts/SS3D/Systems/Area/AreaDebugGizmoDrawer.cs` — Scene-view tile overlay, APC labels, and linked device diagnostics (host/server map only)
- `Assets/Scripts/Tests/EditMode/AreaFloodFillTests.cs` — flood-fill and boundary edit-mode tests
- `Assets/Scripts/Tests/EditMode/ElectricityTests/AreaLightFixturePolicyTests.cs` — fixture policy tests
- `Assets/Scripts/Tests/EditMode/ElectricityTests/AreaLightingStateDeriverTests.cs` — lighting state + wall-switch-off → Dark

## Extension points

- Resolve area for a tile: `AreaSubSystem.TryGetAreaForTile` / `ITileQueryService.TryGetAreaId`.
- Resolve area for wall-mounted devices: `AreaSubSystem.TryGetAreaForDevice` (always uses the tile in front of `Direction`, even if the wall tile has an area id).
- Register APC origins: implement `IAreaApcOrigin` (see `ApcController`).
- Server rename/tag API: `AreaSubSystem.RenameArea`, `SetParentTag` (no editor UI yet).
- Resolve effective APC for a device: `AreaSubSystem.TryGetEffectiveApcForDevice`.
- Area rebuild / APC lifecycle invalidates electricity's per-APC consumer index via `ElectricitySubSystem.InvalidateAreaConsumerIndex`.
- Query lighting by tile: `IAreaLightingStateSource.TryGetLightingStateForTile`.
- Subscribe to area lighting transitions: `AreaSubSystem.OnAreaLightingStateChanged`.
- Toggle area fixture lighting: `AreaSubSystem.ToggleAreaLightingSwitch` via `LightSwitchController` (separate from APC lighting **breaker** in machine interface).
- Subscribe to wall-switch changes: `AreaSubSystem.OnAreaLightingSwitchChanged`.
- Departmental tint API: `SetDepartmentalLightTint` / `ClearDepartmentalLightTint` (server).
- Fixture visuals: `LightPower` + `AreaLightFixturePolicy` + `LightFixtureCapability` on prefabs.
- Dev bypass (`SS3D → Dev → Lighting → Always Power Light Fixtures`) treats fixtures as powered but still respects APC channel toggles and area Normal/Emergency/Dark policy.
- Template restore: `BeginTemplateRestore` → `RestoreFromSave` → APC registration → `EndTemplateRestore` (see `AreaFloodFillTests.TemplateRestore_WithRegisteredApc_PreservesSavedMetadata`).
- **Not yet wired:** fixture subset authoring on `AreaRecord`.

## Pitfalls

- **APC area only fills front/right at game start, left empty until remove/re-add:** `ApcController.OnStartServer` → `RegisterApc` → flood runs during `TileMap.Load` while later chunks are still unplaced. Missing plenums look unwalkable, so BFS never claims that side; live mutation rebuild is deferred. Fix: `PersistenceSubSystem` / legacy `TileSubSystem.Load` wrap load in `BeginDeferredAreaFlood` / `EndDeferredAreaFlood` (refloods after the full map exists, preserving AreaRecord metadata). Do not flood from `RegisterApc` while deferred. Tests: `DeferredFlood_*`, `FloodWithoutDefer_OnIncompleteMap_MissesUnplacedWestTiles`.
- **Light switch usable from across the room:** prefab had no collider, selection never resolved a point, and `RangeCheck` treated zero point as unlimited — see [interactions-framework](interactions-framework.md) Pitfalls. LightSwitch now has a BoxCollider; RangeCheck falls back to target transform.

## Depends on / Used by

- **Depends on:** [tile](tile.md) (`TileMap` area-id storage, `ITileQueryService`), [persistence](persistence.md) (area contributor chunk, `OnAfterRestore` lifecycle)
- **Used by:** [machine-interface](machine-interface.md) (APC overlap diagnostic); [electricity](electricity.md) (area→APC resolver, `LightPower` fixture visuals, `LightSwitchController` consumer); [persistence](persistence.md) (area metadata capture/restore)

## Related docs

- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md)
- Plan: [persistence_architecture_design_2fe61864.plan.md](../../plans/persistence_architecture_design_2fe61864.plan.md)
- Architecture effort: [2026-07_area-foundation](../2026-07_area-foundation.md)
- Architecture effort: [2026-07_mi-area-electricity-debt](../2026-07_mi-area-electricity-debt.md)
- Design (read-only): [Documents/design/area.md](../../design/area.md)
