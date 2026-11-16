# Electricity & power grid — design document

> Status: active

Referenced everywhere as a black box up to now — `area.md` §5 assumes each area has at most one APC, `rendering-lighting.md` assumes an APC that can drop offline and a backup battery that drains, `hacking-interface.md` §6 assumes "the actual power net topology already simulated," `crafting.md` assumes power "drawn from the machine's Area's APC," `shuttles.md` §7 assumes a shuttle has "its own APC, fed by the shuttle's own engine/generator," `ai-cyborgs.md` assumes a dock station "wired to its Area's APC." This doc is what all of those were pointing at. It doesn't relitigate any of them — it fills in what an APC actually is, what feeds it, and what happens when that feed is cut.

## 1. Design philosophy

Same test as always: is this genuinely good design, or a BYOND-era artifact? Applied here, the answer splits the system in two:

**The physical grid is good design, worth keeping.** A cable network you can trace, cut, short, overload, and repair is real engineering gameplay — sabotage and its detection, redundancy planning, the satisfaction of restoring power to a dark section of the station. This isn't a legacy artifact; it's the same "physical causation over hidden RNG" principle combat and health already committed to, just applied to infrastructure. Cutting power to a room should feel like cutting power to a room, not toggling a flag.

**Per-cable circuit simulation is the artifact.** BYOND-era SS13 models real voltage, resistance, and current across every individual wire tile — a simulation almost no player ever reasons about directly, and one Area has already implicitly ruled out by deriving a device's APC from its physical location rather than requiring it be individually wired (`area.md` §5). What actually matters to a player is answerable with a much simpler question: **is this node connected, through unbroken cable, to a live source — and if so, is that source's total supply enough to cover everyone drawing from it.** That's a connectivity and supply-versus-demand question, not a circuit simulation.

**Three physical tiers fall out of this:**
1. **Generation** — something makes power (§2).
2. **The network** — physical cable moves it around the station, with SMES units as optional big buffers (§3).
3. **The APC** — the local node each Area already assumes exists, the leaf of the tree (§4).

## 2. Generation

**First pass: portable generators and solar.** A **portable generator** is a fuel-burning machine you can wheel into place and wire up: load it with fuel sheets, switch it on, and it feeds whatever cable it sits on until the fuel runs out. It's the station's baseline source in this pass, alongside solar below. The reactor described next is a later pass (§11).

**Reactor (later pass).** Eventually a round starts with one station-standard reactor, physically installed in Engineering, sized to comfortably cover a normal station's baseline load. Three things are real, physical, and player-manipulable — no hidden math the player can't see:

- **Throttle.** A physical control (lever or dial, not a slider in a menu) sets target output. Turning it up increases power output *and* heat generation together — the same lever drives both, not two independently tunable numbers.
- **Fuel.** Consumes a real, countable fuel resource (rods or cells, sourced the same way any other material stock is — `crafting.md` §5's "real, countable physical stock" precedent, not an abstract fuel percentage). Running dry drops output to zero over a real depletion curve, not a cliff.
- **Heat.** A visible gauge on the reactor housing itself (world-carries-the-information, same rule vitals and armor wear already follow) tracks core temperature. A safe operating band is marked on the gauge, not hidden in a tooltip. Sustained output above that band climbs toward meltdown.

**Meltdown is a physical consequence, not a percentage roll.** Push the throttle too far for too long and the core breaches — a real localized explosion plus a radiation hazard zone, reusing whatever hazard-zone mechanic already exists for fire/plasma (`rendering-lighting.md` §9's Effect-tier hazard lighting already assumes such a zone can exist and light itself). Not designed here: the exact radiation-damage mechanic. This doc only establishes that overdriving the reactor has a real, telegraphed, physical cost.

**Solar arrays.** A supplemental, zero-risk source. Physical panels that must be aimed at the sun to produce anything — either a manual rotation control or an automated tracker device, both diegetic objects in the world, not a menu toggle. Output follows a real physical cause: station rotation and eclipse periods block the sun on a predictable cycle, so solar output visibly rises and falls rather than sitting at a flat abstracted number. Solar feeds the same cable network as every other source (§3) — no separate "solar grid."

**Out of scope for this pass, flagged deliberately:** exotic/volatile engine types (singularity, supermatter, tesla) that SS13 uses as their own antag-adjacent minigames. The baseline reactor above is what a station runs by default; a more dangerous, higher-ceiling engine as an optional round-config variant is a plausible future extension of this section, not something this pass invents.

## 3. The network — physical distribution

**Cable is a real object.** Use a cable coil on a floor tile to lay a segment; it joins any cable on the neighbouring tiles. Wirecutters cut it. Map authors place cable in the Map Editor (`creative-mode.md` §4). This is construction and destruction gameplay in the world, not a puzzle on a screen.

**One kind of cable.** As in SS13, there's a single cable type. Generators, SMES units and APCs all connect to the same network; an APC wired straight to a generator works.

**The SMES is an optional buffer.** Physically installed roughly one per department, with its own visible charge gauge and a bigger battery bank than any single APC carries. It charges from the network when there's spare supply and discharges into it when there isn't, carrying a department through a generator outage. A station can run without one; it just has no buffer beyond each APC's own cell.

**Topology is authored, not fixed.** Cable can run generator-to-APC directly, or through SMES units and in loops to spread capacity and build redundancy — a loop between two or more SMES units and a generator means a single cut doesn't isolate everything downstream, the same redundancy-by-design an engineer would actually build. A single dead-end run is a legitimate authoring choice for a section that doesn't need the redundancy; it's just accepting that a single cut there is a single point of failure. This is a per-section authoring decision, same category as Area's own manual-override authoring (`area.md` §3), not something this doc mandates station-wide.

**Connectivity is live.** Cutting cable splits the network; everything on the side with no live source stops receiving, even if an SMES or APC there still has its own battery reserve to fall back on (§5). Reconnecting — new cable, a splice — restores the flow.

## 4. The APC — local node

The object every other doc already assumes exists (`APC-ENG-03` in the lighting doc's worked example). Physically mounted to a wall within its Area, with a real panel that opens (screwdriver) to expose its breaker and cell — matching the "physical possession is the gate" precedent already set for every other tool interaction in this project (`surgery.md` §7).

**Three independent channels**, each separately toggleable and separately shed under load:
- **Lighting** — feeds the Key-tier fixtures `rendering-lighting.md` already defines.
- **Equipment** — powers machines, fabricators, and anything else drawing from the Area per `area.md` §5 and `crafting.md` §3.
- **Environment** — atmospherics-adjacent hardware (heaters, coolers, air pumps) — treated here exactly the way Area treats atmospherics generally: a real consumer of this system, not redesigned by it (`area.md` §4).

**Own internal cell.** Charges from the network when supply exceeds local demand, discharges to cover the gap when it doesn't. This is the literal mechanism behind the lighting doc's existing worked example step 3 ("Backup battery kicks in") — that battery was always the APC's own cell, this doc just names it. Cell capacity is finite and visible on the APC's own gauge, same diegetic-readout rule as the reactor.

**Priority shedding order under insufficient supply:** Equipment sheds first, Environment second, Lighting last — an engineer with a specific room to keep equipment-heavy can override this per-channel from the panel, but the default protects light before anything else, consistent with darkness already being the load-bearing diegetic signal `rendering-lighting.md` builds its entire Normal/Emergency/Dark model around.

**Wall light switch.** Separately from the APC's breaker, each area has a light switch on the wall that turns its lights on and off by hand (`area.md` §5). It doesn't cut the Lighting channel; it's just the switch by the door.

**Breaker trips are physical events, not hidden rolls.** If total demand across all three channels exceeds what the APC's wiring is rated for, the breaker flips — visible (a spark, a physically toggled switch state) and locally fixable by walking over and resetting it, or remotely diagnosable through the hacking interface's maintenance-mode view (`hacking-interface.md` §3, "full trace to APC/breaker").

## 5. Load & the failure cascade

This section is what actually drives the three-state Area lighting model — `rendering-lighting.md` names Normal/Emergency/Dark and the triggers ("APC power state and backup battery charge") but leaves the causal chain to this doc:

| Cable state | SMES state (if any) | APC state | Area lighting state |
|---|---|---|---|
| Connected, sufficient supply | Charged | Charged, all channels live | Normal |
| Disconnected (cut) or insufficient supply | Discharging to cover its branch | Discharging its own cell | Emergency once the APC's own cell starts covering the gap |
| Still disconnected | Depleted | Depleted, Lighting channel finally sheds | Dark |
| Cable repaired / supply restored | Recharging | Recharging, channels restore in reverse shed order | Back to Normal |

Nothing here is a hidden number ticking down unseen — every row of that chain is visible somewhere physical (a gauge, a lit/unlit fixture, a breaker position), the same "reason about it, don't guess" principle the hacking interface's real schematics and combat's readable accuracy cones already established.

## 6. Sabotage, damage & repair

**Cutting** a cable segment is the simplest sabotage — wirecutters, a few seconds, immediately splits the network. Detectable in real time by anyone watching an SMES's charge gauge start dropping or lights going out; tracing it on a network panel is a later pass (`hacking-interface.md`, deferred).

**Shorting/overloading** a cable or an SMES by deliberately pushing more demand through it than it's rated for is a more aggressive option — produces a real localized electrical fault (sparks, a small fire) at that node, tying into whatever general fire/hazard propagation atmospherics already handles, same decoupled-but-adjacent relationship shuttle hull breaches already have with atmospherics (`shuttles.md` §6).

**Repair** is ordinary freeform construction — new cable coil, a splice, restoring the graph edge — no special "hacking" tool required, matching the project's running "engineers do maintenance, hackers exploit weaknesses, both use the same physical tools" distinction (`hacking-interface.md` §1).

**Detection trail.** Same principle already established for hacking's security bypass consequences (`hacking-interface.md` §6): a cut cable or a tripped breaker is a real, inspectable event, not something that happens invisibly. Whether it also writes to station logging/cameras is inherited from whatever that infrastructure ends up being — not redesigned here.

## 7. Hazards — electrocution

A live, exposed, or damaged cable (or a device with a compromised ground, e.g. after water contact — reusing whatever wet/conductive interaction already exists for atmospherics/liquids if that's in scope elsewhere) can shock whoever touches it. Reuses the existing per-limb burn damage type wholesale (`health.md` §2) — no new damage pool invented for this. A brief stagger/knockback on contact is the same general hard-hit response already flagged as existing elsewhere and reused, not redesigned, by the shuttles doc (`shuttles.md` §6).

## 8. HUD touchpoints

No new permanent chrome, same discipline as every doc in this project.

- Reactor, SMES, and APC interaction use machine screens, which replace the HUD while open (`main-hud.md` §4).
- Gauges (heat, charge, output) are physical readouts on the object itself, per §2 and §4 — nothing duplicated into a UI panel.
- A player losing local power gets no special power-specific alert icon; the existing lighting-state transition (`rendering-lighting.md` §10) and any equipment simply failing to respond are the feedback. Consistent with "if the world can carry the information, it should" rather than adding a fourth alert to the existing stack (`main-hud.md` §9).

## 9. Integration notes

| Electricity element | Touches existing / needed system |
|---|---|
| APC, its three channels, its cell | `area.md` §5 (each Area optionally owns one APC) |
| Lighting channel, Normal/Emergency/Dark cascade | `rendering-lighting.md` §4, §10 |
| Equipment channel | `crafting.md` §3 (fabricator power draw), any other Area-resident machine |
| Environment channel | `area.md` §4 (atmospherics, deliberately decoupled but adjacent) |
| Cable as physical object | Laid with cable coil, cut with wirecutters (§3); Map Editor placement (`creative-mode.md` §4) |
| Backbone/APC network diagram | `hacking-interface.md` §3, §6 (network panel reads real topology) |
| Electrocution | `health.md` §2 (per-limb burn damage, reused not redesigned) |
| Shuttle power | `shuttles.md` §7 (shuttle's own generator/APC — a small-scale instance of §2 and §4 here) |
| Borg recharge dock | `ai-cyborgs.md` (dock wired to its Area's APC) |
| Reactor meltdown hazard zone | Whatever fire/radiation hazard propagation already exists (not designed here) |

## 10. Worked examples

**A — Cascading blackout and recovery (extends the lighting doc's own worked example with the causal chain behind it):**

| Step | What happens | Grid state |
|---|---|---|
| 1 | Engineering — main bay, normal round | Generators running, cable loop intact, SMES-ENG charged, APC-ENG-03 charged, all three channels live |
| 2 | A fire damages the cable segment linking Engineering to the generators | Network split; Engineering's side has no live source |
| 3 | SMES-ENG discharges its own battery bank to keep APC-ENG-03 alive | SMES-level buffer covering the gap; APC itself still fully charged |
| 4 | SMES-ENG depletes | APC-ENG-03 starts discharging its own cell — Area lighting enters Emergency |
| 5 | APC-ENG-03's cell depletes | Equipment sheds first, then Environment, then finally Lighting — Area enters Dark |
| 6 | An engineer splices new cable across the damaged segment | Network rejoined; SMES-ENG begins recharging from the generators |
| 7 | SMES-ENG recharges enough to feed the branch again | APC-ENG-03 recharges; channels restore in reverse shed order; Area returns to Normal |

**B — Deliberate sabotage and detection:**

| Step | What happens | Grid state |
|---|---|---|
| 1 | A saboteur cuts cable in a maintenance corridor, out of sight | Everything on the far side of that cut loses its source |
| 2 | Nearby crew notice lights failing in the affected department | Same Emergency → Dark cascade as example A, no special "sabotage" flag anywhere — it looks exactly like accidental damage would |
| 3 | An engineer follows the dark section back along the cable | Finds the cut segment by walking the run (a network-panel view on a hacking device is a later pass — `hacking-interface.md` is deferred) |
| 4 | Engineer walks to the marked location and splices it | Ordinary freeform repair, §6 — no special "anti-sabotage" tool needed, same tools that fix accidental damage fix this |

## 11. Out of scope for this pass

- The reactor (§2) — throttle, fuel, heat and meltdown; portable generators and solar carry this pass
- Exotic/volatile engine types (singularity, supermatter, tesla) as an optional higher-ceiling replacement for the reactor (§2)
- The radiation-damage mechanic itself for reactor meltdown (assumed to exist or be designed elsewhere, reused here)
- Exact numeric values — output curves, heat thresholds, cell capacities, breaker ratings (a balancing pass, not a design decision)
- Station logging/camera integration for the sabotage detection trail in §6 (cross-cutting infra, not designed here)
- Handheld battery cells for portable tools/weapons — a small item-economy question, not grid-relevant
- Editor tooling for authoring cable topology — see `creative-mode.md`

