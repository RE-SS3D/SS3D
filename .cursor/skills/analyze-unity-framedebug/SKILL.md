---
name: analyze-unity-framedebug
description: >-
  Analyzes SS3D Unity Frame Debugger markdown exports under Logs/framedebug/.
  Use when the user mentions Frame Debugger, draw calls, SRP Batcher, GPU
  instancing, pick-pass spam, Logs/framedebug/, or asks why a frame has too
  many draws / batch breaks.
---

# Analyze Unity Frame Debugger exports

Do **not** ingest raw Frame Debugger UI copy-paste, multi-MB JSON, or RenderDoc
captures as the primary input. Only read the ranked markdown produced by
**SS3D/Perf/Export Frame Debugger…**
(`Assets/Scripts/SS3D/Editor/Perf/FrameDebuggerExporter.cs`).

Effort doc: [Documents/architecture/2026-07_unity-framedebug-ai-tooling.md](../../../Documents/architecture/2026-07_unity-framedebug-ai-tooling.md).
Tokens / noise: [reference.md](reference.md).

## Checklist

```
- [ ] Open the Logs/framedebug/*.md export (or ask user to export via SS3D/Perf)
- [ ] Read header: scenario, graphics_api, event_count, draw_events, mode
- [ ] Rank SS3D / feature hits first, then pass distribution, then batch breaks / shaders
- [ ] Classify top 1–3 offenders; open INDEX → rendering / selection maps before code
- [ ] Report hypotheses with file/map links and fix class (cull / batch / expected)
```

## Step 1: Get the artifact

If the user has no file yet:

1. Editor on **Vulkan** when possible (Linux OpenGL often yields empty / broken Frame Debugger).
2. Play Mode → Game view rendering → **Window > Analysis > Frame Debugger** → **Enable** (pauses).
3. Menu **SS3D/Perf/Export Frame Debugger (Quick)…** (default) or **(Full)…** for shaders / batch breaks.
4. Save under `Logs/framedebug/` (gitignored via `/[Ll]ogs/`).

Never ask for a raw full-event JSON dump as the primary input.

## Step 2: Read in this order

1. **Header** — `graphics_api` (flag OpenGL caveats), `mode` (`quick` vs `full`), counts.
2. **SS3D / feature hits** — prefer these (`Selection Pick`, `Atmos`, `Vision`, `STDefault`, …).
3. **Pass distribution** — URP / custom pass folders; watch for pick / atmos / vision spikes.
4. **Batch breaks** — Full only; `(skipped — quick mode)` means ask for Full if batching is the question.
5. **Shader / pass hits** — Full: shaders + instance counts; Quick: event-type histogram only.

Dig into **at most three** named offenders.

## Step 3: Classify

| Signature | Likely cause | Where to look |
|-----------|--------------|---------------|
| High `Selection Pick` hits / draws | Pick pass over-drawing AOI / no frustum or ray cull | [selection](../../../Documents/architecture/systems/selection.md) Pitfalls |
| Many identical floor objects as separate Mesh draws | Missing GPU instancing / SRP Batcher break (MPB) | [rendering](../../../Documents/architecture/systems/rendering.md), [srp-batcher-gpu-instancing](../../../Documents/architecture/2026-07_srp-batcher-gpu-instancing.md) |
| Batch break: different materials / MPB | Permanent `MaterialPropertyBlock` on MeshRenderers | rendering + structural-destruction Intact clear |
| `Atmos` / `Vision` / `UiBackdrop` dominating | Expected client features; check necessity / RT size | [rendering](../../../Documents/architecture/systems/rendering.md) Start here |
| `graphics_api: OpenGL*` + empty / tiny tree | Capture invalid on this GPU path | Re-export on Vulkan; do not treat as ship signal |
| Shadows / URP Lit only, no SS3D tokens | Generic URP cost | rendering look plan — only if client-bound scenario |

Docs-first: [INDEX.md](../../../Documents/architecture/INDEX.md) → system map **Pitfalls** → hot path file. Do not full-tree grep as step one.

## Step 4: Output format

Lead with a one-line verdict, then:

1. **Offender** (exact name / token from the export)
2. **Hypothesis** (cull / batch break / expected pass cost)
3. **Evidence** (rank, counts, `mode`, `graphics_api`)
4. **Next code site** (system map + file path if known)

Keep the reply short unless the user asks for a deep dive.

## Do not

- Dump entire Frame Debugger event lists or megabyte JSON into context.
- Treat Editor Game-view draw counts as a release blocker without noting `source: Editor Frame Debugger`.
- “Fix” condemned uGUI / grow `Human.prefab` as the first rendering lever
  ([agent-first composition](../../../Documents/architecture/2026-07_agent-first-composition.md)).
- Edit `Documents/design/*`.
