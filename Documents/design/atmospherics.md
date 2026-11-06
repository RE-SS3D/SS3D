# Atmospherics — design document

> Status: active

Gives a real spec to a system every other doc leans on: chemistry's gas release (`chemistry.md` §6), armor's suit breach (`armor.md` §3), health's environmental damage (`health.md` §2), explosives' breach into atmosphere (`explosives-destruction.md` §4) and shuttles' hull breach (`shuttles.md` §6) all point at "the atmosphere" as the thing that reacts. This doc defines what that atmosphere is, how gas moves through open rooms and through pipes, how the station keeps it breathable, and how it burns.

## 1. Design philosophy

The recurring test: is this a real simulation, or a shortcut wearing a science coat? Per-tile pressure equalization and heat exchange driven by each gas's real specific heat pass that test, and they're the core of this doc.

- **Real properties, not authored flags.** A gas has a real molar mass and specific heat, and the simulation reads them. A player could in principle predict what a gas will do from what it is.
- **A curated set of gases.** The station's atmosphere is made of a small, deliberate list of gases, each one there because it does something a player cares about — something to breathe, something to suffocate in, something that burns. A gas can be **linked to a reagent** where that link is meaningful (plasma gas and liquid plasma are the same substance to chemistry and to atmosphere), but not every reagent can become a gas.
- **Air moves as air.** A pressure difference pushes bulk flow through open rooms and through pipes alike, so a breach pulls a room empty rather than letting it slowly seep. Pipes stay a separate model because a sealed, pump-driven run behaves like one shared, well-mixed volume; open rooms keep tile-by-tile flow, because where the air is matters there.
- **Telegraphed catastrophe over instant catastrophe.** Same standard explosives' structural stages run — a fire spreads, a pipe strains before it bursts, and someone has time to notice.

## 2. Open-tile simulation

Every tile carries a gas mixture: an amount of each gas present, and one temperature.

- **Pressure equalizes** between adjacent open tiles per the ideal gas law. Walls and closed doors block it; an open door, a breach or a vent lets it through.
- **Moving air is wind.** Flow between tiles has speed and direction. It pushes people and loose items toward low pressure: a tug across the room, irresistible close to a breach unless you are wearing active magboots (`armor.md` §3). It carries the gas with it, so a burning or poisoned mixture travels with the draught.
- **Gases mix by their own share of the pressure.** Two neighbouring tiles at the same total pressure still exchange gases if their mixtures differ — a pocket of breathable air in a room full of nitrogen doesn't sit there forever just because the pressures match. It thins out.
- **Heat equalizes** per each gas's own specific heat, so a hot, thin mixture cools quickly and a dense one holds its heat.
- **Space is a sink.** A tile open to space loses its gas outward until it's vacuum; that's what makes a hull breach dangerous.

The rest of this doc builds on this layer.

## 3. The gas set

The baseline gases: **oxygen** and **nitrogen** (breathable air), **carbon dioxide** (what you breathe out, and what makes a sealed room slowly go bad), and **plasma** (toxic and flammable). Each has a real molar mass and specific heat.

New gases join the set only when they bring something a player can do or must survive. A gas linked to a reagent (§1) is how chemistry's gas release lands in the room (`chemistry.md` §6): a reaction that releases a linked substance as gas adds that gas to the tile's mixture.

Breathing reads this mixture directly (`health.md` §2): too little oxygen and oxy debt rises; low pressure damages the lungs; plasma adds toxin.

## 4. Vents, scrubbers and air alarms

The station keeps its own air breathable, room by room.

- **Vents** push air from the distribution pipe network into a room, up to a target pressure.
- **Scrubbers** pull unwanted gases (carbon dioxide and anything toxic, such as plasma) out of a room into a waste network.
- **Air alarms** are the room's brain. Each one watches the air in **its area** (`area.md` §4) and drives that area's vents and scrubbers. It has a small set of **presets** — normal operation, siphon the room, refill after a breach, panic — that an engineer switches between on its screen.

**An alarm actually alarms.** When its area's air goes bad — pressure dropping, oxygen low, plasma present, too hot — it sounds and lights up, so the people inside know to leave or put a mask on. That's the cheapest, most legible survival information on the station, and it costs no HUD chrome: the alarm on the wall is the readout.

## 5. Fire

**Plasma burns.** A tile with enough plasma and oxygen, hot enough, ignites: the burn consumes both, produces carbon dioxide and a lot of heat, and the heat spreads to neighbouring tiles through the same heat equalization as §2 — which can ignite the next tile's mixture. That's how a fire spreads: through real fuel, real oxygen and real heat, not a fire object with a spread rule.

- **A fire stops when it runs out of something.** Vent the room to space, scrub the plasma, or let it eat the oxygen, and it goes out.
- **Fire burns people.** Standing in a burning tile is heat exposure (`health.md` §2), reduced by environmental protection (`armor.md` §1).
- **Sources.** A spark, an explosion (`explosives-destruction.md` §5) or an electrical fault (`electricity.md` §6) can ignite a mixture that's ready to burn.

The first-pass response to a fire is the thin one: air alarms detect it (§4), people leave, and someone vents or scrubs the room. Fire extinguishers and firefighting gear are a later pass (§12).

## 6. Pipe networks — bulk flow vs. open diffusion

Two regimes: open rooms keep pressure and gas per tile, so a gradient, a front and a draught all exist; a sealed, pump-driven pipe at game-tick timescales behaves close to one shared, well-mixed volume. Modeling them differently isn't a shortcut, it's the physically correct call for each case.

**A pipe network** is a connected run of pipe segments (one tile per segment), holding one pooled mixture and total volume. Welding a segment in or cutting one out changes which pipes form one network.

- **Distribution and waste are separate networks** on a normal station: vents draw from one, scrubbers feed the other (§4).
- **Gas pipes only, in this pass.** Liquid plumbing is a later pass (§12).
- **Gas stays when the plumbing changes.** Joining two networks pools their gas, splitting one shares its gas between the pieces, and a removed segment lets its share out into the room.

### Pipe lines

Each tile carries up to four lines, and a pipe's name says what plugs into it, so an engineer can read a run without tracing it.

- **Supply** feeds vents.
- **Waste** takes what scrubbers pull.
- **Mixed** takes both, so a scrubber can feed a vent directly.
- **Surface** runs above the floor, where people can reach it. Pumps, valves and the larger machines sit on this line.

The first three run under the floor. A vent or scrubber connects to the matching line on its own tile by itself; a pipe it can't use may pass through that tile without touching it.

### Where station air comes from

**Storage chambers** are rooms held at a fixed mix that never runs out, so oxygen and nitrogen are places on the map, with intakes, pumps and mixers feeding the distribution network from them. A chamber opened to the station keeps pouring gas into it until someone seals the breach. A map can also start a line already filled, so a station has air from the first moment of the shift.

### Pipe devices

Every device sits where a pipe would, with fixed ports, so its facing decides which way gas moves. Filters and mixers have a flat form that sits on the floor over an underfloor line, reachable without lifting the floor, and a machine form on the Surface line.

- **Manifolds** sit across the pipes running through a tile and join those lines into one network. They are how an underfloor line reaches the Surface line.
- **Valves** connect two networks while open and part them while closed. A manual valve is turned by hand; a digital valve is switched from its panel and needs power.
- **Filters** split a stream: the gases an engineer selects leave by the side port and everything else carries straight on. They offer every gas individually, unlike a scrubber, which also has a broad toxins setting. If either outlet is full, the filter stops.
- **Mixers** combine two inlets at a set ratio up to a target outlet pressure, and stop if either inlet runs dry, so a bad mix can't slip out unnoticed.
- **Volume pumps** move a set amount of gas rather than chasing a target pressure.
- **Meters** read a line's pressure on the device itself, the same "world carries the information" convention as a pump's gauge (§7).
- **Injectors** release a line's gas into the room through a floor nozzle.
- **Ignitors** make a spark on demand (§5).
- **Connector ports** let a portable canister plug into a line, as both a source and a store of gas.

## 7. Pumps — flow as a function of differential

Two real stats on a pump, not one flat rate:

- **Rated max flow** — how much it moves per second under ideal, no-backpressure conditions. The property a better pump upgrades.
- **Max differential** — how much backpressure the motor can push against before stalling.

A pump runs at rated flow while its inlet is at or above its outlet. As the outlet climbs past the inlet, actual flow tapers toward zero, and the pump stalls outright if backpressure exceeds the motor's rated differential.

**A pump's gauge is a physical readout on the device itself** — current flow, and how hard it's working against differential — the "world carries the information" convention, not a hidden number.

This also doubles as the countermeasure for a hostile feed (§11A): pressurizing the receiving side against an unauthorized pump throttles or stalls it without needing a switch.

## 8. Pipe failure

A pipe segment has a pressure rating. Push a network past it — a pump driving into a closed valve, a fire heating a full line — and the weakest segment strains, then ruptures. A ruptured segment vents into the room it runs through, the same venting-before-catastrophe pattern explosives already established for walls (`explosives-destruction.md` §3). Repair is cutting out the segment and welding in a new one.

## 9. What you see

No per-tile objects. Gas that's dense enough or coloured enough to see shows as a soft cloud over the affected tiles, its opacity following the amount present, with soft edges so it never reads as grid-snapped squares. Plasma has its own colour, so a leak is visible before an alarm ever sounds. Fire shows as flame on the burning tiles, and it lights its surroundings (`rendering-lighting.md` §3, Effect tier).

## 10. Integration notes

| Doc | Section | What connects |
|---|---|---|
| `area.md` | §4 | Air alarms work per area; gas flow ignores areas. |
| `health.md` | §2 | Breathing, pressure damage to the lungs, toxin from plasma, burns from heat and cold. |
| `armor.md` | §1, §3 | Seals block pressure effects and cold, and scale heat; internals breathe from a tank instead of the tile; magboots anchor against wind. |
| `chemistry.md` | §6 | Gas release adds a linked gas to the tile's mixture. |
| `explosives-destruction.md` | §2, §4 | A breach connects air; a blast needs air to carry it and can ignite a mixture. |
| `shuttles.md` | §6 | Hull breach into atmosphere. |
| `electricity.md` | §4, §6 | Vents, scrubbers and alarms sit on the APC's Environment channel; electrical faults can ignite. |
| `inventory-storage.md` | §2 | Gas tanks and internals are containers of the same mixture shape. |

## 11. Worked examples

**A — Plasma pumped through the vents:**

| Step | What happens | System state |
|---|---|---|
| 1 | A saboteur wires a plasma canister into the distribution network via a pump | Deliberate, physically visible misuse |
| 2 | Pump throttles up against the network | Flow runs near rated max (§7); plasma spreads through the network |
| 3 | Contaminated air reaches a vent into a populated area | Plasma clouds visibly enter the room (§9) |
| 4 | That area's air alarm detects plasma and sounds | Crew see and hear it; the alarm can switch to siphon (§4) |
| 5 | Engineering valves off the source, or pressurizes the network against the pump | The countermeasure §7 describes — physical, not a permission check |

**B — A fire in a plasma-filled room:**

| Step | What happens | System state |
|---|---|---|
| 1 | A spark lands in a room with plasma and oxygen | The mixture ignites (§5) |
| 2 | The burn consumes plasma and oxygen and heats the tile | Heat spreads to neighbours; the fire grows across the room |
| 3 | The air alarm sounds; crew without protection take burns | Environmental burn on exposed zones (`health.md` §2) |
| 4 | An engineer sets the alarm to siphon, or opens the room to space | Fuel and oxygen leave; the fire goes out |

**C — A pump stalling against backpressure:**

| Step | What happens | System state |
|---|---|---|
| 1 | A pump feeds a network already well above its inlet pressure | Backpressure is close to the motor's rated differential |
| 2 | Actual flow tapers toward zero | Gauge shows the pump straining, low actual throughput |
| 3 | Receiving side is deliberately pressurized further, exceeding rated differential | Pump stalls outright — visible on the gauge |

## 12. Out of scope for this pass

- **Liquids and solids on tiles, and phase changes** — reagents boiling into gas, gas condensing into puddles, freezing into deposits, and the per-tile phase checks that would drive them. A later pass; it would also bring liquid plumbing and pipe clogs.
- **Every reagent as a potential gas** — gases stay a curated set (§3).
- **Reactions inside pipe networks and at junctions** — two networks joined just mix in this pass.
- **Heavy gases pooling low and light gases drifting high** — the station is a single flat level, and faking a vertical falloff is a later rendering pass.
- **Firefighting equipment** — extinguishers, fire suits beyond environmental protection, fire doors.
- **Perception-altering gases** — need an effect target in health or combat that hasn't been decided.
- **Exact numeric values** — molar masses, ignition temperatures, pump ratings, pipe pressure ratings (a balancing pass, not a design decision).
- **Floor/ceiling and multi-level exposure** — no z-level system exists anywhere in this project.
- **Player transit through pipes** — not this system; disposal moves items (`disposal.md`).
