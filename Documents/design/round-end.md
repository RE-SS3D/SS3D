# Round End & Transition — design document

> Status: active

Defines how a round ends, what players are shown when it does, and how the server gets back to
the lobby for the next one. It also spends
the secrecy `round-config.md` §4 spent the whole round protecting — the resolved gamemode identity
that stayed server-side is finally disclosed here, at the one moment revealing it costs nothing.
This pass also closes a second, more specific gap: `shuttles.md` §9 built the evacuation shuttle as
a real physical object but explicitly left its call, countdown, and point-of-no-return to this doc;
`objectives.md` §7's escape objective has been waiting on that same moment to check against. §3 is
that mechanism.

## 1. Design philosophy

**A round needs a legible ending, and the ending should read off real played facts.** The same
thread every other doc in this project pulls on — failure and outcome trace to something concrete
and visible, not an invisible roll — applies to the round as a whole. A round doesn't end in an
abstract team score; it ends in a ledger of things that actually happened: which objectives were
met, who died and how, whether the antagonist got away. The summary screen reports that ledger; it
does not compute a hidden rating.

**Reuse, don't reinvent, three times over.** Like `death-cloning-respawn.md` §1, most of this doc
is other systems already doing their jobs:

- **The spectator framework is observer's.** The dead are already ghosts (`observer.md` §2), and
  round end doesn't build a second spectator mode — it paints a summary over whatever each player
  is looking at, living or ghost.
- **The gamemode identity is round-config's.** The secret that stayed server-side all round
  (`round-config.md` §4) is the exact thing the summary reveals. Round end is where that secrecy is
  deliberately spent, not leaked — the reveal is a designed moment, the payoff for keeping it hidden.
- **The objective outcomes are the gamemode's.** Whether an antagonist's objectives were met is a
  fact the gamemode already tracks during play; round end reads and displays it, it does not
  re-adjudicate anything.

**Evacuation is a real, playable sequence, not an instant proxy for "round over."** The other three
triggers (§2) fire once and resolve instantly. Calling the shuttle is different in kind: it has its
own duration, its own physical risk (`shuttles.md` §6), and its own drama — the same reason a
shuttle got a real transform and real collision instead of an instant teleport in the first place
(`shuttles.md` §1). Round end doesn't shortcut that sequence just because it's the thing that
eventually ends the round (§3).

**The transition is a return to a known state, not a new subsystem.** Ending a round hands control
back to the pre-round flow `lobby.md` §7 and `round-config.md` §3 already define — back to the
lobby, ready up, start again — **without an operator**. A server should be able to run round after
round on its own. Round end owns the seam, not a parallel lifecycle.

## 2. What ends a round

A round ends when one canonical thing becomes true, mirroring the single-trigger discipline
`health.md` §4 uses for death and `death-cloning-respawn.md` §2 uses for permadeath:

- **The active gamemode's end condition is met** — its win/lose/objective-resolution rule fires.
  This is the primary, designed ending; the specific condition belongs to each gamemode, not here
  (Nuclear Operatives: `antagonist-content.md` §6).
- **The round timer elapses** — every round has one, set by the server's round length
  (`round-config.md` §2). A bounded fallback so a stalled round still concludes; the gamemode
  decides what a timeout means for its outcome. A gamemode may set its own length in a later pass.
- **An admin ends it** — an out-of-band manual end, always available, invoked through `admin-tools.md` §6.
- **The evacuation shuttle completes its journey** — arrives at CentCom
  — the common path for any gamemode that resolves through evacuating rather than a bespoke win
  check, **once shuttles exist** (`shuttles.md`). §3 defines the full call/countdown/arrival
  sequence this trigger runs on. Until then, rounds end on the first three triggers.

The first three fire once, from an instantaneous check. The fourth is deliberately the odd one out
— a real sequence with its own duration, not a snapshot. Whichever ultimately fires, the outcome is
fixed at that instant from the facts already on record (objectives, deaths, antagonist status).
Nothing is rolled after the trigger.

## 3. The evacuation shuttle — call, two countdowns, point of no return

Shuttles supplies the physical vehicle (`shuttles.md` §9); this section is what calls it, counts
down to its arrival and its departure, and defines the instant nothing more can be done about who
made it aboard. The flow follows SS13's: the shuttle has to come to the station before it can take
anyone away.

**Calling the shuttle is a physical interaction, not a menu command.** A call console sited at a
fixed station location. Any crew member can use it — no credential required, so a station whose
heads are dead can still leave. **Flagged as a recommendation, not locked.**

**1. Arrival countdown.** A call starts a visible countdown — at the console, as an alert-stack
entry, and as a station announcement (`comms.md` §8). **Recall** is available for its whole length,
by any crew member at the same console, plus an admin at any time (§2). When it ends, the shuttle
arrives and docks at the station's evac dock.

**2. Departure countdown.** Once docked, a shorter countdown runs while the crew board. It can't be
recalled — the shuttle is here.

**3. Departure is the point of no return.** Two things happen at that instant:

- The doors close and the shuttle undocks and leaves along its route (`shuttles.md` §9).
- **Whoever is physically aboard at that instant is locked in as the escape roster, for good.** A
  player not aboard at this instant has missed it, full stop, no grace period. Nobody joins the
  roster later.

**4. Transit and arrival.** The shuttle travels to CentCom. Its arrival is what fires §2's
"evacuation completes" trigger. **Escape succeeds for whoever is on the roster and still alive and
aboard at arrival** — the one rule `objectives.md` §7 resolves against. Anyone who died during
transit simply isn't alive at that check, the same as anywhere else death is checked
(`health.md` §4).

In the first pass the transit is scripted (`shuttles.md` §1): nobody can steer, hijack or crash the
shuttle on the way. A hijacked evac run is a later pass (`shuttles.md` §9).

## 4. The reveal

The resolved gamemode identity and the antagonist roster — kept server-side and non-spoiling all
round per `round-config.md` §4 — are disclosed at round end. This is the deliberate spend of the
round's secrecy: the coarse, non-spoiling category players saw in the lobby (`lobby.md` §4) resolves
into the full truth of what the round actually was, who the antagonists were, and what they were
trying to do. Revealing it earlier would break the secrecy the whole config system exists to
protect; revealing it now is the reward for it having held.

## 5. The summary screen

Painted over every player's screen, living or ghost (`observer.md` §2), the summary reports the round's ledger:

- **Gamemode, revealed** — the true gamemode identity from §4.
- **Objective outcomes** — per antagonist (or team), each objective shown met or failed, as a real
  read of tracked state, not a computed grade. Same "real values, visible failure" discipline the
  fabricator and hacking screens follow. An escape objective (`objectives.md` §7) resolves by §3's
  rule.
- **Notable fates** — who died and the cause, drawn from the same wound/organ state a corpse already
  carries (`death-cloning-respawn.md` §2); who survived; who was cloned back (`death-cloning-respawn.md`
  §5); who missed the evac shuttle, or was lost aboard it (§3). The player's own fate is legible
  without spoilers about others beyond what the reveal grants.

The screen is diegetic-adjacent but sits in the same deferred-UI carve-out the observer experience
does (`main-hud.md` §13) — it is a summary surface, not a redesign of the HUD.

## 6. The transition

From the ended state, control returns to the pre-round flow **automatically**:

- The summary stays up for a short, fixed beat so everyone can read it.
- Every player returns to the lobby. Readiness resets; each saved character keeps its job preferences
  and antagonist opt-ins (`lobby.md` §3, §6).
- The next round uses the server's current settings (`round-config.md` §2), with the same secrecy
  re-established (`round-config.md` §4).
- The pre-round countdown starts again once enough players are connected, and whoever is ready
  when it ends spawns (`lobby.md` §7). No admin action is needed at any step; an admin can still
  hold the countdown.

The transition is a seam between two states both already designed elsewhere; this doc only defines
that the seam exists and in what order it runs.

## 7. Worked examples

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

**Evacuation, first pass:**

| Step | What happens | Round state |
|---|---|---|
| 1 | A crew member uses the call console | Arrival countdown starts, announced station-wide |
| 2 | The countdown ends | The shuttle docks at the evac dock; the departure countdown starts |
| 3 | Most of the crew boards; two stragglers don't make it | Departure — point of no return: doors close, the boarded roster locks |
| 4 | Transit | Scripted; nobody can steer or board |
| 5 | The shuttle arrives at CentCom | "Evacuation completes" (§2) fires; outcome fixes from whoever's on the roster and alive |
| 6 | Round end resolves | Escape objectives (`objectives.md` §7) read true for survivors aboard, false for the two stragglers; both appear in the summary's Notable fates (§5) |

## 8. Integration notes

| Round end leans on | Which does the work |
|---|---|
| Ghosts at round end | `observer.md` §2 |
| Round timer | `round-config.md` §2 |
| Secret gamemode identity, now revealed | `round-config.md` §4 (secrecy) + §3 (next round's mode) |
| Objective tracking | The active gamemode's own objective state (read, not re-adjudicated) |
| Death/clone fates in the summary | `death-cloning-respawn.md` §2, §5 |
| Return to pre-round flow | `lobby.md` §3, §7 |
| Admin manual end | `admin-tools.md` §6 (invocation point; this doc defines the trigger's effect) |
| Evac call console | A station console, any crew may use it |
| Evac shuttle physical behavior | `shuttles.md` §1 (first pass), §9 |
| Escape objective's resolution moment | `objectives.md` §7 (this doc supplies the instant its check reads) |

## 9. Out of scope for this pass

- **Per-gamemode end conditions.** This doc defines that a gamemode ends a round and gets revealed;
  each gamemode's specific win/lose rule and objective set belong to that gamemode, not here.
- **Scoring, MMR, or player-progression rewards.** The summary is a ledger of what happened, not a
  rating; any persistent scoring is a separate, later concern.
- **The ghost experience itself.** `observer.md` — this doc supplies only the summary and
  transition.
- **Antagonist content and objective design.** What antagonists exist is designed in
  `antagonist-content.md`; what they pursue is `objectives.md`'s job. Round end only reports outcomes.
- **Exact countdown duration and the call console's access policy.** §3's no-access-gate call is a
  recommendation, not locked — a balancing/confirmation pass, not a design decision.
- **Multiple simultaneous evac vehicles, or a gamemode-specific evac variant** (a hostile boarding
  party contesting the shuttle, say) — a plausible future extension of §3's shape, not designed here.
