# Observer & Ghost — design document

> Status: active

Formalizes a system four other docs have already been leaning on without ever specifying it: death's consciousness-detach (`death-cloning-respawn.md` §2, with one narrow exception carved at §5), the AI/cyborg mid-round re-entry prompts (`ai-cyborgs.md` §7), and the spectate-only lobby entry point (`lobby.md` §8) — all deferred to "the observer/ghost UI," first flagged out of scope in `main-hud.md` §13. Each of those docs independently built its own narrow, on-demand prompt rather than the real thing. This doc is the real thing: it names the single mechanism underneath all three prompts, and defines the free-roam camera/meta layer itself for the first time.

## 1. Design philosophy

Two things converging, not one.

First: ghosts are **pure meta**, not physical. Everywhere else in this project, the rule has been "make it a real, physically simulated thing" — but a ghost isn't a thing, it's the deliberate absence of one. Treating it as its own category, invisible and undetectable to the living, keeps the living simulation's information economy honest — nothing a dead player learns can leak back into a round still being played by people who are still at risk. That's not a diegetic-feedback question, it's a fairness one, and it gets a different answer than the rest of the project for that reason.

Second: **one shared mechanism, not three.** This project has already built a clone-ready prompt, a become-AI prompt, and a become-cyborg prompt — three separate features that are actually the same event wearing different clothes: a ghost's consciousness attaches to an available body. None of the docs that built those prompts named the thing underneath them. This one does, and gives it exactly one more application worth having: possessing an anonymous ghost-role body (a drone, say) discovered by flying near it, using the same event with a different gate.

## 2. Entering ghost state

- **Death.** Per `death-cloning-respawn.md` §2, consciousness detaches at the moment actual death is reached (not merely critical) — automatic, no choice involved.
- **Spectate-only from lobby.** Per `lobby.md` §8, a player can choose not to take a body at all. They enter ghost state directly, with no reserved possession path waiting for them — no corpse, no clone in progress, no AI/cyborg opt-in — since none of those exist yet for an identity that never had a body.

## 3. The ghost's screen

No body means no diegetic surface to hang feedback on — the same problem the AI's screen already solved for itself (`ai-cyborgs.md` §8), just resolved differently here. The AI still has fixed camera feeds tied to a real network; a ghost has none of that constraint. So this is a genuine third category, distinct from both main HUD's minimal-chrome doctrine (no living body's world-state to protect) and lobby's dense-panel doctrine (there *is* a world here, fully rendered) — closer to an unconstrained director's camera than either.

- **Free-flying camera.** No collision — moves through walls, floors, hull, open space. Adjustable speed.
- **Fully invisible and undetectable to the living**, and to every living-side instrument: no footstep audio, no motion-tracker ping, no camera-feed visibility, nothing. This is the direct consequence of §1's fairness principle, not an oversight to patch later.
- **Follow-lock.** Camera can snap to track a specific living player or point of interest, releasing on manual input — spectating a specific unfolding scene shouldn't require constant manual flying.
- **No physical footprint.** Cannot open doors, trigger sensors, be struck, or otherwise causally touch the living simulation, full stop — with exactly one exception, the Possession event in §5.
- **Permanent chrome:** a slim dead-chat panel (§4) and a nearby-ghosts indicator. That's the entire fixed UI. Everything else is the free camera itself.

## 4. Dead chat

- A meta channel visible only to other ghosts, reusing comms' existing message/channel backend the same way `pda.md` §3 reused it for private messages, rather than standing up a second chat system.
- **Not proximity-gated.** Unlike local speech, this is explicitly non-positional — it's meta, not diegetic, consistent with ghosts being invisible in the first place.
- **No path from dead chat into the living simulation, ever.** No exceptions. Any leak here undermines the entire premise of choosing invisible, undetectable ghosts in §1.

## 5. Possession — the canonical mechanism

One event: a ghost's consciousness attaches to an available body. Ghost state ends; the player now controls that body with whatever HUD/systems it normally has. Two ways to trigger it:

**a) Reserved, on-demand prompt (identity-linked).** A body specifically tied to *this* ghost's own identity becomes available, and a dismissible prompt fires regardless of where the ghost's camera currently is — not a forced camera-yank, since a ghost mid-spectate shouldn't be interrupted against their will. This doc doesn't touch any of the three existing triggers or their gating logic, only their shared shape:
 - Clone-ready — `death-cloning-respawn.md` §5
 - Become-AI — `ai-cyborgs.md` §7
 - Become-cyborg — `ai-cyborgs.md` §7

**b) Proximity discovery (open, anonymous).** Certain unoccupied bodies in the world — ghost-role bodies — are flagged possessable by any ghost who flies close and interacts, discovered the same way any world object is discovered: by looking at it, not by a spawn-in menu. On interaction, a plain confirm prompt: "Possess this body?"

 A ghost-role body must be a real, already-existing, unoccupied object placed in the world by some *other* system — a drone fabricated per `crafting.md` §3's job-state pattern, say — never conjured by this system out of nothing. It keeps whatever stats and behavior its originating system already gave it (a fabricated drone's real integrity and power draw) rather than getting a parallel ghost-role stat block invented here.

**Leaving a possessed body** works exactly like dying does — detaches back to ghost state per §2, no special penalty beyond having lost that body.

## 6. Reconnection — defibrillation before commitment

Per `death-cloning-respawn.md` §3, defib can revive a body within its window. If the ghost hasn't triggered *any* possession event yet — still free-flying, hasn't accepted a clone, become-AI/cyborg, or a ghost-role body — a successful defib on their own original body pulls them straight back in automatically, no prompt needed, since there's no ambiguity about whose body it still is.

If the ghost has already committed elsewhere before defib succeeds, the original body still revives per death/cloning's own rules — it just comes back empty, a live but unoccupied body in the world. This doc flags that edge case rather than leaving it silently implicit, but the actual handling of an unoccupied-but-alive body belongs to `death-cloning-respawn.md` to formalize, the same way `ai-cyborgs.md` §7 flagged its own amendment back to that doc rather than letting two documents quietly disagree.

## 7. Integration notes

| Observer element | Touches existing / resolves |
|---|---|
| Death → ghost detach | `death-cloning-respawn.md` §2 |
| Clone-ready prompt | Reserved possession path, formalizes `death-cloning-respawn.md` §5 |
| Become-AI / become-cyborg prompt | Reserved possession path, formalizes `ai-cyborgs.md` §7 |
| Spectate-only entry | `lobby.md` §8 |
| Dead chat backend | Message/channel backend, `comms.md` §9 — same reuse pattern as `pda.md` §3 |
| Ghost-role body stats/behavior | Whatever system spawned it (e.g. `crafting.md` §3 fabricator job-state) |
| Defib-before-commitment reconnection | `death-cloning-respawn.md` §3 |
| Round-end spectator state | Foundation for `round-end.md` (next doc), which builds its summary screen on top of this |

## 8. Worked examples

**Standard death → clone:**

| Step | What happens | Ghost state |
|---|---|---|
| 1 | Player dies in a firefight | Consciousness detaches; free camera and dead chat begin, invisible to the crew still fighting nearby |
| 2 | Player flies off to watch how the fight resolves | Talks to two other ghosts in dead chat, unseen by anyone living |
| 3 | Medbay finishes printing a clone | Reserved prompt fires — "Clone ready — enter?" — regardless of where the camera currently is |
| 4 | Player accepts | Possession fires; ghost state ends, player is back in a body with a normal HUD |

**Never took a body, drone possession:**

| Step | What happens | Ghost state |
|---|---|---|
| 1 | Player chose observer at lobby | Enters ghost state directly — no corpse, no reserved path exists |
| 2 | Flying through maintenance, spots an idle drone an engineer fabricated earlier | Approaches and interacts |
| 3 | Confirms "Possess this body?" | Possession fires; player now controls the drone with its real existing stats — limited tool loadout, no vitals, real battery drain |
| 4 | Drone runs out of charge and shuts down in a maintenance tunnel | Detaches back to ghost state, no penalty beyond losing that body |

**Defib before commitment:**

| Step | What happens | Ghost state |
|---|---|---|
| 1 | Player dies from blood loss | Ghost state begins |
| 2 | Player stays close, watching, hasn't accepted any prompt | Still uncommitted |
| 3 | A medic reaches the body in time and defibs successfully within the window | No possession event has fired yet |
| 4 | Reconnection is automatic | Player is pulled straight back into their own revived body; ghost state ends without ever presenting a prompt |

## 9. Out of scope for this pass

- Exact roster of ghost-role body types (drones, mice, pAIs, hostile simple mobs, etc.) and their individual stat blocks — a content/balancing pass, not a mechanic
- Any future method for the living to detect or perceive ghosts (a medium-type ability, a specific antagonist power) — this pass commits to fully invisible and undetectable; a later addition would need its own justification against §1
- Admin/staff observer tooling (godmode, jump-to-player, stealth observation) — `in-round admin tools`, a separate still-open pass
- Full handling of a defib-revived-but-unoccupied body (AI takeover, NPC behavior, etc.) — flagged in §6, belongs to `death-cloning-respawn.md` to formalize
- Ghost cosmetic customization (appearance, trail effects, etc.) — no gameplay weight
- The round-end summary screen and transition logic itself — this doc only supplies the camera/state framework round-end will use; the content and transition mechanics are `round-end.md`'s job

## 10. Prototyping this

**Claude Design, prompt 1 — free-cam and chrome:**
> Build the ghost's screen: a free-flying camera view of a station interior (no collision, moves through walls), with a slim dead-chat panel and a nearby-ghosts indicator as the only permanent chrome. No vitals, no elements meant for a living body — this is a distinct third category from the main HUD and the AI's screen, closer to a director's free-cam than either.

**Claude Design, prompt 2 — possession prompts:**
> Show two possession moments side by side: a dismissible on-demand prompt ("Clone ready — enter?") that can appear regardless of camera position without yanking the view, and a proximity possession prompt that only appears when the free-cam is close to and looking at an idle drone in the world ("Possess this body?").

**Cursor, prompt 1 — data contract first:**
> Here's the observer design doc. Define the ghost state itself (camera-only, no world-collision, invisible flag against every living-side detection system) and the single Possession event with its two trigger paths (reserved on-demand prompt keyed to identity; proximity interaction against a body flagged possessable). Show me both before wiring any specific reserved-prompt source.

**Cursor, prompt 2 — one vertical slice:**
> Wire ghost-state entry from death, free-cam movement, and the clone-ready reserved prompt end to end. Become-AI/cyborg prompts and ghost-role possession come after this is reviewed.
