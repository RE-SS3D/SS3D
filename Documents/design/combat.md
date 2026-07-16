# Combat — design document

> Status: active

Builds directly on two things already spec'd in `main-hud.md`: zone targeting (§6 — raycast aiming, seven zones, reticle confirm) and the intent/combat verb chording scheme (§7 — help/harm toggle, Ctrl disarm, Alt grab). Neither of those docs decided what happens once a hit actually lands. This one does.

## 1. Design philosophy

Same thread this whole project has been pulling on since the hacking interface doc: failure should trace to something physical and visible, not an invisible percentage roll. That doc's rule was "you fail a hack because you guessed the wrong protocol, not because a roll went against you." Zone targeting already pushed combat in that direction — you're aiming a raycast at a real body part, not clicking an abstract doll. This doc has to decide whether that holds all the way through to the hit itself.

It splits by weapon class, deliberately:

- **Melee is fully deterministic.** Contact range, valid zone, click — it connects. There's no meaningful "miss cone" for a swing at arm's length; adding one would be RNG for its own sake.
- **Ranged uses a weapon-intrinsic accuracy cone, not a hidden stat check.** This is still "physical, not RNG" — the cone is a visible property of the weapon (recoil kick, reticle bloom) the player can see and manage, not an opaque roll against an invisible evasion stat. It also does real work: movement here is free and continuous, not tile-stepped, so without something governing ranged precision, combat collapses into pure snap-aim — an arena shooter, not the slower, higher-stakes combat SS13 is actually known for.

## 2. Melee combat

**Resolution:** deterministic, per §1. Reticle on a valid zone within contact range, click, hit connects — reuses the exact hover-and-confirm behavior already spec'd in the main HUD doc's zone targeting section, just executed at melee range instead of ranged.

**Windup and recovery.** Each melee weapon has a telegraphed windup (visible wind-back before the swing lands) and a recovery window (brief vulnerability after). This is the pacing lever that keeps melee from being pure instant-spam-click — and it's a physical, visible mechanism (you can see the swing coming) rather than an invisible cooldown gate.

**Blocking.** Not universal — tied to specific equipment (a riot shield, for instance) that can reduce or negate a hit within a timing window. A real timed action, not a stat check, consistent with §1.

**Improvised weapons.** Worth explicitly preserving, not just tolerating — hitting someone with a fire extinguisher or a toolbox is exactly the kind of thing that passes this project's recurring "keep what's good regardless of engine" test, not a legacy artifact. Any held item can function as an improvised weapon at low base damage; dedicated tools (crowbar, wrench) sit a step above; purpose-built weapons above that.

**Damage.** Melee weapon defines a damage type and amount, applied to whichever zone the raycast resolves against — reuses the existing per-limb brute/burn/toxin/oxy model unchanged. Nothing new here; the weapon is just a new input into a data model that already exists.

## 3. Ranged combat

**Resolution:** weapon-intrinsic accuracy cone, per §1. Every ranged weapon has a base spread. It widens with sustained fire (recoil climb), player movement (strafing/moving), and range (falloff past the weapon's effective distance). It narrows when standing still or braced. None of this is hidden from the player — it's readable through weapon handling feedback (recoil kick, reticle bloom), the same "reason about it, don't guess" principle the hacking interface's real schematics already established.

**Hitscan vs. projectile.** Recommend hitscan for most small arms — instant resolution within the accuracy cone, simpler to reason about, matches most modern shooters — reserving actual projectile travel time for slower or thrown things (thrown weapons, heavy ordnance) where the travel time is part of the read. Flagging this as a specific implementation decision rather than baking it in silently — it has real netcode/prediction implications worth confirming before committing.

**Cover and line-of-sight.** Reuses the same raycast/occlusion system already established for comms occlusion (§3 of the comms doc) and hacking device discovery — a wall blocks a shot exactly the same way it blocks a subtitle bubble or a wireless scan. One raycast system, three consumers, no parallel implementation.

**Damage.** Same as melee — weapon defines damage type and amount, applied to the resolved zone, same existing per-limb model.

## 4. Positioning and pacing — why free movement doesn't collapse into a twitch shooter

Free continuous movement (confirmed, not tile-stepped) makes combat inherently more twitch-capable than SS13's grid-stepped fights. The levers that keep it from fully tipping that direction:

- **Windup/recovery on melee, reload/cooldown on ranged** — both already covered above, both physical rather than hidden.
- **Lethality tuned for a handful of solid hits, not a DPS race.** If fights resolve quickly on real hits, positioning and decision-making matter more than sustained aim.
- **Disarm and grab do real tactical work here, not just flavor.** Against a ranged threat, disarm stripping their weapon is enormously valuable precisely because ranged accuracy is otherwise fairly forgiving up close. Against a melee opponent, grab controls their positioning without needing to out-swing them. Both were already spec'd as modifier+click actions in the main HUD doc (§7) — this is just where that scheme starts paying for itself.
- **Stamina/exertion** governs sprinting, swinging, blocking, and sustained fire, and bridges into real oxy debt when pushed past empty — see `stamina.md`.

## 5. What this doesn't redesign

Zone targeting (seven zones, raycast, reticle confirm) and the intent/combat-verb chording (help/harm toggle, Ctrl disarm, Alt grab) are unchanged — see `main-hud.md` §6 and §7. This doc only defines what a weapon does once those systems have already decided *where* and *whether* an action is happening.

## 6. Open questions — explicitly deferred, not designed here

- **Armor/protective gear mitigation per zone.** Combat numbers can't really be finalized without this, but scoping it into this pass would balloon it — natural next follow-up.
- **Hitscan vs. projectile, finally.** Recommended hitscan-by-default in §3, but this needs an implementation-side confirmation given netcode implications.
- **Skill/training modifiers to accuracy.** Not assumed to exist. If a skill system exists elsewhere, it would modulate a weapon's base cone — this doc doesn't invent one.
- **Death and critical-state resolution.** The main HUD doc covers the screen-space feedback (vignette, heartbeat) for critical/dying — what happens after (cloning, cryo, respawn rules) is a distinct, very SS13-specific system worth its own pass.

## 7. Worked example — melee vs. ranged skirmish

| Step | What happens | Combat state |
|---|---|---|
| 1 | Two crew members fight in a corridor — one has a wrench, one has a pistol | Melee attacker within contact range, ranged attacker holding distance |
| 2 | Melee attacker swings | Visible windup telegraphs the attack; if it connects, deterministic hit on the aimed zone |
| 3 | Ranged attacker fires while backing away | Movement widens their accuracy cone — shot has real chance to miss even though aim looked right |
| 4 | Melee attacker closes the distance and lands a solid hit | Recovery window after the swing briefly opens them up |
| 5 | Ranged attacker, now at a disadvantage in close range, gets disarmed | Ctrl+click strips the pistol — no intent switch needed, resolves instantly |
| 6 | Fight ends with a grab | Alt+click restrains rather than continuing to trade damage |

## 8. Out of scope for this pass

- Armor system
- Death, cloning, and respawn
- Weapon crafting or attachments
- Skill/training progression affecting accuracy

