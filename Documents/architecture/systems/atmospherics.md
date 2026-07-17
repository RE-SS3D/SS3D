> Code paths: Assets/Scripts/SS3D/Systems/Atmospherics/, Assets/Scripts/SS3D/Rendering/URP/Atmos*
> Entry points: AtmosSubSystem, AtmosSimulation, AtmosRendererFeature
> Status: partial
> Verified: 8d5428105 — 2026-07-17

# Atmospherics

## Overview

Server-authoritative open-tile gas simulation on the turf grid. Each walkable cell holds a sparse gas mixture; pressure equalizes between neighbours via ideal-gas-law sharing, heat conducts per gas specific heat, and plasma burns with oxygen into CO₂. Runs in a dedicated ECS world with Burst jobs, driven by tilemap mutation notifications. GPU textures feed URP scatter/glow/distortion passes for fog, fire, and plasma visuals. **Pipe layer:** vents, scrubbers, and pumps register with `AtmosPortRegistry` and exchange gas with turf cells after pipe bulk sim; ports cache network IDs against `GasPipeNetworkRegistry.TopologyVersion` so steady ticks do not re-walk the tilemap. **Air alarms** sample the turf cell in front of the wall mount (APC-style tile resolution), discover area vents/scrubbers, and dispatch preset modes to real port devices. Port commands are validated against the air alarm’s resolved area to avoid cross-area toggles on shared wall tiles. **Multiplayer gap:** VFX only publishes on server/host today — pure clients have no snapshot (see effort below).

## Start here

- `Assets/Scripts/SS3D/Systems/Atmospherics/AtmosSubSystem.cs` — tick loop, waits for tilemap, wires observer + visualization
- `Assets/Scripts/SS3D/Systems/Atmospherics/AtmosSimulation.cs` — native cell buffers, active-cell scheduling, job dispatch
- `Assets/Scripts/SS3D/Systems/Atmospherics/Bridge/AtmosTileObserver.cs` — `ITileMutationObserver`; refreshes cells on placement, clear, and door state
- `Assets/Scripts/SS3D/Systems/Atmospherics/ECS/Jobs/ShareGasJob.cs` — pressure-driven mole sharing
- `Assets/Scripts/SS3D/Systems/Atmospherics/ECS/Jobs/ConductHeatJob.cs` — specific-heat heat exchange
- `Assets/Scripts/SS3D/Systems/Atmospherics/ECS/Jobs/ReactAtmosJob.cs` — plasma combustion and burn intensity
- `Assets/Scripts/SS3D/Systems/Atmospherics/Data/GasRegistry.cs` — core gas slot lookup (`CoreGasRegistry.asset`)
- `Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosVisualizationBridge.cs` — post-tick GPU upload
- `Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosGpuUploader.cs` — atlas scratch → Texture2D upload (no per-cell managed allocs)
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRendererFeature.cs` — URP scatter, glow, distortion passes
- `Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosCamera.cs` — registers player camera with render context (all clients)
- `Assets/Scripts/SS3D/Systems/Atmospherics/AtmosDebugController.cs` — runtime overlay (P toggle; server/host)
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosPortRegistry.cs` — registered vent/scrubber/pump port tick list
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/GasPipeNetworkRegistry.cs` — pipe networks; `TopologyVersion` invalidates port caches
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosPortControllerBase.cs` — vent/scrubber tick + cached network resolve
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosAreaDeviceQuery.cs` — list vents/scrubbers in an APC area
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AirAlarmController.cs` — tile-in-front sampling, preset mode dispatch
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosAreaSampler.cs` — area aggregate and single-tile sampling
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/ScrubberController.cs` — per-gas filter scrubbing into pipe networks; flow rate scales rated throughput
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/VentController.cs` — network→turf venting with target-pressure cutoff
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosPumpController.cs` — turf→pipe pump with target outlet pressure; not area-linked
- `Assets/Scripts/Tests/EditMode/Atmospherics/` — flux, combustion, neighbour, GPU, visual-metrics, air-alarm sampler tests

## Extension points

- New gases: add `GasDefinition` assets under `Assets/Content/Systems/Atmospherics/Gases/`; run `AtmosRegistryGenerator` editor tool to refresh registry slots.
- Per-gas visuals: assign `GasVisualProfile` on each `GasDefinition` (scatter tint, emission, smoke). `GasVisualProfileBuilder.Build` caches per registry instance.
- React to tile changes: implement `ITileMutationObserver` or call `TileSubSystem.NotifyTileStateChanged` from dynamic occupants (see [tile](tile.md) `IDynamicTileOccupant`).
- New render passes: extend `AtmosRendererFeature` or add sibling URP features under `Rendering/URP/`.
- Client VFX sync: chunk dirty-patch transport to client `AtmosGpuUploader` — see [2026-07_atmos-client-visualization-sync.md](../2026-07_atmos-client-visualization-sync.md).

## Pitfalls

- **~1 MB GC attributed to `AtmosSubSystem.Update` on GPU upload:** `EncodeComposition` used `new float[4]` per cell and lambdas captured locals — use stack locals / cached method-group delegates; sample flow gradients from atlas scratch, not `TryGetCellIndex`.
- **`TileCoord` dictionary lookups box (~24 B) on Mono:** keys must implement `IEquatable<TileCoord>` / `GetHashCode` (see [tile](tile.md)); otherwise `ValueType.DefaultEquals` dominates flow upload and other hot maps.
- **Port ticks allocating via `GetAllPlacedObject`:** that API always builds a new `List`. Pipe layers are single-occupancy — use `TryGetPlacedObject`. Do not re-resolve pipe networks every tick; cache against `GasPipeNetworkRegistry.TopologyVersion`.

## Depends on / Used by

- **Depends on:** [tile](tile.md) (`ITileQueryService`, `ITileMutationObserver`, `IDynamicTileOccupant`, `TileCoord`), [rendering](rendering.md) (`AtmosRendererFeature`), [area](area.md) (air-alarm area membership and tile-in-front resolution)
- **Used by:** [machine-interface](machine-interface.md) (air alarm / scrubber / vent / pump panels), (future) [substances](substances.md), [health](health.md)

## Related docs

- Design (read-only): [Documents/design/atmospherics.md](../../design/atmospherics.md)
- Effort: [2026-07_atmos-ecs-foundation.md](../2026-07_atmos-ecs-foundation.md)
- Effort (planned): [2026-07_atmos-client-visualization-sync.md](../2026-07_atmos-client-visualization-sync.md) — **client VFX sync missing**
- [tile](tile.md) — occupancy and mutation hooks
- [rendering](rendering.md) — URP feature wiring
- [furniture](furniture.md) — airlock `IDynamicTileOccupant` for door passability
