> Implements: infrastructure — testing policy for AI-driven development; operational runbooks stay in [Testing/ui/README.md](../../Testing/ui/README.md) and [systems/networking-session-testing.md](systems/networking-session-testing.md)
> Touches systems: networking-session, rounds-lobby, ui-shell, all domains with tests
> Status: shipped

# Testing philosophy

The harness exists so an agent can change code and quickly learn whether it broke something, then
keep iterating. Regressions get caught because the checks run often, not because any single run is
exhaustive. Wall-clock time and tokens are the budget. A slow check that rarely fails costs something
on every run and slows development, which is the current priority.

**Rule:** test each behaviour in the cheapest tier that can prove it. Agents run targeted checks.
Expensive suites run nightly, and a regression caught there is acceptable for now.

## Tiers

| Tier | Proves | Doesn't do | Where it runs |
|------|--------|------------|---------------|
| **1. EditMode** | Given these inputs, expect these outputs: rules, state machines, calculations, data and asset validation. | Scenes, frames, networking. | Every change (whole suite, under ~1 min). CI on PRs. |
| **2. Scenarios** | Behaviour that spans systems, in one Play Mode host: e.g. spawn a medkit in hand, hurt the player, apply the bandage (console + inventory + health). | Separate client/server processes. Lobby and round flow, except where that is the subject. | Targeted: the scenarios for the systems a change touched. All of them nightly. |
| **3. Multiplayer** | What only breaks when client and server are separate processes: late join, reconnect and body reclaim, ownership, state syncing to observers. | Gameplay that a host scenario already proves. | Nightly (`multiplayer-smoke-test.yml`). Locally only when the change touches networking or session contracts. |
| **4. Visual** | Rendered UI and screens look right. Screenshots are evidence for a human or agent to review. | Act as the pass/fail gate for logic. | Only when the change is visual (UI Toolkit, USS, rendering, screen effects). |

## Rules

1. **Cheapest tier first.** Before adding a check to tier 2–4, ask whether a pure seam could make it
   an EditMode test. Extracting that seam is usually worth it.
2. **Expensive tiers cover several systems at once.** Tiers 2–3 pay a large setup cost (Play Mode, a
   station, a round, a build). A check that pays it should exercise interactions between several
   systems, not one isolated rule.
3. **Pay setup once.** A scenario run boots once into a test room and runs many checks. Going through
   lobby, ready and round start belongs only to the checks about the lobby and round flow.
4. **Expensive tests justify their cost.** Every permanent tier 2–4 check names, in its header
   comment, the contract or regression it guards and why a cheaper tier can't prove it. A check
   written to validate a feature during development is temporary by default. It stays only if the
   area breaks easily or it caught a real regression. A permanent check covering stable code that
   hasn't failed in a long time should move to a cheaper tier or be deleted.
5. **Assertions decide; screenshots inform.** Take screenshots only in tier 4. Agents read them only
   when the diff is visual.
6. **Keep output short.** A pass is one line. A failure says what failed, where, and which log to read.
   Long reports go to files, not into the agent's context.
7. **Fix the cause, never the gate.** Don't allowlist an error, loosen an assertion or add a
   known-issue entry to make a run green without saying so.

## Agent loop

After a change, with the editor open:

1. `Testing/ui/validate.sh --steps compile,editmode`
2. Add the focused steps or scenarios for the systems the change touched (the step list is in the
   [UI runbook](../../Testing/ui/README.md#validation-loop-agents-start-here)). Add `visual` only for
   visual changes, `lifecycle` only for lobby/round/session changes.
3. Run a multiplayer smoke scenario only when the change touches networking, spawning or session
   contracts: the one scenario that covers it, not the whole set.

Don't run `--nightly` or `--weekly` unless the owner asks for it. The nightly set takes about 25 minutes warm.

## Harness contracts

These came out of the [test harness rework](../archive/architecture/2026-11_test-harness-rework.md)
and stay in force. Code, CI and `AGENTS.md` cite them by number.

| # | Contract |
|---|----------|
| D1 | Tier 2 scenarios are NUnit PlayMode tests: one boot per fixture into a test room, one `[UnityTest]` per scenario, selected by NUnit category (`validate.sh --steps scenarios:<category>`, batchmode `-runTests` nightly). |
| D2 | The world is the checked-in `scenario-testroom` fixture. The host spawns straight into a body: no lobby, no ready. |
| D3 | Isolation: a fresh body per scenario and tracked cleanup of everything it spawned; room reset only for scenarios that change the room. A scenario that only passes after another one has a cleanup bug. |
| D4 | Scenarios drive their steps through the console helper so they stay portable to the DSL. Any logged error fails a scenario; intended ones are declared with `LogAssert.Expect`. |
| D5 | The multiplayer smoke set is curated in one file, `Testing/multiplayer/smoke-set.txt`, and runs against the real dedicated-server build. |
| D6 | Pilots run on demand; the lobby UI, lifecycle core and remote client pilots also run in the weekly graphical set. |
| D7 | Wall-clock budgets on TomNAS (N100): nightly PlayMode 15 min, smoke 20 min, weekly graphical 20 min. Each job prints its time against its budget (`Tools/ci/budget.sh`). Over budget means cull before that tier gets anything new. |
| D8 | Each tier 2–4 check has a header: `Guards: <contract or issue #> · Not cheaper because: <reason>` (rule 4). |
| D9 | A red scheduled nightly or weekly opens or updates one issue labelled `nightly-failure` / `weekly-failure` (`Tools/ci/nightly_issue.sh`), closed by the next green run. Agents check it at session start. |
| D10 | Each system map's **Tests** section names the validate step and scenario category for its domain. |
| D11 | Multiplayer Play Mode (`validate.sh --steps mppm:<scenario>`) is for agent-local multiplayer checks only; the built smoke stays the nightly gate because it exercises `UNITY_SERVER` code paths. |

## Current state

- **EditMode** fits tier 1 and is the most effective suite.
- **PlayMode** (`Assets/Scripts/Tests/PlayMode`) holds the tier 2 scenarios (one boot into the
  `scenario-testroom` fixture) plus a few `Nightly` round start/stop races with no pure seam in the
  round state machine.
- **Pilots** (`LobbyUiPilot`, the `LobbyLifecyclePilot` core, and the sandbox, overlay and remote
  client pilots) mix tier 2 and tier 4 checks. The lobby UI, lifecycle core and remote client pilots
  run in the weekly graphical set; the sandbox pilot is on demand and its reset loop runs as the
  nightly `sandbox-reset` smoke scenario. They are under review against rules 2–4.
- **Nightly CI** on TomNAS runs every PlayMode test in a batchmode editor, then the curated smoke
  set; a weekly run drives the graphical pilots through `validate.sh --weekly`.
