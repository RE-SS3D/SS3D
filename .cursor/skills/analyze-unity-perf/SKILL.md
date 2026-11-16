---
name: analyze-unity-perf
description: >-
  Analyzes SS3D Unity Profiler markdown exports under Logs/perf/. Use when the
  user mentions profiler export, hitch, GC spike, frame time, Logs/perf/, or asks
  why gameplay/Editor Play Mode is slow.
---

# Analyze Unity perf exports

Do **not** ingest raw `.raw` / `.data` Profiler files or full frame dumps. Only read
the ranked markdown produced by **SS3D/Perf/Export…**
(`Assets/Scripts/SS3D/Editor/Perf/ProfilerCaptureExporter.cs`).

Effort doc: [Documents/architecture/2026-07_unity-perf-ai-tooling.md](../../../Documents/architecture/2026-07_unity-perf-ai-tooling.md).
Noise / marker notes: [reference.md](reference.md).

## Checklist

```
- [ ] Open the Logs/perf/*.md export (or ask user to export via SS3D/Perf)
- [ ] Read header: scenario, frames, avg_frame_ms, deep_profile, source
- [ ] Rank Top GC Alloc first, then Top self-time, then SS3D / marker hits
- [ ] Classify top 1–3 offenders; open INDEX → system map before code
- [ ] Report hypotheses with file/map links and fix class (alloc / algorithm / expected)
```

## Step 1: Get the artifact

If the user has no file yet:

1. Play Mode with the Profiler recording (CPU module).
2. Menu **SS3D/Perf/Export Current Profiler Capture…** or **Export Last 300 Frames…**.
3. Save under `Logs/perf/` (gitignored via `/[Ll]ogs/`).

Never ask for a raw capture as the primary input.

## Step 2: Read in this order

1. **Header** — scenario label, Editor vs implied build, frame range, `avg_frame_ms`, `deep_profile`.
2. **Top GC Alloc** — per-tick managed alloc is the recurring SS3D failure mode
   ([TECH_DEBT.md](../../../Documents/architecture/TECH_DEBT.md) §2).
3. **Top self-time** — CPU cost; ignore idle/`WaitForTargetFPS` as a “bug”.
4. **SS3D / marker hits** — prefer these when present (`Vision.*`, `Atmos*`, `FishNet`, `SS3D*`).

Dig into **at most three** named offenders.

## Step 3: Classify

| Signature | Likely cause | Where to look |
|-----------|--------------|---------------|
| GC + SS3D / managed type names | Per-tick alloc | TECH_DEBT §2; [atmospherics](../../../Documents/architecture/systems/atmospherics.md), [tile](../../../Documents/architecture/systems/tile.md), [substances](../../../Documents/architecture/systems/substances.md) Pitfalls |
| `Vision.*` | FOV / vision map | Vision markers in code; [rendering](../../../Documents/architecture/systems/rendering.md) |
| `Atmos*` / ECS jobs | Sim / upload cost | [atmospherics](../../../Documents/architecture/systems/atmospherics.md) |
| `FishNet` / network tick | Net observers / sync | [networking-session](../../../Documents/architecture/systems/networking-session.md) |
| URP / `ScriptableRenderer` / Blitter only | Render path | [rendering](../../../Documents/architecture/systems/rendering.md) — only if this is a **client** scenario |
| `PlayerLoop` / Editor-only stacks with no SS3D child | Noise / Editor overhead | Note capture source; do not treat as ship blocker alone |
| `WaitForTargetFPS` / idle | Not a problem | Stop |

Docs-first: [INDEX.md](../../../Documents/architecture/INDEX.md) → system map **Pitfalls** → hot path file. Do not full-tree grep as step one.

## Step 4: Output format

Lead with a one-line verdict, then:

1. **Offender** (exact sample name from the export)
2. **Hypothesis** (alloc / algorithm / expected cost)
3. **Evidence** (rank, self_ms / gc_bytes, scenario)
4. **Next code site** (system map + file path if known)

Keep the reply short unless the user asks for a deep dive.

## Do not

- Dump entire profiler buffers or megabyte CSVs into context.
- Start with Deep Profiling (skews timings; only if hierarchy is too shallow after a normal export).
- Treat Editor Play Mode cost as a release blocker without noting `source: Editor Profiler`.
- “Fix” condemned uGUI / grow `Human.prefab` as the first performance lever
  ([agent-first composition](../../../Documents/architecture/2026-07_agent-first-composition.md)).
- Edit `Documents/design/*`.
