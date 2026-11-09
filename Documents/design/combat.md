# Combat — design document

> Status: active

Builds directly on two things already spec'd in `main-hud.md`: zone targeting (§6 — raycast aiming, seven zones, reticle confirm) and the intent/combat verb chording scheme (§7 — help/harm toggle, Ctrl disarm, Alt grab). Neither of those docs decided what happens once a hit actually lands. This one does.

## 1. Design philosophy

Same thread this whole project has been pulling on since the hacking interface doc: failure should trace to something physical and visible, not an invisible percentage roll. That doc's rule was "you fail a hack because you guessed the wrong protocol, not because a roll went against you." Zone targeting already pushed combat in that direction — you're aiming a raycast at a real body part, not clicking an abstract doll. This doc has to decide whether that holds all the way through to the hit itself.

It splits by weapon class, deliberately:

- **Melee is fully deterministic.** There's no "miss cone" for a swing at arm's length; adding one would be RNG for its own sake. A swing misses only for a physical reason you can see: the target stepped out of reach during your windup.
- **Ranged uses a weapon-intrinsic accuracy cone, not a hidden stat check.** This is still "physical, not RNG" — the cone is a visible property of the weapon (recoil kick, reticle bloom) the player can see and manage, not an opaque roll against an invisible evasion stat. It also does real work: movement here is free and continuous, not tile-stepped, so without something governing ranged precision, combat collapses into pure snap-aim — an arena shooter, not the slower, higher-stakes combat SS13 is actually known for.

## 2. Melee combat

**Resolution:** deterministic, per §1. A Harm click always starts a swing. The hit resolves when the windup ends, against whatever zone is under the reticle at that moment and within contact range. If the target moved out of reach during the windup, the swing misses — and a miss still pays the full recovery. Committing to a swing is a decision, not a free probe. Zone selection reuses the main HUD doc's zone targeting (`main-hud.md` §6).

**Windup and recovery.** Each melee weapon has a telegraphed windup (visible wind-back before the swing lands) and a recovery window (brief vulnerability after). This is the pacing lever that keeps melee from being pure instant-spam-click — and it's a physical, visible mechanism (you can see the swing coming) rather than an invisible cooldown gate.

**Blocking.** Not in this pass. A timed block that the server must judge against a swing the attacker's client already saw is fragile under real latency, and the windup-and-miss rule above already rewards reading the swing and stepping back. Equipment blocking (a riot shield) returns in a later pass — see §11.

**Improvised weapons.** Worth explicitly preserving, not just tolerating — hitting someone with a fire extinguisher or a toolbox is exactly the kind of thing that passes this project's recurring "keep what's good regardless of engine" test, not a legacy artifact. Any held item can function as an improvised weapon at low base damage; dedicated tools (crowbar, wrench) sit a step above; purpose-built weapons above that.

**Damage.** Melee weapon defines a damage type and amount, applied to the zone the swing resolves against. Weapons deal **brute or burn**, per limb (`health.md` §2). Toxin and oxygen damage are systemic in health's model, never a per-limb weapon output — a poisoned blade applies its poison systemically on top of the per-limb brute. Armor sits between the hit and the limb (`armor.md` §2).

## 3. Ranged combat

**Resolution:** weapon-intrinsic accuracy cone, per §1. Every ranged weapon has a base spread. It widens with sustained fire (recoil climb), player movement (strafing/moving), and range (falloff past the weapon's effective distance). It narrows when standing still. (Bracing against cover or crouching is out of scope for this pass.) None of this is hidden from the player — it's readable through weapon handling feedback (recoil kick, reticle bloom), the same "reason about it, don't guess" principle the hacking interface's real schematics already established.

**Hitscan vs. projectile.** Small arms are hitscan — instant resolution within the accuracy cone, simple to reason about. Actual travel time is reserved for slower or thrown things (thrown weapons, heavy ordnance) where the travel time is part of the read.

**Cover and line-of-sight.** A wall blocks a shot exactly the way it blocks speech in `comms.md` §3: what you can't see, you can't hit, and what blocks your voice blocks your bullet.

**Damage.** Same as melee — weapon defines damage type and amount, applied to the resolved zone as per-limb brute or burn.

**Ammunition.** Guns feed from **magazines**. Swapping a magazine is a visible action that takes time, and an empty gun clicks rather than silently refusing. Examining a gun shows its loaded count (`examine.md` §5: externally readable state). Loose rounds and reloading a magazine round by round are out of scope for this pass — a magazine is the unit you carry, find and trade.

**Two-handed weapons.** Some weapons (rifles, heavy ordnance) need both hands: you can only fire them with your other hand free. Holding something in the off hand blocks firing, rather than firing with a hidden penalty. The rule is physical and visible — you can see what's in your hands.

## 4. Positioning and pacing — why free movement doesn't collapse into a twitch shooter

Free continuous movement (confirmed, not tile-stepped) makes combat inherently more twitch-capable than SS13's grid-stepped fights. The levers that keep it from fully tipping that direction:

- **Windup/recovery on melee, reload/cooldown on ranged** — both already covered above, both physical rather than hidden.
- **Lethality tuned for a handful of solid hits, not a DPS race.** If fights resolve quickly on real hits, positioning and decision-making matter more than sustained aim.
- **Disarm and grab do real tactical work here, not just flavor.** Against a ranged threat, disarm stripping their weapon is enormously valuable precisely because ranged accuracy is otherwise fairly forgiving up close. Against a melee opponent, grab controls their positioning without needing to out-swing them. Both were already spec'd as modifier+click actions in the main HUD doc (§7) — this is just where that scheme starts paying for itself.
- **Stamina/exertion** governs sprinting, swinging, blocking, and sustained fire, and bridges into real oxy debt when pushed past empty — see `stamina.md`.

## 5. Stagger and knockdown

Hits interrupt, not just subtract. Two levels:

- **Stagger** — a heavy hit (a big melee weapon, a point-blank shot) briefly interrupts the victim: their current windup or action is cancelled and they stumble. A fraction of a second, no loss of control beyond that.
- **Knockdown** — a shove (disarm, `main-hud.md` §7), a stun baton or taser hit (below), collapsing from exhaustion (`stamina.md` §3) or a nearby blast (`explosives-destruction.md` §1) puts the victim on the ground for roughly **2–3 seconds**. They drop to prone visibly, get a HUD alert (`main-hud.md` §9), and stand back up when it ends. Knockdown is the window where a disarm, a grab or a finishing blow lands.

Knockdown is always legible: the victim sees themselves fall and sees when they can stand, and everyone around sees a body on the floor. Critical-state collapse (`health.md` §4) is a different, longer state, not a knockdown.

**Stun baton.** Security's signature weapon, and its non-lethal one. It is an ordinary melee weapon under §2 (windup, recovery, deterministic hit), except that a charged hit knocks the target down as above instead of mainly dealing damage. It holds a charge that each knockdown hit spends; empty, it is a club. Armor doesn't make anyone immune: an operative in a hardsuit goes down like anyone else. A stun is a window for whoever follows up, not an arrest — restraints are out of scope for this pass (§11).

**Taser.** The baton's ranged counterpart, and the other half of security's non-lethal kit. A hit on an unarmored body knocks it down as above and drains most of its stamina; it does no brute or burn damage and never harms structures. Unlike the baton, it is stopped by rigid armor: the prongs can't reach skin through a hardsuit, so a hit on a zone covered by an intact hardsuit piece only drains part of the stamina. A spent piece (`armor.md` §2: integrity at zero) no longer stops it, and a hardsuit without its helmet leaves the head open. So against an operative the taser is a follow-up once the suit has been damaged, not an opener. Stunning at range is strong, so the taser is limited by charges rather than fire rate: a few shots, enough for one engagement with a miss or a second target, then a long recharge. Repeated hits keep a body down for the knockdown time but do not stack it. Its exact numbers are a playtest question; the interim values live in the weapon's profile.

## 6. Friendly fire and telling sides apart

Friendly fire is **always on**, as in SS13. A bullet doesn't know whose side you're on, and a crowded corridor is genuinely dangerous to shoot into — that's part of the ranged-combat read in §3.

Telling sides apart is the player's job, using what's physically visible:

- **Outfit** — a team's gear looks like that team's gear (an operative's hardsuit, security's armor).
- **Name on examine** — the identity on a worn ID (`id-access.md` §3, `examine.md` §5).

There are **no coloured friend-or-foe overlays or nameplates**. A disguise works because nothing on the HUD gives it away.

## 7. What this doesn't redesign

Zone targeting (seven zones, raycast, reticle confirm) and the intent/combat-verb chording (help/harm toggle, Ctrl disarm, Alt grab) are unchanged — see `main-hud.md` §6 and §7. This doc only defines what a weapon does once those systems have already decided *where* and *whether* an action is happening.

## 8. Open questions — explicitly deferred, not designed here

- **Skill/training modifiers to accuracy.** Not assumed to exist. If a skill system exists elsewhere, it would modulate a weapon's base cone — this doc doesn't invent one.
- **Death and critical-state resolution.** The main HUD doc covers the screen-space feedback (vignette, heartbeat) for critical/dying — what happens after is a distinct, very SS13-specific system, designed in `death-cloning-respawn.md` (cloning/respawn) and `observer.md` (the ghost you become).

Armor mitigation per zone is designed in `armor.md`.

## 9. Worked example — melee vs. ranged skirmish

| Step | What happens | Combat state |
|---|---|---|
| 1 | Two crew members fight in a corridor — one has a wrench, one has a pistol | Melee attacker within contact range, ranged attacker holding distance |
| 2 | Melee attacker swings | Visible windup telegraphs the attack; if the target is still in reach when it ends, deterministic hit on the aimed zone |
| 3 | Ranged attacker fires while backing away | Movement widens their accuracy cone — shot has real chance to miss even though aim looked right |
| 4 | Melee attacker closes the distance and lands a solid hit | Recovery window after the swing briefly opens them up |
| 5 | Ranged attacker, now at a disadvantage in close range, gets shoved | Disarm knocks them down for a couple of seconds (§5); the pistol hits the floor |
| 6 | Fight ends with a grab | Alt+click restrains rather than continuing to trade damage |

## 10. Integration notes

- `armor.md` — sits between a resolved hit and the limb.
- `health.md` §2 — per-limb brute and burn; toxin and oxy are systemic.
- `stamina.md` — exertion for swinging, sprinting and sustained fire; exhaustion widens the reticle (§4) and can knock you down (§5 here).
- `examine.md` §5 — magazine count and visible weapon state.
- `id-access.md` §3 — the name others read when deciding whose side you're on.

## 11. Out of scope for this pass

- Timed and equipment blocking (riot shields)
- Bracing and crouching for accuracy
- Loose rounds and reloading magazines round by round
- Restraints (handcuffs) and arrest — they belong with an open station round, where a stunned suspect is taken to the brig rather than finished off
- Other ranged stun weapons (disabler, stun shotgun)
- Weapon sway as an exhaustion read (needs aim IK — see `stamina.md` §4)
- Death, cloning, and respawn (see `death-cloning-respawn.md`)
- Weapon crafting or attachments
- Skill/training progression affecting accuracy

