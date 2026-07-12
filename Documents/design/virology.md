# Virology — design document

> Status: active

Resolves an item four other docs deliberately punted on: `health.md` §9 flags "disease/infection" as a natural future system given organs already exist, `chemistry.md` §12 defers "disease/infection interactions," `surgery.md` §11 defers "disease/infection risk from surgery," and `examine.md` §11 flags "a medical scanner tool for exact organ/vitals function values... a plausible future item, not designed here." This doc is that system, and that item. Builds on the organ/systemic-pool model (`health.md` §2–3), the reagent/bloodstream/delivery model (`chemistry.md`), the diegetic device-screen and discovery-tier pattern (`hacking-interface.md` §2–4, `chemistry.md` §6), Area as the current stand-in for co-location (`area.md` §2), armor's zone-coverage check (`armor.md` §2), and the crew identity record DNA already rides (`death-cloning-respawn.md` §4).

## 1. Design philosophy

The native-vs-leftover test, aimed at SS13 virology specifically: its core loop is culturing a sample, exposing it to reagents or radiation, and watching hidden stat thresholds (resistance, stealth, stage-change chance) nudge it toward a randomly-assembled symptom set. That's a mystery-box wrapped around a hidden RNG walk — the same shape this project already cut from hacking's percentage rolls and explosives' concentric rings. It fails "physical causation over hidden RNG" decisively. This doc keeps the *idea* of virology — diagnose, treat, contain — and drops the hidden-stat discovery game underneath it.

Three decisions fall out of that:

- **A fixed, authored disease roster, not generated ones.** Diagnosing a case means identifying *which known disease this is*, the same shape as chemistry's "unknown reagent" problem (§6 there) — not discovering a novel one that didn't exist until a player mutated it into being.
- **No parallel infection meter.** A disease is a package of effects that read and write the same organ functions and systemic pools `health.md` already tracks — exactly the "one shared mechanism" move chemistry already made for reagents (§7 there: "it isn't a new pool sitting beside health's existing ones — it's an input into them"). Virology gets the same treatment, not a bespoke infection stat.
- **Exposure is a dose, not a re-rolled chance.** SS13's "X% chance to catch it this tick" is itself an artifact. This doc replaces it with an accumulating exposure value — contact and shared air add to it, a per-disease threshold decides infection — deterministic given the inputs, the same "visible property, not a hidden roll" standard armor absorption and chemistry's reactions already set.

One more principle from elsewhere in the project applies directly: **detectable, not prevented.** The AI/law-enforcement docs use physical audit trails and crew response tools instead of mechanically blocking bad behavior, preserving player-agency tension. Infection works the same way — nothing stops an infected character from walking wherever they want. Diagnosis, symptom visibility, and ordinary door access are the crew's tools; there's no separate quarantine-enforcement system.

## 2. Disease record — data model

A disease is authored data, not a runtime-generated object:

- `id`, `display name`
- **Transmission modes** — a flag set: `contact`, `airborne`, or both. Not every disease needs both.
- **Infection threshold** — the exposure value (§3) that triggers infection once crossed. A real number, not a probability.
- **Incubation duration** — time between infection and the first visible symptom. The character is infected and contagious during this window but shows nothing on examine or scan-free inspection — see §5.
- **Stage list** — an ordered sequence, each stage defining:
  - a duration before it worsens into the next stage if left untreated
  - an effect definition targeting one or more of health's organ functions or systemic pools, in the exact same shape chemistry's reagent effect definition already uses (`chemistry.md` §2)
  - whether the carrier is contagious during this stage (defaults to yes from end of incubation onward; a disease can be authored to start contagion earlier, during incubation itself, for the "spreading before anyone knows" case)
- **Cure reagent reference** — a chemistry recipe id (§6).

## 3. Exposure & infection resolution

Two accumulation sources feed one exposure value per (character, disease id) pair. Below the infection threshold, exposure decays slowly when no longer exposed — a single brief contact with a contagious carrier isn't a life sentence of rising risk.

**Contact exposure** — a discrete event: touching an open wound or blood, sharing a syringe or needle, a bite. Each event adds a fixed dose, the same one-shot-transfer shape as pouring one container into another (`chemistry.md` §3) — no new interaction grammar, just a new trigger for the existing Tier 3 use-with pattern. Gloves block skin contact with fluids the same way any other coverage check already works.

**Airborne exposure** — accrues per tick while a character shares an Area with a contagious carrier. This is a deliberate interim substitute, not the final model: Area co-location stands in for the real gas/zone system atmospherics would eventually provide (flagged explicitly in §11 — Area itself was never meant to own atmospherics, `area.md` §4). When a proper atmosphere zone system exists, airborne exposure upgrades to read real gas concentration instead of blunt Area membership; nothing about the disease record or the threshold check changes, only the exposure source.

**Protective equipment** cuts airborne intake at both ends — a mask worn by the susceptible character reduces what they take in, a mask worn by the carrier reduces what they emit — reusing armor's existing zone-coverage check (`armor.md` §2) rather than a new equipment flag.

**Crossing the threshold infects**, deterministically. There's no second roll after that — infection either has or hasn't happened, given the accumulated exposure.

## 4. Progression — organ-driven

Once infected, incubation runs silently (§5), then the first stage begins. Each stage applies its effect definition continuously — draining a systemic pool, degrading an organ's function — using the exact update path those pools already have (`health.md` §3). A disease that attacks the liver doesn't invent a new "liver disease debuff"; it reduces liver function, and toxin clearance slows as a direct, already-existing consequence, not a special case this doc has to hardcode.

**Progression is scheduled, not chanced.** Untreated, a stage runs its authored duration and advances to the next one on a real timeline the player can learn and predict. Treatment (§6) is what interrupts that clock.

**Existing organ damage compounds naturally.** A disease targeting the liver hits harder on a character whose liver is already damaged from an unrelated wound or poisoning, because it's reading the same function value that wound already lowered — an emergent consequence of reusing the real model, not an authored interaction.

## 5. Detection & diagnosis — the medical scanner

Builds the item `examine.md` §11 flagged and left undesigned. A handheld tool, Tier 2 action, two modes:

- **Live scan on a character** — works at any stage, including incubation. This is deliberate: a scan is how a silent carrier gets caught before symptoms would ever reveal them, the honest way to surface "detectable, not prevented" rather than blocking transmission outright.
- **Blood sample analysis** — accepts a container holding a drawn blood sample (syringe draw, an ordinary use of chemistry's existing container/transfer grammar), useful when the carrier isn't present or a covert diagnosis is wanted, e.g. testing a corpse or an unattended drink.

Both resolve instantly — no minigame, consistent with the project's stance that information belongs behind a device, not a puzzle (`hacking-interface.md` §1). A successful scan returns disease id, current stage, and time remaining to the next stage — real values read off real state, not a coarse hint.

**Before any scan, symptoms are the only signal, and only once incubation ends.** Past incubation, contagious-and-symptomatic stages render on the character model — a cough animation, a sound cue, a pallor or flush tint — the same diegetic-first rule wounds already follow (`health.md` §2). During incubation, nothing is visible at all without a scan; that silent window is the point, not an oversight.

## 6. Treatment & cure

**Cure delivery is chemistry, unmodified.** Each disease's cure reagent is an ordinary recipe — possibly condition-gated, exactly like antitoxin (`chemistry.md` §4) — synthesized at the dispenser and delivered by any existing method: injection, patch, pill, IV. Once metabolizing, it halts stage progression and reverses the current stage's organ/systemic effects at a defined recovery rate — the same pools the disease was draining, written back in the other direction. Full clearance requires the cure to remain present long enough to fully reverse the active stage's damage, then the infection state itself clears.

**Symptomatic relief isn't a cure.** Ordinary field treatments — antitoxin for a toxin-effect disease, an oxygen tank for a respiratory one — reduce the same pools the disease is writing into, buying time without touching the infection state or stopping the clock. No new item, no new mechanic: this falls directly out of §4's decision to route disease effects through the real pools.

**Immunity** is a permanent per-disease flag on the crew identity record, the same rail DNA already lives on (`death-cloning-respawn.md` §4). Once cleared, exposure resolution (§3) for that disease id simply never triggers again for that character. Prophylactic, pre-exposure vaccination is a separate, plausible future hook — not designed here (§11).

## 7. Quarantine & crew response

No mechanical lockdown exists in this doc, deliberately, per §1. An infected character can walk anywhere their access already permits. What crew actually have:

- **Scanning** (§5) to identify a case, including silent carriers.
- **Visible symptoms** (§5) once past incubation, as an in-fiction tell.
- **Ordinary Area/door access** (`area.md` §5, `id-access.md`) to restrict a ward or a section as a real crew decision — the same tool every other department already has, not a bespoke quarantine system.

## 8. HUD touchpoints

No new permanent chrome.

- **Symptom rendering on the model** once past incubation — cough, tint, sound — matches the wound-rendering precedent exactly (`health.md` §2).
- **A new alert-stack entry**, hidden-until-relevant like chemistry's overdose/sedation chips (`chemistry.md` §10): a generic "Infected" chip once symptomatic and unscanned, upgrading to name the specific disease once that character has actually been scanned themselves — reflects what the character currently knows, not omniscient information.
- **The organ-status readout** (`health.md` §7, already extended once by chemistry §10) gains active infection as one more listed entry once diagnosed, alongside metabolizing reagents.
- No standalone infection bar — consistent with the vitals cluster's existing "only draws the eye when something's actually wrong" rule.

## 9. Integration notes

| Virology element | Touches existing system |
|---|---|
| Disease effects | Organ function / systemic pools — `health.md` §2–3 |
| Cure synthesis & delivery | Reagents, recipes, delivery methods — `chemistry.md` §2–9 |
| Airborne exposure (interim) | Area co-location — `area.md` §2, pending a real atmosphere zone system |
| Protective equipment | Zone coverage check — `armor.md` §2 |
| Medical scanner | Diegetic device-screen, discovery-tier pattern — `hacking-interface.md` §2–4, `chemistry.md` §6 |
| Blood sample | Container/transfer grammar — `chemistry.md` §3 |
| Immunity flag | Crew identity record — `death-cloning-respawn.md` §4 |
| Quarantine | Area/door access — `area.md` §5, `id-access.md` |
| Alert chip | Alert-stack hidden-until-relevant pattern — `chemistry.md` §10 |

## 10. Worked examples

**A — Airborne outbreak in the mess hall:**

| Step | What happens | Virology state |
|---|---|---|
| 1 | A contagious, symptomatic crew member eats lunch in the mess | Airborne exposure begins accruing per tick for everyone sharing that Area |
| 2 | Two unmasked crew linger nearby for several minutes | Their exposure values cross the infection threshold; both infected, still in incubation |
| 3 | A third crew member wearing a mask sits at the same table | Intake reduced enough that their exposure decays back down before crossing threshold |
| 4 | The two infected crew show no symptoms yet | Incubation is silent by design — nothing to see without a scan |

**B — Contact transmission via an unclean tool:**

| Step | What happens | Virology state |
|---|---|---|
| 1 | A surgeon reuses a scalpel on a second patient without cleaning it after operating on an infected one | Contact exposure event fires on the new patient, fixed dose per §3 |
| 2 | Dose alone doesn't cross the threshold | No infection yet — a single contaminated touch isn't automatically fatal to skip past |
| 3 | Same tool used again on the same patient later in the same procedure | Second exposure event stacks; combined dose now crosses the threshold |

**C — Diagnosis and cure, including a silent carrier:**

| Step | What happens | Virology state |
|---|---|---|
| 1 | A doctor is suspicious of a coworker who seems fine but was in the mess hall outbreak (Example A) | Runs a live scan despite no visible symptoms |
| 2 | Scan resolves instantly | Coworker is confirmed infected, still in incubation, disease identified by name |
| 3 | Doctor synthesizes the matched cure reagent at the dispenser | Ordinary condition-gated recipe, same as antitoxin |
| 4 | Cure injected before symptoms ever appear | Infection clears without the character ever entering a symptomatic stage; immunity flag set |

## 11. Out of scope for this pass

- Mutation, strain generation, and the culture/incubator discovery minigame — deliberately cut per §1's native-vs-leftover judgment
- A dedicated Virologist job or access level — this doc assumes Medical Doctor/CMO scope covers it; a separate role is a future content decision, not a systems one
- Exact disease roster, stage durations, exposure thresholds, and cure recipes — a balancing/content pass, not a design decision
- A full atmosphere/gas zone system — airborne exposure here uses Area co-location as an explicit interim substitute (§3); a real zone system upgrades the exposure source, not this doc's model
- Quarantine enforcement beyond existing Area/door access tools — no new lockdown mechanic
- Prophylactic vaccination (pre-exposure immunity) — only post-cure immunity is designed here; a vaccine system is a plausible future hook
- Gamemode-specific antagonist diseases (zombie/parasite-style effects beyond the organ/systemic model) — content for a specific gamemode, not a base mechanic
- Surgical-site infection as a special case — an open, unsealed surgical wound is just another contact-exposure vector under §3, not a separate mechanic this doc needs to invent

## 12. Companion edits flagged

- `health.md` §9 — remove "disease/infection" from the out-of-scope list; point to this doc instead.
- `chemistry.md` §12 — remove "disease/infection interactions"; cure reagents are ordinary recipes per §4 there, nothing further needed.
- `surgery.md` §11 — "disease/infection risk from surgery" resolves as an ordinary contact-exposure vector (§3 here), not a bespoke surgical mechanic; repoint the citation rather than redesigning surgery.
- `examine.md` §11 — the flagged "medical scanner tool" is now built in §5; repoint the citation.

## 13. Prototyping this

**Claude Design, prompt 1 — medical scanner device screen:**
> Using our SS3D design system, build the medical scanner's diegetic device screen, same visual language as the FDU and chemical analyzer: a live-scan result view (disease name, current stage, time-to-next-stage) and a blood-sample-analysis mode showing the same fields for a container instead of a person. Industrial, flat, no neon.

**Claude Design, prompt 2 — symptom rendering and alert states:**
> Show a symptomatic character rendering on the model (cough animation, pallor/flush tint) alongside the two alert-chip states: generic "Infected" before that character has been scanned, upgrading to the named disease after. Confirm the silent-incubation state shows nothing at all — no chip, no model change — for contrast.

**Cursor, prompt 1 — data contract first:**
> Here's the virology design doc. Define the disease record (transmission modes, infection threshold, incubation duration, ordered stage list with per-stage organ/systemic effect definitions and contagion flag, cure reagent reference), the exposure-accumulation function (discrete contact dose, per-tick airborne accrual via Area co-location, decay below threshold), and the infection/stage-progression scheduler reading and writing the health doc's real organ/systemic pools. Show me all three before wiring the scanner UI.

**Cursor, prompt 2 — one vertical slice:**
> Implement one contact-transmitted disease end to end: exposure event → threshold check → infection → incubation → first symptomatic stage writing a real organ/systemic effect → scanner diagnosis → chemistry-synthesized cure → recovery and immunity flag. Airborne exposure and additional diseases come after this is reviewed.
