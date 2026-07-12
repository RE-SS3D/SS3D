# Construction — design document

> Status: active

Resolves two things this project has been carrying as open items since early docs. First, Area's own flagged-but-unconfirmed question about whether live geometry changes should recompute area boundaries (`area.md` §3) — explosives already answered that from the destructive direction (`explosives-destruction.md` §4); this doc answers it from the constructive direction, and the two meet at the same technique. Second, crafting's deliberately deferred deconstruction item (`crafting.md` §9: "plausible future hook, not designed here"). Builds on the Tier 3 freeform combine grammar already established for cable-laying and pipe-laying (`electricity.md` §3, `disposal.md` §3), the four-stage structural model explosives already established for damage (`explosives-destruction.md` §3), and the shared access-check function doors already call automatically (`id-access.md` §6).

## 1. Design philosophy

The native-vs-leftover test, aimed at construction: SS13's build order (wrench girder, secure plating, weld, wire) is a memorized tool-sequence puzzle whose ordering mostly exists to gate speed, not to model anything real. Two things in that sequence are genuinely real and worth keeping — a wall visibly passing through distinguishable physical states, and a handful of real physical dependencies (you can't weld a plate to a girder that isn't there yet, can't erect a girder on open vacuum with nothing under it). Everything else in the classic sequence is order-as-puzzle rather than order-as-physics, and doesn't survive the test.

Two decisions fall out of that:

- **The construction ladder is staged, not a single "place wall" action** — same reasoning explosives used for damage being tiered visible warning rather than a hidden HP counter (`explosives-destruction.md` §1).
- **Construction and destruction are two regimes on one object, not two objects.** A wall has exactly one shared state where the two systems meet — Sealed, which is explosives' Intact — and don't cross anywhere else. Same "two regimes, one system" move stamina made for exertion and oxy debt (`stamina.md` §1), applied here instead to building up versus breaking down.

## 2. The construction ladder — from open tile to sealed wall

| Stage | What it looks like | System effect |
|---|---|---|
| Open | Bare tile (with or without deck plating, §3) | Fully permeable — walkable, no occlusion, no atmosphere boundary |
| Framed | A girder kit combined onto the tile — skeletal lattice model | Blocks casual movement (impassable terrain) but not light, sightline, or atmosphere — air and light pass through the gaps the same way an unfinished scaffold would |
| Plated (unsealed) | A metal plate bolted to the frame — solid-looking, visible unwelded seams | Blocks light, sightline, and occlusion fully, same as a finished wall for those systems — but still leaks atmosphere at a slow rate, the same leak treatment Cracked/venting already gets (`explosives-destruction.md` §3) |
| Sealed | Final weld pass closes the seams | Fully airtight; counts as a real wall for flood fill, occlusion, and Area boundary purposes — this is explosives' **Intact** state, arrived at from the opposite direction |

Each transition is one Tier 3 freeform combine action with a short visible timer, interruptible by moving away or taking a hit — the identical pattern crafting's multi-step assembly already runs (`crafting.md` §2), not a new interaction to learn.

## 3. Deck plating — extending the footprint

Only relevant when building into previously-unclaimed open space rather than reconfiguring an existing interior, which already has floor as a map-authored given.

**Deck plating is a new placeable tile object**, laid via the same freeform combine grammar, consuming metal sheet stock. It's a state change on a tile cell the station's logical grid already spans (unclaimed, unrendered, no Area) — not new coordinate space, the same "operates on an existing tile cell" assumption every other placeable system in this project already makes. Flagged as an assumption for Cursor to confirm against the actual tilemap implementation, not a redesign of the grid itself.

**The one genuine order dependency in this doc:** a girder can't be framed onto a tile with no deck under it. Everywhere else in the ladder, order is staged for visibility, not gated by physics — here it actually is one.

**Real stakes while unsealed.** Until every wall segment enclosing a new region reaches Sealed, the space inside it is exposed to vacuum — reusing whatever vacuum-exposure hazard already exists rather than inventing one, the same assumption disposal's own phase-2 note already made ("reusing whatever vacuum-exposure hazard already exists," `disposal.md` §8). A crew building a hull extension is racing a real, physical risk to themselves the whole time, not filling in an abstract progress bar.

**Connectivity is free, not authored.** Deck plating laid adjacent to existing structure joins the same tile grid the station already uses, so it inherits Area, power, and access resolution the moment it's enclosed (§4). A deliberately disconnected remote plating cluster is legal but stays cut off from the main power backbone until a physical corridor connects it — electricity's own rule already covers this ("an SMES with no live thick-cable path back to any generator gets nothing," `electricity.md` §3). No new design needed for isolated outposts; the existing connectivity rules already produce that behavior.

## 4. Area recompute — the construction direction

The moment a wall or door segment reaches Sealed and completes an enclosure, the same local recompute Area's own doc proposed and explosives already confirmed runs again — re-running flood fill only for the region touching the changed tile (`area.md` §3, `explosives-destruction.md` §4).

Two cases, one code path:

- **Subdividing an existing area** — the local flood fill finds a smaller enclosed region inside what was one Area and splits it off as a new unnamed Area. This is the automatic version of the manual split Area's own doc already allows for real cases the geometry alone can't decide (`area.md` §3) — now triggered by geometry actually changing, not just authored by hand.
- **Claiming new footprint** — deck plating laid into open space and fully walled produces a new unnamed Area from tiles that belonged to no Area at all. This is the identical operation Area's own map-authoring flood fill already performs at round start (`area.md` §3), just running locally and mid-round instead of once, station-wide, at authoring time.

No new area-creation logic to design — both cases call the function Area's doc already specified. Once the Area exists, everything derived from membership re-derives automatically: power, cameras, alarm scope, access defaults, ambience — the identical "falls out of Area already being the single source of truth" point explosives already made for the destructive direction (`explosives-destruction.md` §4), true here for the same reason.

## 5. Doors

A door is a branch off the same ladder, not a separate system: at Framed, instead of continuing to Plated, a girder can receive a door-assembly kit (Tier 3 combine) and finishes into a functional door the same way a wall finishes into Sealed.

**Access resolves automatically, with no separate authoring step.** The moment the door's Area membership is known — immediately after §4's recompute — the shared access-check function reads that Area's default requirement exactly the way it does for any map-authored door (`id-access.md` §6, `area.md` §5). A player-built door is exactly as secure as any other door in its Area from the instant it's sealed. A per-door override is available at the ID console afterward for a real exception, same as everywhere else.

## 6. Deconstruction — the clean counterpart to destruction

Resolves crafting's deferred item (`crafting.md` §9).

**The ladder runs in reverse, tool-by-tool:** Sealed → unweld → Plated → unbolt → Framed → dismantle → Open, recovering a real proportion of the original material at each step. Each step is an ordinary interruptible freeform action, same as construction going forward.

**Meets the damage ladder at exactly one state.** Deconstruction only ever starts from Sealed — the same state explosives calls Intact. A Damaged or Cracked/venting wall has to be repaired back to Intact first (already established, `explosives-destruction.md` §3) before it can be deliberately deconstructed. The two ladders don't cross anywhere else, kept deliberately separate rather than merged into one combined state machine — a wall's accidental-damage recovery path and its deliberate-teardown path never have to reconcile against each other.

**The contrast is the point.** Deconstruction is slow, tool-gated, and generates no debris. An explosive breach is fast, violent, and terminal, spawning rubble and skipping straight past every intermediate stage. Same object, two entirely different exit paths — not a bespoke third mechanic, just the two regimes this doc and explosives each already own, meeting cleanly at one shared state.

## 7. Materials

Real, countable stock — metal sheets and bars — the same treatment crafting already established for every other freeform material (`crafting.md` §5). Girder kits and deck-plating segments are ordinary freeform recipes: known from the start, no research gate, for the same reason crafting already gave for why simple assembly isn't gated (`crafting.md` §4).

**Windows and reinforced glass are a material variant on the same ladder** — a transparent plate substituted at the Plated step — not a separate system.

## 8. HUD touchpoints

No new permanent chrome.

- Partial states render on the tile/object itself — frame, unsealed plate with visible seams, final weld pass — the same "the object is the readout" discipline crafting's partial-construction state and armor's wear already follow.
- Vacuum exposure during unsealed hull construction surfaces through whatever hazard indicator already exists elsewhere for vacuum exposure — not reinvented here.
- A new or split Area produced by §4's recompute is one more real trigger for Area's own flagged-but-unbuilt transient area-name popup (`area.md` §7) — this doc doesn't newly justify building that hook, just adds a trigger for it whenever it does land.
- The PDA construction guide (`crafting.md` §2) gains wall, door, and deck-plating recipes as ordinary entries — no new tab.

## 9. Worked examples

**A — Subdividing Engineering's main bay:**

| Step | What happens | System state |
|---|---|---|
| 1 | An engineer lays a line of girders across the open bay | Each tile blocks movement; light, sightline, atmosphere unaffected |
| 2 | Plates go up one tile at a time | Bay is now visually and optically split, still leaking slowly along the seam |
| 3 | Final weld pass finishes the last tile | §4 recompute runs on the enclosed side; a new unnamed Area splits off |
| 4 | Power, cameras, and access re-derive automatically | The new sub-bay needs its own APC placed to actually have power — Area membership alone doesn't conjure one |

**B — Extending the hull, a new science annex:**

| Step | What happens | System state |
|---|---|---|
| 1 | A suited crew member lays deck plating out from an existing airlock into open space | Each plated tile joins the station's tile grid; still exposed, no walls yet |
| 2 | Girders go up around the perimeter | Impassable frame outline, still fully exposed to vacuum inside |
| 3 | Plating follows, seams unsealed | Sightline and light now contained; slow leak persists; the crew inside is still working in a real vacuum risk the whole time |
| 4 | Final weld on the last wall segment | §4 recompute claims the enclosed tiles as a brand-new Area; whatever handles atmospherics fill takes over from here, not this doc |

**C — Deconstruction versus an explosive breach, side by side:**

| | Deconstruction | Explosive breach |
|---|---|---|
| Speed | Slow, several tool-gated steps | Instant on detonation |
| Material | Recovered, real proportion | Lost, spawns debris instead |
| Path | Sealed → Plated → Framed → Open | Intact → Damaged → Cracked/venting → Destroyed |
| Intent | Deliberate, orderly | Violent, often hostile |

## 10. Integration notes

| Construction element | Touches existing / needed system |
|---|---|
| Girder, plate, weld steps | Tier 3 combine/use-with grammar (`main-hud.md` §8), crafting's freeform pattern (`crafting.md` §2) |
| Deck plating | Existing tilemap grid (assumption flagged for Cursor), material stock (`crafting.md` §5) |
| Sealed state | Explosives' Intact state (`explosives-destruction.md` §3) — same representation, arrived at from the opposite direction |
| Area recompute on enclosure | Area's local recompute (`area.md` §3), reused wholesale from explosives (`explosives-destruction.md` §4) |
| Door access | Shared access-check function (`id-access.md` §6), Area default requirement (`area.md` §5) |
| Vacuum exposure mid-build | Existing vacuum-exposure hazard (not redesigned here, per `disposal.md` §8's identical assumption) |
| Power for a new sub-area | APC placement is a separate, still-required action (`area.md` §5, `electricity.md` §4) |
| Deconstruction | Reverse of the construction ladder, meets damage ladder only at Sealed/Intact |

## 11. Out of scope for this pass

- Exact material costs, build times, and deck-plating costs (a balancing pass, not a design decision)
- Editor/admin tooling for map-authoring-time construction — Area's own doc already scoped this out for its authoring pass (`area.md` §8), not reopened here
- Atmospherics' actual air-filling/pressurization mechanic for a newly sealed room — decoupled per Area §4, assumed to exist elsewhere
- Multi-deck/z-level construction — no z-level system exists anywhere in this project, the same note explosives already made (`explosives-destruction.md` §9)
- RCD-style instant-build tooling — explicitly decided against for this pass; construction stays staged freeform only
- Blueprint/template stamping for multi-tile room layouts — a plausible future addition, not core to this pass
- Structural load-bearing simulation beyond the four-stage ladder — not modeled, the same grain explosives already chose for its own damage model

## 12. Prototyping this

Same two-stage pipeline as the rest of this project.

**Claude Design, prompt 1 — the construction ladder:**
> Using our SS3D design system, show one wall tile across its four construction stages — Open, Framed (skeletal girder lattice), Plated (unsealed, visible seams and a faint hiss/vapor), Sealed (finished weld) — side by side, same toon/half-toon shading and visual language as explosives' four damage stages, since Sealed and Intact are the same state.

**Claude Design, prompt 2 — hull extension into open space:**
> Show a sequence: deck plating laid out from an existing airlock into open space, a girder perimeter going up around it, plating following with a vacuum-exposure hazard indicator visible on a suited crew member inside, and the final weld moment triggering the area-name popup as the new region claims its own Area.

**Cursor, prompt 1 — data contract first:**
> Here's the construction design doc. Define the per-tile construction-stage state (Open/Framed/Plated/Sealed, plus the door branch off Framed), the deck-plating tile state for previously-unclaimed cells, and the freeform recipe records for girder/plate/deck-plating (reusing crafting's recipe record shape from `crafting.md`). Show me the data contract before wiring any interaction.

**Cursor, prompt 2 — one vertical slice:**
> Implement one wall built end to end in an existing interior room — Open through Sealed — reusing Area's existing local-recompute function directly to confirm the subdivide case works. Hull extension into unclaimed space, door construction, and deconstruction come after this is reviewed.
