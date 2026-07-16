# Comms — design document

> Status: active

Split out from `main-hud.md` once this grew past what a single HUD-layout section could hold. Same design philosophy applies throughout (§1 of that doc): if the world can carry the information, use the world instead of a panel; keep what's genuinely good regardless of engine; minimal permanent chrome.

## 1. Design philosophy

Same split already used for vitals: hearing is momentary and diegetic, history is persistent and on-demand. Speech is text throughout — rendered as in-world subtitles, not dependent on voice chat — which keeps the whole system accessible and leaves voice as an additive layer later rather than a requirement now (§10).

## 2. What's being cut, and why

- **The always-on scrolling chat box.** It exists because BYOND's client has no other way to surface dialogue — everything funnels through one text stream regardless of who's talking or where. It works, but it means players spend a surprising amount of a round reading a corner of the screen instead of looking at the world.
- **Typed radio prefixes** (`;`, `:h`, `:c`, etc.). Functional, but keyboard-driven and effectively wiki-only knowledge for new players. Replaced by a visual channel selector (§6).

The text itself isn't being cut — SS13 players use chat for exact quotes, coordination, and investigation, and none of that goes away. It's demoted from the primary interface to the history interface (§9).

## 3. Local speech

Face-to-face talk renders as a subtitle chip anchored near the speaker's head, not a comic-book bubble — flat surface, hairline border, matches the rest of the HUD's visual language. It fades a few seconds after the line finishes, scaling roughly with message length.

**Distance and occlusion — three tiers, not a full propagation sim:**

| Tier | Condition | Rendering |
|---|---|---|
| Clear | Within ~5m, unobstructed | Full text, full opacity |
| Muffled | ~5–10m unobstructed, **or** blocked by solid geometry within a short grace distance | Garbled/partial text — signals someone's talking without revealing content |
| Inaudible | Beyond ~10m, or blocked and beyond the grace distance, or a sealed barrier (airlock) | Nothing renders |

The grace distance matters: occlusion isn't just "beyond range," it can trigger at close range too — a doorframe, a workbench between two adjacent tiles. If blocked always meant inaudible, two people standing right next to each other with a thin obstruction between them would get a jarring hard cutout. So blocked-but-close degrades to muffled (you can hear someone arguing through a door without making out words); only blocked-*and*-far, or an explicitly sealed barrier, drops to fully inaudible.

The muffled tier is worth keeping deliberately even though it's the more complex option: a flat binary (audible / not) loses the "someone's talking, I can't quite make it out" tension that rewards moving closer, which is a genuinely good piece of social gameplay. A full physical propagation model (pressure, vents, open doors modulating range) is real engineering scope on top of atmospherics already simulated elsewhere in the game — worth flagging as a natural later hook once this base version is proven, not something to block v1 on.

**Speaker attribution is free.** The bubble originates at the speaker and sits near their nameplate, so there's no "Name: message" prefix to parse the way a scrolling log needs — position and the character model already answer "who's talking" the way they would in person.

## 4. Crowd handling

Bubbles are capped per viewer, prioritized by proximity — the nearest handful of speakers get bubbles, everyone beyond that compresses into a small "+4 more talking nearby" chip. Same restraint the alerts stack already uses: show what's actionable, compress the rest instead of enumerating everything. A packed bar or a security debrief with a dozen people talking at once needs this from day one, not as a later fix. Shouts get priority in this cap — see §8.

## 5. Composing a message

Everything above is display. Input stays symmetric with how intent works (`main-hud.md` §7) — a base action plus a modifier changes what it does, rather than a separate mode you pre-select and forget:

| Input | Result |
|---|---|
| Compose key (e.g. `T`) | Opens a lightweight input box — appears on demand, gone after sending, no permanent chrome |
| `Enter` | Sends as normal speech |
| `Shift+Enter` | Sends as a whisper |
| `Ctrl+Enter` | Sends as a shout |

A small mode pip inside the box shows which one's about to fire, with the same fading-hint treatment as the intent modifiers — labeled for new players, fades to a minimal tick once each has been used a few times.

**Radio** piggybacks on the channel radial (§6): hold the comms key, pick a channel, the compose box opens pre-tagged to send there instead of locally.

## 6. Radio & other non-positional channels

A bubble means "this is happening at this physical point" — radio isn't a physical point relative to the listener, so it never renders as one. Radio, department broadcasts, and similar channels render instead as a screen-space line in the comms feed (bottom-left of the main HUD), tagged with a channel icon and department, e.g. a small chip reading `[eng] Bob: plasma leak in engineering`.

**Channel selection** reuses the project's existing radial grammar rather than introducing a new widget type: hold the comms key, a radial shows available channels, release on one to select — same interaction language as the object radial and the disarm/grab modifier reuse, not a new pattern to learn. The persistent channel chip in the main HUD is what that selection updates.

## 7. Non-diegetic channels

OOC, LOOC, dead chat, and admin/system messages have no in-world speaker to anchor a bubble to — a ghost has no body, and OOC isn't happening in-character at all. These fall back to the same screen-space feed used for radio, with distinct typography per channel (e.g. bracketed/italic for OOC) so it's visually obvious "this isn't happening in the world," not just differently colored text.

## 8. Communication modes

Shape, size, position, and icon carry the distinction between modes, not color alone — keeps it legible for colorblind players and avoids leaning on color for meaning the way the rest of this HUD already avoids doing.

- **Speak** — standard bubble, standard size and duration, three-tier distance/occlusion per §3.
- **Whisper** — privacy-preserving, not just quieter. No muffled tier: within a tight clear range (~1.5m) the listener reads it in full, outside that range they get nothing — the whole point of whispering is that content stays private. But fully invisible communication is exploitable (two people coordinating right next to a third with zero tell), so bystanders within a slightly wider notice range see a presence-only cue — a small ellipsis-style icon over the speakers, no text, enough to know something private is being said, never what.
- **Shout** — does double duty. Within an extended clear range, full text, same as speak but further out. Beyond that range, players still get a non-verbal directional ping (screen-edge indicator, no words) — the mechanical equivalent of hearing someone yell without catching the words, useful for calling for help across distance. Shouts also get priority in the crowd cap (§4): if the room's at capacity and someone shouts, it bumps a normal speaker rather than competing on proximity alone, since the entire point of shouting is not getting lost in noise.
- **Radio** — screen-space chip with a consistent channel icon, regardless of which department channel.
- **Station/AI announcements** — reserved for genuinely urgent messages (red alert, emergencies): a large, centered, hard-to-miss banner. Routine announcements (shift change, cargo's arrived) use the regular comms feed as a tagged line, same treatment as radio. If everything gets the banner, the banner stops meaning anything — same restraint the alerts stack already follows. Everything still gets logged either way.

## 9. History / log

Everything heard gets logged, including muffled lines the player only partially caught — flagged as "(partially heard)" rather than silently dropped, the way a real half-caught sentence would stick with you. Whisper content a player wasn't in range for is not backfilled into their log (they didn't hear it; the presence-only cue is what they get). Opened on-demand through the PDA, not a permanent on-screen box. Full transcript, timestamped, filterable by channel, with a rough location tag on local-speech entries — this is where the exact-quote, investigation, and coordination use cases that made SS13's chat box valuable actually live.

## 10. Voice compatibility

Nothing here assumes audio exists. If voice chat gets added later, it runs as an additional channel of the same speech event — the subtitle rendering, occlusion tiers, crowd cap, and log all keep working unchanged; voice supplies audio alongside the text that was already the source of truth.

## 11. Integration notes

| Comms element | Touches existing / needed system |
|---|---|
| Local speech, occlusion | Character positions, wall/collision geometry for the raycast |
| Crowd cap | Same restraint pattern as the alerts stack |
| Compose input | Intent module's modifier-chording and fading-hint pattern |
| Radio & channels | Radio/channel system, ID/access for channel permissions, radial menu grammar |
| Log | PDA (main HUD gear strip), timestamped storage keyed to the chat/message backend |

## 12. Worked examples

**Coordinating quietly next to a third party:**

| Step | What happens | Comms state |
|---|---|---|
| 1 | Two crew members want to plan without a third overhearing | Both within ~1.5m of each other, one sends `Shift+Enter` |
| 2 | Whisper resolves | The two see full text; the third crew member, standing a bit further off but in notice range, sees a presence-only ellipsis icon over them — no text |
| 3 | Third party gets curious | Moves closer; if they enter the tight whisper range before the line ends, they'd see it too — otherwise it's already faded |

**Calling for help across a corridor:**

| Step | What happens | Comms state |
|---|---|---|
| 1 | Player is hurt, no one's in normal speaking range | Sends `Ctrl+Enter`: shout |
| 2 | Nearby crew within extended shout range | See the full bubble, larger and bolder than normal speech |
| 3 | Crew further out, beyond even shout's text range | See a directional screen-edge ping only — no words, but they know someone's calling from that direction |
| 4 | A packed room nearby was already at bubble cap | The shout bumps a lower-priority normal speaker's bubble to show instead |

## 13. Out of scope for this pass

- Actual voice chat implementation, if it's ever added (this doc only guarantees the text/subtitle system keeps working alongside it)
- Full physical speech propagation (pressure, vents, open doors modulating range) — flagged as a later hook, not v1
- Material-aware occlusion (glass vs. metal vs. vents transmitting differently) — v1 treats all solid geometry the same
- Lip sync, head-turning, voice portraits, or other animation-driven "who's speaking" cues

