> Implements: Documents/design/atmospherics.md §9 (what you see — client visibility)
> Touches systems: atmospherics, rendering, networking-session
> Status: shipped

**Progress:** Phase 1: dirty-chunk sync; AOI, bandwidth caps, and late-join bootstrap deferred to Phase 2.

# Atmospherics Client Visualization Sync

## Goal

Make gas scatter, plasma glow, heat distortion, and fire visuals visible to **pure clients**, not only the host/server process. Simulation stays server-authoritative; clients receive enough grid visualization data to feed the existing GPU renderer.

## Current state (post Phase 1)

| Piece | Server / host | Pure client |
|---|---|---|
| `AtmosSimulation` tick | yes | no |
| `AtmosVisualizationBridge.PublishSnapshot` | yes (after each tick) | no |
| `AtmosGpuUploader` atlas build | yes | no |
| `AtmosDirtyChunkTracker` + `AtmosChunkPatchBuilder` | yes (after each tick) | no |
| `AtmosChunkPatch` `ObserversRpc` | sends dirty chunks | receives dirty chunks |
| `AtmosClientVisualizationBridge` / `AtmosClientAtlas` | n/a | yes (builds atlas from patches) |
| `AtmosCamera` render request | yes | yes (on `PlayerCamera.prefab`) |
| `AtmosRendererFeature` draw | yes (when snapshot present) | yes (once at least one chunk patch has arrived) |

The render path was already client-ready: `AtmosCamera` → `AtmosRenderContext` → `AtmosRendererFeature`. Phase 1 adds the missing **network transport** layer: `AtmosSubSystem` now tracks which 16×16 chunks changed meaningfully each tick (`AtmosDirtyChunkTracker`), builds a small per-chunk payload (`AtmosChunkPatchBuilder`), and broadcasts it to observers (`ObserversRpc`). Pure clients apply patches into their own `AtmosClientAtlas` (via `AtmosClientVisualizationBridge`) and feed the same `AtmosRenderContext.SetSnapshot` path the host already used.

Known Phase-1 limitation: a client that joins after gas state has stabilized only sees chunks that change again after it connects (no AOI or late-join bootstrap yet — see Phase 2).

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

| Phase | Deliverable | Status |
|---|---|---|
| 0 | Effort doc + system-map gap noted (this doc) | done |
| 1 | Dirty-chunk tracking on server; client bridge applies patches; visuals work in dedicated-server + client | done |
| 2 | AOI-scoped sends, bandwidth cap, late-join bootstrap | planned (partial: host-only skips patch broadcast; dirty scan = active cells only; patch buffer reuse) |
| 3 | Optional: active-region-only atlas bounds, delta encoding | **partial** — host AOI atlas bounds shipped in [2026-07_metastation-scale-perf.md](../archive/architecture/2026-07_metastation-scale-perf.md); delta encoding still open |

## Out of scope

- Client-side sim replay or prediction
- Full-map texture RPC each tick
- Generic VFX framework (particles, decals unrelated to turf grid)
- Liquid/solid phase rendering (separate future work per design §10)

## Documented fork deviation (until Phase 2)

- Pure clients that join **after** gas state has settled won't see already-stable atmos visuals until the next meaningful change ticks a chunk dirty again — there's no late-join bootstrap or AOI scoping yet (Phase 2). Newly-changing chunks (fires, breaches, venting) sync immediately.

## Verification

- EditMode: `Assets/Scripts/Tests/EditMode/Atmospherics/Atmos{DirtyChunkTracker,ChunkPatchBuilder,ChunkPatchSerializer,ClientAtlas}Tests.cs`.
- Multiplayer test harness (real headless server + pure-client process, not just EditMode):
  the `observer-sync` smoke scenario (`Testing/multiplayer/scenarios/observer-sync*.txt`, run via
  `./Testing/multiplayer/run_smoketest.sh observer-sync 2`). Client 0 forces a fresh dirty chunk
  with `atmosdebug heat` (headless equivalent of `AtmosDebugController`'s GUI buttons; it also
  changes a held item, health, a door and the area lights), and client 1 asserts its own
  `AtmosRenderContext` snapshot is valid with `atmosclientstatus assert` — a real regression check
  that a pure client receives and applies chunk patches, not just that the render path compiles.
  Wired into the nightly `multiplayer-smoke-test.yml`. See
  [Multiplayer testing](systems/networking-session-testing.md).

## Related docs

- System map: [atmospherics.md](systems/atmospherics.md)
- Prior effort: [2026-07_atmos-ecs-foundation.md](../archive/architecture/2026-07_atmos-ecs-foundation.md)
- Design (read-only): [atmospherics.md](../design/atmospherics.md) §9
- [rendering.md](systems/rendering.md) — `AtmosRenderContext` / `AtmosRendererFeature`
- [Multiplayer testing](systems/networking-session-testing.md) — headless
  server+client regression coverage (`observer-sync` scenario)
