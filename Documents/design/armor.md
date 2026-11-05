# Armor — design document

> Status: active

Resolves the armor question `combat.md` originally left open, and covers both combat armor and environmental protective gear, since they turn out to hook into different tiers of `health.md`'s damage model rather than being one mechanic wearing two names.

## 1. Design philosophy

Same split the health doc already drew: physical damage is local, systemic damage is regulated. Armor follows the same line.

- **Combat armor** answers "how much of this hit gets through" — it sits between a weapon hit and the existing per-limb brute/burn model, and it's a flat, per-zone, per-damage-type absorption value. Fully legible: this weapon does 15, this armor absorbs 8, 7 gets through. Same "visible property, not a hidden roll" rule the accuracy cone already established for offense, applied here to defense.
- **Environmental protective gear** answers a different question — "does exposure happen at all" — and it isn't a per-hit thing, because ambient hazards (vacuum, toxic atmosphere) aren't discrete hits. It's a seal: intact or not. Binary, not graduated, and just as legible. Breathing is a separate layer: internals supply it from a finite tank (§3), so a suit keeps the vacuum out and a tank keeps you alive inside it.

Heat and cold are the one place exposure isn't binary. Standing in a burning room or a freezing one isn't a series of hits — it's a steady exposure that arrives every second on every part of the body. A flat per-hit absorption value can't model that: any piece that absorbs a little burn would make the wearer immune to a fire while its integrity drained to nothing every tick. So **environmental protection is its own property**, separate from combat absorption: a piece's heat, cold and pressure protection **scales environmental exposure down** for the zones it covers (§3). A fire suit is good at that and poor at stopping a knife; a ballistic vest is the reverse. A flamethrower's burst is still a hit and goes through combat absorption like any other weapon. This follows SS13, where clothing carries heat and cold protection separately from its armor values.

## 2. Combat armor

**Coverage:** per-zone, reusing the existing seven zones (head, chest, l_arm, r_arm, l_leg, r_leg, groin). A piece covers one or more zones — a helmet covers head, a vest might cover chest and groin, a suit might cover everything.

**Absorption:** flat value per damage type (brute, burn) per zone covered. Resolution order on a hit: weapon damage → zone's armor absorption for that damage type → remainder applied to the per-limb model. `damage_applied = max(0, incoming_damage - absorption)`.

**Layering.** When several worn pieces cover the same zone, the hit meets them **outside-in**: the outermost piece absorbs first and wears first, and only what gets past it reaches the next layer. A hardsuit over a vest protects the vest until the hardsuit is spent. Debris from a blast is a hit like any other and goes through the same absorption.

**Why different pieces resist different types differently.** This isn't an arbitrary stat spread — it's the same material logic real armor has. A ballistic vest is built to stop blunt/kinetic force and does little against fire; a fire suit is the reverse. Grounding the numbers in a real physical property keeps this consistent with everything else in the project instead of being balance-table flavor.

**Durability.** Each absorbing piece has an integrity value. A hit depletes it by the amount actually absorbed (`min(incoming_damage, absorption)`), not the full incoming damage. At zero integrity, the piece stops absorbing — broken, not gradually worse, since a wear curve is a tuning question, not a design one. Rendering that wear on the model (scorches, dents, tears) is a later pass (§8); for now integrity is read on examine (§6).

**Improvised/partial protection.** Nothing here requires purpose-built armor specifically — a thick jacket or a welding mask could plausibly have small, non-zero absorption values, the same way an improvised melee weapon has small non-zero damage in the combat doc. Not designing exact values here, just noting the door should stay open.

## 3. Environmental protective gear

**What it protects against:** ambient exposure that would otherwise hit the body directly:

- **Vacuum and thin atmosphere** — feeds oxy debt (the lungs have nothing to take in) and damages the lungs themselves from low pressure.
- **Toxic atmosphere** — feeds toxin.
- **Heat and cold** — burn on every exposed zone, scaled down by the environmental protection of whatever covers that zone (§1).

This is the health doc's systemic tier and lung function (`health.md` §3) being fed by environment instead of, or in addition to, wounds.

**The seal.** A working seal blocks the pressure and cold effects listed above, and scales heat and fire down without removing them (§1). It does not supply air. It is sealed only if every zone with required coverage is intact and equipped — a full seal needs both a sealed suit body *and* a sealed helmet; missing or breaching either one breaks the whole seal, not just that zone's local protection. This matches the real logic of a suit: a hole anywhere depressurizes the whole thing.

**Internals.** A tank and something to breathe through: a mask, or a sealed helmet, which counts as one. Internals are switched on deliberately, not automatically when the gear is worn, and forgetting them is a mistake the player can make. While on, you breathe what is in the tank instead of the room, which supplies oxygen and keeps toxic gas out, so a plasma tank poisons you. While off, you breathe the room, even in a sealed suit; indoors that carries no penalty, and in vacuum or toxic air you need them on.

A tank works from suit storage, a pocket if it is small enough, or a hand, but not from inside a bag (`inventory-storage.md` §3). It holds a finite supply: a full tank should rarely run out on an operative, and a small emergency tank puts crew on a real clock. Running out stops the breath supply and nothing else; the seal holds and you suffocate. Depletion, like a breach, is deterministic and player-visible, not a hidden failure roll. A mask alone connects to a tank and filters nothing.

**What each combination protects:**

| Worn | Pressure and cold | Breath and toxic gas | Heat and fire |
|---|---|---|---|
| Nothing | Exposed | Room air | Exposed |
| Mask and tank, internals on | Exposed | Protected | Exposed |
| Sealed suit and helmet, internals off | Protected | Room air | Reduced |
| Sealed suit and helmet, internals on | Protected | Protected | Reduced |
| Breached suit, internals on | Exposed | Protected | Exposed |

A mask and tank without a suit buy time rather than safety: lungs and skin still take damage in vacuum.

**Breach — the connective mechanism between the two systems.** If a combat hit's damage exceeds a zone's remaining absorption (some damage gets through to the limb), the seal of the **outermost sealed piece** covering that zone is punctured at the same moment. No separate breach chance to roll — "the armor didn't fully stop it" and "the seal didn't fully hold" are the same event, read off the same absorption check. This is the one place combat armor and environmental gear genuinely interact rather than running in parallel. A breach removes the seal and the environmental protection that came with it, and nothing else: internals keep working, even through a holed helmet, so a breached wearer has time to retreat.

**Magboots.** Boots that anchor the wearer against moving air (`atmospherics.md` §2). They are switched on and off. While on and upright, wind cannot knock you down or pull you. The cost is mobility: you move slower and cannot sprint. Anything else that puts you on the floor ends the protection, and the wind can then take you, so a shot or a stun stays dangerous beside a breach. Magboots do nothing over open space.

## 4. Weight and stamina

Every piece has a weight value. Weight is one of the stamina inputs `stamina.md` §2 defines (alongside organ function and blood volume): what you carry lowers both your stamina cap and your regen, not a separate mechanic. Heavier gear generally correlates with more coverage and higher absorption/seal reliability, which is the actual tradeoff this is meant to create: full protection costs mobility, exactly the way it would physically. This needs no new system, just one more input into a function that already exists.

## 5. What this doesn't design

- **Equip slots / the inventory screen.** How a player equips a helmet or suit is `inventory-storage.md`'s job — not re-litigated here.
- **Radiation/genetic protection.** Not designed, because that damage type itself was never confirmed to exist — `health.md` §9 already left it explicitly out of scope. If it gets added later, it would plug into the combat-absorption model the same way burn does.
- **Repair/refill mechanics.** Patching a breach or refilling a tank is a plausible follow-up, not detailed here.

## 6. HUD touchpoints

No new permanent chrome — same discipline as everything else in this project.

- **Armor is visible on the character model** — a worn helmet or vest renders on the mesh. Rendered wear is a later pass.
- **On-demand detail** — examining a piece shows its integrity, and for sealed gear its seal status and remaining resource (`examine.md` §5: externally readable state).
- **Two new alert-stack entries**, following the existing hidden-until-relevant pattern: a low-resource warning (tank running low) and a breach warning (seal just failed) — both acute and actionable, same job the "Bleeding" chip already does.
- **An internals chip**, hidden until you wear a tank with a mask or sealed helmet. It shows whether internals are on, and clicking it toggles them; a rebindable key does the same.
- **Sound carries the rest.** You hear your own breathing while internals are on. Magboots clunk when toggled and change your footsteps while active.

## 7. Worked example

| Step | What happens | State |
|---|---|---|
| 1 | Crew member in a sealed suit takes a shotgun blast to the chest | Vest absorbs its flat brute value; remainder applies to the chest's brute pool |
| 2 | The hit exceeds the vest's remaining absorption | The suit's chest seal punctures at the same moment — no separate roll |
| 3 | Player is now in a depressurizing area | Lung damage and cold start from environmental exposure, on top of whatever the hit itself caused; internals keep supplying breath, so no oxy debt from the vacuum |
| 4 | Player retreats to a sealed room and changes into an intact suit | Environmental exposure stops the moment a seal is restored (repair mechanics not detailed here) |
| 5 | Same player, fully armored, sprints to help a friend | Stamina cap is lower and regen slower than an unarmored character's would be, per §4 |

## 8. Out of scope for this pass

- Exact numeric absorption, weight, and tank-capacity values (balancing, not design)
- Repair and resource-refill mechanics
- Radiation/genetic damage protection
- Filtering masks that clean the air without a tank
- Tanks that rupture or explode when damaged
- Moving through open space or across the outer hull, on magboots or otherwise
- Armor crafting or modification
- Equip-slot UI (see `inventory-storage.md`)
- Wear rendered on the model (scorches, dents, tears)

