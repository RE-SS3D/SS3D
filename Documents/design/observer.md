# Observer & Ghost — design document

> Status: active

Formalizes a system four other docs have already been leaning on without ever specifying it: death's consciousness-detach (`death-cloning-respawn.md` §2, with one narrow exception carved at §5), the AI/cyborg mid-round re-entry prompts (`ai-cyborgs.md` §7), and the observe-only lobby entry point (`lobby.md` §7) — all deferred to "the observer/ghost UI," first flagged out of scope in `main-hud.md` §13. Each of those docs independently built its own narrow, on-demand prompt rather than the real thing. This doc is the real thing: it names the single mechanism underneath all three prompts, and defines the free-roam camera/meta layer itself for the first time.

## 1. Design philosophy

Two things converging, not one.

First: ghosts are **pure meta**, not physical. Everywhere else in this project, the rule has been "make it a real, physically simulated thing" — but a ghost isn't a thing, it's the deliberate absence of one. Treating it as its own category, invisible and undetectable to the living, keeps the living simulation's information economy honest — nothing a dead player learns can leak back into a round still being played by people who are still at risk. That's not a diegetic-feedback question, it's a fairness one, and it gets a different answer than the rest of the project for that reason.

Second: **one shared mechanism, not three.** This project has already built a clone-ready prompt, a become-AI prompt, and a become-cyborg prompt — three separate features that are actually the same event wearing different clothes: a ghost's consciousness attaches to an available body. None of the docs that built those prompts named the thing underneath them. This one does, and gives it exactly one more application worth having: possessing an anonymous ghost-role body (a drone, say) discovered by flying near it, using the same event with a different gate.

## 2. Entering ghost state

**An observer is a ghost.** There's one spectator state in this game, not two: the ghost a player becomes when they die is the same thing a player chooses from the lobby when they'd rather watch.

- **Death.** Per `death-cloning-respawn.md` §2 and `health.md` §4, consciousness detaches at the moment actual death is reached (not merely critical) — automatic, no choice involved. The ghost appears where the body fell.
- **Observe from the lobby.** Per `lobby.md` §7, a player can choose not to take a body at all. The Observe button puts them **straight into a ghost**, with no reserved possession path waiting for them — no corpse, no clone in progress, no AI/cyborg opt-in — since none of those exist for an identity that never had a body.

### First pass

The ghost in this pass is the minimal version of §3:

- a **free-flying camera** that passes through everything;
- it **sees everything** the living world shows;
- it **can't interact** with anything;
- it **can't speak to the living** — out-of-character chat (`comms.md` §7) is the only channel it has.

Follow-lock, the dead-chat channel (§4) and possession (§5) are later passes.

## 3. The ghost's screen

No body means no diegetic surface to hang feedback on — the same problem the AI's screen already solved for itself (`ai-cyborgs.md` §8), just resolved differently here. The AI still has fixed camera feeds tied to a real network; a ghost has none of that constraint. So this is a genuine third category, distinct from both main HUD's minimal-chrome doctrine (no living body's world-state to protect) and lobby's dense-panel doctrine (there *is* a world here, fully rendered) — closer to an unconstrained director's camera than either.

- **Free-flying camera.** No collision — moves through walls, floors, hull, open space. Adjustable speed.
- **Fully invisible and undetectable to the living**, and to every living-side instrument: no footstep audio, no motion-tracker ping, no camera-feed visibility, nothing. This is the direct consequence of §1's fairness principle, not an oversight to patch later.
- **Follow-lock.** Camera can snap to track a specific living player or point of interest, releasing on manual input — spectating a specific unfolding scene shouldn't require constant manual flying.
- **No physical footprint.** Cannot open doors, trigger sensors, be struck, or otherwise causally touch the living simulation, full stop — with exactly one exception, the Possession event in §5.
- **Permanent chrome:** a slim dead-chat panel (§4) and a nearby-ghosts indicator, once those exist. That's the entire fixed UI. Everything else is the free camera itself.

## 4. Dead chat

- A meta channel visible only to other ghosts, one more channel in the same chat feed as everything else (`comms.md` §5), not a second chat system.
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

## 6. No defibrillator reconnection

A ghost exists only once the body has died — brain function at zero (§2, `health.md` §4). That's past the point a defibrillator can help (`death-cloning-respawn.md` §3): defib works in the window between the heart stopping and brain death, while the player is still in their body. So a ghost is never pulled back into their original body by a defib; the ways back are the possession paths in §5 (cloning) and, where the server allows it, respawn (`death-cloning-respawn.md` §6).

## 7. Integration notes

| Observer element | Touches existing / resolves |
|---|---|
| Death → ghost detach | `death-cloning-respawn.md` §2 |
| Clone-ready prompt | Reserved possession path, formalizes `death-cloning-respawn.md` §5 |
| Become-AI / become-cyborg prompt | Reserved possession path, formalizes `ai-cyborgs.md` §7 |
| Observe entry | `lobby.md` §7 |
| Dead chat | One more channel in the chat feed, `comms.md` §5 |
| Ghost-role body stats/behavior | Whatever system spawned it (e.g. `crafting.md` §3 fabricator job-state) |
| Round end | `round-end.md` §5 paints its summary over ghosts and the living alike |

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

## 9. Out of scope for this pass

- Exact roster of ghost-role body types (drones, mice, pAIs, hostile simple mobs, etc.) and their individual stat blocks — a content/balancing pass, not a mechanic
- Any future method for the living to detect or perceive ghosts (a medium-type ability, a specific antagonist power) — this pass commits to fully invisible and undetectable; a later addition would need its own justification against §1
- Admin/staff observer tooling (godmode, jump-to-player, stealth observation) — designed in `admin-tools.md` §4, built on this doc's ghost framework
- Ghost cosmetic customization (appearance, trail effects, etc.) — no gameplay weight
- Follow-lock, dead chat and possession — later passes after the first pass in §2
- The round-end summary screen and transition logic itself — this doc only supplies the camera/state framework round-end will use; the content and transition mechanics are `round-end.md`'s job

