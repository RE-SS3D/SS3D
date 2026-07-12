> Code paths: Assets/Scripts/SS3D/Systems/Atmospherics/, Assets/Scripts/SS3D/Rendering/URP/Atmos*
> Entry points: AtmosSubSystem, AtmosSimulation, AtmosRendererFeature
> Status: partial

# Atmospherics

## Overview

Server-authoritative open-tile gas simulation on the turf grid. Each walkable cell holds a sparse gas mixture; pressure equalizes between neighbours via ideal-gas-law sharing, heat conducts per gas specific heat, and plasma burns with oxygen into CO₂. Runs in a dedicated ECS world with Burst jobs, driven by tilemap mutation notifications. GPU textures feed URP scatter/glow/distortion passes for fog, fire, and plasma visuals. Implements design §2 (open-tile diffusion); liquid/solid buffers, pipes, and pumps are not started.

## Start here

- `Assets/Scripts/SS3D/Systems/Atmospherics/AtmosSubSystem.cs` — tick loop, waits for tilemap, wires observer + visualization
- `Assets/Scripts/SS3D/Systems/Atmospherics/AtmosSimulation.cs` — native cell buffers, active-cell scheduling, job dispatch
- `Assets/Scripts/SS3D/Systems/Atmospherics/Bridge/AtmosTileObserver.cs` — `ITileMutationObserver`; refreshes cells on placement, clear, and door state
- `Assets/Scripts/SS3D/Systems/Atmospherics/ECS/Jobs/ShareGasJob.cs` — pressure-driven mole sharing
- `Assets/Scripts/SS3D/Systems/Atmospherics/ECS/Jobs/ConductHeatJob.cs` — specific-heat heat exchange
- `Assets/Scripts/SS3D/Systems/Atmospherics/ECS/Jobs/ReactAtmosJob.cs` — plasma combustion and burn intensity
- `Assets/Scripts/SS3D/Systems/Atmospherics/Data/GasRegistry.cs` — core gas slot lookup (`CoreGasRegistry.asset`)
- `Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosVisualizationBridge.cs` — post-tick GPU upload
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRendererFeature.cs` — URP scatter, glow, distortion passes
- `Assets/Scripts/SS3D/Systems/Atmospherics/AtmosDebugController.cs` — runtime overlay (P toggle; server/host)
- `Assets/Scripts/Tests/EditMode/Atmospherics/` — flux, combustion, neighbour, GPU, visual-metrics tests

## Extension points

- New gases: add `GasDefinition` assets under `Assets/Content/Systems/Atmospherics/Gases/`; run `AtmosRegistryGenerator` editor tool to refresh registry slots.
- Per-gas visuals: assign `GasVisualProfile` on each `GasDefinition` (scatter tint, emission, smoke).
- React to tile changes: implement `ITileMutationObserver` or call `TileSubSystem.NotifyTileStateChanged` from dynamic occupants (see [tile](tile.md) `IDynamicTileOccupant`).
- New render passes: extend `AtmosRendererFeature` or add sibling URP features under `Rendering/URP/`.

## Depends on / Used by

- **Depends on:** [tile](tile.md) (`ITileQueryService`, `ITileMutationObserver`, `IDynamicTileOccupant`), [rendering](rendering.md) (`AtmosRendererFeature`)
- **Used by:** (future) [substances](substances.md), [health](health.md), explosives/chemistry integrations per design §11

## Related docs

- Design (read-only): [Documents/design/atmospherics.md](../../design/atmospherics.md)
- Effort: [2026-07_atmos-ecs-foundation.md](../2026-07_atmos-ecs-foundation.md)
- [tile](tile.md) — occupancy and mutation hooks
- [rendering](rendering.md) — URP feature wiring
- [furniture](furniture.md) — airlock `IDynamicTileOccupant` for door passability
