# Health — design document

> Status: active

Defines the body's damage, critical, death and treatment model, and where it shows up in the HUD. It builds on the seven body zones that zone targeting already uses (`main-hud.md` §6, `combat.md`) and on a set of organs that regulate the bloodstream.

## 1. Design philosophy

Same thread as every other doc in this project: failure should trace to something physical, not an abstract number. The combat doc drew the line at "a percentage roll going against you"; health draws a version of the same line at death. A character shouldn't die because a generic health bar hit zero — they should die because a specific, real thing (their brain) stopped functioning, for a specific, traceable reason (blood loss, suffocation, poisoning, trauma). Organs are what make that possible here in a way it wasn't in SS13's flatter model.

Two tiers fall out of this:

- **Physical damage is local.** A wound happens *to* a limb.
- **Systemic damage is regulated, not local.** Toxin and oxygen aren't properties of an arm — they're properties of a bloodstream, and organs are the mechanism that governs them.

Same diegetic-first principle as vitals (§5 of the main HUD doc): wounds render on the actual character model, screen-space feedback communicates systemic state, panels are on-demand backup, not the primary way a player learns they're hurt.

## 2. Damage model — two tiers

### Physical (per-limb)

Brute and burn, tracked per zone across the seven body zones. Crossing thresholds within a limb produces escalating local effects: bruising → an open wound → a fracture or severe burn → the limb disabled. Severing is not simply the next threshold — see §5. Function degrades along the way, not just at the end — a damaged leg limps and slows before it stops working, a damaged arm loses grip/aim precision before it goes fully unusable. This is a refinement of, not a departure from, the "body itself is the doll" rule already established: these effects are exactly what should be rendering on the model.

### Systemic (whole-body, organ-regulated)

- **Toxin** — poison concentration in the bloodstream. Liver and kidneys clear it over time; damage to either slows clearance, and losing both means it doesn't clear on its own at all.
- **Oxy** — blood oxygen deficit. Lungs handle intake, heart handles delivery. Damage to either raises oxy debt even in a perfectly breathable atmosphere — this is what makes it possible to be suffocating while standing in clean air.
- **Blood volume** — drained by open wounds. Low blood volume degrades oxygen delivery independent of lung/heart health (there's simply less of it to carry oxygen) and degrades organ function generally through poor perfusion.

### Environment

The environment damages the body directly, without a weapon:

- **Heat and cold** — burn on every exposed zone while you stand in it, reduced by the environmental protection of what you wear (`armor.md` §1, §3).
- **Low pressure** — oxy debt (nothing to breathe) plus damage to the lungs themselves.
- **Toxic gas** — toxin, through the lungs.

A sealed suit blocks the pressure effects on the lungs; internals supply breath and keep toxic gas out (`armor.md` §3).

### What connects the two tiers

- **Bleeding.** An open wound (deep laceration, severe burn) creates a bleeding rate that drains blood volume over time. This is the one mechanism that turns local damage into a systemic problem — a bad arm wound left untreated eventually becomes a blood volume problem, not just an arm problem.
- **Head trauma → brain function.** Severe physical damage to the head zone specifically can push brain function down directly, a second, more direct cross-tier link distinct from bleeding.

## 3. Organs — integration, not redesign

Each organ's function state drives one part of the systemic model:

- **Heart** — circulation. Failing heart function means oxygen doesn't reach tissue even with healthy lungs and full blood volume. Zero function is cardiac arrest — see §4.
- **Lungs** — intake. Failing lung function raises oxy debt accumulation even in breathable air; destroyed lungs can't process air regardless of atmosphere.
- **Liver + kidneys** — toxin clearance rate, as above.
- **Brain** — consciousness, and the sole death trigger. Reduced function degrades vision/control before it's fatal (a natural extension of the existing screen-space feedback pattern — blurred vision, sluggish response); zero function is death.
- Any additional organs (eyes, stomach, etc.) plug into their own existing effects (vision, the existing hunger alert) the same way — this doc doesn't need to invent those if they're already handled elsewhere.

## 4. Critical and death

**Critical** triggers when any systemic value crosses its own threshold — blood volume critically low, oxy debt too high, toxin concentration too high — or when brain function drops below a consciousness threshold. Not one aggregate number: several independent, real thresholds, any one of which is dangerous on its own. Recoverable with treatment (§6) — but **critical is a countdown, not a resting state**. Left untreated, a critical patient's heart weakens until it stops, which starts the cardiac-arrest window below. Someone in critical will die if nobody helps them; that is what makes a medic's arrival matter.

**Death** has exactly one canonical trigger: brain function reaching zero. Cardiac arrest doesn't equal death — it's the fast track toward it, since a stopped heart cuts off the brain's oxygen and its function starts dropping quickly. That's what gives a defibrillator something real to interrupt: a genuine physical window between the heart stopping and brain death finalizing, not a hidden countdown. Blood loss, suffocation, and poisoning all become real paths that lead to the same single, physical endpoint, rather than each needing its own separate "you're dead" rule.

**Decapitation is death.** A head separated from its body has no circulation, so the brain stops — the same single trigger, reached instantly. The brain itself can be recovered from the head afterwards, which is what later revival paths (`death-cloning-respawn.md`) work from. A talking severed head is not part of this design.

On death the player becomes a ghost (`observer.md`).

## 5. Wounds & bleeding

Wound severity is a function of accumulated brute/burn on a given zone, rendered on the model per the existing vitals-cluster rule (visible cuts, burns, blood, a limp). Roughly:

| Tier | What it looks like | Mechanical effect |
|---|---|---|
| Bruised | Discoloration, no open wound | Pain feedback, no bleeding |
| Wound | Visible laceration or burn | Bleeding starts, some function loss |
| Severe | Deep wound / bad burn | Faster bleeding, significant function loss |
| Disabled | Limb unusable | No further local input from that limb |
| Severed | Limb gone | Heavy bleeding until treated, function gone for the rest of the round in this pass |

**Severing needs both damage and an edge.** A limb can only be cut off once it's already Disabled, and only by a sharp weapon (a blade, an axe). Blunt trauma breaks a limb but never removes it. Severing the head is decapitation (§4).

Bleeding rate scales with wound severity and stacks across multiple wounds. This is the acute, actionable version of the systemic story — see §7 for how it surfaces separately from the passive systemic bars.

## 6. Treatment

Organized by tier, and by how immediate the fix is — field treatment anyone can do, surgical treatment that needs medbay-grade tools.

### Field treatment — physical
- Bandages/gauze — stop or slow bleeding, gradual brute healing over time
- Burn dressing — gradual burn healing
- Splints — stabilize a fracture, restore partial limb function before full healing

### Field treatment — systemic
- Oxygen mask/tank — directly reduces oxy debt, independent of lung health (a real bypass, not a fix)
- CPR — manual assist for oxy damage; can stabilize a critical patient without addressing the underlying cause
- Antitoxin — chemically reduces toxin damage directly, doesn't require liver/kidney function to work
- Blood transfusion / IV — restores blood volume directly

### Surgical treatment (medbay-grade)
- Internal repair for severe wounds and internal bleeding

Organ transplants, cybernetics, prosthetics and limb reattachment are out of scope for this pass (§9). The surgical procedure itself is `surgery.md`'s job.

### Revival
- **Defibrillator** — restarts a stopped heart within the physical window described in §4, before brain death finalizes. This is the concrete mechanical payoff of the critical/death model above.

## 7. HUD touchpoints

Not a redesign of the main HUD doc's vitals cluster (§5) — a note on what its bars mean:

- The brute/burn bars read as the *worst-affected limb*, not a whole-body sum — matches the "only draws the eye when something's actually wrong" rule better than an averaged number would.
- The toxin/oxy bars are true systemic values sourced from organ function and blood state, not per-limb stats.
- **Self-examine** (`examine.md` §7) describes your condition in plain sentences ("your left arm is badly bruised", "you feel light-headed"). A per-limb and organ readout is a later pass.
- **Alerts** (`main-hud.md` §9) surface what's acute and actionable: **Bleeding** for active blood loss and **Cardiac arrest** for a stopped heart, each with a warning and a critical severity.

## 8. Worked example

| Step | What happens | Health state |
|---|---|---|
| 1 | Melee hit lands on chest | Brute damage applied to chest zone (existing combat resolution) |
| 2 | Damage crosses the wound threshold | Chest wound opens, visible on the model, bleeding starts |
| 3 | Player doesn't treat it immediately | Blood volume drains steadily; "Bleeding" alert chip lights up |
| 4 | Blood volume keeps dropping | Oxy debt starts rising even though lungs and heart are both fine — there's simply less blood to carry oxygen |
| 5 | Another player applies a bandage | Bleeding stops; blood volume itself is still low until a transfusion |
| 6 | Left untreated long enough instead | Oxy debt crosses the critical threshold; player enters critical state |
| 7 | No one intervenes | Critical counts down until the heart fails; brain function starts dropping fast — the defib window opens and the Cardiac arrest alert fires |
| 8 | A defibrillator is used in time | Heart restarts before brain function reaches zero — recovery, not death |

## 9. Out of scope for this pass

- Disease/infection — designed in `virology.md`, which builds directly on this doc's organ/systemic-pool model
- The surgical procedure itself (`surgery.md`)
- Organ transplants, cybernetics, prosthetics and limb reattachment
- A per-limb and organ readout on self-examine (sentences only in this pass)
- Genetic/DNA or radiation damage, if that exists as a separate type — not assumed here
- Nutrition/hunger specifics — already an existing alert, untouched here

