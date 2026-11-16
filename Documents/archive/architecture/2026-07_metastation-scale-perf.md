> Implements: (perf / scale — no single design §; playable floor for SS13-size maps)
> Touches systems: tile, atmospherics, furniture, disposal, area, entities
> Status: shipped

# Metastation-scale performance pass

## Goal

Make SS13-sized maps (e.g. Metastation import) playable in host Play Mode, and keep Map Editor usable while showing the full station. Capture basis: `Logs/perf/capture-20260728-144924.md` (~58 ms/frame Editor with Map Editor closed).

## Shipped

### A — Map Editor sim suspend

- [`MapEditorSubSystem`](../../../Assets/Scripts/SS3D/Systems/Tile/MapEditor/MapEditorSubSystem.cs) open → `AtmosSubSystem.SimulationPaused`, `ElectricitySubSystem.SuspendCircuitUpdates`, `DisposalSubSystem.CapsulesPaused` (restores prior atmos pause on close).
- Vision already suppressed via `MapEditorLighting`.
- DMM [`MapImportApplier`](../../../Assets/Scripts/SS3D/Systems/Tile/MapImport/MapImportApplier.cs) also pauses atmos for the import duration (electricity suspend was already there). Full-station MeshRenderer bypass unchanged.

### B — Airlock proximity invert

- [`AirLockProximityService`](../../../Assets/Scripts/SS3D/Systems/Furniture/AirLockProximityService.cs) indexes doors by HashGrid cell; FixedUpdate walks players → nearby doors only.
- [`AirLockOpener`](../../../Assets/Scripts/SS3D/Systems/Furniture/AirLockOpener.cs) no longer per-door FixedUpdate; Unity `Animator` uses `CullCompletely`; airlock prefabs have **no** `NetworkAnimator` (SyncVar drives `SetBool` — stock NA TimeManager poll was ~14% of non-editor game work on Metastation).
- [`EntitySubSystem.SpawnedPlayers`](../../../Assets/Scripts/SS3D/Systems/Entities/EntitySubSystem.cs) returns `IReadOnlyList` (no per-call `ToList`); [`Entity.TryGetHumanInventory`](../../../Assets/Scripts/SS3D/Systems/Entities/Entity.cs) caches inventory.
- EditMode: `AirLockProximityServiceTests`.

### C — Host atmos AOI atlas

- [`AtmosVisualizationBridge.PublishSnapshot`](../../../Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosVisualizationBridge.cs) fills [`AtmosGpuUploader`](../../../Assets/Scripts/SS3D/Systems/Atmospherics/Visualization/AtmosGpuUploader.cs) for local HashGrid AOI tile bounds (+1 chunk pad) via [`TileAoiVisibility`](../../../Assets/Scripts/SS3D/Systems/Tile/TileAoiVisibility.cs). Skips GPU upload on dedicated server (no client). Full-map sim unchanged. Advances Phase 3 “active-region atlas” from [atmos-client-visualization-sync](../../architecture/2026-07_atmos-client-visualization-sync.md); Phase 2 network AOI still open.

### D — Overlay / wall-cap AOI hygiene

- [`AreaFloorStripeView`](../../../Assets/Scripts/SS3D/Systems/Area/AreaFloorStripeView.cs) / [`FloorDecalView`](../../../Assets/Scripts/SS3D/Systems/Tile/FloorVisuals/FloorDecalView.cs) spawn only AOI chunks (all chunks while Map Editor authoring).
- Door wall caps call `PlacedTileObject.RefreshHostVisibility` after Instantiate so FishNet renderer cache includes caps.
- Import path already runs `RebuildObservers` + `RefreshAllHostVisibility` (verified).

### E — CircuitsTick GC + empty-pass set (follow-up)

- Electricity hot path reuses scratch lists/sets (`Circuit`, `AreaApcPowerDistribution.FillActiveConsumers`, `PowerConsumerAllocation.AllocateUnderBudget` fill overload, `ElectricitySubSystem.UpdateAreaScopedPower`).
- Airlock empty close: `_pendingEmptyPass` HashSet (doors with occupants) instead of walking all `_openerCells`; still runs when `SpawnedPlayers` is empty.
- Markers: `SS3D.Electricity.CircuitsTick` / `SS3D.Airlock.EmptyPassSweep` — expect GC and empty-sweep CPU down on next `Logs/perf/` export.

### F — Underfloor MeshRenderer occlusion (follow-up)

- [`TileUnderfloorVisibility`](../../../Assets/Scripts/SS3D/Systems/Tile/TileUnderfloorVisibility.cs): play-mode disables MeshRenderers on Plenum/Wire/Disposal/PipeLeft|Middle|Right when the cell’s Turf is Floor/Wall/Door.
- Composed in [`PlacedTileObject.RefreshHostVisibility`](../../../Assets/Scripts/SS3D/Systems/Tile/PlacedObjects/PlacedTileObject.cs) after AOI show; also re-applies on FishNet `OnHostVisibilityUpdated` (spawn paths call `SetRenderersVisible` directly). Disables `Selectable` too; keeps NetworkObject + colliders active (no root `SetActive(false)`).
- Turf place/clear refreshes same-cell underfloor; template `Load` ends with `RefreshAllHostVisibility`.
- Map Editor authoring skips hide (full-station visibility + layer dim toggles unchanged). `PipeSurface` stays drawn.
- EditMode: `TileUnderfloorVisibilityTests`.

## Verification

- Re-export Profiler: `metastation-play` / `metastation-mapedit` under `Logs/perf/`.
- Expect `SS3D.Atmos.Upload` and `AirLockOpener.FixedUpdate` out of top self-time; Map Editor open → atmos/electricity/airlock markers near-idle with full visibility.
- After E: `SS3D.Electricity.CircuitsTick` GC near-floor; `EmptyPassSweep` self-time scales with occupied doors, not station door count.
- After F: `Render.Mesh` / `ApplyShader` / `Batch.DrawInstanced` down vs `capture-20260728-164634`; Map Editor still shows underfloor; clearing a floor in play restores pipes/plenum.

## Related docs

- System maps: [tile](../../architecture/systems/tile.md), [atmospherics](../../architecture/systems/atmospherics.md), [furniture](../../architecture/systems/furniture.md), [disposal](../../architecture/systems/disposal.md), [area](../../architecture/systems/area.md)
- [2026-07_atmos-client-visualization-sync.md](../../architecture/2026-07_atmos-client-visualization-sync.md)
- [2026-07_unity-perf-ai-tooling.md](../../architecture/2026-07_unity-perf-ai-tooling.md)
- Follow-up (shipped): [2026-07_srp-batcher-gpu-instancing.md](../../architecture/2026-07_srp-batcher-gpu-instancing.md) — ST floor instancing + selection MPB off MeshRenderers
- Milestone: [test-server.md](../../milestones/test-server.md) T3 perf floor
