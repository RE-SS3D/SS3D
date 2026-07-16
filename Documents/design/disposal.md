# Disposal — design document

> Status: active

BYOND's disposal system is close to a pure engine artifact: items and players vanish into an invisible off-map pipe layer and teleport across the station after a short delay, with nothing about the transit actually happening in the world. This doc rebuilds it as a physical network — reusing the cable/backbone pattern already established for power (`electricity.md` §3) almost wholesale — and gives it a real hook into Cargo's exporting loop (`cargo.md` §7), which is what makes disposal worth having as more than flavor. Player transit is architected for from the start but deferred to a later phase (§8).

## 1. Design philosophy

Same split the electricity doc found in its own domain applies here almost exactly:

- **A network you can trace, cut, and sabotage is good design** — the same "physical causation over hidden RNG" principle already committed to everywhere else, just applied to waste and mail instead of power. Cutting a pipe to catch someone dumping evidence should feel like cutting a pipe, not toggling a detection flag.
- **Instant, invisible, off-map transit is the artifact.** It exists because BYOND had no other cheap way to move a sprite across the map. This doc replaces it with real transit time along a real network.

Two rules fall out:

1. **An item entering the network is a real object on a real path**, not a state flag that resolves after a timer. It can be found mid-transit if the pipe carrying it is cut open.
2. **One shared routing technique, not a new one.** Junctions resolve destinations using the same BFS/connectivity approach Area's boundary detection, the lighting system's room-adjacency traversal, and electricity's backbone connectivity already share (`area.md` §3, `rendering-lighting.md`, `electricity.md` §1) — a fourth consumer of one technique, not a bespoke pathfinding system.

## 2. The disposal unit (chute)

A physical hopper/funnel machine, placed in departments and public areas. Dropping an item in is the same Tier 3 combine interaction already used for freeform construction and general item-on-object actions (`crafting.md` §2) — no new gesture to learn.

**Sized by collider class, not by an "item" flag.** The chute's accept check keys off physical size rather than a type tag. This is entirely for §8's benefit: when player-sized transit lands later, a body just needs to pass the same size check an oversized item would, rather than the entry point being redesigned around it.

**Some units are ID-locked.** A secure evidence chute in Security, for instance, reuses the exact ID-locked-crate pattern from Cargo — another entry in the FDU's existing device taxonomy (`cargo.md` §5, `hacking-interface.md` §3) rather than a bespoke lock.

## 3. The pipe network — physical distribution

**Pipe segments are real, placeable, cuttable tile objects**, laid via the same freeform construction action cable already uses (`electricity.md` §3, `crafting.md` §2's Tier 3 combine grammar), consuming a pipe-segment material with the same "real, countable physical stock" treatment as cable coil (`crafting.md` §5).

**Visible, not buried.** Recommend running pipe along walls and ceilings in maintenance corridors as an observable industrial fixture — glass-paneled sections where practical — rather than hidden entirely under the floor. This matters for the same reason electricity's cable is a real object instead of an abstract graph edge: a player should be able to look at a pipe run and reason about where it goes, and watching a package visibly travel past is worth the modest art cost.

**Junctions route by tag.** A junction reads the destination tag on whatever's currently passing through it (§4) and forwards it down the correct branch using the shared BFS/connectivity technique from §1 — the fourth consumer of that one technique in this project.

**Cutting a segment is real sabotage.** Same tool interaction as cutting cable. Whatever's currently in transit through that segment spills out physically at the cut, immediately — no hidden loss, no percentage chance of recovery. This directly enables the detection loop in §7.

## 4. Tagging & destination routing

At or near a chute, a quick tagger interaction lets a player attach a destination label before dropping something in — a real, examine-readable property on the object itself (`main-hud.md` §15), the same "manifest is printed on the object" convention Cargo's crates already establish (`cargo.md` §5), not hidden metadata.

- **No tag** → routes to the main disposal outlet (§6) by default.
- **Tagged for a department** → routes via junctions toward that department's local outlet.
- **Tagged for a restricted destination** (e.g. Security evidence lockup) → may itself require the sender to hold matching access, reusing the existing ID/access system rather than a new permission layer.

## 5. Transit

An item entering the network becomes a real object riding along the routed pipe path at a visible, reasonably brisk pace — not a physics simulation inside the pipe, a lightweight "move along this spline toward the next junction" behavior, the same restraint electricity showed choosing connectivity-and-flow over full circuit simulation (`electricity.md` §1).

**Travel time scales with real distance along the routed path** — a package tagged for the far side of the station takes longer than one dropped a few rooms from its destination, the same "real transit time, not a teleport" principle the cargo shuttle's round trip already established (`cargo.md` §6).

**Multiple items travel independently and simultaneously.** Each is its own object on the network; nothing here is a single serialized queue that could bottleneck under load.

## 6. The outlet — and the Cargo hook

The main, untagged outlet sits at a physical location in or adjacent to cargo bay — deliberate placement, not incidental, because it's what makes this doc worth designing rather than just flavor.

**Two real fates for arriving waste:**
- **Unclaimed after a short grace window** — physically ejected into space. Permanent and honest: if you throw something away and nobody intervenes, it's actually gone, not quietly despawned.
- **Intercepted before ejection** — cargo or mining crew can sweep unclaimed disposal contents onto the export pad instead, feeding directly into Cargo's existing exporting loop (`cargo.md` §7): same appraisal, same ledger line, no new economy mechanic. This is the concrete payoff of the two systems sharing a location — routing junk through disposal instead of hand-carrying it to the export pad becomes a real, faster option, and untagged trash that would otherwise be silently discarded becomes a passive trickle of income if anyone bothers to catch it.

Tagged packages arriving at a department outlet simply sit there for pickup — no special handling beyond that.

## 7. Security & sabotage

Classic antag uses carry over directly: dumping a body or stolen goods down a chute, hoping it's ejected into space before anyone looks, or reaches an out-of-the-way outlet unnoticed.

**Detection is physical, never a hidden roll.** Security can search an outlet before its grace window expires, or — the payoff of §3 — cut open a suspicious pipe segment mid-transit to force its contents out at a chosen point, the same cable-cut detection pattern electricity already established for power sabotage (`electricity.md` §6). Whether something is found always depends on someone physically being in the right place at the right time, same principle as everywhere else in this project.

## 8. Phase 2 — player travel (architecture now, mechanics later)

Not mechanically designed this pass, but nothing above needs to be redesigned when it lands, because the shape was chosen with this in mind:

- **Chute entry already keys off size class** (§2), so a player- or body-sized object passes the same accept check an oversized item would — no separate "can this chute take a person" flag to retrofit.
- **Pipe segments and junctions already treat "whatever's transiting" as a generic object on a path** (§3, §5), not an item-specific data structure. A player becomes another instance of that same transiting object, not a parallel system.
- **The outlet's two fates already extend cleanly** (§6): ejection into space is a real, and famously chaotic, consequence — reusing whatever vacuum-exposure hazard already exists rather than inventing space-ejection damage from scratch. Arriving at the cargo-side outlet is just arriving somewhere physically, the same as stepping off a shuttle.

**Genuinely deferred, not just unwritten:** the collision/animation of a player-sized body moving through a tile-width pipe, whether transit is instant-while-prone or an animated crawl, and any additional disorientation or damage on ejection. That's a small, focused follow-up pass once the item network above is built and proven — not a reason to leave the core network's shape ambiguous now.

## 9. HUD touchpoints

No new permanent chrome.

- Dropping an item in is a standard Tier 3 combine interaction, same as everywhere else.
- Tagging is a small in-world interaction at the chute, not a floating panel.
- A tagged package's destination reads via examine (§4), same as a crate's manifest.

## 10. Integration notes

| Disposal element | Touches existing / needed system |
|---|---|
| Pipe segment as physical object | Cable/backbone precedent (`electricity.md` §3) |
| Pipe material | Material-stock precedent (`crafting.md` §5) |
| Chute drop-in interaction | Tier 3 combine grammar (`crafting.md` §2) |
| Junction routing | Shared BFS/connectivity technique — fourth consumer (`area.md` §3, `rendering-lighting.md`, `electricity.md` §1) |
| Package tag / destination label | Examine system (`main-hud.md` §15); manifest-as-physical-property precedent (`cargo.md` §5) |
| Locked/secure disposal unit | ID/access system, FDU device taxonomy reuse (`hacking-interface.md` §3, `cargo.md` §5) |
| Outlet → export pad | Cargo's exporting & selling loop (`cargo.md` §7) |
| Mid-transit interception / sabotage | Same cable-cut detection pattern (`electricity.md` §6) |
| Phase 2 ejection into space | Existing vacuum-exposure hazard, reused not redesigned |

## 11. Worked examples

**A — Routine trash, caught by cargo:**

| Step | What happens | Disposal state |
|---|---|---|
| 1 | Engineer clears scrap from a repair job, drops it untagged into the nearest chute | Item enters the network, no destination tag |
| 2 | Routes through junctions toward the main outlet | In transit, travel time scaling with distance |
| 3 | Arrives at the outlet near cargo bay | Sits in the grace window |
| 4 | A cargo tech notices and sweeps it onto the export pad before it's ejected | Feeds into Cargo's existing appraisal/ledger loop — a real, attributable sale, not a background number |

**B — Tagged internal delivery:**

| Step | What happens | Disposal state |
|---|---|---|
| 1 | A chemist tags a package for Medbay before dropping it in | Destination label attached, readable via examine |
| 2 | Junctions route it toward Medbay's local outlet instead of the main one | In transit along a different path than example A |
| 3 | Arrives at Medbay's outlet | Sits for pickup, no ejection risk — tagged deliveries don't go to the space-ejection outlet |

**C — Security intercepts a cover-up:**

| Step | What happens | Disposal state |
|---|---|---|
| 1 | An antagonist stuffs stolen goods into a chute, untagged, hoping for space ejection | Item enters the network |
| 2 | A suspicious security officer, aware of the timing, doesn't reach the outlet in time | Grace window is closing |
| 3 | Instead cuts open a pipe segment along the likely route | If the item hasn't passed that point yet, it spills out right there — a real, physically-located find, not a lucky roll |

## 12. Out of scope for this pass

- Exact numeric transit speed and pipe throughput values (a balancing pass, not a design decision)
- Phase 2 player travel mechanics themselves (§8 — architecture only this pass, not the mechanics)
- Congestion/priority resolution at a junction under heavy simultaneous traffic — the network is assumed uncongested for v1
- Any dedicated escape/struggle minigame for a player caught in transit — Phase 2's job, if it's wanted at all
- Full server-side logging of disposal contents beyond what security can physically discover by searching or cutting — server logging, cross-cutting infra

