> Implements: (infra tooling — no design-doc section)
> Touches systems: rendering, selection (analysis routing only; Editor tooling under SS3D.Editor)
> Status: shipped

# Unity Frame Debugger AI tooling (Jul 2026)

## Goal

Make Unity Frame Debugger captures usable by agents without pasting raw event trees
or multi-MB JSON: Editor **export** produces a small ranked markdown report; a Cursor
**skill** drives analysis against rendering / selection system maps.

Sibling to [2026-07_unity-perf-ai-tooling.md](2026-07_unity-perf-ai-tooling.md) (CPU Profiler).

## Shipped

### Editor export

- Menu: **SS3D/Perf/Export Frame Debugger (Quick)…** (agent default)
- Menu: **SS3D/Perf/Export Frame Debugger (Full)…** (GPU-replay shaders / batch breaks)
- Code: [`Assets/Scripts/SS3D/Editor/Perf/FrameDebuggerExporter.cs`](../../Assets/Scripts/SS3D/Editor/Perf/FrameDebuggerExporter.cs)
- Binding: [`FrameDebuggerReflection.cs`](../../Assets/Scripts/SS3D/Editor/Perf/FrameDebuggerReflection.cs)
- Output: `Logs/framedebug/<scenario>.md` (covered by `/[Ll]ogs/` in `.gitignore`)
- APIs: reflection on `UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility`
  (verified Editor `6000.3.28f1`); public `UnityEngine.FrameDebugger.enabled` for gating

Report sections (stable contract for the skill):

1. Header — scenario, source, `graphics_api`, `event_count`, `draw_events`, `mode`
2. Pass distribution (top 30)
3. Top draw objects (top 30)
4. Batch breaks (Full only; Quick notes skipped)
5. Shader / pass hits (Full: shaders; Quick: event-type counts)
6. SS3D / feature hits (token substrings)

### Analysis skill

- [`.cursor/skills/analyze-unity-framedebug/SKILL.md`](../../.cursor/skills/analyze-unity-framedebug/SKILL.md)
- Companion: [`.cursor/skills/analyze-unity-framedebug/reference.md`](../../.cursor/skills/analyze-unity-framedebug/reference.md)

## Local workflow

1. Prefer **Vulkan** on Linux (OpenGL Frame Debugger is often empty / unreliable).
2. Play Mode with Game view rendering → Frame Debugger **Enable**.
3. **SS3D/Perf/Export Frame Debugger (Quick)…** → save under `Logs/framedebug/`.
4. Ask an agent to analyze with **analyze-unity-framedebug** (or paste the markdown path).
5. Use **Full** when batch-break causes or per-shader instance counts are required.

Headless smoke (report shape only, no live GPU events): Unity
`-executeMethod SS3D.Editor.Perf.FrameDebuggerExporter.VerifyBatchMode`
writes `Logs/framedebug/_contract-fixture.md`.

## Deferred (follow-up)

- Player / remote Frame Debugger attach
- Auto-enable Frame Debugger from menus
- Continuous draw-call budgets in smoke tests
- Optional JSON sidecar for deep dives

## Known gaps

- Internal API — field / method names can change across Unity minors; reflection discover errors surface as dialogs.
- Full export GPU-replays up to 800 draw events (~2 editor frames each); large Metastation frames may truncate.
- Quick mode cannot report batch-break strings or real shader names.
- Full mode can report `detail_hits: 0` if GPU replay fails to populate event data — treat Batch breaks / Shader tables as missing and re-export; pass distribution from Quick path is still valid.
