# Round Configuration & Gamemode Selection — design document

> Status: active

Resolves the dependency `lobby.md` assumed upstream (§9, §11): lobby's antagonist opt-in section (§4) was deliberately scoped to show only coarse, non-spoiling categories, on the assumption that something else was responsible for actually deciding — and protecting — the round's real gamemode identity. This is that system. Same session/meta carve-out as lobby (§1): no world yet, so the test is native-vs-leftover, not diegetic-vs-panel.

## 1. Design philosophy

**The native-vs-leftover test applies again, to a different screen.** Lobby's job-select redesign targeted the player-facing BYOND browser panel. This doc targets its sibling: the admin-facing config screen where gamemodes and maps get enabled, weighted, and tuned. Same artifact, same fix — same visual system as the rest of this project applied to a real settings surface, not a checkbox-laden webpage.

**One shared mechanism, two pools.** Gamemode selection and map selection are structurally the same problem: a set of enabled entries, each with a relative weight and a precondition (chiefly a player-count range) that has to hold for the entry to be eligible, drawn from with a deterministic fallback if nothing qualifies. Rather than building two systems, this doc defines one pool shape and uses it twice.

**Secrecy is a feature this doc preserves, not an artifact it fixes.** Unlike the targeting doll or the job-select browser panel, "you don't know the round's real gamemode" isn't a BYOND limitation — it's a deliberate design choice that holds up independent of engine, and this project isn't in the business of removing things just because they're old. What this doc does change is making sure the secrecy is actually load-bearing correctly: the resolved gamemode identity stays server-side, and only a coarse, non-spoiling category set crosses over into lobby — confirming that `lobby.md` §4 scoped its opt-in section at exactly the right granularity.

## 2. The pool model

A single record shape, used for both pools:

- **id** — stable, unique
- **display name** — admin-facing; not necessarily shown to players at all (§4)
- **enabled** — admin toggle
- **weight** — relative likelihood among currently-eligible entries
- **precondition(s)** — primarily a min/max connected-player-count range, but structured generically enough that other preconditions (a required map, a cooldown since last selection) could plug in later without reshaping the record

**Gamemode pool** and **map pool** are two independent instances of this same shape — not a gamemode-specific system with a map-specific system bolted on beside it.

## 3. Selection — draw, gate, fallback

At the point round config resolves (as the lobby countdown is about to expire, not fixed earlier), the server draws once from each pool independently, weighted across whichever enabled entries currently satisfy their preconditions given the live connected-player count. "Live" matters: if players joined or left during the lobby period, the check re-evaluates against the population at draw time, not whatever it was when the timer started.

**Deterministic fallback:** if no enabled entry's precondition currently holds — a small population and every enabled mode wants more players than are online, say — the server falls back to a designated safe default (an Extended/no-antag equivalent). This is logged as an explicit "fell back, no eligible entry" event for admins, not a silent substitution nobody notices.

## 4. Secrecy & the lobby handoff

The drawn gamemode's specific identity stays server-side. It isn't shown to players in the lobby, and it isn't surfaced in this doc's own admin config screen to anyone without the access to see it either — the secrecy is real, not just a client-side hide.

What crosses over to lobby is narrower: a resolved set of active antagonist *categories*, at the same non-spoiling granularity `lobby.md` §4 already committed to. This doc is the system that actually populates that list — lobby doesn't invent it, it reads it. Any gamemode-specific change to the ordinary job list (rare; most jobs are mode-independent) crosses over the same way, as a resolved delta lobby's job list reads from rather than something lobby computes itself.

## 5. Admin-facing config screen

The pool (gamemodes, then maps) shown as a list: enable toggle, weight field, precondition editor (min/max player count) — same visual language as the rest of this project, explicitly not the classic browser-rendered checkbox table. Settings persist across rounds — this ties into the persistence/accounts cross-cutting infra already flagged as separate, not-designed-here in `lobby.md` §11.

**A read-only history log** — the last N rounds' drawn mode, drawn map, and whether fallback triggered — sits alongside the pool editor. Real, inspectable data an admin tuning weights over time can actually look at, rather than adjusting numbers against a black box. Same "give real information instead of a hidden mechanism" instinct that's run through this entire project, just applied to server tuning instead of player-facing systems.

## 6. What this doesn't decide

- **Which specific crew member becomes which antag** within a chosen gamemode. That's the gamemode's own runtime logic, and it varies enormously by mode — the same way this project treats the material silo or the R&D tech tree as existing systems it hooks into rather than redesigns, this doc treats "how a gamemode picks its antags from the eligible, opted-in crew" as that mode's business, not round config's.
- **Dynamic/threat-budget-style modes** — a meta-mode that spawns a scaling mix of threats against a budget rather than drawing one fixed gamemode. The pool-and-weight shape here could plausibly host that later as a more sophisticated pool entry, but it isn't designed as one now.
- **Round length and end conditions.** That's `round-end summary & transition`'s territory — a separate, still-open pass.

## 7. Optional: a player-facing preference poll

A light, explicitly advisory poll — map preference is the natural candidate, since gamemode has to stay hidden to preserve §4's secrecy — feeding into the weighting as informational input, not a binding vote. Flagged the same way `lobby.md` §7 flagged its optional "ready" toggle: a reasonable variant a server could enable, not load-bearing for this design.

## 8. Integration notes

| Element | Touches existing / needed system |
|---|---|
| Antag-category handoff | Special/antagonist opt-in section (`lobby.md` §4) — this doc resolves what populates it |
| Job-list deltas (rare) | Job list (`lobby.md` §2) |
| Config persistence | Persistence & accounts, cross-cutting infra (not designed here) |
| Round-start / fallback logging | Server logging, cross-cutting infra (not designed here) |
| Admin override of a draw | In-round admin tools (separate pass) |
| Per-gamemode antag assignment | Gamemode's own runtime logic (not designed here) |

## 9. Worked examples

**A — Normal draw:**

| Step | What happens | Round config state |
|---|---|---|
| 1 | 40 players connected as the lobby timer nears zero | Gamemode pool: 5 enabled entries, all preconditions satisfied at this population |
| 2 | Server draws, weighted across all 5 | "Revolution" is drawn — stored server-side only |
| 3 | Map pool draws independently | "Outpost Station" drawn from 3 eligible maps |
| 4 | Active antag categories for Revolution resolve | Lobby's opt-in section (§4 there) now shows the correct non-spoiling categories |

**B — No eligible mode, fallback:**

| Step | What happens | Round config state |
|---|---|---|
| 1 | Only 6 players connected at draw time | Every enabled mode except the Extended-equivalent requires 10+ |
| 2 | No entry's precondition holds | Fallback triggers |
| 3 | Round starts on the safe default | Admin log records "fell back, no eligible entry" — visible, not silent |

**C — Population shifts mid-timer:**

| Step | What happens | Round config state |
|---|---|---|
| 1 | Lobby timer starts with 12 players online — enough for a mode requiring 10+ | That mode is currently eligible |
| 2 | Several players disconnect before the timer expires | Population drops to 7 by draw time |
| 3 | Draw re-checks preconditions against the live count, not the count at timer start | That mode is no longer eligible for this draw; pool re-weights across what remains |

## 10. Out of scope for this pass

- Round length and end conditions (`round-end summary & transition`, a separate still-open pass)
- The per-gamemode antag-assignment algorithm — who specifically becomes what, within a chosen mode (that mode's own logic)
- Dynamic/threat-budget-style meta-gamemodes (a plausible future extension of the pool shape, not designed as one here)
- Persistence/accounts and server logging themselves (cross-cutting infra, assumed to exist)
- Exact weight values and precondition thresholds (a balancing pass, not a design decision)
- The full admin toolkit — ahelp, player management, audit logging, stealth observation (`in-round admin tools`, a separate pass; this doc only needs a config screen, not the whole toolkit)

## 11. Prototyping this

**Claude Design, prompt 1 — admin config screen:**
> Build the round-config admin screen using our SS3D design system: a gamemode pool list (enable toggle, weight field, min/max player-count precondition) and an identical-shaped map pool list below it, plus a read-only history log showing the last several rounds' drawn mode, drawn map, and whether fallback triggered. Same visual language as the lobby job-select screen — flat surfaces, hairline borders, no browser-panel look.

**Claude Design, prompt 2 — the lobby handoff:**
> Show the round-config draw resolving into the lobby's antagonist opt-in section from the lobby mockup — the resolved, non-spoiling category list populating that section, with the actual drawn gamemode identity visibly absent from every player-facing surface, including this mockup's own "player view" frame.

**Cursor, prompt 1 — data contract first:**
> Here's the round config design doc. Define the shared pool-entry record (used for both gamemode and map pools), the draw function (weighted, precondition-gated against live population, deterministic fallback), and the payload handed to the lobby system (active antag categories, any job-list deltas) — without leaking the drawn gamemode's identity into that payload. Show me all three before wiring any UI.

**Cursor, prompt 2 — one vertical slice:**
> Implement the gamemode pool and draw/fallback logic only, wired to a placeholder lobby hook that just logs the handed-off category payload. Map pool and the admin config screen come after this is reviewed.
