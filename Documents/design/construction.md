# Construction — design document

> Status: active
> Supersedes: the deferred draft of this file (pre-2026-10 review), which built every structure on a combine grammar that no longer exists and re-zoned areas when walls closed

Covers what an engineer does with a toolbelt and a stack of sheets: re-lay a floor, patch a hull hole, weld a cracked wall back to whole, rebuild a wall or window a charge took out, put up a barricade, and take one down again. It is the constructive half of the structure explosives already breaks (`explosives-destruction.md` §3). Fabricating machines, doors and new deck into space are not part of this pass (§14).

The first round this serves is Nuclear Operatives: operatives blow walls, floors and the hull open, and this doc is how the crew answers.

## 1. Design philosophy

The native-vs-leftover test, aimed at construction: SS13's build order (wrench the girder, secure the plating, weld it, wire it) is a memorized tool sequence whose order mostly exists to slow you down. Two things in it are real and worth keeping: a wall visibly passes through states you can tell apart, and a few real physical dependencies (you can't plate a girder that isn't there, can't stand a girder in wreckage). The rest is order as a puzzle, and it goes.

Four decisions fall out of that:

- **Building is staged, not one "place wall" action.** A wall goes up in steps you can see, the same reason explosives makes damage a visible ladder rather than a hidden counter (`explosives-destruction.md` §1).
- **The object is the readout.** What a structure needs next is visible on the structure and in its examine text, never in a build menu or a progress panel (`crafting.md` §2's rule, kept).
- **Few tools, each with a clear job.** Two tools and three materials cover everything in this doc (§2).
- **Construction and destruction meet at one state.** A finished wall is explosives' Intact. The building ladder and the damage ladder never cross anywhere else (§9).

## 2. Tools and materials

| Item | What it does here |
|---|---|
| **Welder** | Repairs damaged walls, windows and plating; seals a plated wall; cuts a finished wall's seams and a bare girder apart |
| **Crowbar** | Pries floor tiles up; clears wreckage; pulls a cut plate off a girder |
| **Steel sheets** | Hull plating, girders, wall plates |
| **Glass sheets** | Window panes |
| **Floor tiles** | The walkable surface over plating |

Sheets and tiles are real, countable stacks (`inventory-storage.md` §5). There is no wrench, screwdriver or rod step in this pass.

**Anyone can build.** Holding the right tool and material is the only requirement; nobody builds faster for their job. The Engineer's advantage is kit and access: a toolbelt, sheets and tiles in their loadout, and the engineering storage. An Assistant who picks up a welder can help.

## 3. How a player builds

Everything in this doc is one action: **use the item in your active hand on a target in the world.** Sheets on a hull hole, a welder on a cracked wall, a crowbar on wreckage. It is the same "use item on item" action crafting needs (`crafting.md`'s 2026-10 note), not a new grammar.

**A placement ghost shows where a placeable item will go.** While the item in your active hand can be placed (sheets over a hull hole or over open floor, floor tiles over bare plating), the hovered tile shows a ghost of the result: green where it can go, red with a short reason where it can't ("Clear the wreckage first", "Needs plating underneath"), and what it will use against what you hold ("2 / 14 steel sheets"). Steps on something already there, like plating a girder or welding a wall, need no ghost: you use the tool on the object.

**Valid targets light up.** While a tool or material is in your active hand, the things it can act on are outlined, in a style distinct from the ordinary selection outline.

**Every step is timed and interruptible.** Moving away, being knocked down (by wind too) or taking a hit cancels it. **Nothing is spent until a step finishes**, so a cancelled step costs time, never sheets.

**There is no build menu.** No recipe list performs an action; the world does.

## 4. Floors and the hull

A floor has two layers, as `explosives-destruction.md` §3 defines: the floor tile you walk on, and the plating under it, which is the hull and holds the tile's pipes and cables.

| Situation | Action | Result |
|---|---|---|
| Hull hole (no plating) | Steel sheets on the hole, one placement | Plating; the tile is airtight again at once |
| Damaged or Cracked plating | Welder | Plating back to Intact |
| Bare plating | Floor tiles | A walkable floor; cosmetic, you can already walk on plating |
| Floor tile, on purpose | Crowbar | The tile comes up into your hands; the plating, pipes and cables underneath are exposed |

The floor is built from the bottom up and every layer shows. Prying a tile never vents a room; only a missing plating layer does.

**Patching a hole is fast enough to do in the wind.** The room is still venting while you work, and the wind can knock you down and cancel the step (§3). Patching a breach in a suit, ideally with magboots, is meant to be the heroic moment (`armor.md` §3).

**No new deck into space.** Plating goes only where the station already has a floor tile cell. Extending the hull outward is out of scope (§14).

## 5. Air after a patch

**A patched room gets its air back by itself.** Once the hole is sealed, the area's vents push air back in from the distribution network up to their normal target pressure (`atmospherics.md` §4), at a rate you can watch on the air alarm. An engineer can set the air alarm to its refill-after-a-breach preset to speed that up.

Without this, patching a hull hole would save nobody without a suit for the rest of the round, and the patch would mean nothing.

## 6. The wall ladder

| Stage | How you get there | What it looks like | Movement | Light and sight | Air |
|---|---|---|---|---|---|
| Open | — | Floor | passes | passes | passes |
| Girder | Steel sheets on a clear floor tile | A steel frame that joins up with neighbouring girders and walls | blocked | passes | passes |
| Plated | Steel sheets on a girder | Sheets bolted onto the frame, seams unwelded | blocked | blocked | **passes** |
| Sealed | Welder on a plated girder | A finished wall | blocked | blocked | sealed |

**Only the weld seals.** A plated wall looks solid and blocks the view, but air goes through its seams as if it were open. There is no slow leak to track: either the seams are welded or they aren't. The review already dropped partial leaks from cracked walls (`explosives-destruction.md` §3); this keeps the same single rule. The plated stage must look visibly unfinished, through seams, bolts or bare frame edges, so a player can tell it from a sealed wall at a glance.

**Sealed is Intact.** A wall the crew built is a wall like any other: it takes damage, cracks and breaks exactly as `explosives-destruction.md` §3 describes.

**Build anywhere on existing floor.** A girder can go on any clear floor tile, not only where a wall used to be. Crews can barricade a corridor, close off a breach route or wall in a choke point. Build time, not a placement rule, keeps the crew from turning the vault into a fortress before the operatives arrive (§10). Requiring "a wall used to be here" would need a hidden memory of the map, which breaks "the object is the readout".

**Areas never change.** A new wall separates the air on its two sides, but both sides keep their area, power, access and cameras (`area.md` §3). Construction never re-zones the station, the same rule explosives follows for breaches (`explosives-destruction.md` §4).

## 7. Windows

**The material decides wall or window.** Steel sheets on a girder make a wall; **glass sheets on a girder make a window**, finished in one step with no weld. There is no grille and no separate low-wall step. A window is the transparent plate substituted at the plated step, sealed on completion, on a slightly longer timer than plating a wall.

A window blocks movement and air and lets light and sight through. Plain glass only; reinforced windows are out of scope (§14).

## 8. Wreckage and salvage

A destroyed wall or window leaves wreckage (`explosives-destruction.md` §3).

- **Wreckage blocks building.** A girder can't go on a tile with wreckage on it. This is a real physical dependency, not an imposed order.
- **Crowbar the wreckage to clear it**, and you get back some of the steel sheets that went into it.

Materials are finite: a loadout's stacks plus a stocked engineering closet. Salvage is the only other source, so an operative breach feeds the crew's repairs, and "do we fix the vault wall or the corridor floor first?" is a real choice.

## 9. Repair and deconstruction

**Repair.** A welder returns a Damaged or Cracked wall, window or plating straight to Intact in one step (`explosives-destruction.md` §3). A Destroyed one is rebuilt (§6, §7), not repaired.

**Deconstruction runs the ladder backwards, from Intact only.** A damaged wall is repaired first; it can't be taken apart while damaged.

| From | Action | To | Returns |
|---|---|---|---|
| Sealed wall | Welder: cut the seams | Plated | — |
| Plated | Crowbar: pull the plate off | Girder | steel sheets |
| Window | Crowbar: lift the pane out | Girder | glass sheets |
| Girder | Welder: cut it apart | Open | steel sheets |

Deconstruction takes clearly longer than building, and it makes no debris. Anyone can do it, operatives included: quietly cutting through a side wall instead of blowing it is fair play.

**Reinforced walls can be repaired, not deconstructed or built.** The vault and other reinforced walls still need explosives to open, so the vault stays the operatives' C4 problem.

**The contrast is the point.** Deconstruction is slow, quiet, tool-gated and gives material back. A breach is instant, loud, and leaves only wreckage. Same structure, two exits.

## 10. Feel targets

Exact numbers are a balancing pass. These set the intended feel for a short round:

| Action | Target |
|---|---|
| Lay a floor tile | ~1 s |
| Patch a hull hole | ~3 s, short enough to finish in the wind |
| Weld a repair | ~4 s |
| Rebuild a wall, girder to sealed | ~15 s across its three steps |
| Deconstruct | about twice the build time |

Patching a breach is fast and heroic; fortifying costs real time while the operatives are on the clock.

## 11. HUD touchpoints

No new permanent chrome. Everything below is temporary or lives on the object.

- **Progress ring** over the target while a timed step runs, for every timed action in the game (defusing and medical too). It shows done, and on a cancel a brief reason ("Knocked down", "Moved away").
- **Placement ghost** with its fail reason and cost (§3).
- **Valid-target outline** while a tool or material is in the active hand (§3).
- **Examine** shows a structure's stage and its next step with the tool or material it needs, e.g. "Plated girder. Seams unwelded: a welder would seal it." (`examine.md` §5, §7).
- **Radial menu verbs** for each action where one target offers more than one: Repair, Weld seams, Cut seams, Pry tile, Clear wreckage, Pull plate, Dismantle girder.
- **Sound and sparks** carry the work: welding hisses and throws sparks, a crowbar scrapes. They are the primary feedback alongside the ring.
- **The air alarm on the wall** is the readout for a room refilling after a patch (§5).

A paper engineering quick reference in the engineering closet is a possible later aid if testers can't work out the steps. The PDA construction guide waits for the PDA (`crafting.md` §2).

## 12. Worked examples

**A — Patching a breached corridor:**

| Step | What happens | System state |
|---|---|---|
| 1 | A charge on a wall next to the corridor blows the floor through to space (`explosives-destruction.md` §8 E) | Hull hole; the corridor vents; wind pulls toward the hole |
| 2 | An engineer in a suit and magboots holds steel sheets over the hole | Green ghost, "1 / 20 steel sheets" |
| 3 | The wind knocks them down halfway through | Step cancelled, "Knocked down"; no sheets spent |
| 4 | They brace and patch it in ~3 s | Plating; the hole is sealed |
| 5 | The corridor's vents refill it; the engineer sets the air alarm to refill | Pressure climbs back on the alarm; the crew can take their masks off |
| 6 | Floor tiles go down over the bare plating | Cosmetic; the corridor looks whole |

**B — Rebuilding the blown wall:**

| Step | What happens | System state |
|---|---|---|
| 1 | The wall the charge took out is wreckage | Open to the next room; no girder can go here |
| 2 | Crowbar on the wreckage | Cleared; a few steel sheets recovered |
| 3 | Steel sheets on the tile | Girder: blocks movement; air and sight pass |
| 4 | Steel sheets on the girder | Plated: blocks sight; air still passes |
| 5 | Welder on the plate | Sealed: an Intact wall; the two rooms' air is separate again; both keep their areas |

**C — A barricade, and an operative's answer:**

| Step | What happens | System state |
|---|---|---|
| 1 | Two crew build a line of girders and plates across the corridor to the vault | Sight blocked; air passes until welded |
| 2 | They weld it before the operatives arrive | A sealed wall on what was open floor |
| 3 | An operative cuts the seams with a welder and pulls a plate off | Slow and quiet, but a route opens without spending a charge |
| 4 | The vault's reinforced wall can't be cut | The vault still needs C4 |

## 13. Integration notes

| Construction element | Touches |
|---|---|
| Floor tile and plating layers, hull holes, wreckage | `explosives-destruction.md` §3 |
| Sealed = Intact; damage on built walls; repair to Intact | `explosives-destruction.md` §3 |
| No re-zoning when walls go up or come down | `area.md` §3, `explosives-destruction.md` §4 |
| Refill after a patch | `atmospherics.md` §4 (vents, air alarm presets) |
| Working in vacuum and wind | `armor.md` §3 (suit seal, magboots), `atmospherics.md` |
| Stacks of sheets and tiles | `inventory-storage.md` §5 |
| Use-item-on-target action | shared with `crafting.md` |
| Examine stage and next-step text | `examine.md` §5, §7 |
| Radial verbs, outlines | `main-hud.md` |

## 14. Out of scope for this pass

- **Doors.** Rebuilding or building a door needs electronics and access, the start of machine fabrication. A destroyed door stays destroyed this pass
- **Machine and furniture fabrication or repair**, including broken tables and machines (`explosives-destruction.md` §9)
- **New deck into space**: lattice, catwalks, plating beyond the station's existing floor, hull extensions
- **Reinforced walls and windows**: building or deconstructing them (repairing them is in)
- **Grilles**
- **Wrench, screwdriver and rod steps**
- **A build menu, blueprints or multi-tile templates**, and RCD-style instant building
- **The PDA construction guide**, until the PDA exists
- **Area changes from construction**, by the 2026-10 review (`area.md` §3)
- Exact material costs and timings (a balancing pass; §10 sets the feel)
- Multi-deck construction: the station is a single plane (`vision.md` §3)
- Load-bearing structure beyond the stages above
