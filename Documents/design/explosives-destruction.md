# Explosives & structural destruction — design document

> Status: active

Defines how structure takes damage and breaks, and how blasts travel. It also closes a loop chemistry deliberately left open — its own bad-mix explosions were scoped as "a lightweight, local, container-centered effect... not a redesign of blast mechanics generally" (`chemistry.md` §6, §13), on the assumption a real system would exist elsewhere. This is that system. Builds on shuttles' hull-breach-into-atmosphere pattern (`shuttles.md` §6), electricity's local-fault-into-hazard pattern (`electricity.md` §6) and health's per-limb brute damage (`health.md` §2).

## 1. Design philosophy

The native-vs-leftover test, aimed at explosions: SS13's blast resolution is concentric rings drawn from an epicenter — devastation, heavy, light — indifferent to whether a wall stands in the way. That's a BYOND-grid simplification, not a model of how overpressure actually behaves. A real blast wave travels preferentially through open space and gets stopped by intact structure. SS3D already has the real data to resolve that properly — a tile-adjacency graph, not an invented one.

Three decisions fall out of that:

- **Propagation follows connectivity, not raw distance.** A blast travels tile-to-tile through open space, blocked by intact walls and closed doors, falling off per hop — not a radius that ignores geometry.
- **Structural failure is diegetic warning before catastrophe**, not a silent HP counter that's fine until it's gone. A wall under attack looks progressively wronger before it opens, the same tiered-visibility convention wound severity already established for bodies (`health.md` §5).
- **One damage model, many sources.** A blast doesn't care whether it started as a thrown grenade, a chemistry bad-mix, an electrical overload, or a shuttle impact — all of them terminate in the same event this doc defines. Weapons feed the same structural damage too: a heavy melee weapon or a gunshot adds structural force to the wall it hits, just much less than a charge. A breach that results is not a special case either — it's the same atmosphere-connects event chemistry's gas hazard and a shuttle's hull breach already are.

## 2. Blast resolution — propagation

An explosive event is: an epicenter tile, a yield (starting force), and a falloff rate.

**A blast spreads outward from the epicenter tile by tile**, spending force as it goes. Each step into an adjacent open tile costs force per the falloff rate; a closed door or intact wall blocks that path outright, the same physical-blocking rule occlusion, atmosphere and light already share. There's no artificial radius — a blast ends naturally once its remaining force is too small to do anything.

**Overpressure needs air to carry it.** A blast stops at vacuum: set off a charge in space, or in a room that has already vented, and the shock wave has nothing to travel through. That makes a vented section genuinely safer from a second blast, and it's a real decision for an attacker.

**What force does on arrival at a tile:**
- A structural object present (wall, door, window) takes structural damage per §3.
- The floor of that tile takes damage per §3 (Floors).
- Furniture and machines on that tile take damage per §3 (Furniture and machines).
- A character present takes brute damage per §5.
- Nothing present: the hop simply continues outward until force runs out.

**Cascading breaches happen for free, not as a special case.** If a door or thin wall crosses its own Destroyed threshold (§3) mid-resolution, that edge opens and the same in-progress traversal can continue through it — the blast punching a second hole doesn't need separate sequencing, it's the same pass reading updated state.

## 3. Structural failure — multi-stage, per tile

Damage accumulates **per structural tile**, not room-wide. This is a deliberate departure from the shuttle doc's per-Area hull integrity (`shuttles.md` §6) — a ship-scale pool was the right grain there; here the entire point is that *this specific wall* tears open while its neighbor doesn't, so the grain has to be the wall itself.

| Stage | What it looks like | Effect |
|---|---|---|
| Intact | Normal | Full integrity |
| Damaged | Visible dent or scorch | Cosmetic only — the first real warning something hit it |
| Cracked | Visible crack, audible hiss, a vapor/particle wisp | The last warning. The wall no longer seals perfectly, but the rooms on either side stay separate — no room-to-room leak until it's Destroyed |
| Destroyed | The tile's wall object is removed from the world; becomes an open tile plus spawned debris/rubble | Full breach: atmosphere connects fully (§4 below) |

**Repair returns a damaged wall straight to Intact** — one ordinary repair with a welder, not a walk back through every stage. A Destroyed wall is rebuilt, not repaired. No explosion-specific repair tool. (Rebuilding is construction's job; `construction.md` is deferred.)

**Floors.** Every tile has a floor, and blasts damage it. The station is a single plane (`vision.md` §3), so a hole in the floor opens to space, never to a deck below. A floor has two layers:

| Layer | What it is | Air | When destroyed |
|---|---|---|---|
| Floor tile | The walkable surface | Not a seal: prying a tile up to reach the wiring never vents the room | Knocked loose, leaving bare plating |
| Plating | The hull, with the tile's pipes and cables in it | Airtight | A hole to space: the room vents through it, and the pipes and cables in that tile are destroyed |

A blast knocks floor tiles loose over a wider area than it breaches plating; only a strong blast, close to its epicenter, punches through the hull. Exposed plating shows its pipes and cables, which can be damaged on their own (`electricity.md` §6). Re-laying a floor tile is cosmetic. A hull hole is sealed by laying plating over it from metal sheets — a single placement (`construction.md` §3), not the construction ladder — after which the room can be repressurized (`atmospherics.md`).

**Furniture and machines.** Furniture and machines take damage from blasts and heavy hits and break: a broken object stops working and leaves debris (a smashed table, a dead APC, a burst canister — whose contents then follow their own system's rules). They don't get a wall's staged warnings; they hold no air, so there is no breach to warn about. Repairing or rebuilding them is out of scope for this pass (§9).

## 4. Area & atmosphere consequences

**A breach never changes areas.** A blown wall connects the air of two rooms, not their departments: each side keeps its area, its APC, its access and its cameras (`area.md` §3). A passable hole is not a department merger — security's armory stays security's armory even when there's a hole in its wall.

Atmosphere connects or leaks exactly as any other breach already does — chemistry's gas hazard (§5 there) and a shuttle's hull breach (`shuttles.md` §6) both already established this same decoupled-but-adjacent relationship; a station wall breaching from an explosive is not a new atmospherics-adjacent mechanic, it's the same one firing again.

A hull hole in a floor is the same: the tile keeps its area, and the room vents to space.

What a breach *does* change is physical: air rushes through, people and loose items can pass, and the hole is a route. The station's map of rooms doesn't move.

## 5. Damage to crew

Force reaching an occupied tile applies **brute damage through the existing per-limb model** (`health.md` §2) — a blast is a physical impact, not a new damage type. Severity scales with however much force is left when it reaches that character — the same falloff figure §2's traversal already computed, not a second calculation. Close to the epicenter it also knocks the character down (`combat.md` §5). Blast damage is a hit, so armor absorbs it (`armor.md` §2).

**Secondary effects aren't a bespoke explosion-only system** — they're this doc's event re-triggering other systems' existing hazard resolution. Atmosphere ignited by the blast is fire's job (`atmospherics.md` §5), not this doc's; a chemistry storage caught in the radius runs its own bad-mix resolution (`chemistry.md` §6) if the force crosses its container. Recursion, not duplication.

## 6. Explosive items

A dedicated item class, since this is designed here rather than left purely emergent:

- **Grenade** — a thrown item with a fuse timer, detonating on expiry or on impact. Reuses whatever general throwable-item interaction already exists; this doc only defines what happens once it goes off.
- **Breaching charge** — placed against a wall or door tile by using it on that tile, sticks to the structural surface, detonates on timer or remote trigger. Yield is tuned to reliably push its target tile through Cracked/venting into Destroyed rather than being a generalized area-damage weapon — its job is opening a route, not clearing a room.
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
| 4 | The hole opens | Atmosphere connects; both rooms keep their own area, power, cameras and access |

**B — Grenade in a corridor:**

| Step | What happens | System state |
|---|---|---|
| 1 | Grenade detonates mid-corridor | BFS spreads down the corridor in both directions and around one corner |
| 2 | Force falls off per hop | Crew near the epicenter take heavy brute damage; crew around the corner take little or none, having cost more hops to reach |
| 3 | A thin interior wall alongside the corridor absorbs partial force | Crosses into Cracked, not Destroyed — hissing and visibly failing, but still holding |

**C — Timed charge, defused:**

| Step | What happens | System state |
|---|---|---|
| 1 | Charge armed, countdown visible | Ticking down |
| 2 | An engineer spots it and applies the defuse interaction in time | §2 never runs — no detonation, no resolution pass at all |

**D — Chemistry's deferred bad-mix, closed:**

| Step | What happens | System state |
|---|---|---|
| 1 | A bad chemical mix resolves as an explosive consequence (`chemistry.md` §6) | Instead of a bespoke local effect, it now calls this doc's §2 resolution directly, epicenter at the container's tile, a small yield |
| 2 | Chemistry's "assumes a lightweight effect exists elsewhere" note is resolved | It was deferring to this doc all along |

**E — Breaching charge on a corridor floor:**

| Step | What happens | System state |
|---|---|---|
| 1 | An operative sets a charge on the corridor floor and it goes off | §2 resolution runs, epicenter at that tile |
| 2 | Floor tiles around the epicenter are knocked loose; the epicenter's plating breaks | A hull hole: the corridor vents to space; the cable in that tile is cut and the room beyond goes dark |
| 3 | A table and a vending machine nearby break | Debris; the machine stops working |
| 4 | An engineer in a suit lays plating over the hole and splices the cable | The hull is sealed; the corridor can be repressurized and the lights come back |

## 9. Out of scope for this pass

- Exact numeric values — yields, falloff rates, per-material structural thresholds, defuse timing windows (a balancing pass, not a design decision)
- Ceiling destruction and multi-level exposure — the station is a single plane (`vision.md` §3); a floor hole opens to space, never to a deck below. Worth its own pass only if a multi-level system ever gets designed
- General sustained fire propagation after ignition — this doc and electricity §6 both trigger fire as a consequence, but ongoing spread and duration stay atmospherics' domain (`atmospherics.md` §5)
- Staged damage warnings on furniture and machines, and repairing or rebuilding them once broken
- Staged repair and rebuilding (repair goes straight to Intact; construction is deferred)
- Shrapnel as a distinct damage type or projectile simulation — folded into §5's blast brute damage, not a separate mechanic
- Remote-trigger signal/frequency specifics (§6) — assumed to reuse comms' existing model, not detailed here
- Grenade throw arc/mechanics themselves — assumed to reuse whatever general throwable-item interaction exists elsewhere
- Explosive crafting/synthesis recipes — the items exist as designed objects in §6; whether they're player-craftable via chemistry or crafting recipes is a content question, not this doc's

