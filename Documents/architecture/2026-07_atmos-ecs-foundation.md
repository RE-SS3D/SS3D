> Implements: Documents/design/atmospherics.md §2 (open-tile diffusion), §10 (gas rendering — partial)
> Touches systems: atmospherics, tile, rendering, furniture (airlock occupancy)
> Status: shipped

# Atmospherics ECS Foundation

## Goal

Ship turf gas simulation with tile-driven neighbour graph, plasma combustion, GPU visualization, and EditMode test coverage. Formalizes the "existing atmospherics system" referenced across design docs.

## Shipped

1. **ECS world** — `AtmosWorld`, native cell buffers, active-cell sleep/wake, chunk resize preservation.
2. **Jobs** — `ShareGasJob` (pressure equalization), `ConductHeatJob`, `ReactAtmosJob` (plasma burn).
3. **Gas data** — four core gases (O₂, N₂, CO₂, plasma); `GasRegistry` + `GasDefinition` ScriptableObjects; molar mass and specific heat on each gas.
4. **Tile bridge** — `AtmosTileObserver` on `ITileMutationObserver`; `AtmosNeighbourBuilder`; deferred refresh on tile clear.
5. **Dynamic occupancy** — `IDynamicTileOccupant` on airlocks; `TileSubSystem.NotifyTileStateChanged` reopens/closes gas paths when doors move.
6. **Visualization** — `AtmosGpuUploader` → pressure/temperature/composition/fire textures; `AtmosRendererFeature` scatter + plasma glow + heat distortion; per-gas visual profiles.
7. **Debug** — `AtmosDebugController` overlay; shader debug views on renderer feature.
8. **Scene wiring** — `AtmosSystem` on `Game.unity`; renderer feature on `SS3D_ForwardPlusRenderer.asset`.
9. **Tests** — `AtmosFluxTests`, `AtmosCombustionTests`, `AtmosNeighbourTests`, `AtmosGpuUploaderTests`, `AtmosVisualMetricsTests`.

## Deferred (design §4–§9, §13)

- LiquidBuffer / SolidBuffer and phase transitions
- Pipe networks, pumps, vents, valves, gauges
- Chemistry reagent integration (boiling/freezing points)
- Liquid/solid rendering (puddles, decals)
- Junction reactions, breach hooks (explosives, suit breach, wet-floor shocks)
- Perception-altering gas effects

## Documented fork deviations

- Single flat z-level only (design §13 acknowledges no multi-level)
- Fire propagation duration explicitly out of scope for this pass
- `AirLockOpener` combines develop power gating with atmos tile-state notifications after merge with `develop`

## Related docs

- System map: [atmospherics.md](systems/atmospherics.md)
- Design (read-only): [atmospherics.md](../design/atmospherics.md)
