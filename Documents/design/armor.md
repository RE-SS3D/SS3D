# Armor — design document

> Status: active

Resolves the open question flagged in `combat.md` (§6, §8), and covers both combat armor and environmental protective gear, since they turn out to hook into different tiers of `health.md`'s damage model rather than being one mechanic wearing two names.

## 1. Design philosophy

Same split the health doc already drew: physical damage is local, systemic damage is regulated. Armor follows the same line.

- **Combat armor** answers "how much of this hit gets through" — it sits between a weapon hit and the existing per-limb brute/burn model, and it's a flat, per-zone, per-damage-type absorption value. Fully legible: this weapon does 15, this armor absorbs 8, 7 gets through. Same "visible property, not a hidden roll" rule the accuracy cone already established for offense, applied here to defense.
- **Environmental protective gear** answers a different question — "does exposure happen at all" — and it isn't a per-hit thing, because ambient hazards (vacuum, toxic atmosphere) aren't discrete hits. It's a seal: intact and resourced, or not. Binary, not graduated, and just as legible.

One mechanism unifies more of this than it first looks like it should: thermal/fire protective gear (a fire suit) doesn't need its own system — burn from an environmental fire and burn from a flamethrower are the same damage type, so a fire-resistant zone piece just needs high burn absorption in the combat-armor model, and it works against both sources for free.

## 2. Combat armor

**Coverage:** per-zone, reusing the existing seven zones (head, chest, l_arm, r_arm, l_leg, r_leg, groin). A piece covers one or more zones — a helmet covers head, a vest might cover chest and groin, a suit might cover everything.

**Absorption:** flat value per damage type (brute, burn) per zone covered. Resolution order on a hit: weapon damage → zone's armor absorption for that damage type → remainder applied to the existing per-limb model. `damage_applied = max(0, incoming_damage - absorption)`.

**Why different pieces resist different types differently.** This isn't an arbitrary stat spread — it's the same material logic real armor has. A ballistic vest is built to stop blunt/kinetic force and does little against fire; a fire suit is the reverse. Grounding the numbers in a real physical property keeps this consistent with everything else in the project instead of being balance-table flavor.

**Durability.** Each absorbing piece has an integrity value. A hit depletes it by the amount actually absorbed (`min(incoming_damage, absorption)`), not the full incoming damage. At zero integrity, the piece stops absorbing — broken, not gradually worse, since a wear curve is a tuning question, not a design one. Wear should render on the model (scorches, dents, tears) the same way wounds do — the piece **is** part of the character model, so it gets the same "let the world carry the information" treatment as everything else.

**Improvised/partial protection.** Nothing here requires purpose-built armor specifically — a thick jacket or a welding mask could plausibly have small, non-zero absorption values, the same way an improvised melee weapon has small non-zero damage in the combat doc. Not designing exact values here, just noting the door should stay open.

## 3. Environmental protective gear

**What it protects against:** ambient exposure that would otherwise tick the health doc's systemic pools directly — vacuum/thin atmosphere (feeds oxy debt, since lungs have nothing to take in), and toxic/hazardous atmosphere (feeds toxin). This is the health doc's organ-regulated systemic tier being fed by environment instead of, or in addition to, organ damage.

**The seal.** A hazard type (pressure/oxygen, toxin/gas) is sealed only if every zone with required coverage is intact and equipped — a full pressure seal needs both a sealed suit body *and* a sealed helmet; missing or breaching either one breaks the whole seal, not just that zone's local protection. This matches the real logic of a suit: a hole anywhere depressurizes the whole thing.

**Resource consumption.** A sealed system draws down a finite resource while active — an air tank, a filter charge. Running out fails the seal exactly like a physical breach does, just from depletion instead of damage. Both are deterministic and player-visible, not a hidden failure roll.

**Breach — the connective mechanism between the two systems.** If a combat hit's damage exceeds a zone's remaining absorption (some damage gets through to the limb), any seal that zone was providing is punctured at the same moment. No separate breach chance to roll — "the armor didn't fully stop it" and "the seal didn't fully hold" are the same event, read off the same absorption check. This is the one place combat armor and environmental gear genuinely interact rather than running in parallel.

## 4. Weight and stamina

Every piece has a weight value; total equipped weight is a second input into the stamina regen function `stamina.md` §2 already defines (alongside organ function and blood volume) — reduces stamina cap and/or slows regen, not a separate mechanic. Heavier gear generally correlates with more coverage and higher absorption/seal reliability, which is the actual tradeoff this is meant to create: full protection costs mobility, exactly the way it would physically. This needs no new system, just one more input into a function that already exists.

## 5. What this doesn't design

- **Equip slots / the inventory screen.** How a player actually equips a helmet or suit is part of the full inventory/backpack screen already flagged out of scope in `main-hud.md` §13 — not re-litigated here.
- **Radiation/genetic protection.** Not designed, because that damage type itself was never confirmed to exist — `health.md` §9 already left it explicitly out of scope. If it gets added later, it would plug into the combat-absorption model the same way burn does.
- **Repair/refill mechanics.** Patching a breach or refilling a tank is a plausible follow-up, not detailed here.

## 6. HUD touchpoints

No new permanent chrome — same discipline as everything else in this project.

- **Armor is visible on the character model** — a worn helmet or vest, and its accumulating damage/wear, render directly on the mesh, consistent with how wounds already work.
- **On-demand detail** extends the existing per-limb/organ readout (hold examine-self) with per-zone armor integrity and, for sealed gear, seal status and remaining resource.
- **Two new alert-stack entries**, following the existing hidden-until-relevant pattern: a low-resource warning (tank running low) and a breach warning (seal just failed) — both acute and actionable, same job the "Bleeding" chip already does.

## 7. Worked example

| Step | What happens | State |
|---|---|---|
| 1 | Crew member in a sealed suit takes a shotgun blast to the chest | Vest absorbs its flat brute value; remainder applies to the chest's brute pool |
| 2 | The hit exceeds the vest's remaining absorption | The suit's chest seal punctures at the same moment — no separate roll |
| 3 | Player is now in a depressurizing area | Oxy debt starts rising from environmental exposure, on top of whatever the hit itself caused |
| 4 | Player retreats to a sealed room and patches the suit later | Environmental exposure stops the moment the seal is restored (repair mechanics not detailed here) |
| 5 | Same player, fully armored, sprints to help a friend | Stamina cap is lower and regen slower than an unarmored character's would be, per §4 |

## 8. Out of scope for this pass

- Exact numeric absorption, weight, and tank-capacity values (balancing, not design)
- Repair and resource-refill mechanics
- Radiation/genetic damage protection
- Armor crafting or modification
- Equip-slot UI (covered by the already-deferred inventory screen)

## 9. Prototyping this

**Cursor, prompt 1 — data contract first:**
> Here's the armor design doc. Define the armor-piece record (zone coverage, per-damage-type absorption, integrity/durability, weight, and — where applicable — seal type and resource capacity). Define the hit-resolution insertion point: armor absorption applies between the existing combat hit resolution and the per-limb damage model, and define the breach check (absorbed damage exceeds remaining absorption → seal on that zone fails). Show me the data contract before wiring any specific item.

**Cursor, prompt 2 — one vertical slice:**
> Wire one combat armor piece (a vest covering chest, brute/burn absorption, integrity that depletes and eventually breaks) end to end against the existing hit resolution. Environmental sealing, weight/stamina interaction, and the alert-stack additions come after this is reviewed.
