# Round End & Transition — design document

> Status: draft

Closes the gap flagged in `observer.md` §7 and §10: that doc supplies the camera and
spectator-state framework a finished round drops players into, but explicitly leaves the summary
screen and the transition back to a new round to "the next doc." This is that doc. It also spends
the secrecy `round-config.md` §4 spent the whole round protecting — the resolved gamemode identity
that stayed server-side is finally disclosed here, at the one moment revealing it costs nothing.

## Design philosophy

**A round needs a legible ending, and the ending should read off real played facts.** The same
thread every other doc in this project pulls on — failure and outcome trace to something concrete
and visible, not an invisible roll — applies to the round as a whole. A round doesn't end in an
abstract team score; it ends in a ledger of things that actually happened: which objectives were
met, who died and how, whether the antagonist got away. The summary screen reports that ledger; it
does not compute a hidden rating.

**Reuse, don't reinvent, three times over.** Like `death-cloning-respawn.md` §1, most of this doc
is other systems already doing their jobs:

- **The spectator framework is observer's.** A player at round end is in exactly the detached
  camera/state observer already defines (`observer.md` §7) — round end doesn't build a second
  spectator mode, it drops everyone into the existing one and paints a summary over it.
- **The gamemode identity is round-config's.** The secret that stayed server-side all round
  (`round-config.md` §4) is the exact thing the summary reveals. Round end is where that secrecy is
  deliberately spent, not leaked — the reveal is a designed moment, the payoff for keeping it hidden.
- **The objective outcomes are the gamemode's.** Whether an antagonist's objectives were met is a
  fact the gamemode already tracks during play; round end reads and displays it, it does not
  re-adjudicate anything.

**The transition is a return to a known state, not a new subsystem.** Ending a round hands control
back to the pre-round flow `lobby.md` §7 and `round-config.md` §3 already define — draw the next
config, reset readiness, start again. Round end owns the seam, not a parallel lifecycle.

## 1. What ends a round

A round ends when one canonical thing becomes true, mirroring the single-trigger discipline
`health.md` §4 uses for death and `death-cloning-respawn.md` §2 uses for permadeath:

- **The active gamemode's end condition is met** — its win/lose/objective-resolution rule fires.
  This is the primary, designed ending; the specific condition belongs to each gamemode, not here.
- **A hard round timer elapses**, where a gamemode defines one — a bounded fallback so a stalled
  round still concludes.
- **An admin ends it** — an out-of-band manual end, always available.

Whichever fires, the outcome is fixed at that instant from the facts already on record (objectives,
deaths, antagonist status). Nothing is rolled after the trigger.

## 2. The reveal

The resolved gamemode identity and the antagonist roster — kept server-side and non-spoiling all
round per `round-config.md` §4 — are disclosed at round end. This is the deliberate spend of the
round's secrecy: the coarse, non-spoiling category players saw in the lobby (`lobby.md` §4) resolves
into the full truth of what the round actually was, who the antagonists were, and what they were
trying to do. Revealing it earlier would break the secrecy the whole config system exists to
protect; revealing it now is the reward for it having held.

## 3. The summary screen

Painted over the observer spectator state (`observer.md` §7), the summary reports the round's ledger:

- **Gamemode, revealed** — the true gamemode identity from §2.
- **Objective outcomes** — per antagonist (or team), each objective shown met or failed, as a real
  read of tracked state, not a computed grade. Same "real values, visible failure" discipline the
  fabricator and hacking screens follow.
- **Notable fates** — who died and the cause, drawn from the same wound/organ state a corpse already
  carries (`death-cloning-respawn.md` §2); who survived; who was cloned back (`death-cloning-respawn.md`
  §5). The player's own fate is legible without spoilers about others beyond what the reveal grants.

The screen is diegetic-adjacent but sits in the same deferred-UI carve-out the observer experience
does (`main-hud.md` §13) — it is a summary surface, not a redesign of the HUD.

## 4. The transition

From the ended state, control returns to the pre-round flow:

- The next round's gamemode and map are drawn per `round-config.md` §3 (draw, gate, fallback), with
  the same secrecy re-established for the new round (`round-config.md` §4).
- Player readiness and job preferences reset into the lobby's resolution pass (`lobby.md` §5, §7).
- Players leave the summary/observer state and re-enter the lobby for the next round.

The transition is a seam between two states both already designed elsewhere; this doc only defines
that the seam exists and in what order it runs.

## Worked examples

**Antagonist wins on objectives.** The gamemode's end condition fires when its antagonist completes
their last objective. Outcome fixes at that instant. Everyone drops into observer spectator state;
the summary reveals the gamemode and the antagonist, shows all objectives met, lists who died. After
a beat, the transition draws the next round's config and returns players to the lobby.

**Timer elapses with objectives unmet.** No side met its condition before the round timer ran out.
The outcome fixes on the tracked state: objectives shown failed, survivors listed, the gamemode and
antagonists still revealed (secrecy is spent regardless of who "won"). Transition proceeds identically.

**Admin ends a broken round.** An admin ends the round manually. The same reveal and summary run off
whatever state exists at that moment — a real read, even of an incomplete round — and the transition
returns to the lobby.

## Integration notes

| Round end leans on | Which does the work |
|---|---|
| Spectator camera/state at round end | `observer.md` §7 (the framework this builds its screen over) |
| Secret gamemode identity, now revealed | `round-config.md` §4 (secrecy) + §3 (next-round draw) |
| Objective tracking | The active gamemode's own objective state (read, not re-adjudicated) |
| Death/clone fates in the summary | `death-cloning-respawn.md` §2, §5 |
| Return to pre-round flow | `lobby.md` §5, §7 |

## Out of scope for this pass

- **Per-gamemode end conditions.** This doc defines that a gamemode ends a round and gets revealed;
  each gamemode's specific win/lose rule and objective set belong to that gamemode, not here.
- **Scoring, MMR, or player-progression rewards.** The summary is a ledger of what happened, not a
  rating; any persistent scoring is a separate, later concern.
- **Full observer/ghost experience.** Unchanged from `observer.md` §10 and `main-hud.md` §13 — this
  doc supplies only the summary and transition, not the spectator UI itself.
- **Antagonist content and objective design.** What antagonists exist and what they pursue is its own
  pass; round end only reports outcomes.
