> Implements: Documents/design/atmospherics.md §10 (gas rendering — client visibility)
> Touches systems: atmospherics, rendering, networking-session
> Status: planned

# Atmospherics Client Visualization Sync

## Goal

Make gas scatter, plasma glow, heat distortion, and fire visuals visible to **pure clients**, not only the host/server process. Simulation stays server-authoritative; clients receive enough grid visualization data to feed the existing GPU renderer.

## Current state

| Piece | Server / host | Pure client |
|---|---|---|
| `AtmosSimulation` tick | yes | no |
| `AtmosVisualizationBridge.PublishSnapshot` | yes (after each tick) | no |
| `AtmosGpuUploader` atlas build | yes | no |
| `AtmosCamera` render request | yes | yes (on `PlayerCamera.prefab`) |
| `AtmosRendererFeature` draw | yes (when snapshot present) | no (empty `AtmosRenderContext`) |

The render path is already client-ready: `AtmosCamera` → `AtmosRenderContext` → `AtmosRendererFeature`. The missing layer is **network transport** from the authoritative snapshot to each client.

## Why a quick fix is not enough

- **No sync exists today** — zero RPCs or sync vars on the visualization path.
- **Full-atlas broadcast is too heavy** — `AtmosGpuUploader` can cover the whole loaded map (six textures, power-of-two atlas). A 512×512 station slice is multiple MB per tick at 5 Hz.
- **Uploader bounds all created chunks** — not just active or player-visible cells, which inflates payload if sent naively.

Do **not** block on a generic VFX/particle system. Atmos visuals are simulation-backed grid rendering (design §10), not ephemeral one-shot effects.

## Proposed approach

Thin sync layer on top of existing types — no renderer rewrite.

### 1. Chunk dirty patches (server)

- Reuse tile chunk grain (`AtmosConstants.ChunkSize` = 16×16).
- After each sim tick, mark chunks dirty when active/semiactive cells inside changed meaningfully (pressure, temperature, composition, burn intensity).
- Build patch payloads from `AtmosGpuUploader` scratch arrays for dirty chunks only (pressure, temperature, composition, flow, fire, mask + atlas bounds).

### 2. Interest management

- Send patches only for chunks in each connection's area of interest (FishNet HashGrid / tile radius around observer).
- Cap bandwidth: coalesce patches, skip chunks with no visual delta, throttle if over budget.

### 3. Client visualization bridge

- Run `AtmosVisualizationBridge` (or a sibling `AtmosClientVisualizationBridge`) on clients without `AtmosSimulation`.
- `ApplyChunkPatch(...)` writes into a client-side `AtmosGpuUploader` atlas.
- Call existing `AtmosRenderContext.SetSnapshot` — same path `AtmosRendererFeature` already consumes.

### 4. Late join

- On observer start, send an initial snapshot of visible chunks before incremental patches.

## Phases

| Phase | Deliverable |
|---|---|
| 0 | Effort doc + system-map gap noted (this doc) |
| 1 | Dirty-chunk tracking on server; client bridge applies patches; visuals work in dedicated-server + client |
| 2 | AOI-scoped sends, bandwidth cap, late-join bootstrap |
| 3 | Optional: active-region-only atlas bounds, delta encoding |

## Out of scope

- Client-side sim replay or prediction
- Full-map texture RPC each tick
- Generic VFX framework (particles, decals unrelated to turf grid)
- Liquid/solid phase rendering (separate future work per design §10)

## Documented fork deviation (until shipped)

- Atmospherics VFX is **host/server-only** in multiplayer. Pure clients see no fog, fire, or plasma glow despite `AtmosRendererFeature` and `AtmosCamera` being wired.

## Related docs

- System map: [atmospherics.md](systems/atmospherics.md)
- Prior effort: [2026-07_atmos-ecs-foundation.md](2026-07_atmos-ecs-foundation.md)
- Design (read-only): [atmospherics.md](../design/atmospherics.md) §10
- [rendering.md](systems/rendering.md) — `AtmosRenderContext` / `AtmosRendererFeature`
