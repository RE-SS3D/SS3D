> Implements: Documents/design/audio.md §1 (wind, sound needs air), §2 (layered ambience), §3 (impacts, destruction, machines); Documents/design/area.md §2, §3, §5 (departments, ambience override); Documents/design/atmospherics.md §4 (an alarm alarms)
> Touches systems: audio, audio-content, area, structural-destruction, combat, atmospherics, electricity, map-editor-creative-hooks, persistence
> Status: shipped

**Progress:** All phases shipped. Compile, EditMode and `mppm:breach` are green on the merged tree, and the owner auditioned D2, A, B and C. The owner dropped the targeted smoke (D1, A) and the D1 traffic measurement; the open traffic question is in [TECH_DEBT](../../architecture/TECH_DEBT.md).

# Ambience and impact audio (Nov 2026)

The station sounds empty. Explosions, wall breaks and melee hits on structures make no world sound, machines are mostly silent, and no area has ambience. This effort adds those sounds in the order that pays off soonest: impacts first because they need no map authoring and C4 is silent in a Playtest 1 slice, room tone third because it depends on tagged areas. [2026-07_audio-foundation](../../architecture/2026-07_audio-foundation.md) built the mechanisms and keeps only its lobby music phase.

## Starting state (2026-11-04, `a2f75673d`)

| Area | Today |
|---|---|
| Explosion | Silent. `_boomClip` is empty in `Assets/Resources/BlastVfxCatalog.asset`; `BlastExplosionEffect` would play it through its own `AudioSource` |
| Wall or table breaking | Visuals only, through `TileSubSystem.RpcSteelBreakup` |
| Melee on a structure | No positional sound. `ServerNotifyMeleeConnectHit` only reaches the attacker |
| Bullet on a non-living target | The same six clips for every material (`CombatAudioTrackIds.SurfaceHit`) |
| Material data | `RpcStructuralImpact` carries a byte, but only for overlay-capable targets and only wood and glass tables differ from the default |
| Items hitting things | `NoisyCollision` exists; only `SodaCanGeneric` and its variants use it |
| Machines | Air alarm and vents have real state and no sound. `FireAlarm.prefab` has no controller |
| Room tone | `AmbienceSubSystem` crossfades one looped clip per area. No area has a track |
| Authoring | `AmbienceTrackId` isn't saved. `areaambience` needs a raw GUID. Department is free text |
| Live changes | `PollCurrentArea` returns early when the area is unchanged, so a new track waits until the player leaves the room |
| Maps | `ministation` has one area. Metastation has 82 named `APC · ENGINEERING BAY`. No department tags on either |
| Atmos on clients | `FlowData` is CPU-side. Pressure is only a GPU texture in `AtmosRenderContext.Snapshot` |
| Attribution | `StationAmbience1–15` and `SpaceAmbience2` are SS14 copies with no rows in `ATTRIBUTIONS.md` |

`StationAmbience1` and `StationAmbience2` are byte-identical, so there are 14 distinct station clips.

## Decisions

| # | Decision |
|---|---|
| 1 | Destruction sounds play on clients from existing messages (`RpcStructuralImpact`, `RpcSteelBreakup`, the blast RPC), through a new client-side play path on the occluded pool, which protects [test-server.md](../../milestones/test-server.md) T3. Only a client can choose the debris nearest its listener. Only steel has a breakup, so any other break sends one `RpcStructuralImpact` with no surface: a new use of an existing message, one per destroyed tile. |
| 2 | `RpcStructuralImpact` fires for every landed melee, ranged or console hit, not only overlay-capable targets, and gains a one-byte `StructuralImpactCue` (protocol 17). Blast hops still send nothing per hit. A shot on a structure drops its `RpcPlayAudioSource`, so gunfire traffic should fall. Measure the traffic. |
| 3 | One pure function derives metal, glass or wood from `genericType` and `specificType`. `TileObjectSo.impactMaterial` overrides it for exceptions, which today are the two windows, typed `Wall`/`Steel`. Unknown objects are metal. |
| 4 | Machine loops use a client-local `AmbientLoopSource`, added by prefab recipes and driven by synced power or alarm state. Only the nearest 12 or so play. It reuses `AudioSourceOcclusion`. |
| 5 | Room tone lives in `AmbienceProfile` assets in a catalog: bed, occasional clips, a power-gated flag. Each has a readable id such as `engineering`, which also fixes the `areaambience` GUID problem. |
| 6 | `ParentTag` stays a string, validated against the fixed list from [area.md](../../design/area.md) §2. Saved values match case-insensitively; anything else loads as no department. |
| 7 | A power-gated bed follows the Area's Equipment channel. `SyncedAreaLighting` is the existing client path and can carry it. |
| 8 | Clients get a CPU copy of pressure beside `FlowData`. Wind and pressure are sampled locally with no new messages. |
| 9 | Muffling is per source in `AudioSourceOcclusion`, because a mixer filter can't exempt sounds within one tile. Ambience takes the same factor. Personal audio doesn't. |
| 10 | Ad-hoc emitters that should muffle move onto the pool, including `BlastExplosionEffect` and the cracked-wall hiss. |
| 11 | Sound effects may be CC0, CC-BY, CC-BY-SA or CC-BY-NC, each with an `ATTRIBUTIONS.md` row. A commercial release would have to replace the NC clips. Custom-licensed music stays out. |
| 12 | Bullets on metal keep the ricochet set, because melee can't share it. Wood and glass use one set for every source. |
| 13 | A client holds each break 50 ms so a blast can claim it as debris. The server sends breakups before the blast message, and reversing that would run the scorch raycast against furniture about to be destroyed. |
| 14 | Item collisions stay server messages, because clients keep items kinematic and get no collision events. `CollisionSoundLimiter` caps them with a 0.25 s cooldown per item and a shared budget of 5 at once, refilling at 5 per second. No protocol change. |
| 15 | `NoisyCollision` takes one material and draws on `ItemCollisionAudioTrackIds`, instead of clip lists on every prefab. Speed sets volume, not a light or hard clip set. `AudioContentPrefabRecipes` holds the item lists, and a test fails if an item is in neither. Soft items such as clothing, patches and food stay silent, because there are no soft clips. Body parts and organs are left out. |
| 16 | Voices belong to `AmbientLoopSubSystem`, not to machines: 12 `AudioSource`s, each following the nearest running `AmbientLoopSource`. A station's machines cost no audio sources or occlusion rays until they're in reach. Each kind's clip, level and reach live in `AmbientLoopTrackIds`, so tuning touches no prefab. |
| 17 | Vents, scrubbers and pumps sound while gas is moving (`IsPortFlowing`), not merely while switched on. The APC hums while it holds charge, the SMES while it's on and charged, a vendor while powered, and an air alarm while powered and alarming. |
| 19 | Clients get one resolved profile id per area, not the department. The id is the override, else the department in lower case, and `AmbienceProfileCatalog.Resolve` falls back to `station`. A department with no profile yet sounds generic, and outside any area is silence. |
| 20 | `Maintenance` and `Hallways` join the dropdown and `Civilian` leaves it, matching [area.md](../../design/area.md) §2. Neither new department has default access. |
| 21 | The room form gets an Ambience dropdown, which the plan left to `areaambience`. That command targets a spawned body, so it can't reach a room in the Map Editor. |
| 22 | `classic-ministation` replaces `ministation` as the map to tag, because `ministation` has two rooms and #163 removed Metastation. The owner tags and renames its rooms in the Map Editor. |
| 23 | A bed's Equipment fade needs the channel on and the grid covering the load. A battery carries only the lights, so the hum goes first. It rides the lighting snapshot as one more field, with no new message. Protocol 18, because develop is also on 17 with a different wire shape. |
| 24 | Only `engineering` is power-gated, because `PowerHum` implies a machine. Command, Security, Science, Service, Cargo and Hallways play `station`, because there are no distinct clips for them yet. `morgue` is available as an override. |
| 25 | One clock schedules occasional sounds across rooms, with 25 to 70 s gaps from the end of the last one, so walking through short rooms can't put them off forever. The air bed plays in any area until C ties it to real air. |
| 26 | Air is the one tile under the listener, not a blend, because a blend would read walls as low pressure and leak vacuum through the hull. Hearing runs from full at 70 kPa to nothing at 5 kPa. A wall tile keeps the last reading. |
| 27 | A tile with no data counts as air unless the server's `IsFloating` says the body is in open space. A pure client only holds chunks that changed since it joined, so missing data can't mean vacuum. A late joiner who missed a settled breach therefore hears air in that room until [atmos client sync](../../architecture/2026-07_atmos-client-visualization-sync.md) Phase 2 sends a late-join bootstrap. |
| 28 | Everything positional takes the muffle through `AudioSourceOcclusion`, so footsteps and the cracked-wall hiss gain it, and with it wall occlusion. The Music pool takes air only (`IgnoresWalls`), which keeps the jukebox clear of the wall lowpass. The hiss stays on its own source rather than moving to the pool as Decision 10 planned. |
| 29 | Wind is three looped layers, `WindLight`, `WindHeavy` and `WindViolent`, crossfaded by speed in `AudioAirState.WindGain`. The clips are SS3D-Art and sit about 18 dB apart, so moving up the layers is what makes wind louder. Space uses `SpaceAmbience2`, the attributed clip. |
| 30 | The air bed now plays wherever there's air, in or out of an area. `Breathing.wav`, imported earlier and unused, is the calm breath below 20% air, on its own source, and heavy breathing takes over from it. "Well away from the hull" reads loaded tiles within 6, because the atlas may not hold them. A ghost hears the air on its tile, with no breathing. |
| 18 | SS14's `vending_machine_hum`, `engine_hum` and `light_hum` have no attribution entry, so they're out. Vendors and APCs use `PowerHum` and the SMES uses SS3D-Art `Machinery2`. The air alarm uses SS3D-Art `Alarm 1` instead of SS14 `alarm.ogg`. The generator keeps the loop it gets in `develop` from #159, so A leaves it alone. |

## Clips

| Use | First cut | License |
|---|---|---|
| Blast | SS14 `explosion1–3`, `explosionfar` | CC-BY-SA-3.0 |
| Metal break | SS14 `metal_break*`, `metal_crunch` | CC-BY-NC-4.0 |
| Glass break | SS14 `glass_break*`, `window_shatter*` | CC-BY-SA-3.0 |
| Wood break | SS14 `wood_destroy*` | CC-BY-SA-3.0 |
| Hit and strain | SS14 `metal_scrape*`, `glass_smack`, `glass_crack*`; SS3D-Art metal and wood hits | CC-BY-SA-3.0; project assets |
| Wreckage thud | SS14 `metal_thud*` | CC-BY-SA-3.0 |
| Item collisions | SS3D-Art `World/Collisions` metal, wood, plastic | Project assets |
| Machine loops | SS14 `gas_vent`, `portable_scrubber`, `gas_pump`; SS3D-Art `Machinery2`, `Alarm 1`, `PowerHum` | CC0-1.0; project assets |
| Engineering | `ambitech*`, `ambiatmos*` | CC-BY-SA-3.0 |
| Medical | `ambinice` | CC-BY-SA-3.0 |
| Maintenance | `ambimaint*`, `maintambience` | CC-BY-SA-3.0 |
| Morgue override | `ambimo*` | CC-BY-SA-3.0 |
| Everything else | `StationAmbience*` | CC-BY-SA-3.0 |
| Bed candidates | `PowerHum`, `AirStagnant`, SS14 `shipambience` | Check per file |
| Wind | `WindLight`, `WindHeavy`, `WindViolent` | Project assets |
| Space bed | `SpaceAmbience2` | CC-BY-SA-3.0 |

Confirm each license in SS14's `attributions.yml` at import. `metal_slam1–3` are CC-BY-SA-3.0 and `metal_slam5` is CC-BY-NC-4.0. `metal_slam4`, `glass_knock` and `airlock_creaking` have no entry, so leave them out. The owner auditions beds in-game, because seams and repetition need a human ear.

## Phases

| Phase | Work | Verify |
|---|---|---|
| D1 | Material function. Client-side pool play. Hits, breaks and blasts play material sounds; Cracked adds strain; wreckage landing adds a thud. One boom with a capped debris tail; a distant variant by range. `BlastExplosionEffect` moves to the pool. Attribution rows, including the 16 missing ones. | EditMode for material and the debris cap; `mppm:breach`; targeted smoke |
| D2 | Thrown and dropped items through `NoisyCollision`, with a rate cap so a breach doesn't turn into clatter. | EditMode for the cap; `scenarios:Environment` |
| A | `AmbientLoopSource` on air alarms, vendors, APCs, SMES, vents, scrubbers, gas pumps and generators, power-gated and occluded. Confirm each has a real state and cut any that doesn't. The fire alarm is out until it has a controller. | EditMode for nearest-source selection; targeted smoke |
| B1 | Fixed department list and Map Editor dropdown. Starting areas get neutral names instead of the APC title. `AmbienceProfile` catalog, `SavedAreaRecord` override, friendly `areaambience`, live track changes. Generic bed for untagged areas. | EditMode for save round trip and profile lookup; map editor scenario |
| B2 | Bed plus occasional clips in `AmbienceSubSystem`, the thin air bed, Equipment-channel fading, then department profiles. Owner tags `classic-ministation`. | EditMode for the scheduler; owner audition on `classic-ministation` |
| C | CPU pressure copy. Wind bed from `FlowData`. Per-source muffling with the one-tile exception. Space bed away from the hull, near-silence and breathing elsewhere. | EditMode for the muffle curve; `mppm:breach`; T3 8-client line unchanged |

Validate each phase with `Testing/ui/validate.sh --steps compile,editmode` plus the focused steps above ([testing philosophy](../../architecture/2026-11_testing-philosophy.md)). Prefab and asset work needs the Editor open with UnityMCP connected. Run `update-system-docs` as each PR lands.

## Not in this effort

- Lobby music, which stays in [2026-07_audio-foundation](../../architecture/2026-07_audio-foundation.md) Phase 5
- Sounds for light fixtures, which stay silent by design. A wall switch clicks when it toggles
- Fire alarm behaviour
- Volume balancing beyond what's needed to audition ([audio.md](../../design/audio.md) §10)

## Related docs

- Maps: [audio](../../architecture/systems/audio.md), [audio-content](../../architecture/systems/audio-content.md), [area](../../architecture/systems/area.md), [structural-destruction](../../architecture/systems/structural-destruction.md), [atmospherics](../../architecture/systems/atmospherics.md)
- Milestone: [mvp1-nuke-ops.md](../../milestones/mvp1-nuke-ops.md) M1c, M8
