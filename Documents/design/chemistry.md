# Chemistry — design document

> Status: active

Resolves two deferred items: the "anesthesia/sedation/chemistry system" `surgery.md` §6 and §11 explicitly assumed to exist without designing, and the antitoxin/field-treatment entries `health.md` §6 name-checks without defining what produces them. Builds on the systemic toxin/organ model (`health.md` §2–§3), the Tier 3 combine/use-with grammar (`main-hud.md` §8), the diegetic device-screen pattern and discovery-tier principle (`hacking-interface.md` §2–§4), ID/access gating (same doc, §3), Area's power derivation (`area.md` §5), and atmosphere as a system Area already decoupled from itself (`area.md` §4).

## 1. Design philosophy

Same recurring test as everything else in this project: does a mechanic trace to something physically real, or is it a legacy simplification wearing a science coat? SS13's classic chemistry is a fixed-ratio lookup table — combine exact quantities, the database returns an exact result — legible, but disconnected from *why* a reaction happens. This doc keeps the legibility and fixes the disconnect:

- **Reactions stay recipe-driven** — ratio-based, deterministic, no hidden roll. This is the same "visible property, not a hidden roll" rule the accuracy cone and armor absorption already established, just applied to a beaker instead of a weapon.
- **Some recipes require a genuine physical condition beyond ratio** — heat or a catalyst reagent — so chemistry isn't purely a lookup table. A recipe that needs heat needs a heater in the world, actually running, the same way a fabricator recipe needs actual power. This is the hybrid approach chosen over pure fixed-ratio or full thermodynamic simulation — reactions now also carry a real thermal profile (exothermic/endothermic) that writes back into this same temperature check (§5), without escalating into a full simulation.
- **Unknown reagents aren't free information.** Possessing a beaker of something doesn't mean knowing what it is — same discovery-tiers principle the hacking interface already established for unfamiliar hardware (§3–4 there): a raw hardware ID until fingerprinted, a reagent's visible properties until analyzed.
- **Bad mixing is a real hazard, not a safe non-event.** An incompatible combination or a badly missed ratio produces a genuine world consequence — foam, gas, fire, a blast — not a silent "nothing happens." This is "physical causation over hidden RNG" again: getting it wrong should look and feel like getting it wrong.
- **No parallel effect system.** A reagent doesn't carry its own bespoke "heals for X" stat sitting outside the health model. It's an input into the systemic pools (toxin, oxy, blood volume, organ function) `health.md` already defines — the same pools bandages, antitoxin, and CPR already read and write.

## 2. Reagents

A reagent record: name, category (medicinal, toxic, industrial/precursor, catalyst), physical state (liquid for this pass — see §13), a color (a real, always-visible property — see §7), an effect definition (which systemic pool(s) or organ function it touches, and how), a metabolism rate (how fast a dose in the bloodstream depletes), an overdose threshold, and a thermal profile — a reaction it participates in can be exothermic or endothermic, with a magnitude (see §5).

**Color is diegetic partial information, not full identification.** A player can always see a beaker holds a clear blue liquid — that's a property of the world, not hidden — but "clear blue liquid" and "this is antitoxin" are two different facts. The first is free; the second needs §7.

## 3. Containers and transfer

Beakers, bottles, syringes, patches, and pill casings are the physical vessels reagents live in — each with a capacity, holding one mixed volume of reagents rather than segregated compartments. Moving reagent between two containers, or dosing a patch/pill from a beaker, reuses the Tier 3 combine/use-with grammar unchanged: drag one container onto another, or arm it and click a target. No new interaction pattern — this is that grammar's job.

**A container is the reaction vessel.** When two incompatible or matching-recipe volumes end up sharing one container, resolution runs immediately per §4–§6. There's no separate "mix" button; pouring is the trigger.

## 4. Reactions

**Fixed-ratio recipes** define required reagents, their ratio, and a yield — deterministic given the inputs, same as a crafting recipe. Most reactions are this simple: right ingredients, right ratio, in they go.

**Condition-gated recipes** additionally require a real physical state at the moment of mixing — most commonly a temperature threshold (needs a heater actually running under the container) or the presence of a catalyst reagent that participates but isn't consumed. This is deliberately scoped narrow: not every recipe needs a condition, only the ones where "this actually requires heat" is a meaningful, teachable fact rather than busywork. A heater is a powered device drawing from its Area's APC exactly like a fabricator (`area.md` §5) — no separate power model for it. **A heat requirement doesn't care where the heat came from** — an external heater and the residual warmth left in a container by a prior exothermic reaction (§5) satisfy the identical check, since both are just the container's actual temperature at the moment of the pour.

**Resolution order on a pour:** check whether the resulting mixture matches a known recipe's ratio (and condition, if the recipe has one) → if yes, react to the defined yield and apply its thermal profile (§5) → if the ratio is close to a real recipe but the condition isn't met, or two reagents are flagged incompatible regardless of ratio, resolve as a bad mix (§6) instead of silently doing nothing.

## 5. Reaction thermodynamics

Every reaction — fixed-ratio or condition-gated — carries a thermal profile: exothermic (releases heat) or endothermic (absorbs heat), with a magnitude. This is the mechanism that makes "needs heat" in §4 a real physical fact rather than a flavor label on a recipe.

**A container tracks its own temperature**, starting at ambient (whatever the local atmosphere already provides, per the same black-box treatment atmospherics gets everywhere else in this project) and shifting by a reaction's magnitude the moment that reaction fires. Left alone, it decays back toward ambient over time — a rate, not a design decision. This one real number is read by §4's condition gates, written by every reaction that fires, and read again by the consequences below — one shared value, several consumers, not a parallel heat system bolted on beside the recipe system.

**Reaction chaining falls out of this for free.** Because a heat gate only cares about the container's actual temperature (§4), an exothermic reaction's own output can satisfy a second recipe's heat requirement without an external heater ever being involved — two reagents combine, the exotherm heats the container, a third reagent poured in immediately reacts against that residual heat. Nothing new to build for this; it's the same check reading the same number at a different moment.

**Runaway risk — correct doesn't mean safe.** An otherwise-correct exothermic reaction (right ratio, right condition) that pushes the container's temperature past that mixture's flash threshold triggers Ignition (§6) even though the recipe itself resolved exactly as intended. This is deliberate: getting the recipe right is necessary, not sufficient — walking away from a hot reaction, or stacking exotherms in the same container, is a real, learnable risk rather than something a correct recipe forecloses.

**Heat vents to the room, not just the container.** Once a container's temperature crosses a threshold, the excess heat bleeds into the local atmosphere — the same decoupled relationship every other doc already has with atmospherics (this doc's own gas-release consequence, a shuttle's hull breach, an electrical fault). A sufficiently hot reaction can measurably warm the room it's in, and if that room's atmosphere is already primed by something unrelated — a gas leak from a different mistake entirely — the vented heat is what ignites it. A real cross-system causal chain, not two systems that happen to share a word.

**Endothermic reactions are the mirror, and a real one.** A container's temperature can drop below ambient the same way it rises above it. Cold enough, it produces a visible frost/ice effect on the container and its immediate surroundings — an examinable, real cold object, not just a recipe flag. This isn't cryogenics — no cryo mechanic is designed here — but it's the exact physical primitive a future cryogenics pass would need (a real, trackable temperature value on a container), so that pass wouldn't have to invent one from scratch.

## 6. Bad mixes — real consequences

Three triggers produce a hazard event instead of an inert result:

- **Flagged-incompatible pairs** — reagents that are volatile together regardless of ratio (an oxidizer and a reducer, for instance).
- **A near-miss on a real recipe** — right reagents, wrong ratio, or right ratio missing a required condition.
- **A runaway exotherm** — a *correctly* fired reaction whose own heat output (§5) pushes the container past a flash threshold. Not a near-miss at all; the recipe worked exactly as intended and the danger showed up afterward.

The consequence is chosen by what's actually in the container, not a random roll off a table:

| Consequence | What causes it | Where it plugs in |
|---|---|---|
| Corrosive foam | Certain incompatible pairs | Local area-of-effect; damages exposed limbs on contact using the existing brute/burn model |
| Gas release | Volatile pairs, or an overheated condition-gated attempt | Changes local atmosphere composition — the existing atmospherics system Area already decoupled from itself (`area.md` §4), which feeds the health doc's toxin/oxy pools exactly the way a suit breach already does (`armor.md` §3) |
| Ignition | Combustible reagents crossing their flash condition, including a correct exotherm's own container temperature crossing that same point (§5) | Existing burn damage type; propagation is whatever fire system exists elsewhere, not redesigned here |
| Flash-freeze | The cold mirror of ignition — an endothermic near-miss, or a flagged-incompatible cold pair, dropping container temperature hard enough to burst into frost | Local area-of-effect; a visible ice/frost burst, cold exposure on contact — reuses whatever cold/environmental-exposure handling exists elsewhere in the health model, not a new damage type invented here |
| Small blast | A narrow set of genuinely explosive combinations | A lightweight, local, container-centered effect (short-radius brute damage, knockback) — not a general blast-physics system, which isn't assumed to exist and isn't designed here |

No separate "chemistry accident chance" stat exists anywhere — the mixture and its actual temperature (§5) are the only inputs, so a player (or a wiki) could in principle predict every bad mix from first principles, the same "reason about it, don't guess" standard the hacking interface's real schematics already set.

## 7. Identifying unknowns

A reagent's visible property (§2's color) is always free. Its name and effects are not, until analyzed:

- **Chemical analyzer** — a handheld tool, Tier 2 action on a container. Resolves instantly (no minigame), consistent with this project's stance that gating information behind a device beats gating it behind a puzzle.
- **Before analysis**, examine shows only color, approximate volume, and state — enough to reason about ("don't drink the unlabeled orange stuff"), not enough to know exactly what it does.
- **After analysis**, the container's contents are legible the same way any other identified item is — full name, and on hover/examine, its known effect category.

This mirrors the hacking interface's crew/engineer/hacker discovery split in spirit (`hacking-interface.md` §3–4) without needing three separate views — chemistry only needed the one axis: analyzed or not.

## 8. Bloodstream — feeding the existing systemic model

This is the integration point the rest of the doc exists to set up. Once a reagent enters the bloodstream (§9), it isn't a new pool sitting beside health's existing ones — it's an input into them:

- **Metabolism.** Each reagent in the bloodstream depletes at its own rate. While present, it continuously applies its defined effect.
- **Medicinal reagents** subtract directly from the brute, burn, or toxin pools `health.md` §2 already tracks — this is what antitoxin (already name-checked there, never defined) turns out to be: a reagent whose effect is "reduce toxin pool per tick while present," nothing more exotic.
- **Overdose is not a separate exotic state.** Crossing a reagent's dose threshold routes into the same toxin pool everything else poisons through — "too much of a good thing" and "poison" are mechanically the same event, the "one shared mechanism" principle already used elsewhere in this project rather than a parallel overdose system.
- **Sedation resolves surgery's deferred item.** A sedative reagent doesn't touch brain function (that's organ damage, and sedation isn't damage) — it pushes a separate, reversible consciousness-suppression value that gates against the same unconscious/critical threshold check `health.md` §4 already defines for brain function. Cross the threshold from either source and the character is unconscious; only the recoverability differs, and that difference is exactly correct — sedation wears off, brain damage doesn't. This is what makes surgery's "already unconscious or restrained" precondition (`surgery.md` §6) satisfiable through chemistry without inventing anything new there.

## 9. Delivery methods

- **Ingestion** (drinks, pills) — slowest onset; enters the bloodstream at a metabolism-gated rate rather than instantly, giving food/drink a real, if minor, timing cost.
- **Injection** (syringe) — a Tier 2 targeted action against an exposed zone; fastest onset, direct to bloodstream.
- **Patches** (transdermal) — applied to a zone, delivers passively over time. Sits in the same family as bandages and burn dressings in the health doc's field-treatment tier (§6 there) — a zone-applied item with a gradual effect, not a new interaction pattern.
- **IV** — a container attached and left running, continuous delivery over time. Reuses the existing "Blood transfusion / IV" field-treatment entry health §6 already names without defining the mechanism; the furniture/attachment specifics are out of scope (§13).

## 10. Production

- **Chem dispenser** — the wall-mounted source of base reagents. Reuses the diegetic device-screen pattern (`hacking-interface.md` §2): a tap list of unlocked base reagents with an amount selector, gated by the existing ID/access system (same doc, §3) rather than a bespoke chemistry-specific permission, and drawing power from its Area's APC exactly like every other powered device (`area.md` §5).
- **Reagent grinder** — same diegetic-screen family, a small machine that extracts reagent from an inserted item (a plant, a piece of food). Assumes those source items already exist as holdable objects; doesn't design botany or food itself.
- **Pill press** — same family again, packages a measured reagent volume into a pill casing rather than a beaker. No new interaction beyond selecting a fill amount and confirming.

None of these three invent a new machine-interaction pattern — they're all the fabricator/FDU screen wearing a chemistry-specific reagent list instead of a parts list.

## 11. HUD touchpoints

No new permanent chrome — same discipline as every other doc in this project.

- **Container contents** are legible via hold-examine, matching the existing on-demand readout convention: known contents show name and volume, unanalyzed contents show only color, approximate volume, and state (§7).
- **Container temperature is a free diegetic property, same as color.** Hold-examine shows a qualitative read — cool, warm, hot, frosted — without needing analysis (§7) to unlock it, and a hot or frosted container renders that state visibly on the object itself (steam, condensation, frost), the same "let the world carry the information" rule the rest of this project already follows.
- **Active bloodstream reagents** extend the existing organ-status readout (`health.md` §7) with a short list of what's currently metabolizing and roughly how much dose remains — same interaction, more revealed, no new panel.
- **Two new alert-stack entries**, following the existing hidden-until-relevant pattern: an overdose warning (toxin pool rising from dose rather than injury) and a sedation/unconscious-risk warning as the suppression value approaches the threshold in §8.
- **Dispenser, grinder, and pill press** are diegetic device screens only, rendered on the machine itself — no separate chemistry panel.

## 12. Worked examples

**A — Standard treatment:**

| Step | What happens | Chemistry state |
|---|---|---|
| 1 | Chemist dispenses base reagents at the chem dispenser, ID grants access | Screen shows the unlocked tap list; amounts selected and confirmed |
| 2 | Correct ratio poured into one beaker | Reaction resolves immediately to antitoxin, yield per the recipe |
| 3 | Antitoxin drawn into a syringe, injected into a poisoned patient | Reagent enters bloodstream at injection's fast onset |
| 4 | Antitoxin metabolizes | Toxin pool ticks down each tick it's present, per §8 — the same pool the poisoning raised |

**B — Bad mix:**

| Step | What happens | Chemistry state |
|---|---|---|
| 1 | Player pours two reagents that need a heat step into an unheated beaker, ratio otherwise correct | Condition isn't met — resolves as a near-miss, not the intended recipe |
| 2 | Mixture is also a flagged-volatile near-miss | Gas release triggers |
| 3 | Released gas changes local atmosphere composition | Feeds into the existing atmospherics system, which raises toxin/oxy debt on anyone nearby exactly as a breached suit already would |

**C — Sedation enabling field surgery:**

| Step | What happens | Chemistry state |
|---|---|---|
| 1 | No table available, patient conscious | Surgery's restrained-or-unconscious precondition isn't met yet |
| 2 | Chemist applies a sedative patch to the patient | Suppression value begins rising per the patch's delivery rate |
| 3 | Suppression crosses the consciousness threshold | Patient goes unconscious — same threshold check health §4 already runs for brain function, different source |
| 4 | Surgeon proceeds without needing the grab-restraint path | Surgery's deferred precondition (`surgery.md` §6) is now satisfied through chemistry |

**D — Identifying an unknown:**

| Step | What happens | Chemistry state |
|---|---|---|
| 1 | Security finds an unlabeled vial on a suspect | Examine shows only "clear liquid, ~10u" — no name, no effect |
| 2 | Vial is run through a chemical analyzer | Full identification resolves instantly, name and effect category now legible |

**E — Runaway exotherm:**

| Step | What happens | Chemistry state |
|---|---|---|
| 1 | Chemist mixes a condition-gated recipe correctly — right ratio, heater running | Reaction fires as intended, begins releasing heat per its exothermic profile (§5) |
| 2 | Chemist leaves the heater running instead of pulling the beaker once the reaction completes | Container temperature keeps climbing past the reaction's own flash threshold |
| 3 | Threshold crossed | Ignition triggers per §6 — despite the recipe itself having resolved correctly |

**F — Endothermic coolant, deliberate use:**

| Step | What happens | Chemistry state |
|---|---|---|
| 1 | Chemist mixes an endothermic recipe on purpose | Container temperature drops well below ambient per §5 |
| 2 | Frost visibly forms on the container | A real, examinable cold object — free diegetic information, per §11, same as color |
| 3 | (A future cryogenics pass, not designed here) reads this same container-temperature value | No new primitive needed when that pass happens — §5 already put one in place |

## 13. Out of scope for this pass

- Exact reagent list, ratios, dose thresholds, and metabolism rates (a balancing pass, not a design decision)
- Exact thermal numbers — reaction magnitudes, flash thresholds, ambient decay rate (§5) (a balancing pass, not a design decision)
- Cryogenics as a player-facing system — §5 only establishes the real container-temperature primitive a future pass would need, doesn't design what that pass does with it
- General blast/explosion physics, if a dedicated system doesn't already exist elsewhere — chemistry's own explosive mixes assume a lightweight, local, container-centered effect rather than a redesign of blast mechanics generally
- Gas diffusion/propagation specifics — assumed to already be atmospherics' domain (`area.md` §4), not redesigned here
- Botany/plant growth and food preparation as reagent sources — the grinder assumes harvestable/cookable items already exist as objects, doesn't design where they come from
- Gas and solid reagent states — this pass designs liquid only; a plausible future extension, not a hard limitation of the model
- IV furniture specifics (bed/rack attachment, drip rate UI) — the delivery method itself is defined in §9, the hardware isn't
- Disease/infection interactions — already flagged as a separate future system in `health.md` §9
- Chemist-specific job/access balancing (exactly which reagents which access level unlocks)
