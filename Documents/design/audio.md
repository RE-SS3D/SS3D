# Audio — design document

> Status: active

Formalizes a system four other docs have already been quietly assuming without anyone ever
specifying it. `area.md` §5 gives every Area "an ambience track id" driving "ambient audio,
crossfading at boundaries" without saying what that system actually is. `rendering-lighting.md` §6
cites that same field as precedent for departmental light color, calling it "free audio wayfinding"
— a claim about a system that was never itself designed. `main-hud.md` §5 and
`death-cloning-respawn.md` §7 both assume "heartbeat audio" exists for critical/dying and defib.
`stamina.md` §4 assumes "heavier breathing audio" for exertion. `virology.md` §5 assumes a "sound
cue" for symptomatic disease stages. None of those docs invented a parallel audio model — they all
pointed at one and moved on. This doc is what they were pointing at. `comms.md` §10 stays untouched:
voice chat, if it ever exists, is that doc's own additive layer, not this one's.

## 1. Design philosophy

**Real 3D geometry earns audio the same upgrade it already gave zone targeting and comms
occlusion.** BYOND-era SS13 audio is positional in spirit but tile-flat in practice — it never had
real geometry to occlude against or real distance to fall off across. This project already has both.
The upgrade here is the same move this project keeps making: replace an engine limitation with the
real physical model the limitation was standing in for.

**Everything maps to something physical, extended to hearing.** Every sound effect has a real source
position and falls off with real distance, the same "if you can't point at the real thing it
represents, it doesn't belong" rule the hacking interface already runs for its own panels
(`hacking-interface.md` §1). A global, sourceless sound layer is exactly the kind of artifact this
project cuts everywhere else — it just hasn't been named for audio until now.

**The air around you counts as a source.** Wind has no single point it comes from. It is the air
moving past the listener, so it plays at the listener, as strong as the airflow where they stand.
That passes the rule above: the real thing it represents is right there, and the same air is pushing
them. The rule still bars any layer with nothing physical behind it.

**Sound needs air.** Hearing follows the same physical model as everything else, so it thins out
when the air does (§3). A hull breach is something you hear go quiet.

**Occlusion is one more consumer of the shared raycast, not a parallel model.** Comms occlusion, the
hacking interface's wireless scan, combat cover, light occlusion, shuttle obstacle detection, and the
AI's camera line-of-sight already share one system rather than each maintaining its own
(`ai-cyborgs.md` §3 counts camera line-of-sight as the sixth consumer of that chain). Diegetic sound
occlusion is the seventh — a wall blocks a sound exactly the same way it already blocks a shot, a
subtitle bubble, or a wireless scan.

**Music is diegetic-sourced or it doesn't play.** If music happens during a round, it comes from a
real object in the world, with the same positional falloff and occlusion any other sound gets — not
a global soundtrack layer riding over the top of everything.

**Personal, internal audio is its own small, named exception.** A heartbeat or labored breathing
isn't a world event with a position — it's how a player learns their own state "by how it feels,"
the exact diegetic-but-personal category `main-hud.md` §5 already invented for this case. This doc
names that category so every future internal cue reuses it instead of reinventing its own rule.

## 2. Ambience — per-area, non-positional, crossfading

Formalizes `area.md` §5's existing field. Each Area has a real ambience — engineering's machinery
hum, medbay's quiet, a bar's murmur — played non-positionally at the player's own current Area,
crossfading to the new one the instant the same one-tile Area lookup every other Area-driven system
already uses (`area.md` §5) reports a change.

**Two layers: a bed and occasional sounds.** The bed is a quiet continuous sound that runs the whole
time you're in the Area, and it's what identifies the place. On top of it, a few sounds play now and
then with long gaps between them, so a room never settles into an obvious loop. A bed alone gets
monotonous. Occasional sounds alone leave a room silent between them and make a department hard to
recognise.

**The department sets the sound, and an Area can override it.** Tagging a room with its department
(`area.md` §2) is enough to give it the right ambience, so a map author doesn't have to pick a sound
for every room. A room that should differ from its department, like a morgue or a bar, sets its own.
An Area with no department gets a generic station sound, so no map is silent.

**Free wayfinding, the same instinct `rendering-lighting.md` §6 already borrowed from this field for
light color.** A player can tell which department they're in with their eyes closed, the identical
payoff that doc's departmental color extension claims for sight.

**Some beds are honestly power-dependent, not all of them.** A machinery hum implies something
actually running, so it depends on the Area's power, the same "lighting going out when an area loses
power is the same event, not two" principle `area.md` §5 already applies to its own Lighting bullet.
It fades out when the Area's Equipment channel goes off (`electricity.md` §4), which by default
happens before the lights go, because the machines it implies have stopped. A quiet room tone with no implied machine behind it has no such
dependency. Which beds are gated is authored per-bed, not a blanket rule. The occasional sounds carry
on either way.

**A thin air bed sits underneath everything.** It plays wherever there's air, so a dark room sounds
dead rather than broken, and the moment the hum dies is one a player notices.

**Wind.** Moving air (`atmospherics.md` §2) is heard continuously, rising and falling with the
airflow where the listener stands (§1). It's the same wind that pushes them and moves loose items,
not a separate sound effect laid over a breach.

**Outside the station.** Space belongs to no Area (`area.md` §2). There you hear near-silence and
your own breathing (§4). Well away from the hull, a very faint space bed plays, only so that the
silence doesn't read as broken audio.

## 3. Diegetic SFX — every sound has a source

Any sound tied to a specific object or event — footsteps, tool use, a door cycling, a weapon firing,
an explosion, an alarm, a machine running — carries a real position and falls off with distance,
occluded by walls and closed doors the same way light and comms already are (§1's seventh-consumer
claim).

**No new trigger mechanism.** This doc doesn't invent new events to hang sound off of — a door
already has an open/close event, a weapon already has a fire event, an explosion already has its own
resolution (`explosives-destruction.md` §2). Playing a positioned, occluded sound is a side effect
those events already produce; the roster of exactly which object gets which sound is content (§10).

**Impacts sound like what was hit.** Metal, glass and wood each have their own impact sound, and a
bullet, a melee hit and a blast all draw on the same set. A bullet hitting a wooden table doesn't
ricochet like steel. Living targets keep their own hit sounds.

**A structure sounds the way it fails.** Structural failure has stages
(`explosives-destruction.md` §3), and each is audible. Every landed hit plays an impact. Reaching
Cracked adds a heavier strain, then the hiss takes over. Destroyed plays the break, followed by a
thud when the wreckage lands.

**One blast is one event.** An explosion reads as a single dominant boom with a short tail of nearby
debris, never a full break sound for every wall it took out. Far away, it's a distant rumble rather
than a quieter copy of the same boom.

**Thrown and dropped items sound like their material too,** with restraint: a breach blowing a room's
loose items around should rattle, not become a wall of clatter.

**Machines sound only when they're really running.** A powered machine has its hum and an unpowered
one is silent. An alarm sounds only while it's alarming (`atmospherics.md` §4). Every light fixture
stays silent, because the room's bed already implies that hum and a station has far too many of them
to hear individually.

**Sound needs air.** As pressure falls where the listener stands, world sounds and ambience muffle
together toward silence, in a room with a hole in it as well as in open vacuum. Engineering's hum dies
as the air leaves, which tells you the air has gone before any alert does. Anything within about one
tile stays faintly audible, like a sound felt through a suit. Personal audio (§4) is unaffected,
because it was never carried by air.

## 4. Personal audio — heartbeat, breathing, and internal cues

Formalizes what `main-hud.md` §5 and `stamina.md` §4 already assume piecemeal: heartbeat audio for
critical/dying and resuming on a successful defib (`main-hud.md` §5, `death-cloning-respawn.md` §7),
and heavier breathing as stamina depletes (`stamina.md` §4).

**Heard only by the affected player, no position, no occlusion.** These represent an internal
sensation, not a world event — the same distinction that keeps this tier deliberately separate from
§3 rather than folded into it as a zero-distance special case.

**The shared pattern any future systemic cue should follow.** Virology's symptomatic-stage sound cue
(`virology.md` §5) is exactly this category, just triggered by a different system — this doc names
the pattern once so it doesn't get reinvented per domain.

## 5. Music

**In-round, music is diegetic-sourced only** — a jukebox, a radio, any real object already capable of
playing something — using the identical positional-falloff-and-occlusion rule every other diegetic
sound follows (§3). No global background score plays during a round.

**Lobby and menu screens are the one exception, because they already are one.** `lobby.md` §1
already carves that screen out of the diegetic-vs-panel test entirely, on the grounds that there's no
world yet to test against. A non-diegetic music bed there doesn't violate §1's in-round rule, because
it isn't in-round.

## 6. Alerts — a sound accompanies a new chip

The alert stack (`main-hud.md` §9) and PDA notification chips (`pda.md` §6) are visual-only today. A
short, restrained cue accompanies a *new* alert appearing — not a continuous loop — the same
"glanceable, only draws attention when something's actually wrong" restraint the visual chip already
follows, just extended to hearing. This is personal audio (§4), not positional.

## 7. HUD & touchpoints

No new permanent chrome — audio has no visual footprint of its own.

- Ambience, SFX, personal audio, and music are each their own real, separately-adjustable volume
  category — a plain settings concern, not a design decision (§10).
- Nothing here adds a visual indicator; a player learns about audio the way they hear it, not by
  watching for it.

## 8. Worked examples

**A — Crossing into a new Area:**

| Step | What happens | Audio state |
|---|---|---|
| 1 | A player walks from a corridor into Engineering's main bay | Area lookup resolves the moment the boundary tile is crossed (`area.md` §5) |
| 2 | Corridor's room tone fades out, Engineering's machinery hum fades in | Crossfade, not a hard cut |

**B — An explosion occludes through a wall:**

| Step | What happens | Audio state |
|---|---|---|
| 1 | A breaching charge detonates in an adjacent room | Real source position at the epicenter tile |
| 2 | A crew member in the next room over, behind an intact wall | Hears a muffled, attenuated version — the same occlusion rule blocking a shot or a subtitle bubble |

**C — Critical state, heard only by the patient:**

| Step | What happens | Audio state |
|---|---|---|
| 1 | A character's brain function starts dropping toward zero | Heartbeat audio begins, personal tier (§4) |
| 2 | Nobody else standing next to them hears anything | Not positional, not a world event |
| 3 | Defib succeeds | Heartbeat resumes; brain function reaching zero instead would have silenced it entirely |

**D — A jukebox in the mess hall:**

| Step | What happens | Audio state |
|---|---|---|
| 1 | Someone starts a track on the mess hall jukebox | Real diegetic source, positioned at the jukebox |
| 2 | A crew member two rooms away, around a corner | Hears nothing — falloff and occlusion apply exactly like any other diegetic sound |

**E — A hull breach in Engineering:**

| Step | What happens | Audio state |
|---|---|---|
| 1 | A breaching charge goes off against Engineering's outer wall | One dominant boom, then a short tail of nearby debris |
| 2 | Air rushes toward the hole | Wind rises with the airflow where the player stands |
| 3 | The room's pressure falls | Hum, wind and every other world sound muffle together toward silence |
| 4 | The room is vacuum | The player hears their own breathing, plus whatever they're touching |

**F — The lights go out:**

| Step | What happens | Audio state |
|---|---|---|
| 1 | Engineering's APC runs short of power | Equipment sheds first; the machinery bed fades while the lights stay on |
| 2 | The shortage continues | Lighting sheds last and the Area goes Dark |
| 3 | A player stands in the dark room | The thin air bed and occasional sounds remain |

## 9. Integration notes

| Audio element | Touches existing / needed system |
|---|---|
| Ambience track, per-Area crossfade | `area.md` §5 |
| Ambience power dependency | `electricity.md` §4 (channel shedding) |
| Department defaults and per-Area override | `area.md` §2, §3 |
| Wind and pressure at the listener | `atmospherics.md` §2 |
| Staged structural sounds, blast as one event | `explosives-destruction.md` §2, §3 |
| Alarm sound while alarming | `atmospherics.md` §4 |
| Departmental wayfinding parallel | `rendering-lighting.md` §6 |
| SFX occlusion | Shared raycast/occlusion system, seventh consumer (`ai-cyborgs.md` §3 names the chain through six) |
| Heartbeat audio | `main-hud.md` §5, `death-cloning-respawn.md` §7 |
| Breathing audio | `stamina.md` §4 |
| Symptom sound cue | `virology.md` §5 |
| Alert/notification cue | `main-hud.md` §9, `pda.md` §6 |
| Music source | Diegetic objects (jukebox, radio) — no new object type |
| Lobby/menu music exception | `lobby.md` §1's existing meta carve-out |
| Voice chat | `comms.md` §10 — explicitly not extended here |

**Companion edits needed:** `area.md` §5's Ambience/audio bullet, `main-hud.md` §5's heartbeat
mention, `stamina.md` §4's breathing mention, `death-cloning-respawn.md` §7's heartbeat-resume
mention, and `virology.md` §5's sound-cue mention should each cite this doc as the fuller spec.

## 10. Out of scope for this pass

- **Voice chat itself** — `comms.md` §10's own explicit territory, not extended here.
- **Exact volume levels, category mix balancing, and default slider values** — a balancing/settings
  pass, not a design decision.
- **The full sound-effect roster per object, weapon, or action** — a content pass.
- **Music track roster or licensing** — content, not a mechanic.
- **Which specific beds are power-dependent** — authored per-bed (§2), a content decision, not fixed
  here.
- **Which sounds belong to which department, and the exact gaps between occasional sounds** — content.
- **The exact pressure curve for muffling** — a balancing pass.
- **Any stealth/sneaking mechanic tied to footstep audio** — no such mechanic exists anywhere in this
  project yet; footstep audio is just an ordinary diegetic SFX (§3) that a future stealth pass could
  consume, not something this doc invents a use case for.
