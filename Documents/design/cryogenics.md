# Cryogenics — design document

> Status: deferred

**Changed by the 2026-10 review.** This doc is deferred and its body predates the review. These points no longer hold or have moved; read them before building on anything below:

- A disconnected player's body stays where it is, asleep, for the whole round, and they can reclaim it any time (`networking.md` §3). There is no automatic move to cryo after a grace period (§4 here).
- The reactor is deferred (`electricity.md` §2); don't lean on its gauge.
- The crew manifest needs the PDA, which is also deferred.
- **Believable first pass:** a voluntary pod. Entering it removes the character and their inventory, frees their job slot, and lets them resume later in the round.

Resolves a gap flagged three times without ever being designed: `combat.md` once name-checked "cryo" alongside cloning and respawn as a distinct, very SS13-specific system worth its own pass; `death-cloning-respawn.md` §10 draws the line explicitly — "a genuinely separate 'pause' mechanic, not a revival path, and worth its own pass"; `chemistry.md` §5 goes further and deliberately builds this doc's own physical primitive early ("a real, trackable temperature value on a container... so that pass wouldn't have to invent one from scratch"), flagged again in §13's out-of-scope list. This is that pass.

## 1. Design philosophy

**This isn't a BYOND artifact needing replacement — it's a genuinely good idea that's never been given this project's treatment.** Unlike most of what this project fixes, SS13's cryo cell already solves a real multiplayer problem well: a player who has to log off shouldn't leave a defenseless, un-piloted body sitting in the world for someone else to exploit, and a department shouldn't stay short-staffed for an entire round because someone disconnected without a way to hand the slot back. The idea survives the native-vs-leftover test cleanly. What it's never gotten is a real physical object, a real power draw, and a real inspectable state — the same treatment every other system in this project already has. That's this doc's job.

**Not death, not respawn — a pause on a living character.** `death-cloning-respawn.md` §10 already drew this boundary and this doc holds it: nothing here intersects a dead character, a DNA record, or a clone. A cryo'd character is simply not being played right now, and stays exactly as alive as they were the moment they left.

**The mirror of observer's meta exception, pointed the other way.** `observer.md` §1 carves ghosts out of "everything maps to something physical" on purpose — a dead player is deliberately pure meta so their knowledge can't leak back into a round still being played by people still at risk. A paused character is the same category of deliberate exception for the opposite reason: administratively removed from live simulation not to protect information, but to protect a *living* character from being a stranded, exploitable object while nobody's actually playing them. Different reason, same shape: real while it matters, meta while it doesn't.

**Real physical footprint even though the pause itself is meta.** The pod is a real, powered, gauge-readable object. Who's inside, and since when, is a real fact — not a hidden despawn nobody could reconstruct.

## 2. The cryo pod — physical object

A station-placed device, sited in a cryo storage room, drawing power from its Area's APC exactly like every other powered device (`area.md` §5) — no separate power model invented for it.

**A real gauge, not a hidden state.** The pod's own internal temperature is a real, trackable value, delivered on exactly the physical primitive `chemistry.md` §5 flagged this doc would need — the same "world-carries-the-information" gauge convention the reactor's heat readout already established (`electricity.md` §2), not a UI-only status.

**Occupancy is real, examinable data.** A pod currently in use shows whose it is and how long they've been in it, readable via examine (`examine.md` §7) the same way any other labeled device already is — no separate crew-manifest-style lookup needed just to tell a pod is occupied.

## 3. Entering cryo — deliberate departure

A player walks to an unoccupied pod and interacts with it — a single confirm, not a multi-step or interruptible sequence, since nothing about leaving a round is adversarial.

**Whatever's currently worn or held goes with them.** The character and their equipped/carried inventory pause together as one unit, restored intact on return (§5) — no drop-everything-first requirement, no duplication or loss risk to design around.

**The character is administratively removed from live simulation** — the meta exception from §1, applied here. Nothing about them ticks while paused: no brute/burn healing or worsening (`health.md` §2), no toxin/oxy drift, no disease incubation or progression (`virology.md` §3–4), no reagent metabolism (`chemistry.md` §8). A character resumes in exactly the condition they left, deliberately — cryo is never a free heal, and never a hidden ongoing risk either.

**The job slot frees up the instant they enter.** Same as if they'd never taken it — lobby's latejoin flow already resolves against whatever's "currently open" (`lobby.md` §7), so a cryo-vacated slot needs no special handling there; it's just open.

**Crew manifest reflects it as a real, distinct fact.** The connection-status field the crew manifest tab already reads (`id-access.md` §2, `pda.md` §3) gains a third value — in cryo storage — distinct from an ordinary disconnect. A crewmate can tell the difference between "stepped away safely" and "just vanished," the same "real, visible reason" standard this project runs everywhere else. Companion edit, §9.

## 4. Automatic cryo — the abandoned-body case

A player who disconnects without reaching a pod isn't left as a permanent liability. **After a real, stated grace period of continuous disconnection, the system moves them into cryo automatically** — the identical §3 mechanism, just system-triggered instead of player-walked.

**Relocation is a deliberate, visible act, not a silent teleport.** Since an abandoned body could be anywhere on the station, the system moves it to the cryo room to actually enter a pod — a real, system-triggered event, inspectable the same way any other administrative action in this project is (through the tools `admin-tools.md` already establishes), never a mystery disappearance nobody could account for.

**The grace period exists specifically so a connection hiccup doesn't cost someone their character.** A brief drop and reconnect within the window does nothing at all — `networking.md` §3 owns exactly what that window looks like and how control resumes; cryo only ever fires once the window actually closes.

## 5. Returning — resuming, not rejoining

Reconnecting while your character is still paused offers a direct resume: open the pod (the one you entered, or the one §4 relocated you to) and walk out, inventory and condition exactly as you left them.

**This is not lobby's latejoin flow.** Latejoin (`lobby.md` §7) is for picking a job when you don't already have one in this round. Resuming from cryo is the opposite case — you're not competing for your old slot, because you never stopped being that character. If someone else has since filled that job via ordinary latejoin, that's fine and expected; you're simply not part of that job's count anymore, the same way anyone who left the round any other way wouldn't be.

**Nothing runs while paused, so nothing to catch up on.** Per §3, every systemic clock stopped the moment they entered — there's no backlog of missed damage, hunger, or disease progression to resolve on return.

**A round ending before someone returns needs no special handling.** They simply weren't part of whatever happened while paused; round-end's own summary (`round-end.md` §5) reports real events, and a paused character generated none.

## 6. What this doesn't touch

**AI and cyborgs already have their own logoff-adjacent paths.** A cyborg recharges at its own dock (`ai-cyborgs.md` §6); the AI's driver-agnostic action interface (`ai-cyborgs.md` §9) already anticipates non-human or absent drivers. Neither needs this doc's mechanism, and this doc doesn't extend to them.

**Death, cloning, and respawn are entirely untouched.** A cryo'd character is never dead; nothing here reads or writes anything `death-cloning-respawn.md` owns.

## 7. HUD & touchpoints

No new permanent chrome, same discipline as everywhere else in this project.

- The pod's temperature gauge and occupancy label render on the object itself (§2), examine-only otherwise — no dedicated cryo panel.
- The crew manifest's new "in cryo storage" status (§3) is a small addition to an existing tab, not a new surface.
- Entering and leaving are single confirm interactions with no dedicated UI beyond the pod itself.

## 8. Worked examples

**A — Logging off cleanly:**

| Step | What happens | Cryo state |
|---|---|---|
| 1 | A player needs to log off, walks to the cryo room | Finds an unoccupied pod |
| 2 | Interacts, confirms | Character and held inventory pause together; job slot frees immediately |
| 3 | Crew manifest updates | Shows "in cryo storage," distinct from an ordinary disconnect |
| 4 | A latejoiner picks the now-open job | Ordinary latejoin resolution (`lobby.md` §7), no special-casing needed |

**B — Abandoned body, auto-cryo:**

| Step | What happens | Cryo state |
|---|---|---|
| 1 | A player's connection drops mid-round, far from any pod | Character remains in the world, vulnerable, briefly (`networking.md` §2's real, visible disconnect tell) |
| 2 | The stated grace period elapses with no reconnect | System relocates the character to the cryo room and enters them into a pod |
| 3 | Job slot frees | Same as a deliberate departure |
| 4 | The relocation is a real, logged event | Inspectable through existing admin tooling, not a silent disappearance |

**C — Reconnecting and resuming:**

| Step | What happens | Cryo state |
|---|---|---|
| 1 | The player from example A reconnects mid-round | Their character is still paused in its pod |
| 2 | They resume directly, walk out | Same inventory, same condition as the moment they entered |
| 3 | Their old job may or may not still be theirs | Irrelevant to resuming — they're returning to their character, not applying for a job |

**D — Never returns:**

| Step | What happens | Cryo state |
|---|---|---|
| 1 | A cryo'd character's player never reconnects | Character stays paused for the rest of the round |
| 2 | Round ends | Round-end's summary reports nothing about them — they weren't part of what happened (`round-end.md` §5) |
| 3 | Next round starts | Job slots reset entirely regardless, same as any other round transition |

## 9. Integration notes

| Cryogenics element | Touches existing / needed system |
|---|---|
| Pod power draw | Area → APC derivation (`area.md` §5) |
| Pod temperature gauge | Physical primitive flagged for this purpose (`chemistry.md` §5); gauge convention (`electricity.md` §2) |
| Pod occupancy readout | Examine system (`examine.md` §7) |
| Job slot freeing on departure | Lobby's latejoin, already resolves against "currently open" (`lobby.md` §7) — no edit needed there |
| Crew manifest status value | `id-access.md` §2, `pda.md` §3 — companion edit needed |
| Paused systemic state | `health.md` §2, `virology.md` §3–4, `chemistry.md` §8 — all frozen, not redesigned |
| Abandoned-body relocation, inspectability | `admin-tools.md` (existing observation/audit tooling, not a new log) |
| Round-end reporting | `round-end.md` §5 (unchanged; a paused character simply isn't reported) |
| Meta-exception precedent | `observer.md` §1 (ghosts), mirrored here for a living character |

**Companion edit needed:** `pda.md` §3's crew-manifest bullet should gain "in cryo storage" as a third connection-status value alongside online/offline and alive/no-signal.

## 10. Out of scope for this pass

- **Exact grace-period duration** before automatic cryo fires (§4) — a balancing pass, not a design decision.
- **Pod count, capacity, and cryo-room siting** — a map-authoring/content question.
- **Any power-loss failure or hazard mechanic for the pod itself** — §2 establishes a real gauge; this pass doesn't invent a consequence for it running out.
- **Multiple cryo rooms or department-specific pods** — a plausible content variation, not load-bearing for this design.
- **Any bonus regeneration or degradation while paused** — deliberately excluded by §3/§5; nothing runs while paused, full stop.
