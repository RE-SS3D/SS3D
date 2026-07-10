# Lobby & Role Selection — design document

> Status: active

The first server-side system in this pass, and a genuinely different kind of design problem from everything before it. Every prior doc in this project tested itself against "does the 3D world carry this better than a panel" — but there's no world yet here. No character, no station, nothing to be diegetic about. This doc gets the same honest carve-out already used for OOC (`comms.md` §7) and the respawn timer (`death-cloning-respawn.md` §6): it's explicitly a session/meta screen, not an in-fiction one.

## 1. Design philosophy

**The test here isn't diegetic-vs-panel, it's native-vs-leftover.** SS13's job-select screen is literally rendered through BYOND's embedded HTML browser output — real tables, real hover tooltips, an actual webpage sitting inside the game window. That's the artifact this doc cuts. It's not a rendering-fidelity problem the way the targeting doll was; it's that the engine's only menu tool happened to be a browser, so every menu ended up looking like one. The fix is simple: same visual system as the rest of this project (flat surfaces, hairline borders, muted status colors, no neon) applied to a full-screen menu, so it reads as part of the same game instead of a tool embedded inside it.

**The mechanical decision: preferences, not a race.** SS13 has shipped both first-come-first-served job locking and ranked-preference resolution across different codebases. This doc adopts ranked preferences deliberately, for the same reason chording replaced intent-cycling in the main HUD doc (§7) — a live scramble rewards reflexes and being logged in at exactly the right second, not an informed decision. Here, every player builds a preference list at their own pace, and the whole list resolves in one fair pass when the round actually starts. Nobody loses a role because someone else's click landed a frame earlier.

## 2. Job list

Grouped by department (Command, Security, Engineering, Medical, Science, Cargo, Service, Civilian) — pure information architecture, not engine-dependent, so it's kept as-is. Each entry shows the job name, department color (muted, matching the existing status-color palette), and a slot count (e.g. "Engineer — 2/4", "Captain — 0/1").

**Locked jobs stay visible, not hidden.** A playtime-gated job (e.g. "Chief Engineer — requires 10 hours as Engineer") shows grayed out with the real requirement stated, rather than disappearing from the list entirely — the same "real, visible reason" rule armor's absorption values and the hacking interface's failure states already follow. A new player should be able to see the whole game's shape, including the parts they can't access yet, not discover job tiers exist by their absence.

**Short description on hover/expand** for any job whose name alone doesn't explain the role. This is the same trap this project has flagged twice already — comms' typed radio prefixes (§2) and crafting's freeform recipes (§1) — applied to job names: nothing here should be effectively wiki-only knowledge for a new player.

A minimal server-info strip (message of the day, player count) sits above the job list — plain text, not a designed subsystem, just acknowledged so the screen doesn't look incomplete without it.

## 3. Ranked preferences

**A single drag-ordered list**, not discrete High/Medium/Low buckets. Buckets create ties within a tier that need their own resolution rule; an ordered list is unambiguous by construction — first entry is the first thing the resolution pass tries. Players build the list by adding jobs from the department browser in §2; reordering is a standard drag gesture, nothing borrowed from this project's existing interaction grammar needs inventing here since this isn't a world-space or item interaction.

**Live demand, not hidden math.** Each job in the list (and in the browser) shows how many players currently have it somewhere in their top preferences relative to its slot count — e.g. a small "12 interested / 4 slots" indicator. This is real, visible information a player can act on (deprioritize an oversubscribed role, or accept the odds) rather than a black box they're guessing against. Same "give real info instead of a hidden mechanism" instinct that's shown up everywhere else in this project, just applied to social/demand data instead of physical state.

**"No preference" is a real, explicit option** — a player can leave the list short or empty and be assigned wherever needed, rather than being forced to rank jobs they don't care about just to have a valid submission.

Preferences are editable at any point up until resolution (§5) — there's no lock-in moment before round start, so changing your mind costs nothing.

## 4. Special/antagonist role opt-in

A separate section, visually distinct from the job list — this is an eligibility toggle, not a job, and conflating the two would misrepresent what it does. Presented as a short list of the general categories a player is willing to be considered for, with a real (but deliberately non-spoiling) description of what opting in means — the same restraint real SS13 servers already apply here: enough context that a new player isn't clicking blind, not so much that it spoils round content for everyone else.

## 5. Resolution — one pass, not a scramble

At round start — timer expiry, or an admin/host override (touches in-round admin tools, a separate pass) — every player's preference list resolves server-side in a single allocation pass: try each player's highest-ranked still-open job, respecting slot caps, moving down their list as needed.

**A guaranteed fallback role** (Assistant/Civilian-equivalent) exists specifically so nobody comes out of resolution unable to join at all — the one hard guarantee the algorithm has to honor.

The exact fairness/weighting rule inside that pass — random tiebreak among equally-ranked requests, seniority weighting, anything more sophisticated — is a balancing and backend decision, not designed here. What this doc commits to is the *shape*: ranked preferences in, one fair resolution pass, guaranteed fallback out.

## 6. Identity

Name entry only — the minimal field actually required to have a functioning lobby. Full appearance and loadout customization is real scope, but it's its own design pass, the same way the inventory/equip screen and observer UI were deferred out of earlier docs rather than folded in.

## 7. Round start & latejoin

**Countdown timer** is the primary trigger. An optional "ready" toggle that shortens the timer once every connected player has readied is a reasonable variant, but a minor one — not load-bearing for this design.

**Latejoin reuses the identical job-list interface**, just filtered live to whatever's currently open. This is now genuinely first-come-first-served, and that's fine — a latejoiner is joining a world already in motion, not competing in an artificial scramble at a shared starting line, so the race concern §1 raised doesn't apply here.

**Observer/spectate is always available** as an entry point — a player can choose not to take a body at all. This connects directly to the deferred ghost/observer UI (`main-hud.md` §13) without redesigning it here.

## 8. HUD/visual notes

This screen doesn't inherit main HUD's minimal-permanent-chrome rule, and that's a deliberate distinction, not an oversight — that rule exists specifically to keep panels from competing with a 3D world that's actually there to look at. Here there's no world yet, so a dense, everything-visible-at-once screen (job list, preference builder, antag opt-in, timer) is the right call rather than something to collapse or hide. What carries over is the visual *language* — flat surfaces, hairline borders, muted status colors — not the restraint.

## 9. Integration notes

| Element | Touches existing / needed system |
|---|---|
| Job unlock/playtime gating | Persistence & accounts (cross-cutting infra — not designed here) |
| Gamemode-dependent job/antag availability | Round config & gamemode selection (separate pass, assumed to have already run) |
| Resolution trigger override | In-round admin tools (separate pass) |
| Observer/spectate entry | Deferred ghost/observer UI (`main-hud.md` §13) |
| Character identity | Full appearance/loadout customization (separate pass, not designed here) |

## 10. Worked examples

**A — Standard round start:**

| Step | What happens | Lobby state |
|---|---|---|
| 1 | Player browses departments, builds a ranked list: Engineer, then Atmospheric Technician, then Cargo Technician | List shows live demand — Engineer reads "9 interested / 4 slots" |
| 2 | Player reconsiders given the demand, reorders Atmospheric Technician to the top | No cost to changing their mind — nothing is locked yet |
| 3 | Player opts into consideration for one antagonist category | Antag section shows the opt-in as active, separate from the job list |
| 4 | Timer reaches zero | Resolution runs once, server-side, across every submitted list |
| 5 | Engineer was oversubscribed; Atmospheric Technician wasn't | Player is assigned their (now) first choice |

**B — Latejoin:**

| Step | What happens | Lobby state |
|---|---|---|
| 1 | Round has been running for 20 minutes; a new player connects | Same job-list interface opens, filtered to currently-open slots only |
| 2 | Only two jobs show open | Player picks one directly — no ranking, no waiting for a resolution pass |
| 3 | Selection resolves immediately | Player spawns in right away, consistent with joining a world already in motion |

## 11. Out of scope for this pass

- Round configuration & gamemode selection itself (separate pass — this doc assumes a gamemode has already been chosen upstream)
- In-round admin tools, including manual override of round start or resolution (separate pass)
- Full character appearance/loadout customization (separate pass)
- The exact fairness/weighting algorithm inside preference resolution (a balancing/backend decision, not a design one)
- The persistence/accounts system underlying playtime-gated jobs (cross-cutting infra, assumed to exist)
- The observer/ghost UI itself (already deferred in `main-hud.md` §13 — this doc only adds the entry point)

## 12. Prototyping this

**Claude Design, prompt 1 — job-select screen:**
> Build the lobby job-select screen using our SS3D design system: departments grouped with muted color coding, slot counts per job, one locked job showing its real playtime requirement instead of being hidden, and a hover/expand description for an unfamiliar job name. Full-screen, information-dense — this screen doesn't follow the minimal-chrome rule the in-round HUD does.

**Claude Design, prompt 2 — preference builder:**
> Build the ranked-preference panel: a drag-to-reorder list built by adding jobs from the department browser, a live demand indicator per job ("N interested / M slots"), an explicit "no preference" option, and a visually separate antagonist-opt-in section below it. Include the round-start countdown timer.

**Cursor, prompt 1 — data contract first:**
> Here's the lobby design doc. Define the job record (department, slot cap, unlock requirement), the preference record (player, ordered job list, antag opt-in flags), and the resolution function's shape (input: all submitted preferences + slot caps; output: final roster + guaranteed-fallback assignments for anyone unfulfilled). Show me all three before wiring any UI.

**Cursor, prompt 2 — one vertical slice:**
> Implement the job list, preference submission, and a placeholder resolution pass (simple sequential best-available allocation is fine) end to end for a small fixed job set. Live demand indicators and the real fairness algorithm come after this is reviewed.
