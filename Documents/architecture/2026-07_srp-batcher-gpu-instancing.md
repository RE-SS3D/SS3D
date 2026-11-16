> Implements: (perf / scale — no single design §; playable floor for SS13-size maps)
> Touches systems: rendering, selection, tile, structural-destruction
> Status: shipped

# SRP Batcher + GPU Instancing

## Goal

Cut Metastation `Render.Mesh` / `ApplyShader` cost by unlocking URP **SRP Batcher** and **GPU Instancing** for station tile draws. Simple Toon already had the shader contract (`UnityPerMaterial` CBUFFER + `#pragma multi_compile_instancing`); this effort is content + runtime hygiene.

## Shipped

### Materials

- Enabled `m_EnableInstancingVariants` on ST floor mats: TileGrey, GreyDark, Bar, Kitchen, Wood, Reinforced (TilePlating / Palette already on).
- `GenericShadeless` (URP Unlit — airlock door-light submesh) instancing On — Frame Debugger Unlit instances rose; door leaf Mesh events dropped (~70→~28).
- Left URP Lit overlays / TileWhite for a later shader migration.

### Adjacency `sharedMesh`

- [`TileAdjacencyView`](../../Assets/Scripts/SS3D/Systems/Tile/Connections/TileAdjacencyView.cs), [`AbstractHorizontalConnector`](../../Assets/Scripts/SS3D/Systems/Tile/Connections/AbstractHorizontalConnector.cs), [`DirectionalAdjacencyConnector`](../../Assets/Scripts/SS3D/Systems/Tile/Connections/DirectionalAdjacencyConnector.cs), [`DisposalPipeAdjacencyConnector`](../../Assets/Scripts/SS3D/Systems/Tile/Connections/DisposalPipeAdjacencyConnector.cs) assign `_filter.sharedMesh` (not `.mesh`) so adjacency variants keep the shared FBX asset for instancing.

### Selection pick without permanent MeshRenderer MPBs

- [`Selectable`](../../Assets/Scripts/SS3D/Systems/Selection/Selectable.cs) registers as [`SelectionPickContext.ISelectionPickSource`](../../Assets/Scripts/SS3D/Rendering/URP/SelectionPickContext.cs); MeshRenderers stay MPB-free.
- [`SelectionPickRendererFeature`](../../Assets/Scripts/SS3D/Rendering/URP/SelectionPickRendererFeature.cs) draws registered meshes with `DrawMesh` + transient `MaterialPropertyBlock` (skinned still use permanent MPB + `DrawRenderer` — few characters).
- Pick collect frustum-culls with the request camera (`IsInPickFrustum`) — AOI alone is not enough after leaving MeshRenderer draws.
- Permanent `_SelectionColor` MPBs were the main SRP Batcher breaker on every floor/wall tile.

### Structural integrity Intact clear

- [`StructuralIntegrityPresenter.Apply(Intact)`](../../Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralIntegrityPresenter.cs) calls `SetPropertyBlock(null)` instead of writing a white tint MPB.

### Emissive/power MPB for fixtures

- Runtime emissive / powered visuals for station fixtures should avoid `renderer.material(s)` (material-instance clones).
- Use `Renderer.SetPropertyBlock` (typically `_Lumin` + `_EmissionColor`, and the shader’s base-color property for indicators) so SRP Batcher can keep draws grouped.
- Fixed in code: `LightPower`, `LightSwitchController`, `ConsumerPowerVisual`, `AirLockOpener`, `AirAlarmController`.

### Light / reflection probes Off

- SS3D does not use probe lighting. BlendProbes on MeshRenderers showed up as Frame Debugger
  "Non-instanced properties set for instanced shader" and blocked GPU instancing on ST/Palette.
- Content recipe: **SS3D/Rendering/Run Content Prefab Recipes** → `RendererProbeUsageSetup`
  (all `Assets/Content` prefab renderers → `LightProbeUsage` / `ReflectionProbeUsage` Off).
- Bulk applied Jul 2026 (~188 prefabs); EditMode guard on TileGrey / SteelWall / window / airlock samples.

### EditMode

- `SrpBatcherInstancingTests` — floor mat instancing flags, adjacency sharedMesh, Selectable no permanent MPB, station structure probes Off.
- `StructuralIntegrityPresentationTests.Presenter_ApplyIntact_ClearsMaterialPropertyBlock`.

## Verification (Play Mode)

- Frame Debugger on Metastation (Map Editor closed): identical TileGrey floors should GPU-instance / SRP-batch; damaged walls may still split.
- After probe Off + GenericShadeless instancing: expect “material doesn't have GPU instancing” gone; remaining named breaks ≈ different meshes + non-instanced props (emissive MPBs). STDefault draw/instance ratio stays high until mesh combine.
- Re-export Profiler under `Logs/perf/` — expect `Render.Mesh` / `ApplyShader` down vs early Metastation baselines; Vision/Atmos/Electricity markers still dominate CPU when present.
- Hover/examine/interaction pick still resolves on floors and doors.
- After frustum cull: `SS3D Selection Pick` should stay tiny in Frame Debugger feature hits (~1).

## Explicitly deferred

- Chunk-/room-combined floor meshes (main remaining ST GPU-instancing wall).
- TileWhite / overlay Lit → STDefault.
- GPU Resident Drawer (stays off — Linux/OpenGL).
- Narrowing `ConsumerPowerVisual` / fixture MPBs off shared Palette bodies (incremental only).

## Related docs

- Prior scale pass: [2026-07_metastation-scale-perf.md](../archive/architecture/2026-07_metastation-scale-perf.md)
- System maps: [rendering](systems/rendering.md), [selection](systems/selection.md), [tile](systems/tile.md), [structural-destruction](systems/structural-destruction.md)
