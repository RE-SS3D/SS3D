# Health — design document

> Status: active

Defines the body's damage, critical, death and treatment model, and where it shows up in the HUD. It builds on the seven body zones that zone targeting already uses (`main-hud.md` §6, `combat.md`) and on a set of organs that regulate the bloodstream.

## 1. Design philosophy

Same thread as every other doc in this project: failure should trace to something physical, not an abstract number. The combat doc drew the line at "a percentage roll going against you"; health draws a version of the same line at death. A character shouldn't die because a generic health bar hit zero — they should die because a specific, real thing (their brain) stopped functioning, for a specific, traceable reason (blood loss, suffocation, poisoning, trauma). Organs are what make that possible here in a way it wasn't in SS13's flatter model.

Two tiers fall out of this:

- **Physical damage is local.** A wound happens *to* a limb.
- **Systemic damage is regulated, not local.** Toxin and oxygen aren't properties of an arm — they're properties of a bloodstream, and organs are the mechanism that governs them.

The same rule covers staying on your feet. Nobody takes half a rifle magazine and keeps standing until they bleed out: the body is overwhelmed by the injuries themselves. That is **shock** (§4), and it is no more a hit-point bar than blood volume is. It is built from specific wounds on specific limbs, it falls when those wounds are treated, and it never kills by itself. It only hands off to the one death trigger. What is broken counts too: a body whose legs are both ruined falls over, whatever its shock (§5).

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

## 4. Shock, going down, critical and death

A badly hurt body goes down in two stages: **Downed**, still conscious, then **Critical**, unconscious and dying. Shock is what usually drives the first stage; the systemic thresholds below drive the second.

### Shock

Shock is the body being overwhelmed by trauma: pain, and the circulation faltering under it. It is a whole-body value, built from local wounds.

- **Every hit adds shock**, weighted by where it lands. Torso, groin and head count heavily; arms and legs count less. Burns hurt more than brute of the same amount. Aim matters: center mass drops people, leg shots slow them.
- **Wounds getting worse add a spike.** When a zone crosses into a worse wound tier (§5), shock jumps on top of the damage itself. The first serious wound on a limb hurts more than further damage to an already wrecked one. Players can see these moments, because they are the same moments a wound opens and blood sprays.
- **Armor doesn't make you immune.** Only damage that gets through armor counts in full, but a small part of what armor absorbs still becomes shock: the blunt force of a round stopped by a plate (`armor.md` §2). An operative in a hardsuit who soaks a whole magazine still staggers and eventually goes down, so a focused crew response feels like it's working before the suit breaks.
- **It builds over a few seconds.** A hit adds incoming pain that becomes shock over roughly 2–4 seconds, not instantly. Someone can take a burst, return fire, and then crumple. That leaves time for one last desperate act, such as throwing the disk or keying the radio, and the shooter can't be sure of a takedown the moment the shots land.
- **It drains over time, down to a floor.** Once the hits stop, shock falls, but every untreated open wound keeps a minimum under it, and low blood volume raises that minimum further. A bandage lets you get back up; a transfusion stops the shakes. Every treatment in §6 now counts toward staying on your feet, not just toward staying alive.
- **It wears you out.** Rising shock lowers the stamina ceiling (`stamina.md` §2): a hurt body tires faster and can't sprint as long, so a badly shot target can't simply run away.

Shock never kills on its own. Its last threshold hands off to Critical, below, and Critical's countdown to the heart.

### Downed

When shock crosses its first threshold, or both legs are disabled (§5), the body goes **Downed**: it falls to the floor, still conscious.

- **What you can still do:** crawl slowly, talk, use the radio, and keep what you are holding. You can use a one-handed item, such as a pistol with a very wide reticle or a bandage on yourself. You cannot fire a two-handed weapon. Others can drag you.
- **What it is for:** the downed crew member crawling into cover, calling for a medic, or taking a last shot, and the attacker's choice to finish them, drag them off, or leave them for later.
- **It is not knockdown.** A knockdown (`combat.md` §5) is a fixed couple of seconds on the floor; Downed lasts as long as the shock does.
- **There is no finishing move.** You finish a downed body by doing what put it down: more damage builds more shock, toward Critical.
- **Down is not out.** A Downed or Critical antagonist is still alive. A win condition that needs them dead or restrained (`antagonist-content.md` §6) still needs exactly that, so a downed operative can still be rescued by their squad.

**Getting back up.** A Downed body stands up on its own once shock falls below a **stand threshold**, set lower than the one that put it down, so a body near the line doesn't flicker up and down. Another player can **help it up** sooner, once shock is no higher than the Downed threshold. The helped-up body is unsteady for a few seconds. A body downed because both legs are disabled can't stand, on its own or with help, until at least one leg is splinted (§6).

### Critical

**Critical** triggers when shock crosses its second, higher threshold, when any systemic value crosses its own threshold (blood volume critically low, oxy debt too high, toxin concentration too high), or when brain function drops below a consciousness threshold. Not one aggregate number: several independent, real thresholds, any one of which is dangerous on its own. Recoverable with treatment (§6) — but **critical is a countdown, not a resting state**. Left untreated, a critical patient's heart weakens until it stops, which starts the cardiac-arrest window below. Someone in critical will die if nobody helps them; that is what makes a medic's arrival matter. Shock is one more traceable route into this same countdown, like blood loss: someone who takes a full magazine passes through Downed into Critical quickly. Someone shot a few times stays Downed, and recovers if nobody finishes them off.

### Death

**Death** has exactly one canonical trigger: brain function reaching zero. Cardiac arrest doesn't equal death — it's the fast track toward it, since a stopped heart cuts off the brain's oxygen and its function starts dropping quickly. That's what gives a defibrillator something real to interrupt: a genuine physical window between the heart stopping and brain death finalizing, not a hidden countdown. Blood loss, suffocation, and poisoning all become real paths that lead to the same single, physical endpoint, rather than each needing its own separate "you're dead" rule.

**Decapitation is death.** A head separated from its body has no circulation, so the brain stops — the same single trigger, reached instantly. The brain itself can be recovered from the head afterwards, which is what later revival paths (`death-cloning-respawn.md`) work from. A talking severed head is not part of this design.

On death the player becomes a ghost (`observer.md`).

## 5. Wounds & bleeding

Wound severity is a function of accumulated brute/burn on a given zone, rendered on the model per the existing vitals-cluster rule (visible cuts, burns, blood, a limp). Roughly:

| Tier | What it looks like | Mechanical effect |
|---|---|---|
| Bruised | Discoloration, no open wound | Adds shock (§4), no bleeding |
| Wound | Visible laceration or burn | Bleeding starts, some function loss |
| Severe | Deep wound / bad burn | Faster bleeding, significant function loss |
| Disabled | Limb unusable | No further local input from that limb |
| Severed | Limb gone | Heavy bleeding until treated, function gone for the rest of the round in this pass |

**Severing needs both damage and an edge.** A limb can only be cut off once it's already Disabled, and only by a sharp weapon (a blade, an axe). Blunt trauma breaks a limb but never removes it. Severing the head is decapitation (§4).

**What a broken limb does to staying upright.** Crossing into a worse tier also spikes shock (§4). Beyond that:

- **One leg disabled:** a heavy limp.
- **Both legs disabled:** the body falls and is Downed (§4), whatever its shock, until at least one leg is splinted.
- **A wounded leg while sprinting:** a hit on a leg that is already at Wound or worse trips a sprinting body, a knockdown (`combat.md` §5). Fleeing on a shot leg is a gamble.
- **An arm disabled:** it drops what it is holding and can't hold anything new.
- **A severe head wound:** a large shock spike and blurred vision. It doesn't knock you out by itself: brain damage from head trauma (§2) covers that.

Bleeding rate scales with wound severity and stacks across multiple wounds. This is the acute, actionable version of the systemic story — see §7 for how it surfaces separately from the passive systemic bars.

## 6. Treatment

Organized by tier, and by how immediate the fix is — field treatment anyone can do, surgical treatment that needs medbay-grade tools.

### Field treatment — physical
- Bandages/gauze — stop or slow bleeding, gradual brute healing over time
- Burn dressing — gradual burn healing
- Splints — stabilize a fracture, restore partial limb function before full healing; a splinted leg lets a body downed by two broken legs stand again (§4)
- Helping up — pull a Downed body back to its feet sooner than it would stand on its own (§4)

### Field treatment — systemic
- Oxygen mask/tank — directly reduces oxy debt, independent of lung health (a real bypass, not a fix)
- CPR — manual assist for oxy damage; can stabilize a critical patient without addressing the underlying cause
- Antitoxin — chemically reduces toxin damage directly, doesn't require liver/kidney function to work
- Blood transfusion / IV — restores blood volume directly, which also lowers the floor under shock (§4)
- Painkillers — slow the buildup of shock for a while. The cost is that they also dull the pain you feel, so new wounds go less noticed: you can bleed out feeling fine. Too much is an overdose. This trade-off is the point: a painkiller is a choice, not a pure buff.
- Stimulants (adrenaline) — for a short time, shock can't put you Downed. When the stimulant wears off, the shock held back arrives as a crash. It's the "fight through it" button, and a natural part of an operative's kit as a medipen.

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
- **Alerts** (`main-hud.md` §9) surface what's acute and actionable: **Bleeding** for active blood loss, **Shock** for rising shock and **Cardiac arrest** for a stopped heart, each with a warning and a critical severity.
- **Feeling shock rise.** Screen-space feedback steps up with shock: the edges darken and desaturate, sound muffles, and the view sways once collapse is near. While incoming pain is still building (§4) and would take you down, a heartbeat thump and a pulsing screen warn you: you are on the clock, and this is the moment for a last action.

What everyone else can read, without asking in chat:

| State | What onlookers see and hear | Examine says |
|---|---|---|
| High shock, still standing | A stumbling gait and a hunched posture | "is pale and shaking" |
| Downed | On the floor but moving, crawling; occasional groans | "is down, conscious and in pain" |
| Critical | Limp and still; ragged, shallow breathing up close | "is unconscious and breathing shallowly" |
| Dead | Ragdoll, silent | "is not breathing" |

Sound is the main cue at range: groaning means Downed, gasping means Critical, silence means dead.

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

A gunfight, where shock does the work:

| Step | What happens | Health state |
|---|---|---|
| 1 | An unarmored crew member takes a rifle burst to the torso | Chest wounds open with blood spray; incoming pain starts building |
| 2 | They return fire for a second or two | The heartbeat warning pulses; shock climbs past the Downed threshold |
| 3 | They crumple | Downed: on the floor, conscious, groaning; they key the radio and call for a medic |
| 4 | The operative moves on rather than finishing them | Shock stops rising but stays high, held up by the open wounds and the blood already lost |
| 5 | The Doctor arrives and bandages the wounds | The floor under shock drops; shock drains |
| 6 | The Doctor helps them up | Back on their feet, unsteady for a few seconds, with less stamina than before |
| 7 | Had the operative fired again at step 4 instead | Shock crosses the second threshold: Critical, and the countdown to the heart begins |

## 9. Out of scope for this pass

- Disease/infection — designed in `virology.md`, which builds directly on this doc's organ/systemic-pool model
- The surgical procedure itself (`surgery.md`)
- Organ transplants, cybernetics, prosthetics and limb reattachment
- A per-limb and organ readout on self-examine (sentences only in this pass)
- Genetic/DNA or radiation damage, if that exists as a separate type — not assumed here
- Nutrition/hunger specifics — already an existing alert, untouched here
- Sedatives that put a patient to sleep, and drug interactions beyond the painkiller and stimulant in §6

