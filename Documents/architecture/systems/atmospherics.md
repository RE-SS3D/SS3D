> Code paths: Assets/Scripts/SS3D/Systems/Atmospherics/, Assets/Scripts/SS3D/Rendering/URP/Atmos*
> Entry points: AtmosSubSystem, AtmosSimulation, AtmosRendererFeature
> Status: partial
> Verified: 557058016 — 2026-11-04

# Atmospherics

## Overview

Server-authoritative open-tile gas simulation on the turf grid. Each walkable cell holds a sparse gas mixture; pressure equalizes between neighbours via ideal-gas-law sharing, and when total pressures already match, composition still mixes via slower partial-pressure diffusion (`DiffusionSpeed`). Heat conducts per gas specific heat, and plasma burns with oxygen into CO₂. Runs in a dedicated ECS world with Burst jobs, driven by tilemap mutation notifications. GPU textures feed URP scatter/glow/distortion passes for fog, fire, and plasma visuals. **Pipe layer:** vents, scrubbers, and pumps register with `AtmosPortRegistry` and exchange gas with turf cells after pipe bulk sim; ports cache network IDs against `GasPipeNetworkRegistry.TopologyVersion` so steady ticks do not re-walk the tilemap. **Air alarms** sample the turf cell in front of the wall mount (APC-style tile resolution), discover area vents/scrubbers via a mutation-driven `areaId → ports` index (`AtmosAreaPortIndex`, invalidated on port register/unregister and area reflood), and dispatch preset modes to real port devices. Port commands are validated against the air alarm’s resolved area to avoid cross-area toggles on shared wall tiles. **Client VFX:** Phase 1 dirty-chunk sync shipped ([2026-07_atmos-client-visualization-sync.md](../2026-07_atmos-client-visualization-sync.md)) — pure clients build atlases from `AtmosChunkPatch` RPCs via `AtmosClientVisualizationBridge`; late-join bootstrap / AOI remain Phase 2.

Destroyed walls automatically produce pressure-driven wind through open gas edges. The server samples `AtmosWind.SampleAt` per body, blended through open edges; strong gusts collapse bodies, and bodies ease toward wind speed every frame. Clients draw wind as `AtmosWindView` streaks from the same flow atlas the gas shaders read, so wind visuals add no traffic of their own; atlas flow keeps direction and strength. Wind decays with the gas gradient and respects closed edges. Protocol 15 adds the owner wind RPC. Wind affects humanoids; loose item wind physics remains open. Rooms still drain by diffusion, slowly, which lags design §1, §2 and §6: the planned fix is [2026-11_atmos-edge-flow.md](../2026-11_atmos-edge-flow.md).

## Start here

- `Assets/Scripts/SS3D/Systems/Atmospherics/AtmosSubSystem.cs` — tick loop; `IWorldReady`; awaits `TileMapLoaded` then notifies `AtmosReady` (`SS3D.Atmos.Sim` / `Upload` markers)
- `Assets/Scripts/SS3D/Systems/Atmospherics/AtmosWind.cs` / `AtmosSubSystem.Wind.cs` / `AtmosWindView.cs` — edge-aware continuous wind sampling, body dispatch and atlas-driven client streaks
- `Assets/Scripts/SS3D/Systems/Atmospherics/AtmosSimulation.cs` — native cell buffers, active-cell scheduling, job dispatch
- `Assets/Scripts/SS3D/Systems/Atmospherics/Bridge/AtmosTileObserver.cs` — `ITileMutationObserver`; refreshes cells on placement, clear, and door state
- `Assets/Scripts/SS3D/Systems/Atmospherics/ECS/Jobs/ShareGasJob.cs` — pressure-driven mole sharing + equal-pressure composition diffusion
- `Assets/Scripts/SS3D/Systems/Atmospherics/ECS/Jobs/ConductHeatJob.cs` — specific-heat heat exchange
- `Assets/Scripts/SS3D/Systems/Atmospherics/ECS/Jobs/ReactAtmosJob.cs` — plasma combustion and burn intensity
- `Assets/Scripts/SS3D/Systems/Atmospherics/Data/GasRegistry.cs` — core gas slot lookup (`CoreGasRegistry.asset`)
- `Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosVisualizationBridge.cs` — post-tick GPU upload (host client); AOI-window atlas via `TileAoiVisibility`
- `Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosGpuUploader.cs` — atlas scratch → Texture2D upload (no per-cell managed allocs; optional AOI tile bounds)
- `Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosClientVisualizationBridge.cs` / `AtmosClientAtlas.cs` — pure-client patch → atlas
- `Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosChunkPatch.cs` — ObserversRpc payload
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRendererFeature.cs` — URP scatter, glow, distortion passes
- `Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosCamera.cs` — registers player camera with render context (all clients)
- `Assets/Scripts/SS3D/Systems/Atmospherics/AtmosDebugController.cs` — runtime overlay (F1 → Atmospherics; server/host)
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosPortRegistry.cs` — registered vent/scrubber/pump port tick list
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosAreaPortIndex.cs` — `areaId → vents/scrubbers`; dirty on register/unregister + area reflood
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/GasPipeNetworkRegistry.cs` — pipe networks; `TopologyVersion` invalidates port caches
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosPortControllerBase.cs` — vent/scrubber tick + cached network resolve
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosAreaDeviceQuery.cs` — air-alarm / preset queries over indexed area ports (not full registry scan)
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AirAlarmController.cs` — tile-in-front sampling, preset mode dispatch
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosAreaSampler.cs` — area aggregate and single-tile sampling
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/ScrubberController.cs` — per-gas filter scrubbing into pipe networks; flow rate scales rated throughput
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/VentController.cs` — network→turf venting with target-pressure cutoff
- `Assets/Scripts/SS3D/Systems/Atmospherics/Pipes/AtmosPumpController.cs` — turf→pipe pump with target outlet pressure; not area-linked
- `Assets/Scripts/Tests/EditMode/Atmospherics/` — flux, combustion, neighbour, GPU, visual-metrics, air-alarm sampler tests

## Extension points

- Air alarm power/alarm visuals use `Electricity/EmissiveMaterialSlots` to preserve each material slot's authored emission/luminance; avoid one renderer-wide baseline for mixed-material meshes.

- New gases: add `GasDefinition` assets under `Assets/Content/Systems/Atmospherics/Gases/`; call `AtmosRegistryGenerator.CreateCoreGasRegistry` (tier B static — no MenuItem) to refresh registry slots.
- Per-gas visuals: assign `GasVisualProfile` on each `GasDefinition` (scatter tint, emission, smoke). `GasVisualProfileBuilder.Build` caches per registry instance.
- React to tile changes: implement `ITileMutationObserver` or call `TileSubSystem.NotifyTileStateChanged` from dynamic occupants (see [tile](tile.md) `IDynamicTileOccupant`).
- New render passes: extend `AtmosRendererFeature` or add sibling URP features under `Rendering/URP/`.
- Client VFX Phase 2 (late-join bootstrap / AOI): extend dirty-chunk path — see [2026-07_atmos-client-visualization-sync.md](../2026-07_atmos-client-visualization-sync.md).

## Pitfalls

- **Chunk borders were one-way valves:** `CreateChunk` linked a new chunk to existing ones, but existing border cells never linked back, and a cell only pushes gas through its own links. Rooms crossing a border stayed at full pressure beside a depleted row, with no error. `RebuildBordersAround` now refreshes them. Compare both directions of `Neighbours` when gas stops at a straight line on a multiple of 16. Hit 2026-11-04.
- **Flow atlas must fill before it reads neighbours:** the AOI path wrote flow in the same pass as pressure, so +X/+Z neighbours read empty and host flow became a constant diagonal. Flow also used to be normalized, uphill and treated walls as 0 kPa. Encode through `AtmosCellVisualEncoding.EncodeFlow`, passing the cell's own pressure for walls.
- **A green streak count is not visible streaks:** `VisibleStreaks` passed while one-pixel streaks were invisible, then near-camera ones became planks. Review a live breach screenshot after changing `AtmosWindView` tuning.
- **Render-context consumers need a request in PlayMode scenarios:** the batchmode harness never renders the player camera, so `AtmosCamera` never calls `AtmosRenderContext.SetRequest` and `AtmosWindView.VisibleStreaks` stays zero. `HullBreach_FillsFlowAtlas_AndShowsWindStreaks` registers a look-down test camera and clears the request in `finally`; use the same pattern for any scenario asserting a render-context consumer.

- **Map editor must suppress atmos visuals, not only the sim:** pausing `AtmosSubSystem.SimulationPaused` stops the server tick but leaves the last GPU snapshot in `AtmosRenderContext`, so the shaders keep animating and `AtmosWindView` keeps drawing streaks; a remote client's editor cannot pause the server at all. `MapEditorSubSystem.SetActive` toggles `AtmosRenderContext.SetRenderingSuppressed`; both `AtmosRendererFeature` and `AtmosWindView` early-out on it, and the wind view expires its streaks so none resume mid-flight. Hit 2026-11-04 (#147).

- **Round restarts must dispose the previous simulation before restoring a template:** readiness resets outlive the subsystem. Keeping `_simulation` makes the initialization guard accept old turf/pipe gas as ready. Unregister both observers, dispose simulation/world, reset dirty tracking and tick/debug state, then seed the restored map.

- **Host `SS3D.Atmos.Upload` on large maps:** full-chunk AABB + `ForEachCell` + six `Texture2D.Apply` every tick dominated Metastation (~10 ms/tick). Host upload now fills only local HashGrid AOI tile bounds (+1 chunk pad). Dedicated server skips GPU publish. Network dirty-chunk Phase 2 AOI still open. Hit 2026-07-28.
- **`ShareGasJob` / `ConductHeatJob` must not touch passive cells:** copying / energy-seeding / T-resolve over all `_cellCount` (~27k on Meta) kept `LastTick` ~20 ms with only ~83 active. Jobs use a **working set** (actives + neighbours) and commit only that set; `RebuildActiveList` is incremental (`_pendingActivate` + prior actives), not a full-grid scan. Hit 2026-07-28.
- **`SS3D.Atmos.NetworkSync` GC on host-only Metastation:** ObserversRpc patch build/serialize ran every tick even with no remote clients (host atlas is `PublishSnapshot`). Skip broadcast when no non-local server clients; dirty scan walks `ActiveCells` only; `AtmosChunkPatchBuilder` reuses array buffers. Hit 2026-07-28 (~1.6 MB / 101 ticks with ~83 active tiles).
- **`GasRegistry` runtime caches:** `Initialize` must treat `_sortedDefinitions` + `_byId` as one unit. An array-only early-return (Play Mode without domain reload, or leftover SO state across hub unload) makes `GetSlotCount` succeed then `TryGetDefinition` NRE during `AtmosVisualizationBridge.PublishSnapshot`.
- **Do not poll `CurrentMap != null` for init.** Await `WorldReadyPhase.TileMapLoaded` via `WorldReadinessSubSystem`, then notify `AtmosReady`. Epoch reset (`Phase == None`) re-runs init.
- **~1 MB GC attributed to `AtmosSubSystem.Update` on GPU upload:** `EncodeComposition` used `new float[4]` per cell and lambdas captured locals — use stack locals / cached method-group delegates; sample flow gradients from atlas scratch, not `TryGetCellIndex`.
- **`TileCoord` dictionary lookups box (~24 B) on Mono:** keys must implement `IEquatable<TileCoord>` / `GetHashCode` (see [tile](tile.md)); otherwise `ValueType.DefaultEquals` dominates flow upload and other hot maps.
- **Port ticks allocating via `GetAllPlacedObject`:** that API always builds a new `List`. Pipe layers are single-occupancy — use `TryGetPlacedObject`. Do not re-resolve pipe networks every tick; cache against `GasPipeNetworkRegistry.TopologyVersion`.
- **Do not scan all ports to find area vents/scrubbers:** `AtmosAreaDeviceQuery` used to `ForEachPort` + `TryGetAreaForDevice` per port (O(all ports) with full tile resolve). That bites when an air alarm opens. Use `AtmosAreaPortIndex` / `GetIndexedPortsForArea` — rebuild on port register/unregister and area membership invalidate (`AreaSubSystem` → `InvalidateAreaPortIndex`). Hit 2026-07-29.
- **`GasPipeNetworkId` must implement `IEquatable<>`:** Dictionary lookup without it boxes the key every `TryGetNetwork` (~18 B × ports/tick under `SS3D.Atmos.Sim`). Same class of bug as `TileCoord`. Hit 2026-07-29.
- **Scrubber filter iteration must not `yield return`:** `GetActiveFilteredGases()` allocated an iterator state machine per scrubber per atmos tick. Unroll filter flags instead. Hit 2026-07-29.
- **Equal-pressure breath pockets:** a 1:1 O₂→CO₂ swap does not change total pressure, so pressure-only sharing never diluted the tile. `ShareGasJob` must run equal-P partial-pressure diffusion (`DiffusionSpeed`); composition gradients keep cells awake until the room mixes, then sleep again. Vacuum edges stay pressure-vent only.

## Tests

- `AtmosWindTests` covers sealed edges, destruction/repair, vacuum fade, continuous sampling, drag and flow encoding. `AtmosNeighbourTests` covers chunk borders in both load orders. `scenarios:Environment` covers owner gust knockdown, upright drift, and a live hull breach through to streaks and pull. `Testing/ui/validate.sh --steps mppm:breach` proves the same on a remote owner. Wind tuning is in `AtmosWind`; streak tuning is in `AtmosWindView`; visuals use `Assets/Resources/Atmospherics/Wind.shader`.

- `Testing/ui/validate.sh --steps compile,round-reset` runs a focused host stop/restart, depletes a cell before Stop and checks oxygen returns in a fresh simulation. The sandbox pilot also carries this regression. Remote dedicated-server replay remains required for release acceptance.

## Depends on / Used by

- **Depends on:** [tile](tile.md) (`ITileQueryService`, `ITileMutationObserver`, `IDynamicTileOccupant`, `TileCoord`), [rendering](rendering.md) (`AtmosRendererFeature`), [area](area.md) (air-alarm area membership and tile-in-front resolution)
- **Used by:** [machine-interface](machine-interface.md) (air alarm / scrubber / vent / pump panels), [health](health.md) (turf sample → breathability / O₂↔CO₂ / env burn; armor seal still deferred), (future) [substances](substances.md)

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: atmospherics (air alarms that alarm)
- Design (read-only): [Documents/design/atmospherics.md](../../design/atmospherics.md)
- Effort: [2026-07_atmos-ecs-foundation.md](../../archive/architecture/2026-07_atmos-ecs-foundation.md)
- Effort: [2026-07_atmos-client-visualization-sync.md](../2026-07_atmos-client-visualization-sync.md) — Phase 1 shipped; Phase 2 late-join/AOI open; Phase 3 host AOI atlas partial via [metastation-scale-perf](../../archive/architecture/2026-07_metastation-scale-perf.md)
- Effort: [2026-11_atmos-edge-flow.md](../2026-11_atmos-edge-flow.md) — planned; room-scale bulk flow
- Effort: [2026-07_session-world-lifecycle.md](../../archive/architecture/2026-07_session-world-lifecycle.md)
- [tile](tile.md) — occupancy and mutation hooks
- [rendering](rendering.md) — URP feature wiring
- [furniture](furniture.md) — airlock `IDynamicTileOccupant` for door passability
