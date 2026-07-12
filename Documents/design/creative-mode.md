# Creative Mode & Map Authoring — design document

> Status: active

Resolves an item Area's own doc flagged and set aside: "actual editor tooling UI for the merge/split/rename workflow (separate from this data-architecture pass)" (`area.md` §8). Reuses round_config's pool model directly rather than inventing a new selection mechanism (`round-config.md` §2), and reuses construction's structural vocabulary and Area's local recompute wholesale (`construction.md` §2–§5).

## 1. Design philosophy

**Creative mode is a normal gamemode-pool entry, not a bolted-on special case.** Round config already treats gamemode selection as "a set of enabled entries, each with a relative weight and a precondition... drawn from with a deterministic fallback" (`round-config.md` §2) — creative mode fits that shape exactly, so it needs no selection mechanism of its own.

**It builds real data, not a parallel sandbox.** Every wall, door, and Area placed in creative mode is the same object every other doc already reads and writes — there's no separate "creative buffer" to reconcile later. That's what makes exporting a genuinely usable map possible at all, and it's why this doc can answer Area §8 instead of just adding a new deferred item next to it.

**The construction menu is a deliberate exception to "no abstract menu."** Everywhere else in this project, a floating menu independent of the world is exactly what gets redesigned away. Creative mode is different in kind, not degree — it's explicitly a meta/authoring context, not in-fiction gameplay, the same carve-out round config's own admin config screen already makes for itself: "a real settings surface, not a checkbox-laden webpage" (`round-config.md` §1), still a deliberately-designed UI, just not a diegetic one. Confining that exception to this one mode keeps it from becoming precedent anywhere else.

## 2. Construction menu

A real panel/palette, not a radial or Tier 3 hand-tool grammar — the exception above, made concrete.

- **Palette entries reuse construction's exact vocabulary**: wall, door, window, deck plating (`construction.md` §2, §3, §5, §7) — no new object types invented for this mode.
- **Placement is instant.** Selecting an entry and clicking a valid tile places it directly at its finished state — Sealed wall, functional door, laid deck plating — skipping the staged ladder, material cost, and timers that earn their keep in normal play but add nothing here. Whichever adjacency mesh variant a normal build would resolve to, creative placement resolves to the same way, automatically — same underlying object and rules, just no intermediate stages to click through.
- **An eraser tool is the mirror of placement, not of deconstruction.** Clicking a built tile clears it straight back to Open. This is deliberately not the tool-gated reverse ladder from `construction.md` §6 — there's no material to recover and no reason to gate a meta tool behind a real-world action.
- **Single-tile only for this pass.** One placement or erase per click — no rectangle-fill, multi-tile stamps, or copy-paste. A natural extension once this baseline is proven, not designed here.

## 3. Area authoring

The construction menu's area tool is the concrete answer to Area §8's deferred item: select tiles and rename, merge, or manually split their Area — the exact manual-override behavior Area's own doc already specified as needed "for real cases the geometry alone can't decide" (`area.md` §3), now given a real interface instead of remaining unbuilt.

**Automatic recompute still runs underneath, unchanged.** Sealing a wall that encloses a subregion still splits it via the same local recompute this project already established (`construction.md` §4). The manual area tool exists for the cases recompute alone can't decide — a pillar-fragmented open bay that should stay one Area, a walled-off private office that shouldn't — not as a replacement for the automatic path.

## 4. Saving — this is the map pool

No new file format or export pipeline needed. Per §1, creative mode's tile and Area data already *is* the same data every round loads at start — "save" is a snapshot of that live state to a named file, nothing more.

**A save becomes a new entry in round config's existing map pool** (`round-config.md` §2), disabled by default. Getting it into rotation is the identical admin action that already enables any other map pool entry — the pool's own enabled toggle is the review gate; there's no separate approval system to design.

**Saving isn't a once-per-session action.** Nothing about the underlying data model requires waiting for round end — a builder can snapshot iterative versions under different names as a session progresses.

## 5. Round integration

**One more gamemode pool entry**, same weight/precondition/enabled shape as everything else in the pool (`round-config.md` §2) — no parallel selection path. In practice a server keeps its weight at or near zero so it never randomly drafts into ordinary rotation, and an admin raises it deliberately when a build session is wanted.

**No antagonist categories resolve.** Round config already handles a gamemode with an empty antag-category set — its own safe-default fallback is exactly that (`round-config.md` §3) — creative mode is simply another mode that resolves to nothing there, chosen deliberately rather than reached by fallback.

**Job list collapses to a single Builder role** for the session — a job-list delta handed to lobby the same way any gamemode-specific delta already crosses over (`round-config.md` §4). Department and job distinctions don't mean anything when everyone present is there to build, not to staff a station.

**Hazards and antagonists simply don't spawn**, because nothing in the mode's own runtime logic spawns them — the same boundary round config already draws around "how a gamemode picks its antags... that mode's own business" (`round-config.md` §6); this mode's business is just building. Preventing griefing or PvP during a session is a server-policy question, not a mechanic this doc needs to design.

## 6. HUD touchpoints

The construction menu panel is the one deliberate departure from "no abstract menu" in this entire project, and it's confined to this mode's own session — nothing about a normal round's HUD changes because this mode exists.

## 7. Worked examples

**A — Designing a new outpost annex:**

| Step | What happens | System state |
|---|---|---|
| 1 | Admin enables and weights the creative-mode pool entry for one round | Round config draws it like any other mode (§5) |
| 2 | Builders lay deck plating out from the station's edge, wall a perimeter, place a door | Every object placed is the same one construction's doc defines — instant, no material or timer |
| 3 | Final wall seals the perimeter | Area's local recompute claims the new footprint as its own Area, exactly as it would in a normal round (`construction.md` §4) |
| 4 | Builders save the result as "Outpost — Annex Draft 1" | New disabled entry appears in round config's map pool |

**B — Area tool resolving a real edge case:**

| Step | What happens | System state |
|---|---|---|
| 1 | A private office sits inside a larger open department bay with no wall of its own | Auto-detection alone would fold it into the surrounding Area |
| 2 | A builder selects its tiles with the area tool and splits it out, naming it | Exactly the manual-override case Area §3 already anticipated, now actually doable in-game |

**C — A saved map reaching rotation:**

| Step | What happens | System state |
|---|---|---|
| 1 | "Outpost — Annex Draft 1" sits disabled in the map pool after the session | No different from any other disabled pool entry |
| 2 | An admin reviews it, sets a weight, enables it | Ordinary round config admin action (`round-config.md` §5) — no separate promotion step exists or is needed |
| 3 | A later round draws it | Same draw mechanism as any other map |

## 8. Integration notes

| Creative mode element | Touches existing / needed system |
|---|---|
| Gamemode selection | Round config's pool/weight/precondition/fallback shape (`round-config.md` §2–§3) |
| Job list override | Job-list delta handoff (`round-config.md` §4, `lobby.md` §2) |
| Placeable objects | Construction's wall/door/window/deck-plating vocabulary (`construction.md` §2, §3, §5, §7) |
| Instant placement/erase | Skips construction's staged ladder and deconstruction sequence entirely (`construction.md` §2, §6) |
| Area boundary changes | Same local recompute as normal play (`construction.md` §4) |
| Manual area rename/merge/split | Area's deferred manual-override tooling (`area.md` §3, §8) |
| Saving | Round config's existing map pool (`round-config.md` §2) |
| Promotion to rotation | Ordinary admin config-screen action (`round-config.md` §5), no new system |

## 9. Out of scope for this pass

- Bulk/multi-tile tools — rectangle-fill, multi-tile stamps, copy-paste rooms — deferred per this pass's scope
- Placing non-structural objects — APCs, machines, fabricators, cargo pads, furniture, and every other device from every other doc. This pass covers the structural vocabulary construction already defined; full object placement is a larger, later pass
- Spawn-point authoring — a real gap in whether a saved map is actually round-usable yet, flagged explicitly rather than silently assumed, not designed here
- Undo/redo — a natural addition once bulk tools land, not needed for single-tile placement
- Simultaneous multi-builder edit-conflict resolution — last-write-wins is a reasonable default, not a design decision this pass makes formally
- Map metadata beyond a name — author, tags, thumbnail generation — a content/tooling nicety, not this pass's job
- Griefing/PvP prevention during a session — server policy, not a mechanic

## 10. Prototyping this

**Claude Design, prompt 1 — the construction menu:**
> Using our SS3D design system, build the creative-mode construction menu: a palette panel (wall, door, window, deck plating, eraser) and an area-authoring tool (select tiles, rename/merge/split). This is the one deliberate exception to diegetic-only UI in this project — a real panel, not a radial or in-world tool — so it should read as clearly a meta/editor surface, closer to the round-config admin screen's visual language than to any in-fiction HUD element.

**Claude Design, prompt 2 — hull extension and save flow:**
> Show a builder laying deck plating into open space and sealing a perimeter with the construction menu (instant placement, no timers), the moment the enclosed region claims its own Area, and the save dialog producing a new named, disabled entry in round config's map pool.

**Cursor, prompt 1 — data contract first:**
> Here's the creative mode design doc. Define the gamemode pool entry for creative mode (job-list override, empty antag categories), the instant-placement/erase functions (same wall/door/deck-plating objects as `construction.md`, skipping staged states and cost), and the save function (snapshot of live tile/Area state into a new round-config map pool entry, disabled by default). Show me all three before wiring any UI.

**Cursor, prompt 2 — one vertical slice:**
> Implement instant wall placement and erase in an existing interior room, confirm it reuses Area's local recompute correctly on enclosure, and wire one save producing a real disabled map pool entry. The area-authoring tool, hull extension into unclaimed space, and job-list override come after this is reviewed.
