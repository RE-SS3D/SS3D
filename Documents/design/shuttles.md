# Shuttles — design document

> Status: active

Not yet named as an open item anywhere else in this project, but implicitly assumed by two things already on paper: the round-end flow both `lobby.md` (§11) and `round-config.md` (§10) gesture at without designing ("round-end summary & transition," flagged as a separate, still-open pass), and Area's tile-ownership model (`area.md` §2), which has so far only ever had to describe tiles that don't move. This doc is the dependency the round-end pass will eventually need, not the other way around — the same relationship round config had to lobby's antag opt-in section before round config existed to resolve it. Evac is covered here only as this doc's worked example (§9); the call/timer/point-of-no-return logic itself still belongs to round-end, not here.

## 1. Design philosophy

The native-vs-leftover test, aimed at physics this time instead of UI or lighting: in SS13, a shuttle isn't flying anywhere. It's a block of turfs copy-pasted (or teleported) from a docked template to a destination, sometimes with an animated transit z-level bolted on to fake continuous motion. That's not a design choice — it's the only thing possible when the world is a stack of independent 2D grids with no real geometry between them. Unity doesn't have that constraint. A shuttle can be a real object with a real transform, genuinely translating and colliding through continuous 3D space, the same way zone targeting replaced the click-a-doll workaround once real geometry existed to aim at.

This isn't a novel bet. Space Station 14 — the other major from-scratch remake of SS13, built in a real engine with real physics instead of BYOND — already validated this exact shape: shuttles fly automated routes by default, but a player who takes the helm gets real thrust/heading control, and flying badly has real physical consequences, including hitting the station. SS3D doesn't need to invent this pattern, just build its own version of it, consistent with everything already established here.

Three rules fall out of that, extending the project's recurring threads:

1. **Automated and manual piloting are one mechanism, not two.** Both drive the identical movement/collision resolution; only the source of thrust/heading commands differs — a waypoint-following behavior versus direct player input. This is the same move stamina made treating exertion and oxy debt as one system with two regimes (`stamina.md` §1), not a parallel model.
2. **A crash is a real collision, not a roll.** Impact force follows from real relative velocity and mass at the moment of contact — the same "physical, not RNG" line combat drew for accuracy and health drew for death (`combat.md` §1, `health.md` §1).
3. **A shuttle's interior is Area, not a parallel space.** It doesn't need its own zone/damage/power system — it needs Area's existing model (tiles, APC ownership, per-tile data) attached to something that moves, the same "no parallel model" refrain that's run through every doc since the hacking interface (`area.md` §1).

## 2. What a shuttle physically is

A shuttle is a real object — a root transform carrying its own rigidbody and its own local tilemap/Area hierarchy, parented beneath it. Characters standing on a shuttle's floor are physically on that hierarchy, the same way standing on any moving platform in a 3D engine works; there's no data-layer "you are now considered to be at these station coordinates" trick to maintain.

**This resolves the fork Area's own doc left open.** A shuttle's tiles are never grafted into the station's tile-to-area lookup (`area.md` §2) at dock time — that BYOND-style relocation isn't available, and isn't needed, once collision is real. Instead:

- A shuttle's Area(s) exist permanently, in the shuttle's own local space, whether docked or not.
- **Docking is a physical connection between two Areas, not a data merge.** A docking port on the shuttle and a matching port on the station each sit at the edge of an Area. When aligned and clamped, the seam between them behaves exactly like a door between two station Areas already does (`area.md` §3) — a normal, walkable boundary, camera-adjacent, access-gated the normal way. Undock, and that boundary simply closes again; nothing about either Area's own data changes.
- **Camera network compatibility is already fine, unforced.** Area's camera grouping keys off a stable Area id, not a fixed world position (`area.md` §5) — a shuttle's cameras registering under the shuttle's own Area id works whether the shuttle is docked at the station or three kilometers out. No extension needed there, just confirmation that the existing assumption already holds.

## 3. Docking

**Docking ports** are physical objects, one or more per shuttle and per station dock, each with a defined connection point and orientation. A dock resolves when a shuttle's port physically aligns with a station port within a tolerance — reuses the same "concrete, checkable precondition" shape as defib pad placement (`death-cloning-respawn.md` §3) or an armor seal's coverage check (`armor.md` §3), not a hidden roll.

**Clamping and sealing is a short, real, interruptible timed action** once alignment holds — cycling the seam mechanically and atmospherically closed, the same timed-interaction convention already used for wrenching, welding, and freeform crafting steps (`crafting.md` §2). A shuttle that drifts out of tolerance mid-clamp aborts the sequence rather than snapping into a broken half-docked state.

**Autopilot docks the same way a manual pilot would**, executing a scripted approach along its route to the exact same alignment tolerance — docking isn't special-cased for automation; it's the same check either input source has to satisfy.

## 4. Automated piloting

Most shuttles default to this. A route is an authored sequence of waypoints between two or more docks (e.g., station ↔ a fixed rendezvous point). The shuttle follows it, decelerating and aligning into each dock per §3.

**Autopilot never crashes on its own.** Obstacle detection along the route reuses the project's shared raycast/occlusion system — already carrying comms occlusion, the FDU's wireless scan, combat cover, and light occlusion (`rendering-lighting.md` §5 lists it as a fourth consumer; this is a fifth). If something's in the path — another shuttle, debris, a station structure that wasn't there when the route was authored — autopilot holds position and reports the obstruction rather than forcing through. This is a deliberate asymmetry: automated flight is the safe, boring default; taking manual control is what introduces real risk, not a strictly-worse way to get the same automated outcome.

## 5. Manual piloting

**Taking the helm** means physically occupying a helm console and interacting with it — the same "physical presence is the gate" principle already governing tools in crafting and surgery (`surgery.md` §7), not a menu selection from anywhere on the ship. Whether a given shuttle's helm is itself access-gated (a pilot credential required, versus open to any crew) is left as a per-shuttle-type configuration, not a global rule — an evac shuttle deliberately left open invites exactly the kind of emergent chaos or heroics this genre thrives on; a proper vessel's helm might reasonably require access.

**Control is direct and continuous** — thrust and heading input, not point-and-click "fly to X." This is what makes manual flight meaningfully different from autopilot rather than a fancier way to trigger the same route, and it's what gives crashing a real skill dimension worth the risk of taking control at all.

**Momentum, bounded rather than fully Newtonian.** Zero-drag, frictionless rotation reads as simulator-grade flight-model work this project isn't scoping into, and would fight the "approachable, station-sim" register everything else here has kept. Recommend real momentum on thrust (so a heavy shuttle genuinely carries speed and a bad approach genuinely overshoots) with a soft velocity cap and mild stabilizing damping — justified in-fiction the same way SS13/14 already justify it, as inertial dampeners doing normal background work, not a special ability. This keeps controls learnable while leaving real, felt consequences for pushing them too hard. Flagged here as a recommendation, not locked — same status as combat's hitscan-vs-projectile call (`combat.md` §3) or the camera dependency in the main HUD doc (§6): worth confirming before implementation-locked.

**Handing control back:** stepping away from the console, or an explicit "engage autopilot" action, returns the shuttle to automated behavior, which recomputes a route from wherever the shuttle currently is. Both states — autopilot and manual — drive the identical underlying movement/collision system per §1; nothing about §6's damage resolution cares which one was in control at the moment of impact.

## 6. Collision & damage

**Hull integrity is tracked per-Area, not per-tile and not as one whole-ship number** — reusing the partition Area already provides rather than authoring a separate hull-zone map. This is the same "physical damage is local" move health already made for limbs (`health.md` §1, §2): a shuttle's bridge, engine room, and cargo hold each take damage — and can breach — independently, the same way a wounded arm doesn't affect an untouched leg.

**Resolution on impact:** relative velocity and mass at contact resolve to a real impact force. If that force exceeds the affected Area's remaining integrity, that Area breaches — the same event-not-two logic armor already established for combat damage exceeding absorption ("the armor didn't fully stop it and the seal didn't fully hold are the same event," `armor.md` §3), just at ship scale instead of suit scale.

**A breach ties into atmospherics exactly like any other hull breach** — decompression happens the way it already would for a blown station wall, no new atmospherics-adjacent mechanic invented. Same deliberate decoupling precedent Area already set for atmospherics generally (`area.md` §4).

**Crew aboard a section that takes a hard impact** take brute damage and a knockdown, reusing the existing per-limb damage model (`health.md` §2) and whatever general stagger/ragdoll mechanic exists elsewhere in the project for a hard hit — not designed here; this doc only establishes that a bad landing has a real, physical cost to the people inside it, not that it invents a new impact-response system from scratch.

## 7. Power

A shuttle's Areas draw from the shuttle's own APC, fed by the shuttle's own engine/generator — not the station grid. This needs no extension to Area's power model (`area.md` §5, "each area optionally owns one APC"); it's the same rule, just sourced from a different generator.

**Shore power at dock:** once clamped (§3), a shuttle's APC can additionally draw from the station grid through the connected Area, the same way any door-adjacent Area boundary already relates two power contexts — recharging or supplementing the shuttle's own supply while docked, dropping back to internal-only the moment it undocks.

**If shuttle power fails mid-flight**, the existing three-state Area lighting model (Normal / Emergency / Dark, `rendering-lighting.md` §4) applies to shuttle interiors exactly as it does to station ones — no new lighting behavior needed, just confirmation that shuttle Areas plug into the same Area → APC → lighting chain everything else already does.

## 8. HUD touchpoints

No new permanent chrome, consistent with the rest of this project.

- **The helm console's screen is a diegetic HUD element**, taking over the center of the screen the same way diagnostic/hacking device screens already do (`main-hud.md` §4, `hacking-interface.md` §2) — layered temporarily over the normal HUD, not a full replacement. Intent, comms, and vitals stay exactly where they are; the player is still their character, just currently interacting with a console.
- **The radar/proximity readout is a real sensor display, not an abstract minimap** — relative positions and velocity vectors of real nearby objects, the same "if you can't point to the real thing it represents, it doesn't belong on screen" rule the hacking interface's panels already follow (`hacking-interface.md` §1).
- **A docking-alignment guide** appears only when near a port in range — a real "line it up" visual, not a hidden auto-snap.
- **Two new alert-stack entries**, following the existing hidden-until-relevant pattern (`armor.md` §6 set this precedent for low-resource/breach warnings): a collision-imminent warning during manual flight, and a hull-breach warning per affected Area.

## 9. Worked example — evac shuttle (this doc's scope, not round-end's)

The evac shuttle is a shuttle instance, nothing more — configured to default to autopilot on a fixed route between a station dock and a rendezvous point, with an open (non-access-gated) helm. What triggers its call, what the countdown timer does, and what "point of no return" means are round-end's job, not this doc's; this doc only guarantees the physical object exists, with real dock/undock/transit/collision behavior, for that system to drive.

| Step | What happens | Shuttle state |
|---|---|---|
| 1 | (Round-end triggers a call — not detailed here) | Shuttle undocks from its home dock, autopilot engages |
| 2 | Autopilot flies the authored route toward the station | Obstacle check running continuously per §4; nothing blocking |
| 3 | An antag reaches the open helm and takes manual control | Autopilot disengages; direct thrust/heading input now active |
| 4 | Antag deliberately rams the shuttle into a station structure | Real collision resolves per §6; the struck Area's integrity check fails |
| 5 | That Area breaches | Decompression begins there exactly as it would for any blown wall; crew in that section take impact damage and a knockdown |
| 6 | Antag disengages or is removed from the helm | Autopilot re-engages, recomputes a route from current position, resumes toward the rendezvous point — still damaged, still able to fly |

## 10. Other shuttle types (framework generality, not fully spec'd)

Kept brief — the point of §§2–8 is that none of this needs re-deriving per type:

- **Mining/exploration shuttles** — manual-piloted by default, since flying somewhere is the point of the trip; same helm, same collision model.
- **Cargo shuttles** — automated hauling on a fixed route, structurally identical to the evac shuttle's autopilot behavior, just without the round-end hook.
- **Escape pods** — the minimal case: autopilot-only, likely no helm console object at all, so manual override simply isn't reachable rather than being specifically disabled.

None of these need a new system — they're the same shuttle framework with different route/helm/access configuration, the same way armor's fire suit and ballistic vest are one absorption model with different numbers (`armor.md` §1).

## 11. Integration notes

| Shuttle element | Touches existing / needed system |
|---|---|
| Interior tiles, per-Area hull integrity | Area's tile/APC/data model, attached to a moving root transform (`area.md` §2, §5) |
| Docking as an Area-boundary connection | Door-as-Area-seam precedent (`area.md` §3) |
| Clamp/seal timed action | Timed-interaction convention (`crafting.md` §2) |
| Route obstacle detection | Shared raycast/occlusion system, fifth consumer (`rendering-lighting.md` §5) |
| Hull breach | Atmospherics (black box, decoupled per `area.md` §4) |
| Impact damage to crew | Per-limb damage model (`health.md` §2); stagger/ragdoll mechanic (not designed here) |
| Shuttle power, shore power at dock | Area → APC derivation (`area.md` §5) |
| Power loss lighting behavior | Existing three-state Area lighting model (`rendering-lighting.md` §4) |
| Helm console screen | Diegetic device-screen pattern (`hacking-interface.md` §2) |
| Helm/area access gating | Existing ID/access system (`area.md` §5) |
| Camera network | Area's stable-id camera grouping (`area.md` §5) — confirmed compatible, no change needed |
| Evac call/timer/point of no return | `round-end summary & transition` — not designed here; this doc is upstream of it |

## 12. Out of scope for this pass

- Round-end's call/timer/point-of-no-return logic itself (separate, still-open pass; §9 only supplies the physical shuttle it will drive)
- Exact numeric values — thrust, mass, integrity, impact-damage thresholds, velocity caps (a balancing pass, not a design decision)
- Full physics/netcode implications of continuous shuttle movement and docking under multiplayer prediction (flagged in §5 as needing implementation-side confirmation)
- Ship-to-ship weapons or shuttle combat (plausible for some gamemodes, not a base mechanic designed here)
- Exact roster of shuttle types beyond the sketches in §10
- Destination content itself (asteroid fields, exploration sites) — map/content authoring, not a mechanic
- Cargo economy specifics — assumed to exist as an already-existing system, treated as a black box the same way the material silo is in `crafting.md` §3
- Shuttle ownership/manifest/trading systems

