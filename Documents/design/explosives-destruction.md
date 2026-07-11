# Explosives & structural destruction — design document

> Status: active

Resolves an open scope question Area raised and never answered: "a hole blown in a wall should be able to affect area boundaries as much as it affects atmosphere... flagged as an open scope question rather than assumed" (`area.md` §3). Also closes a loop chemistry deliberately left open — its own bad-mix explosions were scoped as "a lightweight, local, container-centered effect... not a redesign of blast mechanics generally" (`chemistry.md` §5, §12), on the assumption a real system would exist elsewhere. This is that system. Builds on shuttles' per-Area hull-breach-into-atmosphere pattern (`shuttles.md` §6), electricity's local-fault-into-hazard pattern (`electricity.md` §6), health's per-limb brute damage (§2), and the BFS hop-based traversal the lighting doc already uses for Ambient-tier bleed-through (`rendering-lighting.md` §3) — reused here for force instead of lux.

## 1. Design philosophy

The native-vs-leftover test, aimed at explosions: SS13's blast resolution is concentric rings drawn from an epicenter — devastation, heavy, light — indifferent to whether a wall stands in the way. That's a BYOND-grid simplification, not a model of how overpressure actually behaves. A real blast wave travels preferentially through open space and gets stopped by intact structure. SS3D already has the real data to resolve that properly — a tile-adjacency graph, not an invented one.

Three decisions fall out of that:

- **Propagation follows connectivity, not raw distance.** A blast travels tile-to-tile through open space, blocked by intact walls and closed doors, falling off per hop — not a radius that ignores geometry.
- **Structural failure is diegetic warning before catastrophe**, not a silent HP counter that's fine until it's gone. A wall under attack looks progressively wronger before it opens, the same tiered-visibility convention wound severity already established for bodies (`health.md` §5).
- **One resolution function, many sources.** A blast doesn't care whether it started as a thrown grenade, a chemistry bad-mix, an electrical overload, or a shuttle impact exceeding integrity — all of them terminate in the same event this doc defines, the "one shared mechanism" principle already run everywhere else in this project. A breach that results is not a special case either — it's the same atmosphere-connects, Area-recomputes event chemistry's gas hazard and a shuttle's hull breach already are.

## 2. Blast resolution — propagation

An explosive event is: an epicenter tile, a yield (starting force), and a falloff rate.

**Resolution is a breadth-first traversal from the epicenter tile**, spending force as it spreads — exactly the hop-based approach the lighting doc already runs for Ambient bleed-through (`rendering-lighting.md` §3), applied to force instead of lux. Each hop into an adjacent open tile costs force per the falloff rate; a closed door or intact wall blocks that edge outright, the same physical-blocking rule occlusion, atmosphere, and light already share. The traversal needs no artificial radius cap — it terminates naturally once remaining force drops below the minimum needed to do anything.

**What force does on arrival at a tile:**
- A structural object present (wall, window, machine housing) takes structural damage per §3.
- A character present takes brute damage per §5.
- Nothing present: the hop simply continues outward until force runs out.

**Cascading breaches happen for free, not as a special case.** If a door or thin wall crosses its own Destroyed threshold (§3) mid-resolution, that edge opens and the same in-progress traversal can continue through it — the blast punching a second hole doesn't need separate sequencing, it's the same pass reading updated state.

## 3. Structural failure — multi-stage, per tile

Damage accumulates **per structural tile**, not room-wide. This is a deliberate departure from the shuttle doc's per-Area hull integrity (`shuttles.md` §6) — a ship-scale pool was the right grain there; here the entire point is that *this specific wall* tears open while its neighbor doesn't, so the grain has to be the wall itself.

| Stage | What it looks like | Effect |
|---|---|---|
| Intact | Normal | Full integrity |
| Damaged | Visible dent or scorch | Cosmetic only — the first real warning something hit it |
| Cracked / venting | Visible crack, audible hiss, a vapor/particle leak | Atmosphere begins leaking through this tile at a slow rate even before full breach — a small, real leak source, the same decoupled relationship to atmospherics Area already established (§4 there), just triggered earlier than a full breach |
| Destroyed | The tile's wall object is removed from the world; becomes an open tile plus spawned debris/rubble | Full breach: atmosphere connects fully, Area boundary recompute triggers (§4 below) |

**Repair reverses through the same stages via ordinary freeform construction** — welder and metal sheets, the same tool-based repair convention electricity's cable splicing and crafting's assembly already use (`electricity.md` §6, `crafting.md` §2). No explosion-specific repair tool.

## 4. Area & atmosphere consequences

A tile crossing Destroyed is exactly the trigger Area §3 described and left unresolved: **a local recompute, re-running the flood fill only for the region touching the changed tile**, not the whole station. This doc is the answer to that open question.

Atmosphere connects or leaks exactly as any other breach already does — chemistry's gas hazard (§5 there) and a shuttle's hull breach (`shuttles.md` §6) both already established this same decoupled-but-adjacent relationship; a station wall breaching from an explosive is not a new atmospherics-adjacent mechanic, it's the same one firing again.

**Everything derived from Area membership re-derives automatically once the recompute runs** — power net, camera groups, alarm scope, access defaults (`area.md` §5) — because they were never authored per-wall, they were always resolved from Area membership. A blown wall changing who's on-camera or who's powered isn't extra work; it falls out of Area already being the single source of truth.

## 5. Damage to crew

Force reaching an occupied tile applies **brute damage through the existing per-limb model** (`health.md` §2) — a blast is a physical impact, not a new damage type. Severity scales with however much force is left when it reaches that character — the same falloff figure §2's traversal already computed, not a second calculation. A hard-hit stagger/knockback reuses whatever general hard-hit response already exists elsewhere in the project, the same "not designed here, just reused" note the shuttle doc already made for its own impact damage (`shuttles.md` §6).

**Secondary effects aren't a bespoke explosion-only system** — they're this doc's event re-triggering other systems' existing hazard resolution. Atmosphere ignited by the blast is fire propagation's job, not this doc's; a chemistry storage caught in the radius runs its own bad-mix resolution (`chemistry.md` §5) if the force crosses its container. Recursion, not duplication.

## 6. Explosive items

A dedicated item class, since this is designed here rather than left purely emergent:

- **Grenade** — a thrown item with a fuse timer, detonating on expiry or on impact. Reuses whatever general throwable-item interaction already exists; this doc only defines what happens once it goes off.
- **Breaching charge** — placed against a wall or door tile (Tier 3 use-with, `main-hud.md` §8), sticks to the structural surface, detonates on timer or remote trigger. Yield is tuned to reliably push its target tile through Cracked/venting into Destroyed rather than being a generalized area-damage weapon — its job is opening a route, not clearing a room.
- **Timed charge** — freely placed anywhere, not wall-locked. Arm, visible countdown, defuse. Defusing is a real interaction against a countdown a player can see and act on in time — a tool check, not a hidden roll or a minigame — the same "info is legible, skill is real-time" principle the hacking interface already runs.
- **Remote detonator** — a paired trigger arming a charge for detonation on signal instead of timer. Pairing should reuse comms' existing channel/frequency concept if that extends to short-range device signaling, rather than inventing a parallel radio system — flagged as an implementation-side check, not a fork worth blocking this doc on.

All four terminate in the identical §2 resolution — different epicenter, yield, and trigger condition, no per-item damage logic.

## 7. HUD touchpoints

No new permanent chrome.

- **Armed charges show their state on the object itself** — a visible countdown or blink, the object as the readout, the same discipline crafting's partial-construction state and armor's wear already follow.
- **Structural damage stages render on the wall model itself** — crack, scorch, hiss and vapor — no HUD overlay, the same "let the world carry the information" rule darkness already established in the lighting doc.
- **One new alert-stack entry**: a proximity warning if a character is within a charge's blast radius as it arms or nears zero, following the existing hidden-until-relevant pattern (`armor.md` §6 set this precedent).
- **This is what already feeds lighting's Effect tier.** "Explosions" is already listed there as a hazard light source alongside fire and hull-breach arcs (`rendering-lighting.md` §3) — this doc's detonation event is exactly what should trigger that Effect-tier light; no new lighting hook needed, just the confirmation that this is the thing it was always meant to plug into.

## 8. Worked examples

**A — Breaching charge opens a sealed room:**

| Step | What happens | System state |
|---|---|---|
| 1 | Charge placed on a wall tile, armed | Countdown visible on the object |
| 2 | Timer expires | §2 resolution runs, epicenter at that wall tile |
| 3 | Charge's tuned yield crosses the wall's Destroyed threshold in one hit | Wall tile removed, debris spawned |
| 4 | Area's local recompute runs | Two rooms merge into one area; atmosphere connects; power/camera/access re-derive automatically |

**B — Grenade in a corridor:**

| Step | What happens | System state |
|---|---|---|
| 1 | Grenade detonates mid-corridor | BFS spreads down the corridor in both directions and around one corner |
| 2 | Force falls off per hop | Crew near the epicenter take heavy brute damage; crew around the corner take little or none, having cost more hops to reach |
| 3 | A thin interior wall alongside the corridor absorbs partial force | Crosses into Cracked/venting, not Destroyed — hissing, slowly leaking atmosphere without a full breach |

**C — Timed charge, defused:**

| Step | What happens | System state |
|---|---|---|
| 1 | Charge armed, countdown visible | Ticking down |
| 2 | An engineer spots it and applies the defuse interaction in time | §2 never runs — no detonation, no resolution pass at all |

**D — Chemistry's deferred bad-mix, closed:**

| Step | What happens | System state |
|---|---|---|
| 1 | A bad chemical mix resolves as an explosive consequence (`chemistry.md` §5) | Instead of a bespoke local effect, it now calls this doc's §2 resolution directly, epicenter at the container's tile, a small yield |
| 2 | Chemistry's "assumes a lightweight effect exists elsewhere" note is resolved | It was deferring to this doc all along |

## 9. Out of scope for this pass

- Exact numeric values — yields, falloff rates, per-material structural thresholds, defuse timing windows (a balancing pass, not a design decision)
- Floor/ceiling destruction and multi-level exposure — no multi-deck/z-level system exists anywhere else in this project; explosions here affect walls, doors, and windows on the station's single level, not what might be "below." Worth its own pass only if a multi-level system ever gets designed
- General sustained fire propagation after ignition — this doc and electricity §6 both trigger fire as a consequence, but ongoing spread and duration stay atmospherics' domain, not redesigned here
- Shrapnel as a distinct damage type or projectile simulation — folded into §5's blast brute damage, not a separate mechanic
- Remote-trigger signal/frequency specifics (§6) — assumed to reuse comms' existing model, not detailed here
- Grenade throw arc/mechanics themselves — assumed to reuse whatever general throwable-item interaction exists elsewhere
- Explosive crafting/synthesis recipes — the items exist as designed objects in §6; whether they're player-craftable via chemistry or crafting recipes is a content question, not this doc's

## 10. Prototyping this

**Claude Design, prompt 1 — structural damage stages:**
> Using our SS3D design system, show one wall tile across its four damage stages — intact, damaged (dent/scorch), cracked/venting (visible crack, hiss/vapor particle), destroyed (open gap, debris) — side by side, toon/half-toon shading, same visual language as armor's wear states.

**Claude Design, prompt 2 — breaching charge sequence and area merge:**
> Show a breaching charge placed on a wall with a visible countdown, then the detonation moment, then the resulting hole with the two formerly separate rooms now visibly one continuous space — lighting, camera feed indicator, and any visible power state updating to reflect the area merge.

**Cursor, prompt 1 — data contract first:**
> Here's the explosives & structural destruction design doc. Define the blast event record (epicenter tile, yield, falloff rate), the per-tile structural damage state (four stages per §3), and the BFS resolution function (hop-based traversal, blocked by intact walls/closed doors, spending force per hop, applying structural or brute damage on arrival). Show me the data contract before wiring any item behavior.

**Cursor, prompt 2 — one vertical slice:**
> Implement one breaching charge end to end: place → arm → countdown → detonate → §2 BFS resolution → target wall crosses Destroyed → Area's local recompute per §4 → atmosphere connects. Grenades, timed charges, remote detonators, and crew damage come after this is reviewed.
