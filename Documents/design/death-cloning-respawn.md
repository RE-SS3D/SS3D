# Death, Cloning & Respawn — design document

> Status: active

Resolves the open item flagged in `combat.md` (§6, §8). Builds on the single death trigger and revival window already defined in `health.md` (§4, §6), and reuses three systems wholesale rather than inventing new ones: the fabricator job-state pattern from `crafting.md` §3, the per-zone armor coverage from `armor.md` §2, and Area's power derivation from `area.md` §5.

## 1. Design philosophy

Three reuses do most of the work in this doc:

- **A cloning pod is a fabricator.** Same diegetic device screen, same material/power check before a job starts, same visible running state, same stall-on-interruption behavior already defined for the protolathe-equivalent in `crafting.md` §3 — just printing a body from a DNA record instead of an item from a blueprint. No new machine pattern to learn.
- **Defibrillation reuses combat's zone/armor model.** Pad placement targets the chest zone like any other hit; armor covering that zone blocks contact the same way it blocks incoming damage. One coverage check, two consumers, not a device-specific special case.
- **A DNA record is an identity record**, so it rides the same ID/access rail everything else already keys off, rather than becoming a second, parallel genetic database — the same "no parallel model" move Area made for permissions and the hacking interface made for ID/access.

This doc also draws a symmetric line to health's. Health defined the one canonical trigger that ends a life: brain function reaching zero. This doc defines the one canonical state that ends a *story* — no valid DNA record and no usable remains. Everything between those two points is real, playable recovery, not a percentage roll standing in for one.

## 2. What death is, and isn't

Not redefined here — brain function reaching zero remains the sole trigger (`health.md` §4). What this doc adds is what happens on either side of it:

- The body becomes a corpse: a physical object in the world, carrying whatever wound and organ state it had at the moment of death, same rendering rules as a living character's damage (§5 of the main HUD doc).
- The player's consciousness detaches into an observer. The full observer/ghost experience is out of scope here, same as it's flagged out of scope in `main-hud.md` §13 — this doc only adds one narrow exception (§5).
- Death is not automatically permanent. Cardiac arrest is the fast track toward it, not the event itself (health §4) — that's the real physical window defibrillation interrupts (§3). Separately, cloning offers a path back even after brain death has fully finalized (§5), as long as the state in §6 hasn't been reached.

## 3. Defibrillation — field revival

**The window:** unchanged from `health.md` §4 — cardiac arrest cuts brain oxygen and brain function starts dropping quickly, but doesn't reach zero instantly. Defib restarts the heart inside that window. Past it (brain function already at zero), defib does nothing — no exception, no partial effect, matching health's rule that there's exactly one death trigger and no backdoor around it.

**Pad placement:** targets the chest zone specifically, reusing the existing seven-zone system (`main-hud.md` §6) rather than a bespoke hit-check.

**Blocked by armor:** a chest piece with active coverage (`armor.md` §2) blocks pad contact — it has to be removed or already breached before the pads can do anything. This is the same per-zone coverage check combat already runs on a hit, not a second system to maintain.

**Charge:** the unit runs on a real internal battery, depleted per shock, recharged on a wall-mounted dock that draws from its Area's APC (`area.md` §5) — same derivation every other powered device already uses.

**Using it on a beating heart isn't free.** A shock to a heart that's still running causes real burn damage rather than doing nothing — a genuine reason not to spam-click it on an ambiguous case, the same physical-cost logic that already discourages spam via melee's windup/recovery (`combat.md` §2).

**Failure is visible, not silent.** Past the window, the unit gives a clean diegetic "no response" readout — a real failure state read off real values, the same discipline the hacking interface and fabricator screens already follow, not a hidden check the player can't see.

**It doesn't heal.** Defib restarts the pump; it doesn't fix what stopped it. Field or surgical treatment (health §6) is still needed afterward for whatever caused the arrest in the first place.

## 4. DNA records

Tied to the same crew identity record the ID/access system already maintains — not a standalone genetics database. Two ways to capture one:

- **Proactive:** a routine medical body scan while alive — fast, reliable, the kind of thing a standard medbay checkup already implies.
- **Reactive:** extracted from a corpse after death, provided the remains are intact enough. A husked or sufficiently destroyed body can't be sourced this way — a real, visible, physical reason (nothing left to extract), not a percentage roll.

Records live on the genetics console / crew records as a real, breakable database — destructible in an explosion or sabotage like any other physical station system, not an always-available save file sitting outside the simulation. That's a deliberate stake, not an oversight: protecting the records matters precisely because they're physical.

## 5. Cloning

**The pod is a fabricator (§1):** it needs a valid DNA record and sufficient biomass — the "material" input, drawn from the same kind of physical stock crafting already established (`crafting.md` §5) — plus power from its Area's APC. It does **not** need the actual corpse; once a record exists, the pod grows an entirely new body from biomass. The corpse only matters upstream, as a possible source for reactive record extraction (§4) if no proactive scan was ever taken.

**Running the job:** identical shape to a fabricator print (`crafting.md` §3) — material and power checked before the job starts, a real visible build-time while the body grows in the pod, a clean stall (visible, not silent) if power cuts or biomass runs out mid-grow, resuming from where it stalled once restored.

**A real, visible cost to the process itself:** a cloned body wakes with brain function starting at a fixed, reduced value rather than the character's prior full state — a deterministic fidelity loss from the printing process, visible on the existing organ readout (`health.md` §7), recoverable through normal treatment. Not a hidden penalty; the number is just lower and the player can see why.

**On completion:** the pod opens with the finished body, and the waiting observer gets a minimal, on-demand prompt — "Clone ready — enter?" This is the one narrow exception carved out of the deferred ghost/observer UI (`main-hud.md` §13); it's a single prompt, not a redesign of the observer experience.

## 6. Permanent death & respawn

**The one canonical unrecoverable state**, symmetric to health's single death trigger: no valid DNA record **and** no remains usable for reactive extraction — husked, destroyed, or missing entirely. Both halves are physical and inspectable; a husked corpse looks husked, the same "let the world carry the information" rule already governing everything else in this project. There's no ambiguity to roll against.

**Respawn is a session mechanic, not a physical one, and this doc says so plainly** — the same honesty the comms doc already applies to OOC (§7): it's not happening in-fiction. Once the state above is reached, a real-world/round-time threshold passes and the player is offered the choice to respawn as a new crew member. This exists purely to avoid leaving a player dead for an entire round with nothing recoverable, not to simulate anything.

**Respawn is a new person**, not a resurrection — no claimed continuity with the previous character.

## 7. HUD & feedback touchpoints

- **Critical/dying feedback** (heartbeat audio + synced pulse, `main-hud.md` §5) is unchanged and unextended here. The moment brain function hits zero, that feedback simply stops — the absence of the heartbeat is the tell, not a separate "you died" overlay stacked on top of it.
- **Defib use:** a real diegetic shock effect, and the heartbeat audio picking back up on success — or the clean "no response" readout on the unit (§3) if not.
- **Cloning pod:** the same diegetic device-screen treatment as the fabricator (`hacking-interface.md` §2 pattern, reused via crafting §3) — record status, material/power check, running/stalled/complete states, identical visual language to the fabricator mockups already built.
- **Clone-ready prompt:** the single narrow exception to the deferred ghost/observer UI, per §5.

## 8. Integration notes

| Element | Touches existing / needed system |
|---|---|
| Death trigger | Brain function reaching zero (`health.md` §4) — unchanged, not redefined here |
| Defib window | Cardiac-arrest-to-brain-death window (`health.md` §4) |
| Defib pad placement | Seven-zone system (`main-hud.md` §6) |
| Defib blocked by armor | Per-zone coverage (`armor.md` §2) |
| Defib charge/recharge | Area → APC derivation (`area.md` §5) |
| DNA records | Existing ID/access identity rail (reused, not a parallel database) |
| Cloning pod | Fabricator job-state pattern and material stock (`crafting.md` §3, §5), Area → APC power |
| Cloning fidelity-loss penalty | Organ function readout (`health.md` §7) |
| Clone-ready prompt | Narrow exception to deferred ghost/observer UI (`main-hud.md` §13) |
| Critical/dying feedback | Unchanged (`main-hud.md` §5) |

## 9. Worked examples

**A — Defib save (continuing the exact chain `health.md` §8 started):**

| Step | What happens | State |
|---|---|---|
| 1 | Untreated bleeding drains blood volume; oxy debt climbs; heart eventually fails | Cardiac arrest, brain function starts dropping fast (health §4/§8) |
| 2 | A teammate reaches the patient inside the window | Chest zone exposed — armor already removed or never breached there |
| 3 | Defib has charge, pads placed on the chest zone | Shock delivered |
| 4 | Heart restarts before brain function reaches zero | Heartbeat audio resumes; patient is stable but still whatever caused the arrest — bleeding still needs a bandage, blood volume still needs a transfusion |

**B — Full cloning after brain death:**

| Step | What happens | State |
|---|---|---|
| 1 | A character dies with brain function already at zero — defib's window has passed | Corpse persists in the world; consciousness detaches to observer |
| 2 | A prior medical scan already recorded their DNA | Reactive extraction isn't even needed |
| 3 | Medbay staff start a cloning job at the pod | Record + biomass + Area power all check out; job starts, visible build time begins |
| 4 | Pod finishes | Body ejects; brain function starts at a reduced but visible value |
| 5 | Observer gets the clone-ready prompt, accepts | Player wakes in the new body |

**C — Permanent death → respawn:**

| Step | What happens | State |
|---|---|---|
| 1 | A character dies in a fire with no prior DNA scan on file | Corpse is husked — reactive extraction isn't possible |
| 2 | No valid record exists anywhere | Cloning isn't an option — a physical reason, visible on inspection, not a roll |
| 3 | Round-time threshold passes with no revival | Player is offered respawn as a new crew member — a session mechanic, not an in-fiction event (§6) |

## 10. Out of scope for this pass

- Full observer/ghost UI/experience (deferred in `main-hud.md` §13; this doc only adds the single clone-ready prompt in §5)
- Exact numeric values — biomass cost, pod build time, defib charge capacity/recharge rate, cloning fidelity-loss amount, respawn time threshold (a balancing pass, not a design decision)
- Antagonist- or gamemode-specific permadeath rules (server/gamemode policy, not a base mechanic)
- Corpse decomposition over time (not modeled; husk/destruction is the only disqualifying condition, not elapsed time)
- Cryogenics/cryo storage as a holding state for logged-off characters — flagged alongside cloning in `combat.md` §6, but it's a genuinely separate "pause" mechanic, not a revival path, and worth its own pass
- Cybernetic revival paths beyond what `health.md` §9 already covers

