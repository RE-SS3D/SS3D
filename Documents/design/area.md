# Area — design document

> Status: active

Not a UI document — this is the foundational systems layer that `main-hud.md`, `comms.md`, and `hacking-interface.md` already lean on implicitly (power net, ID/access, camera network). This doc gives that layer an actual shape.

## 1. Design philosophy

Same principle already applied everywhere else in this project: reuse one real system instead of maintaining parallel models. The hacking interface doc already established this for power net and ID/access ("no parallel permission model to maintain"). Area is the same move, generalized — six systems currently would each reinvent their own idea of "what room am I in" if this doesn't exist as one shared abstraction.

## 2. Data model

**Area record:**
- `id` — stable, unique
- `display name` — e.g. "Engineering — main bay"
- `department` — optional, picked from a fixed list: Command, Security, Engineering, Medical, Science, Service, Cargo, Maintenance, Hallways. Used for display/query grouping, the department's colour and its default ambience (§5), never for spatial resolution
- `ambience` — optional override of the department's ambience, for a room that should differ from it (§5)
- `access` — what the area's doors require by default (§5)
- `tile set` — which tiles belong to this area

**A tile belongs to at most one area.** Strict partition, not nesting — six independent systems each needing their own overlap-resolution rule is worse than the occasional workaround of giving a "sub-room" its own full area in the same department. Space and unpainted tiles belong to none.

Given a tile, its area is always immediately known — every consumer below asks "which area is this tile in" constantly.

## 3. Authoring

**Areas are authored rooms.** A map author paints an area onto tiles in the map editor (`creative-mode.md` §2) and sets its name, department and access. This is SS13's model: areas are part of the map, not something the game guesses. Painting is what makes the real cases work that geometry alone can't decide — an open-plan bay fragmented by support pillars that should stay one area, a private office with no wall separating it from its department, or a locked vault.

**Doors are boundaries.** Every door, open or closed, is a seam between areas — which is also where power and access already care about the transition.

**A default for imported maps.** A map without authored areas (for example one imported from SS13, `creative-mode.md` §11) gets a starting set by spreading outward from each APC until walls and doors stop it. Tiles no APC reaches start with no area. These starting areas get neutral names and no department, because an APC's own label says nothing about the room. The author then repaints from there.

**Areas don't change during a round.** A wall blown open connects two rooms' air, not their departments. A hole is not a merger: the tiles on each side keep their area, their APC, their access and their camera group (`explosives-destruction.md` §4).

## 4. Atmospherics and areas

Gas doesn't care about areas: it flows tile by tile wherever walls and doors let it (`atmospherics.md` §2). A breach connecting two areas' air doesn't make them one area.

What *does* use areas is atmospheric **equipment**. An air alarm watches the air in its area and drives that area's vents and scrubbers (`atmospherics.md` §4) — the same "which room am I in" question every other consumer below asks.

## 5. Consumer systems

**Power net.** Each area has **at most one** APC. An area without one (a maintenance tunnel, a vault) simply has no local power. This is the concrete simplification Area buys: the hacking interface doc already assumes devices know "which APC they draw from" — with Area in place, that's derived from physical location instead of authored per-device. Place a device on an area's tiles, it inherits that area's APC automatically.

**Lighting.** Rides the same area→APC link as power rather than existing as a separate system — lighting going out when an area loses power is the same event, not two. A **wall light switch** turns an area's lights off and on by hand, independent of the APC's breaker. An area's department also gives it a colour — tinted light and floor stripes — so you can tell where you are at a glance. Concretely, each area's lighting sits in one of three states driven by its APC's power state — Normal, Emergency (backup battery), Dark (no power left) — per `rendering-lighting.md` §4, which owns the full visual spec; this doc just supplies the APC state that drives it.

**Camera network.** Cameras on an area's tiles auto-register to a network group named after the area (or its department, for broader grouping on the security console — e.g. all of Engineering's sub-areas under one channel).

**Job/access restrictions.** Doors default to requiring whatever access the area they open into requires, rather than each door authoring its own requirement redundantly. Per-door override still available for real exceptions (a captain's private office inside a command area that itself only requires bridge access).

**Alarms & announcements.** An area is the default alarm/announcement scope — a fire alarm pulled in an area scopes to that area, with the department as the mechanism for bubbling a critical alert up to the whole department or station. The AI or crew target lockdowns/announcements at an area or its department.

**Ambience/audio.** An area sounds like its department unless it overrides that, and an area with no department gets a generic station sound. The player's current area (one tile lookup) drives ambient audio, crossfading at boundaries. `audio.md` §2 owns the full spec, including which sounds are power-dependent.

## 6. Worked example — Engineering, main bay

| System | What Area resolves |
|---|---|
| Power | Owns APC-ENG-03; every device on its tiles draws from that APC without being individually wired to it |
| Lighting | Tied to APC-ENG-03's power state — lights go dark if the APC does |
| Cameras | All cameras on its tiles register to the "Engineering — main bay" network group, filed under the "Engineering" department on the security console |
| Access | Doors into it default to requiring Engineering access; one door (workshop) is overridden to also require an additional cert |
| Alarms | A fire alarm here scopes locally by default; a station-wide emergency can additionally target the "Engineering" department to hit every area in it |
| Ambience | Plays Engineering's department ambience, a machinery hum; crossfades on crossing into the adjacent hallway's area |

## 7. Touchpoint back to the HUD

Worth a small, minimal hook later rather than a fixture: a transient area-name display on crossing into a new area (appears briefly, fades), consistent with the main HUD doc's minimal-permanent-chrome rule — not a permanent "you are here" panel.

## 8. Out of scope for this pass

- Areas changing during a round (rebuilding or re-zoning mid-round)
- The HUD touchpoint in §7 (noted, not spec'd)
- Multi-area effects beyond the department mechanism (e.g. area groups that aren't strictly hierarchical)
