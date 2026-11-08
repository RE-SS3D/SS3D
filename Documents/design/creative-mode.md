# Creative Mode & Map Authoring — design document

> Status: active

How maps get made: the in-game **Map Editor** an admin opens to build or change a station, what it can place, how a map is checked before it's played, and how a finished map gets from one person's editor into everyone's game. It also covers bringing an existing SS13 map in as a starting point.

## 1. Design philosophy

**An authoring tool, not a gamemode.** The Map Editor is something an admin opens in **any** session — on an empty server, between rounds, or in the middle of a playtest to fix a wall. It isn't a special round type with its own role. That keeps the loop short: build, play a round on it, open the editor again, fix what the round showed you. A dedicated Builder gamemode is a later pass (§15).

**It builds real data, not a parallel sandbox.** Every wall, door, area and device placed in the editor is the same object every other doc already reads and writes. There's no separate "creative buffer" to reconcile later, which is what makes a saved map genuinely playable.

**The editor's palette is a deliberate exception to "no abstract menu."** Everywhere else in this project, a floating menu independent of the world is exactly what gets redesigned away. The editor is different in kind: it's an out-of-fiction authoring context, the same carve-out round config's settings screen makes (`round-config.md` §1). Confining the exception to this tool keeps it from becoming precedent anywhere else.

**Cover what a playable map needs, then grow.** The editor's first job is to make every map a supported gamemode can run on buildable without leaving the game. Every device from every system doc is the eventual goal, not the first pass.

## 2. The Map Editor

An admin opens it from the in-game admin tools. While it's open, the admin flies a free camera over the station and builds with a palette.

**The palette is grouped by what you're placing** — floors, walls, wall attachments (lights, alarms, APCs), furniture and devices, items, pipes and cables, and markers (spawn points, areas). The groups follow how a map is layered, so it's always clear what a placement will sit on top of or replace.

### The palette's layout

**Written for someone who has never mapped SS13.** The first people outside the project to build a station arrive with no SS13 vocabulary, so tab and sub-tab names are plain words (*Subfloor*, not *Plenum*; *Starting Air*, not gas markers). The entries themselves keep their in-game names (an SMES is called SMES), which is what an SS13 veteran searches for and what examining the object in a round shows.

**An object goes where it sits, not with the system it serves.** An APC is a wall attachment even though it belongs to the power network; a vent stands on the floor with the machines even though it belongs to atmospherics. This keeps the layering promise above: the tab you took something from tells you what it occupies. When an object exists both as a placed structure and as something you carry (a floor tile, a cable coil, a sheet of steel), what you place decides: the placed form follows its layer, the carried form is an item.

Four tabs, each sub-tab in the order a room gets built (§13 example A):

| Tab | What it holds | Sub-tabs |
|---|---|---|
| **Upper** | Everything the crew walks on, through, or past | **Floors** · **Walls & Windows** · **Doors** · **Wall Mounts** (lights, switches, alarms, APCs, posters) · **Furniture** (tables, seats, plants, lockers, crates) · **Machines** (anything powered or interactive that stands on the floor: vendors, kitchen and medical machines, SMES and generators, vents and scrubbers, disposal bins) · **Decals** |
| **Lower** | The runs laid under the floor, dragged out tile by tile | **Subfloor** (plenum, lattice, catwalk) · **Cables** · **Gas Pipes** (with the valves, pumps, filters, mixers and meters that sit inline on them) · **Disposal Pipes** |
| **Items** | Anything lying loose at round start, grouped by what it *is*, not who uses it | **Tools** · **Materials & Parts** · **Clothing** · **Medical** · **Food & Drink** · **Devices** (IDs, PDAs, headsets, radios) · **Weapons** · **Misc** |
| **Scripts** | The markers of §2's list — things a player never sees as an object | **Spawn Points** (jobs and antagonists) · **Rooms** (areas, §7) · **Starting Air** |

Lower mirrors §4's three run types one for one, under the subfloor they all rest on. Items are grouped by kind because each item then has exactly one obvious home; a crowbar belongs to every department but is only ever a tool. Objects a mode's fit check requires (§8), such as the nuclear device and its disk, are filed like anything else — the device with the machines, the disk with the devices — and the fit check names what's missing so search can find it.

**Search reaches the whole palette.** Typing a name finds the entry in any tab and shows which tab it lives in, so knowing where something is filed is never a requirement for placing it. Searching by department ("security") turns up that department's items, which covers stocking a room without grouping items by department.

**Big sub-tabs get headings, not more tabs.** Once a sub-tab passes roughly twenty entries it is divided under headings (Machines under Atmos, Power, Kitchen, Medical, Vendors, Disposal; Clothing under uniforms, shoes, headwear and so on), which gives the per-department view a veteran mapper expects. A heading becomes its own sub-tab only when it holds about a dozen entries *and* a newcomer can tell which side of the line an object falls on. A sub-tab with nothing in it is not shown, and nothing appears in the palette ahead of the system that makes it work. The editor reopens on the tab last used, and on Upper › Floors the first time.

**The eraser is not a palette entry.** It's a tool beside the others, because it clears every kind of placement the same way (below).

**Placing works like placing anything in the game.** A selected entry follows the cursor as a preview on the tile it would occupy; rotate it, then click to place. What gets placed is the **finished object** — a sealed wall, a working door, a device in its default state — with no material cost and no build stages. Placing onto a tile resolves adjacency (wall joins, pipe connections) exactly as a normal build would.

**The eraser applies uniformly** — a tile, a device, an item or a pipe segment clears the same way, straight back to absent.

**What a first-pass map needs, the editor places:**

- **Structure** — floors, walls, doors, windows.
- **Devices** — APCs, air alarms, vents, scrubbers, lights, lockers, vending machines, and the rest of the station's furniture.
- **Items** — anything that should be lying on the map at round start, including the **nuclear device** and its **authentication disk** (`antagonist-content.md` §6).
- **Spawn points** — job spawn points, and antagonist spawn points such as the Nuclear Operative spawn pad.
- **Areas** — painted onto tiles, each with a **name, department and access** (§7).
- **Starting air** — enclosed rooms start the round full of breathable air; the author can mark a room as vacuum or as holding something else.

## 3. Placing and configuring

**Most objects carry no placement-time setup.** An APC inherits whatever area its tile belongs to (`area.md` §5). A vending machine's stock, a locker's contents or an air alarm's preset are set through that object's own interface after it's placed, exactly as a player would in a round.

**Areas and markers are the exception.** An area's name, department and access, and a spawn point's job or antagonist role, *are* what those things are — there's no in-world interface for them — so the editor sets them in fields when you place or select them.

**Devices start in their default state** — an SMES starts empty, a portable generator unfuelled. A newly built section stays honest about what it needs to come online.

## 4. Utility routing — cables & pipes

**Cable and pipe segments place the same way tiles do**, and a placed segment connects to any matching segment on an adjacent tile. Three types, never one generic "pipe":

- **Cable** — one type (`electricity.md` §3).
- **Gas pipe** — distribution and waste networks (`atmospherics.md` §6).
- **Disposal pipe** (`disposal.md` §3).

Placing the wrong type where a builder meant another is a real, visible mistake, not something the tool smooths over. **Endpoints** (APC, SMES, vent, scrubber, chute) are ordinary devices from §2 — place the device, then route to it.

## 5. Bulk tools

- **Rectangle-fill** — for floors, walls and repeated objects. Drag a rectangle; every tile inside gets the selected entry, exactly as if placed one by one.
- **Path draw** — for cables and pipes. Drag from a start tile to an end tile; the tool lays a straight or right-angled run between them. Nobody hand-clicks two hundred cable tiles.
- **Copy-paste (stamp)** — select a built region (tiles, objects and areas together) and paste it elsewhere in the same session.

**A bulk operation is one undo step**, not one per tile (§6).

## 6. Undo/redo

**One shared history for the session.** Undo reverts the most recent edit, whoever made it; redo puts it back. A new edit after an undo clears the redo history. With one admin building, as is usual, that's simply their history. Separate per-builder histories, which only matter with several people editing at once, are a later pass (§15).

## 7. Areas

**Areas are painted.** Select the area tool, paint tiles, and set the area's name, department and access (`area.md` §3). This is how a vault becomes a locked vault and a maintenance tunnel becomes a place with no power.

**A starting set for maps without areas.** An imported map (§11) or a fresh build gets areas spread outward from each APC as a default (`area.md` §3); the author repaints from there.

## 8. Checking a map fits a mode

A map is only playable for a gamemode if it has what that mode needs. The check is **per gamemode**:

- **Every job** the mode offers has at least one spawn point.
- **The mode's own needs are on the map.** Nuclear Operatives needs the nuclear device, the authentication disk and an operative spawn point (`antagonist-content.md` §6).

The check runs in two places:

- **On save**, as a **warning** — "this map can't run Nuclear Operatives: no disk" — so the author knows while they're still editing. Saving a map that isn't ready for every mode is fine.
- **Before a round starts**, as a **block** — a round can't begin on a map that doesn't fit its mode (`round-config.md` §5).

## 9. Saving, playtesting and promotion

**Saving writes the map to a station file** on the server: tiles, areas, objects, items, pipes and cables, spawn points and starting air. Save as often as you like, under different names.

**The edit → playtest → edit loop.** Editing happens inside a running round: an admin with a body opens the editor on the live station, builds, closes it and plays what they built straight away. Ending the round unloads the station, so edits must be saved before then. An admin who presses Stop with unsaved edits is asked to **save and stop, discard and stop, or cancel**. A round that ends any other way (timer, gamemode) saves unsaved edits as a separate `<map>-autosave` file and never overwrites the original. A saved map can be picked for the next round (`round-config.md` §2). That loop is the whole point of the tool.

**Promotion is a file handoff.** A map gets into everyone's game when its author hands the station file to the maintainers and it's added to the game itself. There's no in-game approval queue.

**Maps survive game updates.** Every station file records which version of the format it was saved in. When the game changes, older maps still load; anything that can't be carried over is **listed when the map loads** ("12 objects of a removed type were dropped") rather than silently vanishing.

## 10. Round integration

The editor runs inside an ordinary session (§1), so it needs nothing from round config beyond the ability to pick a saved map for the next round. Players in the same session can play while an admin edits; what the admin changes, they see.

## 11. Importing an SS13 map

An existing SS13 map can be imported as a **starting point**. The import converts what it can — walls, floors, doors, windows, common devices and their positions — and produces a **gap report**: every object it couldn't convert, by type and count, so the author knows exactly what to place by hand. Areas start from the APC default (§7). An imported map is never playable unedited; it's a head start, not a port.

## 12. HUD touchpoints

The editor's palette and fields are the one deliberate departure from "no abstract menu" in this project, and they appear only while the editor is open. Nothing about a normal round's HUD changes because the editor exists.

## 13. Worked examples

**A — Building a Nuclear Operatives test station:**

| Step | What happens | System state |
|---|---|---|
| 1 | Admin opens the Map Editor on an empty server | Free camera, palette open |
| 2 | Builds a small station: floors, walls, doors, an APC per room, lights, air alarms | Finished objects, no material cost |
| 3 | Paints a vault area with Command access | The vault's doors now require Command access (`area.md` §5) |
| 4 | Places the disk in the vault, the nuclear device, crew spawns and an operative spawn pad | Every piece the mode needs |
| 5 | Saves | The fit check passes for Nuclear Operatives |

**B — The fit check catching a gap:**

| Step | What happens | System state |
|---|---|---|
| 1 | Author saves a map with no Cargo Technician spawn | Save succeeds with a warning |
| 2 | An admin picks the map for a round | The start is blocked: "Cargo Technician has nowhere to spawn" |
| 3 | Author places a Cargo spawn and saves again | The check passes |

**C — Playtest loop:**

| Step | What happens | System state |
|---|---|---|
| 1 | A round is played on the new map | The breach route into the vault turns out to be too short |
| 2 | After the round, the admin opens the editor, thickens the vault walls, saves | Same file, new version |
| 3 | The next round uses the updated map | No restart needed |

**D — Importing an SS13 station:**

| Step | What happens | System state |
|---|---|---|
| 1 | Author imports an SS13 map | Walls, floors, doors and common devices convert |
| 2 | Gap report lists 40 unconverted object types | The author's to-do list |
| 3 | Areas start from the APC default | Author repaints the vault and the maintenance tunnels |

## 14. Integration notes

| Element | Touches existing / needed system |
|---|---|
| Areas painted with fields | `area.md` §2, §3, §5 |
| Cables, gas pipes, disposal pipes | `electricity.md` §3, `atmospherics.md` §6, `disposal.md` §3 |
| Nuclear device, disk, operative spawn | `antagonist-content.md` §6 |
| Picking a map for a round; start blocked by the fit check | `round-config.md` §2, §5 |
| Job spawn points | `lobby.md` §5, §7 |
| Opening the editor | `admin-tools.md` |

## 15. Out of scope for this pass

- **A Builder gamemode** — a round type with a single Builder role where everyone present builds.
- **Separate undo histories per builder**, and arbitration between several builders editing the same tiles.
- **The full device vocabulary** — every device from every system doc; the palette grows as systems ship.
- **Liquid pipes** — `atmospherics.md` §12.
- **An in-game map pool or approval queue** — promotion is a file handoff (§9).
- **A persistent stamp/template library** across sessions — copy-paste is session-scoped (§5).
- **Map metadata beyond a name** — author, tags, thumbnails.
- **Griefing prevention while editing** — server policy, not a mechanic.
