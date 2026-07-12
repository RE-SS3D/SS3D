> Code paths: Assets/Scripts/SS3D/Systems/Area/
> Entry points: AreaSubSystem, AreaFloodFillService, AreaBoundaryEvaluator
> Status: partial

# Area

## Overview

APC-seeded area flood-fill: each APC owns one `AreaRecord` and claims reachable floor tiles with a per-chunk `ushort[]` area-id layer. Walls and doors block expansion; unclaimed tiles stay `AreaId.None`. Live boundary recompute on tile mutation is deferred — rebuild runs on map load and APC place/remove only.

Per-consumer power gating and **area-scoped APC cell drain** via [electricity](electricity.md) `AreaApcPowerDistribution`: devices in an assigned area use their area APC's channels and cell, not circuit-wide OR or equal battery split. `AreaLightingState` (Normal/Emergency/Dark) is derived each electricity tick from the area APC's circuit stats and `AreaRecord.LightingSwitchOn`; a disabled lighting channel forces Dark regardless of cell charge. Transitions fire `OnAreaLightingStateChanged` and sync to clients via ObserversRpc. Wall `LightSwitchController` toggles `LightingSwitchOn` for its area. `LightPower` consumes area state for fixture on/off/emergency visuals; optional `DepartmentalLightTint` on `AreaRecord` tints normal-mode emission.

**Fork deviations from** [area.md](../../design/area.md): areas are APC-seeded (not generic auto-detection); unclaimed tiles have no fallback area; all doors block expansion regardless of open/closed state. Wall-mounted APCs seed flood fill from the walkable tile **in front of** `FacingDirection`, not from every cardinal neighbor.

## Start here

- `Assets/Scripts/SS3D/Systems/Area/AreaSubSystem.cs` — registry, APC lifecycle, rebuild orchestration
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
- Resolve area for wall-mounted devices: `AreaSubSystem.TryGetAreaForDevice` (uses tile in front of `Direction`).
- Register APC origins: implement `IAreaApcOrigin` (see `ApcController`).
- Server rename/tag API: `AreaSubSystem.RenameArea`, `SetParentTag` (no editor UI yet).
- Resolve effective APC for a device: `AreaSubSystem.TryGetEffectiveApcForDevice`.
- Query lighting by tile: `IAreaLightingStateSource.TryGetLightingStateForTile`.
- Subscribe to area lighting transitions: `AreaSubSystem.OnAreaLightingStateChanged`.
- Toggle area fixture lighting: `AreaSubSystem.ToggleAreaLightingSwitch` via `LightSwitchController` (separate from APC lighting **breaker** in machine interface).
- Subscribe to wall-switch changes: `AreaSubSystem.OnAreaLightingSwitchChanged`.
- Departmental tint API: `SetDepartmentalLightTint` / `ClearDepartmentalLightTint` (server).
- Fixture visuals: `LightPower` + `AreaLightFixturePolicy` + `LightFixtureCapability` on prefabs.
- Dev bypass (`SS3D → Dev → Lighting → Always Power Light Fixtures`) treats fixtures as powered but still respects APC channel toggles and area Normal/Emergency/Dark policy.
- **Not yet wired:** fixture subset authoring on `AreaRecord`.

## Depends on / Used by

- **Depends on:** [tile](tile.md) (`TileMap` area-id storage, `ITileQueryService`, save/load)
- **Used by:** [machine-interface](machine-interface.md) (APC overlap diagnostic); [electricity](electricity.md) (area→APC resolver, `LightPower` fixture visuals, `LightSwitchController` consumer)

## Related docs

- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md)
- Architecture effort: [2026-07_area-foundation](../2026-07_area-foundation.md)
- Design (read-only): [Documents/design/area.md](../../design/area.md)
