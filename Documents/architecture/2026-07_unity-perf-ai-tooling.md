> Implements: (infra tooling — no design-doc section)
> Touches systems: (none as gameplay domains; Editor tooling under SS3D.Editor)
> Status: shipped

# Unity perf AI tooling (Jul 2026)

## Goal

Make Unity Profiler captures usable by agents without pasting raw `.raw` dumps:
Editor **export** produces a small ranked markdown report; a Cursor **skill** drives
analysis against system maps and TECH_DEBT.

## Shipped

### Editor export

- Menu: **SS3D/Perf/Export Current Profiler Capture…**
- Menu: **SS3D/Perf/Export Last 300 Frames…**
- Code: [`Assets/Scripts/SS3D/Editor/Perf/ProfilerCaptureExporter.cs`](../../Assets/Scripts/SS3D/Editor/Perf/ProfilerCaptureExporter.cs)
- Output: `Logs/perf/<scenario>.md` (covered by `/[Ll]ogs/` in `.gitignore`)
- APIs: `UnityEditorInternal.ProfilerDriver` + `UnityEditor.Profiling.HierarchyFrameDataView` (main thread, merge-same-name)

Report sections (stable contract for the skill):

1. Header — scenario, source, frame range, `avg_frame_ms`, `deep_profile`
2. Top self-time (top 30)
3. Top GC Alloc (top 30)
4. SS3D / marker hits (prefixes: `SS3D`, `Vision.`, `Atmos`, `FishNet`)

### Analysis skill

- [`.cursor/skills/analyze-unity-perf/SKILL.md`](../../.cursor/skills/analyze-unity-perf/SKILL.md)
- Companion: [`.cursor/skills/analyze-unity-perf/reference.md`](../../.cursor/skills/analyze-unity-perf/reference.md)

## Local workflow

1. Play Mode with Profiler recording (CPU).
2. **SS3D/Perf/Export…** → save under `Logs/perf/`.
3. Ask an agent to analyze with **analyze-unity-perf** (or paste the markdown path).

Headless smoke (compile + report shape, no live frames): Unity
`-executeMethod SS3D.Editor.Perf.ProfilerCaptureExporter.VerifyBatchMode`
writes `Logs/perf/_contract-fixture.md`.

## Deferred (follow-up)

- In-game console `perf start/stop`
- `Profiler.logFile` player / dedicated-server capture pipeline
- Continuous `ProfilerRecorder` budgets / smoke thresholds
- Separate `perf_summarize.py` (export already aggregates)

Sibling GPU / draw-call tooling: [2026-07_unity-framedebug-ai-tooling.md](2026-07_unity-framedebug-ai-tooling.md).

Gameplay `ProfilerMarker`s currently include Vision FOV, `SS3D.Interactions.Outline` /
`Discover`, and `SS3D.Atmos.Sim` / `Upload` — extend via export `GameMarkerPrefixes` when
adding domains outside `SS3D` / `Vision.` / `Atmos` / `FishNet`.

## Known gaps

- Export is Editor-Profiler only; Development player builds need a later capture path.
- Main-thread hierarchy only (thread index 0).
- Marker-hit filtering is prefix-based; extend `GameMarkerPrefixes` when new markers land.
