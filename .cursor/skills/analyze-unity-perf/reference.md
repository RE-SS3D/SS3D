# Perf analysis reference

Companion to [SKILL.md](SKILL.md). Extend when exports produce recurring false positives.

## Export contract

Produced by `SS3D/Perf/Export…` → `Logs/perf/<scenario>.md`:

- `# Perf capture` header fields: `scenario`, `source`, `frames`, `avg_frame_ms`, `deep_profile`
- `## Top self-time` — top 30 by summed self time (ms)
- `## Top GC Alloc` — top 30 by summed GC bytes (samples with gc > 0)
- `## SS3D / marker hits` — subset matching prefixes below, or `(none)`

`self_%` is relative to **sum of frame times** over the exported range (not a single frame).

## Marker / game prefixes (exporter)

Matched into **SS3D / marker hits**:

| Prefix / token | Notes |
|----------------|-------|
| `SS3D` | Starts-with (`SS3D.Interactions.*`, `SS3D.Atmos.*`, URP `SS3D Atmos *`, …) |
| `Vision.` | `Vision.ViewPoints` / `Vision.VisionMap` in `VisionSubSystem` |
| `Atmos` | Starts-with (legacy / URP atmos samples); prefer `SS3D.Atmos.*` for gameplay ticks |
| `FishNet` | Substring |

### Gameplay markers (install intentionally)

| Marker | Where |
|--------|--------|
| `Vision.ViewPoints` / `Vision.VisionMap` | `VisionSubSystem` |
| `SS3D.Interactions.Outline` | `InteractionController` hover LateUpdate |
| `SS3D.Interactions.Discover` | `InteractionPipeline.Discover` (click/radial/RPC) |
| `SS3D.Atmos.Sim` / `SS3D.Atmos.Upload` | `AtmosSubSystem.SimTick` |
| `SS3D.Electricity.FixedUpdate` / `.CircuitsTick` / `.AreaPower` / `.OnTick` | `ElectricitySubSystem` Coimbra FixedUpdate path (`OnTick` = visual subscriber fan-out after CircuitsTick) |
| `SS3D.Airlock.NearbyTick` / `.EmptyPassSweep` | `AirLockProximityService` |

## Noise allowlist (usually not root cause)

Treat as secondary unless they dominate **and** the scenario is explicitly render/client-bound:

- `WaitForTargetFPS`
- `Gfx.WaitForPresentOnGfxThread` / present waits
- Bare `PlayerLoop` / `Update.ScriptRunBehaviourUpdate` with no SS3D child in marker hits
- Editor-only overhead when `source` is Editor Profiler and no player/server capture exists
- UI Toolkit / uGUI layout spam during condemned-panel work (do not “optimize” condemned UI first)

## Related maps

- [TECH_DEBT.md](../../../Documents/architecture/TECH_DEBT.md) § Hot-path GC
- [atmospherics.md](../../../Documents/architecture/systems/atmospherics.md)
- [tile.md](../../../Documents/architecture/systems/tile.md)
- [substances.md](../../../Documents/architecture/systems/substances.md)
- [rendering.md](../../../Documents/architecture/systems/rendering.md)
- [networking-session.md](../../../Documents/architecture/systems/networking-session.md)
