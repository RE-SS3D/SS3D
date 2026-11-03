---
name: diagnosing-bugs
description: Diagnosis loop for hard bugs and performance regressions. Use when the user says "diagnose"/"debug this", or reports something broken/throwing/failing/slow.
---

# Diagnosing Bugs

A discipline for hard bugs. Skip phases only when explicitly justified.

## Phase 0: Check the banked failures

Find the domain in `Documents/architecture/INDEX.md` and read its system map's **Pitfalls** first. SS3D's worst bugs are silent (UI Toolkit layout, FishNet serialization, prefab wiring that no-ops, init order), and the map is where past ones were banked as symptom → cause → fix. A match there may end the diagnosis; still build the loop below to prove it.

## Redact

Show commands, outputs and captured artifacts with every secret replaced by `<REDACTED>`. Quote only the log lines that carry the signal. If the redacted output is not enough to diagnose the bug, say so and ask the user.

## Phase 1: Build a feedback loop

**This is the skill.** Everything else is mechanical. If you have a **tight** pass/fail signal for the bug (one that goes red on _this_ bug), you will find the cause; bisection, hypothesis-testing, and instrumentation all just consume it. If you don't have one, no amount of staring at code will save you.

Spend disproportionate effort here. **Be aggressive. Be creative. Refuse to give up.**

### Ways to construct one, tightest first

1. **EditMode test** (`Assets/Scripts/Tests/EditMode`, `SS3D.Tests.EditMode`) against the pure logic. Many systems keep their rules in static/pure classes (e.g. `ArmorSimulation`); if the bug lives in a MonoBehaviour, look for or extract that seam. Run via Unity MCP `run_tests`, or headless per `.cursor/skills/fix-editmode-tests/SKILL.md`.
2. **PlayMode test** (`Assets/Scripts/Tests/PlayMode`) when the bug needs scenes, frames, or the network stack. Not-yet-fixed repros go in `Assets/Scripts/Tests/KnownIssueReproduction`; promote them into the regular suite once green.
3. **Live editor probe** through Unity MCP: `execute_code` to drive the code path, `read_console` to read the result. Needs `Testing/ui/start-mcp.sh` + `Testing/ui/open-unity.sh` (see `Testing/ui/README.md`). An editor stalled at 0% CPU is a modal dialog: `Testing/ui/watch-dialogs.sh`.
4. **UI / session pilot**: `Testing/ui/validate.sh --steps <focused step>` (the step list is in `Testing/ui/README.md`) produces a summary, plus screenshots for visual steps. Not `--nightly`.
5. **Multiplayer smoke**: `Testing/multiplayer/run_smoketest.sh` with a scenario from `Testing/multiplayer/scenarios/` (late-join, reconnect, observer-sync, ...), triaged with `Testing/multiplayer/tools/triage_run.sh`. Slow (needs builds); reach for it when the bug only shows across real client/server processes. See `.cursor/skills/run-multiplayer-smoke/` and `.cursor/skills/triage-multiplayer-smoke/`.
6. **Differential loop**: same input through old vs new commit, or host vs dedicated server, and diff the logs.
7. **Bisection harness**: `git bisect run` over a scripted EditMode test. Each step can trigger a reimport, so bisect only with a loop that's already tight.
8. **HITL bash script.** Last resort. If a human must click in the editor or client, drive _them_ with `scripts/hitl-loop.template.sh` so the loop is still structured. Captured output feeds back to you.

Build the right feedback loop, and the bug is 90% fixed.

### Tighten the loop

Treat the loop as a product. Once you have _a_ loop, **tighten** it:

- Can I make it faster? (Pure-logic EditMode over PlayMode; one test filter, not the suite.)
- Can I make the signal sharper? (Assert on the specific symptom, not "didn't crash".)
- Can I make it more deterministic? (Seed RNG, fix tick/frame count, control spawn order.)

A 30-second flaky loop is barely better than no loop; a 2-second deterministic one is tight, a debugging superpower.

### Non-deterministic bugs

The goal is not a clean repro but a **higher reproduction rate**. Loop the trigger 100×, add stress, narrow timing windows (network latency simulation, frame stepping). A 50%-flake bug is debuggable; 1% is not, so keep raising the rate until it's debuggable.

### When you genuinely cannot build a loop

Stop and say so explicitly. List what you tried. Ask the user for: (a) access to whatever environment reproduces it, (b) a redacted captured artifact (`Logs/`, a smoke `.runs` directory, a `Logs/perf/` profiler export, a screen recording with timestamps), or (c) permission to add temporary instrumentation. Do **not** proceed to hypothesise without a loop.

### Completion criterion: a tight loop that goes red

Phase 1 is done when the loop is **tight** and **red-capable**: you can name **one command** (a test filter, a script path, an `execute_code` snippet) that you have **already run at least once** (show the invocation and its output, redacted), and that is:

- [ ] **Red-capable**: it drives the actual bug code path and asserts the **user's exact symptom**, so it can go red on this bug and green once fixed.
- [ ] **Deterministic**: same verdict every run (flaky bugs: a pinned, high reproduction rate, per above).
- [ ] **Fast**: seconds, not minutes (multiplayer smoke is the accepted exception).
- [ ] **Agent-runnable**: you can run it unattended; a human in the loop only via `scripts/hitl-loop.template.sh`.

If you catch yourself reading code to build a theory before this command exists, **stop: jumping straight to a hypothesis is the exact failure this skill prevents.** No red-capable command, no Phase 2.

## Phase 2: Reproduce + minimise

Run the loop. Watch it go red as the bug appears.

Confirm:

- [ ] The loop produces the failure mode the **user** described, not a different failure that happens to be nearby. Wrong bug = wrong fix.
- [ ] The failure is reproducible across multiple runs (or, for non-deterministic bugs, at a high enough rate to debug against).
- [ ] You have captured the exact symptom (error message, wrong output, slow timing) so later phases can verify the fix.

### Minimise

Shrink the repro to the **smallest scenario that still goes red**. Cut inputs, callers, prefab components, config, and steps **one at a time**, re-running the loop after each cut, and keep only what's load-bearing.

Done when **every remaining element is load-bearing**: removing any one of them makes the loop go green.

## Phase 3: Hypothesise

Generate **3–5 ranked hypotheses** before testing any of them. Single-hypothesis generation anchors on the first plausible idea. For networked bugs, always consider which side runs the code (server, client, host) and whether the state is synced at all.

Each hypothesis must be **falsifiable**:

> Format: "If <X> is the cause, then <changing Y> will make the bug disappear / <changing Z> will make it worse."

**Show the ranked list to the user before testing.** They often have domain knowledge that re-ranks instantly. Don't block on it; proceed with your ranking if the user is AFK.

## Phase 4: Instrument

Each probe must map to a specific prediction from Phase 3. **Change one variable at a time.**

1. **Live inspection** via Unity MCP `execute_code` / `find_gameobjects` beats logging.
2. **Targeted logs** at the boundaries that distinguish hypotheses, through the project logger (`Log.Info(sender, context, message)`; see `Documents/architecture/systems/logging.md`).
3. Never "log everything and grep".

**Tag every debug log** with a unique prefix, e.g. `[DEBUG-a4f2]`. Cleanup becomes a single grep.

**Perf branch.** For performance regressions, establish a baseline first: a Profiler export under `Logs/perf/` or a Frame Debugger export under `Logs/framedebug/`, analysed per `.cursor/skills/analyze-unity-perf/` and `.cursor/skills/analyze-unity-framedebug/`. Then bisect. Measure first, fix second.

## Phase 5: Fix + regression test

Write the regression test **before the fix**, but only if there is a **correct seam** for it: one where the test exercises the real bug pattern as it occurs at the call site. If the only available seam is too shallow, a test there gives false confidence. **If no correct seam exists, that itself is the finding.** Note it.

If a correct seam exists:

1. Turn the minimised repro into a failing test at that seam.
2. Watch it fail.
3. Apply the fix.
4. Watch it pass.
5. Re-run the Phase 1 loop against the original (un-minimised) scenario.

## Phase 6: Cleanup

Required before declaring done:

- [ ] Original repro no longer reproduces (re-run the Phase 1 loop)
- [ ] Regression test passes (or absence of seam is documented); a repro in `KnownIssueReproduction` is promoted into the regular suite
- [ ] All `[DEBUG-...]` instrumentation removed (`grep` the prefix)
- [ ] Throwaway prototypes deleted
- [ ] A silent failure (no error, no log) is banked in the owning system map's **Pitfalls** as symptom → cause → fix, via `update-system-docs`
- [ ] The hypothesis that turned out correct is stated in the commit / PR message
