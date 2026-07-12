# Atmospherics — design document

> Status: active

Gives a real spec to a system every other doc has been referencing without ever designing: chemistry's gas release (`chemistry.md` §5), armor's suit breach (`armor.md` §3), explosives' venting on wall breach (`explosives-destruction.md` §4), and shuttles' hull breach (`shuttles.md` §6) all point at "the existing atmospherics system" as a stub. Area §4 deliberately excluded atmosphere as a consumer of its own boundary system, reasoning that gas simulation needs its own dynamic zone lifecycle — this doc is that lifecycle. Builds on and formalizes the working tile-based ECS/DOTS implementation already built (ideal-gas-law pressure equalization, specific-heat-driven heat exchange, Burst-driven, tied directly to tilemap change notifications), then extends it to cover liquid and solid phase and a real pipe network, closing chemistry's deferred "gas and solid reagent states" item (`chemistry.md` §12) and giving chemistry a two-way relationship with atmosphere instead of a one-directional gas dump.

## 1. Design philosophy

The recurring test, aimed at a system that already exists in code rather than only on paper: is the current implementation a real simulation, or a shortcut wearing a science coat? Per-tile ideal-gas-law equalization and specific-heat-driven heat exchange pass that test on their own — this doc's job is formalizing and extending what's already correct, not replacing it.

- **Real thresholds, not authored flags.** Every reagent gets a real boiling point and freezing point, the same "visible property, not a hidden roll" standard chemistry's bad mixes already set. A reagent's phase behavior is something a player could in principle predict from first principles, not a category tag someone decided.
- **One shared substance record across every phase.** A reagent isn't redefined per system — the same record chemistry already tracks gains the two fields this doc needs, feeding both docs off one source of truth.
- **Two real techniques, not one stretched to cover both.** An open room is diffusion-limited; a sealed, pump-driven pipe is bulk flow. Forcing one model to cover both would misrepresent whichever case it wasn't built for.
- **Telegraphed catastrophe over instant catastrophe.** Same standard the reactor's meltdown curve and explosives' structural stages already run — a bad pipe connection is a spreading, interruptible event, not an unlogged bomb the moment it completes.

## 2. Existing simulation — open-tile diffusion

The layer already built and already correct: each tile carries a flexible-count gas record (no hard cap on the substance list), with pressure equalizing between adjacent tiles per the ideal gas law and heat equalizing per each gas's own specific heat. Runs in ECS/DOTS with Burst for performance, integrated directly into the tilemap rather than as a shadow structure — tile state changes (a wall going up, a door opening) trigger recomputation via tilemap notifications rather than polling. Four default gases exist today (oxygen, nitrogen, CO2, plasma), but the substance list itself isn't fixed at four — it's whatever's flagged phase-capable (§3).

This is the formal spec for what every other doc has been calling "the existing atmospherics system." The rest of this doc extends it; nothing here replaces the pressure/heat equalization already working.

## 3. The substance record — one shared shape across every consumer

Extends chemistry's reagent record (`chemistry.md` §2: name, category, color, effect target, metabolism rate, overdose threshold) with two fields this doc requires: **molar mass** and **phase-transition thresholds** (boiling point, freezing point) — both real numbers, not authored flags.

Every reagent gets these fields. Most medicinal reagents simply have a boiling point set far above anything the station ever reaches — they never appear in a GasBuffer (§4) in practice, not because they're forbidden to, but because nothing in a normal round gets them that hot. This is what keeps the baseline gases dominant without an authored allowlist: it's an emergent fact of the numbers, not a restriction (companion edit, §11).

Color, effect target, and the analyzer-gated discovery tier (`chemistry.md` §6) are unchanged and apply identically regardless of which buffer a reagent currently sits in.

## 4. Phase — three buffers, one tile

A tile's atmospheric state is three separate buffers — **GasBuffer**, **LiquidBuffer**, **SolidBuffer** — each a sparse list of (substance, amount) entries for whatever's actually present. Sparse by construction: most tiles carry a handful of GasBuffer entries and nothing else, regardless of how large the total substance list grows.

Kept as three buffers rather than one buffer with a phase field, because the math per phase is genuinely different — ideal-gas-law equalization, threshold-overflow diffusion, and static-until-triggered respectively — and a single generalized buffer would force every equalization job to filter by phase before doing anything, pure overhead on a hot path.

- **GasBuffer** — moles per substance, existing pressure/heat equalization (§2) unchanged.
- **LiquidBuffer** — pooled volume per substance. Spreads via a simpler overflow-threshold rule (a tile above some volume pushes the excess to lower-volume neighbors) reusing the same tile-adjacency job structure gas equalization already runs — same technique, cheaper math, not a new one.
- **SolidBuffer** — a static deposit, no diffusion, no continuous read. Set once on a phase-transition event (§5), cleared by a cleanup interaction or a reverse transition. **Distinct from disposal's solid items** (`disposal.md`) — this is reagent residue sitting on a tile, not a transiting object. Same word, unrelated system, worth keeping separate on purpose.

## 5. Phase transitions

Every transition is temperature-driven, reading the same real per-tile temperature value heat equalization already computes — no second thermal model anywhere in this doc.

- **Evaporation** — a LiquidBuffer entry above its boiling point at the tile's current temperature moves into the GasBuffer. This is also what bounds a puddle: without cleanup, most eventually evaporate rather than sitting as a permanent decal.
- **Condensation** — the reverse: a GasBuffer entry cooling below its boiling point precipitates into the LiquidBuffer.
- **Freezing / deposition** — a Liquid or Gas entry crossing its freezing point moves into the SolidBuffer.
- **Melting / sublimation** — the reverse, crossing the same threshold from the other direction.

No new machine-interaction pattern for any of this — it's a background per-tile check each tick, the same unconditional way heat equalization already runs.

## 6. Pipe networks — bulk flow vs. open diffusion

Two genuinely different physical regimes: an open room is diffusion-limited (real, but slow over distance); a sealed, pump-driven pipe is bulk flow, which at game-tick timescales behaves close to one shared, well-mixed volume. Modeling them differently isn't a shortcut — it's the physically correct call for each case.

**GasPipeNetwork / LiquidPipeNetwork** — a connected component of pipe-segment tiles (one tile per segment, matching the existing grain), holding one pooled mixture and total volume. The same connected-component technique Area's flood fill, lighting's bleed-through, electricity's backbone, disposal's network, and explosives' blast traversal already establish, applied here to bulk substance flow instead of boundaries, light, current, items, or force.

- **Topology only recomputes on a physical event** — a segment welded or cut — the same local-recompute-on-change discipline electricity's graph and Area's boundary already run.
- **Gas and liquid infrastructure are separate physical pipe types**, not one pipe carrying either — matches real plumbing/atmos engineering (different pressure ratings, different valves) and keeps failure legible at a glance: which kind of pipe failed tells a player what was in it, without inspecting contents. Running chem output into atmos-rated pipe is a deliberate, visible act of misuse, not an ambiguous data state.
- **No SolidPipeNetwork.** Nobody deliberately pumps a solid; solid phase inside a network is a failure mode of the other two (§8), not infrastructure of its own.

## 7. Pumps — flow as a function of differential

Two real stats on a pump, not one flat rate:

- **Rated max flow** — moles or volume per tick under ideal, no-backpressure conditions. The property a better pump upgrades.
- **Max differential capacity** — how much backpressure the motor can push against before stalling.

Actual flow is computed from both against the real pressures already known on each side — the network's pooled pressure, and the room tile's open-diffusion pressure at the interface — tapering toward zero as the two sides equalize, stalling outright if backpressure exceeds the motor's rated differential. No separate flow formula invented; this reads off the same pressure values ideal-gas-law equalization already produces.

**A pump's gauge is a physical readout on the device itself** — current flow, and how hard it's working against differential — same "world-carries-the-information" convention the reactor's heat gauge already sets (`electricity.md` §2), not a hidden number.

This also doubles as the intended countermeasure for a hostile feed (§12A): pressurizing the receiving side against an unauthorized pump throttles or stalls it without needing a switch — the same detectable-not-prevented shape AI laws and hacking's consequences already run.

## 8. Pipe temperature & failure modes

A pipe segment's temperature derives from the ambient tile it physically runs through — a run through an unheated corridor or an exterior wall is cold on its own, no special-casing required — plus an optional heat exchanger device that actively pulls a segment's temperature away from ambient. Same shared temperature field as everything else in this doc, gaining a new consumer, not a second thermal model.

- **Clog.** A substance crossing its freezing point inside a sealed segment solidifies in place — §5's freezing transition, same mechanism, occurring inside a pipe instead of on a room tile. Flow stops at that segment; a pump or gauge upstream reads the pressure/flow drop as a real, inspectable symptom. Clearing it means reheating the segment (a heat exchanger, or a handheld torch) or cutting it open — the same tool-based repair convention electricity's cable splicing already uses (`electricity.md` §6).
- **Rupture.** The mirror case — a liquid-carrying segment running too hot boils with nowhere for the expanding gas to go, building pressure toward a real breach rather than a quiet vent. A ruptured segment becomes a leak interface at that one tile, venting into open-tile diffusion scoped to that tile — the same Cracked/Destroyed venting-before-catastrophe pattern explosives already established for walls (`explosives-destruction.md` §3), applied to a pipe instead.

Deliberately a matched pair, both temperature-driven, opposite direction — kept symmetric rather than designed as two unrelated hazards.

## 9. Reactions crossing a junction

A network's pooled contents is describable as an instance of the Container primitive (`inventory-storage.md` §2: capacity, contents, a mixed volume) — the same shape a beaker, a crate, and a disposal unit already turned out to be. Chemistry's reaction resolution (`chemistry.md` §4–5: ratio check → condition check → yield, or near-miss/incompatible-pair → hazard) needs no pipes-specific rewrite — it's the same function, a new caller.

Two full networks merging and instantly reacting their entire pooled volume would be physically defensible under §6's well-mixed bulk-flow assumption — but deliberately not what this doc specifies, for the same reason structural damage is graded across stages instead of a binary wall: an instant, station-scale reaction gives no one a chance to notice or interrupt it, breaking the "diegetic warning before catastrophe" standard this project runs everywhere else.

Instead: **the junction itself is the reaction vessel, not the full network.** A valve or cross-connection has its own real flow capacity — the same pressure-differential mechanics as a pump (§7) — and reagents cross at that limited rate. Chemistry's resolution runs continuously on whatever volume has crossed; the yield or hazard enters each side's pool at that same rate rather than being stirred instantly through the whole volume. A cross-connected line reads as a spreading, worsening event an engineer can spot and valve off mid-reaction — not a bomb that goes off the moment a connection completes.

## 10. Rendering

Same performance discipline the existing simulation already runs on: no per-tile GameObjects or sprites. A Burst job writes per-tile density/composition into a texture or compute buffer; one shader samples it per pixel, with neighbor sampling for a soft edge so phase boundaries don't read as grid-snapped squares. Cost is one draw call regardless of how many tiles are affected, not N.

- **Gas** — a volumetric-fog look, density-driven opacity with a vertical falloff derived from molar mass, so a heavy gas like plasma visibly pools low and a light one drifts and thins. The falloff is a shader parameter reading real molar-mass data the substance record already carries (§3) — a rendering trick faking a third dimension on a flat, single-level station, not a second simulation.
- **Liquid** — a flat puddle/film shader off the same texture-write approach, volume-driven size and opacity, smoothstep-blended edges between neighboring wet tiles.
- **Solid** — a simple decal toggled on the SolidBuffer's presence, no continuous buffer read, since nothing about a solid deposit changes between transitions.

Three renderers, one shared tile anchor and write pattern — the same "one shared technique, phase-appropriate consumer" shape this doc runs at the data layer, carried through to rendering.

## 11. Integration notes

| Doc | Section | What changes |
|---|---|---|
| `area.md` | §4 | "The existing atmospherics system" now has a real spec to point at; Area's decoupling reasoning (different lifecycle, code-adjacent not code-coupled) stands unchanged. |
| `chemistry.md` | §2 | Reagent record gains molar mass and boiling/freezing point fields. |
| `chemistry.md` | §5, §12 | "Gas diffusion... atmospherics' domain" and "gas and solid reagent states... a plausible future extension" are both resolved by this doc. |
| `electricity.md` | §7 | "Wet/conductive interaction... if in scope elsewhere" is now in scope — a tile's LiquidBuffer presence is the physical trigger for a compromised-ground shock hazard. |
| `explosives-destruction.md` | §4 | Breach-into-atmosphere already assumed this doc; now resolves against a real spec. |
| `shuttles.md` | §6 | Same — hull breach into atmosphere now resolves against a real spec. |
| `armor.md` | §3 | Suit breach feeding toxin/oxy pools unchanged; the atmosphere composition it reads is now a real GasBuffer. |
| `inventory-storage.md` | §2 | Gains a new Container instance: a pipe network's pooled volume (§9), alongside beakers, crates, and disposal units. |
| `health.md` | — | **Open gap, not resolved here:** a perception-altering gas (§12A) needs an effect target. Shouldn't be a new pool — should ride an existing stat (accuracy cone, movement input) the way sedation rides the consciousness threshold — but which one is undecided. |

## 12. Worked examples

**A — Industrial precursor pumped through the vents:**

| Step | What happens | System state |
|---|---|---|
| 1 | A chemist wires a mixing room's output into atmos-rated pipe via a pump, instead of a proper chem line | Deliberate, physically visible misuse — right infrastructure, wrong substance (§6) |
| 2 | Pump throttles up against an empty receiving network | Flow runs near rated max (§7); network pressure climbs |
| 3 | Contaminated network reaches a vent into a populated area | Interfaces with that room's open-tile GasBuffer at the vent's local flow rate, not instantly |
| 4 | An atmos tech's console shows a composition alarm on the affected loop | Detectable the same way a cut cable or bad disposal junction already is |
| 5 | Security or engineering valves off the source, or pressurizes the receiving side against the pump | The countermeasure §7 describes — physical, not a permission check |

**B — Pipe freeze clog:**

| Step | What happens | System state |
|---|---|---|
| 1 | A liquid line runs through an unheated maintenance corridor | Segment temperature derives from ambient (§8), no heat exchanger present |
| 2 | Segment temperature drops below the carried substance's freezing point | §5's freezing transition fires inside the pipe; that volume blocks the segment |
| 3 | Flow stops; a pump upstream reads a pressure/flow drop | Real, inspectable symptom, not a silent failure |
| 4 | An engineer applies a heat exchanger or a torch to the segment | Temperature rises past melting; the clog clears and flow resumes |

**C — Cross-connected lines reacting at the junction:**

| Step | What happens | System state |
|---|---|---|
| 1 | Two chem lines carrying incompatible reagents get cross-connected at a junction | Junction's real flow capacity gates how much of each crosses per tick (§9) |
| 2 | Chemistry's resolution runs continuously on the crossing volume | Near-miss/incompatible trigger fires per `chemistry.md` §5 |
| 3 | Hazard enters each side's network pool at the junction's flow rate, not instantly | Contamination visibly spreads down both networks over several ticks |
| 4 | An engineer spots the junction or a downstream symptom and closes the valve | Event stops spreading — same interruptible-mid-event standard the reactor already sets |

**D — A pump stalling against backpressure:**

| Step | What happens | System state |
|---|---|---|
| 1 | A pump feeds a network already near the receiving side's pressure | Differential is small |
| 2 | Actual flow tapers toward the pump's max differential capacity, not its rated max | Gauge shows the pump straining, low actual throughput |
| 3 | Receiving side is deliberately pressurized further, exceeding rated differential | Pump stalls outright — visible on the gauge, no special-casing needed |

## 13. Out of scope for this pass

- Perception-altering/debuff gas effects — needs an effect target in the health or combat model; flagged in §11, not decided here
- Exact numeric values — reagent boiling/freezing points, molar masses, pump rated flow and max differential by tier, liquid overflow-diffusion thresholds (a balancing pass, not a design decision)
- Vent/valve/pipe fixture physical object models and placement UI — pipes are established as tile-grain, weldable/cuttable physical objects; the exact fixture set is content, not this doc
- Mop/cleanup interaction for puddles — puddles are assumed cleanable via the existing freeform-tool convention; the specific interaction isn't detailed here
- Fire propagation and ongoing spread duration — already flagged as atmospherics' domain but out of scope by both chemistry (§12) and explosives (§9); still true here
- Floor/ceiling and multi-level exposure — no z-level system exists anywhere in this project; this entire doc assumes a single flat plane, which is what makes liquid/solid phase tractable without a height-field simulation
- Player transit through pipes — not this system. Disposal's chute network already moves solid items (`disposal.md`); this doc's SolidBuffer is reagent residue, not a transiting object

## 14. Prototyping this

**Claude Design, prompt 1 — pump/gauge device screen:**
> Using our SS3D design system, build a pump's diegetic device readout: rated max flow, current actual flow, and the differential/backpressure it's working against, rendered as a physical gauge on the device itself — same visual language as the reactor's heat gauge and the SMES charge readout. Show a healthy-flow state and a straining-against-backpressure state side by side.

**Claude Design, prompt 2 — gas vs. liquid rendering:**
> Show a heavier-than-air gas leak (plasma) pooling low in a room as a volumetric fog with a visible density falloff, next to a liquid spill spreading across the floor as a flat puddle with soft edges — same room, same lighting. Meant to check the two phases read as clearly different substances at a glance, not just different colors of the same effect.

**Cursor, prompt 1 — data contract first:**
> Here's the atmospherics design doc. Define the extended substance record (adds molar mass, boiling point, freezing point to the existing reagent shape), the three per-tile buffers (GasBuffer, LiquidBuffer, SolidBuffer — sparse entry lists, matching the existing gas buffer's shape), the PipeNetwork record (connected segment set, pooled substance volumes, total volume, gas- or liquid-typed), and the Pump record (rated max flow, max differential capacity, current computed flow as a function of the pressure difference on each side). Show me the data contract before wiring any rendering or reaction hooks.

**Cursor, prompt 2 — one vertical slice:**
> Implement one pump moving a single gas between two networks separated by a real pressure differential — flow rate computed from that differential and the pump's two stats, tapering to zero as the sides equalize. Then implement one phase transition end to end: a liquid-carrying pipe segment whose ambient-derived temperature crosses its substance's boiling point, converting that volume into the segment's local gas state and venting it as a rupture per §8. Junction reactions, rendering, and liquid/solid open-tile diffusion come after this is reviewed.
