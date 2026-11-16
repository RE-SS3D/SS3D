# Frame Debugger analysis reference

Companion to [SKILL.md](SKILL.md). Extend when exports produce recurring false positives.

## Export contract

Produced by `SS3D/Perf/Export Frame Debugger…` → `Logs/framedebug/<scenario>.md`:

- `# Frame Debugger capture` header: `scenario`, `source`, `graphics_api`, `event_count`, `draw_events`, `mode` (`quick`|`full`); Full also has `detail_hits`
- `## Pass distribution` — top 30 by event-path / pass key
- `## Top draw objects` — GameObject name counts on draw-ish events
- `## Batch breaks` — Full only; Quick prints `(skipped — quick mode)`
- `## Shader / pass hits` — Full: shaders + draws + instances; Quick: `event_type` histogram
- `## SS3D / feature hits` — substring tokens below, or `(none)`

## Feature tokens (exporter)

Matched into **SS3D / feature hits** (case-insensitive substring on event path / object / shader):

| Token | Notes |
|-------|-------|
| `Selection Pick` | URP pick pass / `SelectionPickRendererFeature` |
| `Atmos` | Atmos scatter / glow / distortion passes |
| `Vision` | FOV mask composite |
| `UiBackdrop` | Dual Kawase blur behind diegetic UI |
| `SS3D` | Generic SS3D pass / object naming |
| `STDefault` | Simple Toon opaque |
| `Simple Toon` | ST shader family |
| `ObjectIcon` | UI icon preview unlit path |

## Draw-ish event types

Counted toward `draw_events`: `Mesh`, `InstancedMesh`, `SRPBatch`, `DynamicBatch`, `StaticBatch`, `DynamicGeometry`, `GLDraw`, `SkinOnGPU`, `DrawProcedural*`, `HybridBatch`.

## Noise allowlist (usually not root cause)

Treat as secondary unless they dominate **and** the scenario is explicitly render-bound:

- Depth / shadow cascades alone with no SS3D feature spike
- UI Toolkit overlay draws during condemned-panel work
- Editor-only Gizmo / Handles events when inspecting Scene view (prefer Game view capture)
- OpenGL captures with suspiciously low `event_count` (re-run on Vulkan)
- **DepthNormals prepass** when Decal Layers / DBuffer are on — expected cost for blood; SSAO off does **not** remove it
- Absent SSAO passes after Forward+ `ScreenSpaceAmbientOcclusion` disabled — intentional

## Prerequisites

- Frame Debugger **Enabled** (Play Mode paused) with a populated event tree
- Linux: prefer **Vulkan** graphics API for reliable capture
- Full mode GPU-replays draw events (slower); use Quick first

## Related maps

- [rendering.md](../../../Documents/architecture/systems/rendering.md)
- [selection.md](../../../Documents/architecture/systems/selection.md)
- [structural-destruction.md](../../../Documents/architecture/systems/structural-destruction.md) (Intact MPB clear)
- [2026-07_srp-batcher-gpu-instancing.md](../../../Documents/architecture/2026-07_srp-batcher-gpu-instancing.md)
- Sibling CPU tooling: [analyze-unity-perf](../analyze-unity-perf/SKILL.md)
