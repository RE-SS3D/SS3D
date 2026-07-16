# Area — design document

> Status: active

Not a UI document — this is the foundational systems layer that `main-hud.md`, `comms.md`, and `hacking-interface.md` already lean on implicitly (power net, ID/access, camera network). This doc gives that layer an actual shape.

## 1. Design philosophy

Same principle already applied everywhere else in this project: reuse one real system instead of maintaining parallel models. The hacking interface doc already established this for power net and ID/access ("no parallel permission model to maintain"). Area is the same move, generalized — six systems currently would each reinvent their own idea of "what room am I in" if this doesn't exist as one shared abstraction.

## 2. Data model

**Area record:**
- `id` — stable, unique
- `display name` — e.g. "Engineering — main bay"
- `parent tag` — optional, organizational only (e.g. "Engineering"), used for display/query grouping, never for spatial resolution
- `tile set` — which tiles belong to this area

**Tiles belong to exactly one area.** Strict partition, not nesting. See the reasoning above — six independent systems each needing their own overlap-resolution rule is worse than the occasional workaround of giving a "sub-room" its own full area with a shared parent tag.

**Storage:** a per-tile area-id field, the same shape as any other tile data layer already on the tilemap (comparable to whatever layer already backs occlusion). O(1) lookup: given a tile, its area is a direct read, not a spatial query.

## 3. Authoring

**Auto-detection:** flood fill from any unvisited walkable tile, bounded by walls and closed doors (both already boundary data the tilemap has). Every enclosed region becomes an unnamed area. Doors are boundaries by design — the seam between two areas naturally sits at a door, which is also where power/access already care about the transition.

**Manual override:** rename, merge, or split auto-detected regions. Needed for real cases the geometry alone can't decide — an open-plan bay fragmented by support pillars that should stay one area, or a private office that should be its own area despite having no wall separating it from the surrounding department.

**Live geometry changes:** SS13-style play includes breaching and rebuilding walls mid-round. Area boundaries can't be purely a map-author-time artifact if that's true here too — a hole blown in a wall should be able to affect area boundaries as much as it affects atmosphere. Recommended approach: a local recompute, re-running the flood fill only for the region touching the changed wall/door tile, not the whole station. Flagged as an open scope question rather than assumed — worth confirming this is actually wanted before it's built in.

## 4. Why atmospherics isn't here

Deliberately not selected as an Area consumer. Gas simulation wants its own dynamic zone system — merging and splitting live as doors open or walls breach — which is a different lifecycle than Area boundaries (which change rarely, and only on structural events). In practice the two will usually align spatially, since both respect walls and doors, but they shouldn't be code-coupled: a breach connecting two areas' air doesn't mean those tiles suddenly share a power grid or a camera group.

## 5. Consumer systems

**Power net.** Each area optionally owns one APC. This is the concrete simplification Area buys: the hacking interface doc already assumes devices know "which APC they draw from" — with Area in place, that's derived from physical location instead of authored per-device. Place a device on an area's tiles, it inherits that area's APC automatically.

**Lighting.** Rides the same area→APC link as power rather than existing as a separate system — lighting going out when an area loses power is the same event, not two. This isn't a flat on/off, though: an area's lighting sits in one of three states, driven directly by its APC's power state and backup battery charge.

| State | Trigger | What it looks like |
|---|---|---|
| **Normal** | APC powered | Full fixture set, at the area's authored color/intensity |
| **Emergency** | APC unpowered, backup battery available | A distinct, reduced fixture subset — dim, red-tinted, battery-backed. Not a red-tinted variant of normal lighting; a separate authored state |
| **Dark** | APC unpowered, no backup remaining | No area-sourced light at all — only carried personal light or hazard light (fire, sparking consoles) illuminates the space |

Emergency exists as its own state deliberately, not as normal-lighting-with-a-filter — it's one of the most recognizable "something is wrong" signals in the source game, and it's free diegetic information the same way a dark alert chip or a bleeding wound already are elsewhere in this project: no icon required, the room says it. Dark means genuinely dark, not dimly lit — no ambient fallback papering over a dead APC, or the state stops meaning anything.

Full rendering/shading treatment (shader model, post-processing, shadow behavior) lives in `rendering-lighting.md`; this bullet is the systemic trigger the renderer reads, not the visual spec itself.

**Camera network.** Cameras on an area's tiles auto-register to a network group named after the area (or its parent tag, for broader grouping on the security console — e.g. all of Engineering's sub-areas under one channel).

**Job/access restrictions.** Doors default to requiring whatever access the area they open into requires, rather than each door authoring its own requirement redundantly. Per-door override still available for real exceptions (a captain's private office inside a command area that itself only requires bridge access).

**Alarms & announcements.** An area is the default alarm/announcement scope — a fire alarm pulled in an area scopes to that area, with the parent tag as the mechanism for bubbling a critical alert up to the whole department or station. The AI or crew target lockdowns/announcements at an area by referencing its id or parent tag.

**Ambience/audio.** An area carries an ambience track id; the player's current area (one tile lookup) drives ambient audio, crossfading at boundaries.

## 6. Worked example — Engineering, main bay

| System | What Area resolves |
|---|---|
| Power | Owns APC-ENG-03; every device on its tiles draws from that APC without being individually wired to it |
| Lighting | Tied to APC-ENG-03's power state: Normal while powered, Emergency if the APC drops but backup battery holds, Dark once that battery depletes |
| Cameras | All cameras on its tiles register to the "Engineering — main bay" network group, filed under the "Engineering" parent tag on the security console |
| Access | Doors into it default to requiring Engineering access; one door (workshop) is overridden to also require an additional cert |
| Alarms | A fire alarm here scopes locally by default; a station-wide emergency can additionally target the "Engineering" parent tag to hit every area under it |
| Ambience | Plays the "engineering — machinery hum" track; crossfades on crossing into the adjacent hallway's area |

## 7. Touchpoint back to the HUD

Worth a small, minimal hook later rather than a fixture: a transient area-name display on crossing into a new area (appears briefly, fades), consistent with the main HUD doc's minimal-permanent-chrome rule — not a permanent "you are here" panel.

## 8. Out of scope for this pass

- Actual editor tooling UI for the merge/split/rename workflow (separate from this data-architecture pass)
- Atmospherics' own zone-merging system (deliberately decoupled, per §4, but not designed here)
- The HUD touchpoint in §7 (noted, not spec'd)
- Multi-area effects beyond the parent-tag mechanism (e.g. area groups that aren't strictly hierarchical)

