# Stamina — design document

> Status: active

Resolves the exertion question `combat.md` §4 raises and builds directly on `health.md`'s organ/systemic model. Not a new independent resource — a fast, shallow layer on top of the oxy system that already exists.

## 1. Design philosophy

Most games give stamina its own meter, disconnected from anything else the character tracks. That's a parallel model this project has been avoiding since the hacking interface doc — "no parallel permission model to maintain" was the rule there for ID/access; the same move applies here. Real exertion depletion is physiologically an oxygen-debt phenomenon, and the health doc already built a real oxy-debt system with real organs regulating it. Stamina should be the everyday, harmless front end of that system, not a second one sitting next to it.

The two have to stay genuinely separate, though. Oxy debt in the health doc means something is actually wrong — it feeds the "Bleeding"-style alert treatment and the critical/death thresholds. Jogging down a corridor is not that, and shouldn't trip the same feedback. So: normal exertion drains a fast, harmless stamina pool. Only *pushing past* that pool — continuing to sprint, swing, block, or fire after it's empty — is what starts drawing real oxy debt. One mechanism connects the two tiers, the same shape as bleeding connecting brute damage to blood volume in the health doc, not a second parallel resource.

## 2. The stamina pool

- Fast-draining, fast-recovering. Depletes over seconds of exertion, refills over seconds of rest — this is the layer that makes momentary sprints, swings, and blocks feel weighty without being a health mechanic.
- **Regeneration rate is modulated by heart/lung function and blood volume**, per the health doc's organ model. This isn't a new rule — it's the existing organ system doing more work. The direct consequence: a character who's already bleeding from an earlier hit tires out faster for the rest of a fight, entirely as a result of systems already defined, no extra bookkeeping required.
- **Shock lowers the cap** (`health.md` §4). A badly hurt body tires faster and can't sprint as long, which is part of why a target who has been shot can't simply outrun the shooter.
- **Weight lowers both the cap and the regen.** Everything you carry counts — worn armor (`armor.md` §4), what's in your hands, and what's in your backpack. Full protection and a full bag cost mobility, exactly the way they would physically. Nothing about weight decides whether an item fits in a container (`inventory-storage.md` §4); weight's only job is this one.
- **Drivers of drain:**
  - Sprinting — continuous drain while moving above walking speed
  - Melee swings — each swing costs stamina on top of the existing windup/recovery timing (`combat.md` §2), discouraging spam-clicking beyond what the timing gate alone does
  - Sustained ranged fire — an additional modifier on top of the existing recoil-climb mechanic (`combat.md` §3): firing while winded widens the accuracy cone further than recoil alone would
  - Grab struggle — both the grabber and the grabbed spend stamina contesting a grab, giving escalation (`main-hud.md` §7) a real physical stake rather than a pure click-race
  - Blocking with equipment returns with blocking itself in a later combat pass (`combat.md` §11)

## 3. Pushing past empty

Hitting zero stamina doesn't lock the player out of acting — that reads as unresponsive input, not exhaustion. Instead:

- **Performance degrades hard.** Movement speed drops, melee windup/recovery lengthens, the ranged accuracy cone widens well beyond its normal recoil-driven range.
- **Collapse.** Pushing hard enough past empty knocks you down (`combat.md` §5) — the body simply stops.
- **Continued exertion at zero starts drawing real oxy debt**, tracked in the health model exactly as described there. This is the "second wind" choice: push through exhaustion to finish a fight or escape a threat, at the cost of an actual, trackable health consequence if it's sustained.

This gives stamina real stakes without inventing a new failure state — the failure state it escalates into already exists.

## 4. HUD treatment

Same rule as vitals (`main-hud.md` §5): minimal permanent chrome, diegetic feedback first, panel as backup.

- **No permanent stamina bar.** Consistent with the vitals cluster's "only draws the eye when something's actually wrong" pattern — a full bar sitting on screen at all times when a character is fully rested adds chrome for no reason.
- **The aiming reticle is the main read.** It blooms wider as stamina drops, on top of recoil (`combat.md` §3) — the same element the player is already watching in a fight, so exhaustion shows exactly where it matters.
- **Screen-space feedback carries the rest**, the same way it does for vitals: heavier breathing audio (personal tier, `audio.md` §4), mild FOV narrowing or screen sway when winded. The player learns their exertion state by how it *feels* to move and fight, not by watching a meter. Visible weapon sway on the character model is a later pass.
- **A compact indicator fades in only when stamina drops below a threshold** (e.g. under ~50%), living near the vitals cluster, and fades back out once it recovers — same hidden-until-relevant pattern the alerts stack already uses.
- **On-demand detail** rides along with the self-examine readout (`examine.md` §7) rather than opening a separate panel.

## 5. Integration notes

| Stamina element | Touches existing system |
|---|---|
| Regen rate and cap | Organ function (heart/lungs) and blood volume — `health.md` §3; shock lowers the cap — `health.md` §4; carried weight — `armor.md` §4, `inventory-storage.md` |
| Push-past-empty cost | Oxy debt pool and critical/death thresholds — `health.md` §4 |
| Melee drain | Windup/recovery timing — `combat.md` §2 |
| Ranged drain | Accuracy cone and reticle bloom — `combat.md` §3 |
| Collapse | Knockdown — `combat.md` §5 |
| Grab struggle | Grab escalation — `main-hud.md` §7 |
| HUD | Vitals cluster's diegetic-feedback pattern — `main-hud.md` §5 |

## 6. Worked example

| Step | What happens | Stamina/health state |
|---|---|---|
| 1 | Player sprints down a corridor toward a fight | Stamina drains steadily, no health effect |
| 2 | Player arrives and starts swinging a weapon | Stamina drains further with each swing |
| 3 | Stamina hits zero mid-fight | Windup/recovery lengthens, movement slows, screen feedback signals exhaustion |
| 4 | Player keeps swinging anyway to finish the fight | Real oxy debt starts accumulating, tracked in the health model |
| 5 | Player wins, disengages, stops moving | Stamina begins refilling; refill rate is slower than normal, because the fight left an untreated wound draining blood volume |
| 6 | Player treats the wound | Blood volume stabilizes, organ perfusion improves, stamina regen returns to normal rate |

## 7. Out of scope for this pass

- Exact numeric drain/regen rates per action (a balancing pass, not a design decision)
- Stamina effects on non-combat actions (climbing, heavy lifting, etc.) — plausible future hook, not designed here
- Character-level fitness/training modifiers to stamina capacity — not assumed to exist, same caveat as accuracy modifiers in the combat doc
- Weapon sway on the character model as an exhaustion read (needs aim IK)

