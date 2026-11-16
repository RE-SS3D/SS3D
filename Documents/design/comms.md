# Comms — design document

> Status: active

Split out from `main-hud.md` once this grew past what a single HUD-layout section could hold. Same design philosophy applies throughout (§1 of that doc): if the world can carry the information, use the world instead of a panel; keep what's genuinely good regardless of engine; minimal permanent chrome.

## 1. Design philosophy

Same split already used for vitals: hearing is momentary and diegetic, history is persistent and on-demand. Speech is text throughout — rendered as in-world subtitles, not dependent on voice chat — which keeps the whole system accessible and leaves voice as an additive layer later rather than a requirement now (§10).

## 2. What's being cut, and why

- **The always-on scrolling chat box.** It exists because BYOND's client has no other way to surface dialogue — everything funnels through one text stream regardless of who's talking or where. It works, but it means players spend a surprising amount of a round reading a corner of the screen instead of looking at the world.
- **Cryptic radio prefixes** (`;`, `:h`, `:c`, etc.). Functional, but effectively wiki-only knowledge for new players. Replaced by a visible channel in the compose box that **Tab** cycles through, plus readable slash words (`/ooc`, `/eng`) for players who like typing (§5).

The text itself isn't being cut — SS13 players use chat for exact quotes, coordination, and investigation, and none of that goes away. It's demoted from the primary interface to a collapsible feed and its history (§9).

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

**Choosing a channel.** The compose box shows which channel the message will go to — local speech by default. **Tab** cycles through the channels you can use (local, your radio channels, OOC, LOOC, Admin help); the box's channel tag updates as you go. A **slash word** at the start of a message (`/ooc`, `/looc`, `/eng`) sends that one message to that channel without changing the selection. Both are visible: you always see where a message is going before you send it.

## 6. Radio & other non-positional channels

A bubble means "this is happening at this physical point" — radio isn't a physical point relative to the listener, so it never renders as one. Radio, department broadcasts, and similar channels render instead as a screen-space line in the comms feed (bottom-left of the main HUD), tagged with a channel icon and department, e.g. a small chip reading `[eng] Bob: plasma leak in engineering`.

**Channel selection** happens in the compose box (§5). Which radio channels you can use depends on the headset you wear and the access it was issued for (`id-access.md`).

## 7. Non-diegetic channels

OOC, LOOC, dead chat, and admin messages have no in-world speaker to anchor a bubble to — a ghost has no body, and OOC isn't happening in-character at all. They get **their own column on the right edge of the screen**, apart from everything in-character. Where a line appears tells you whether it's happening in the world, before you read a word: the bottom-left feed (§6, §9), subtitles and action lines (`action-messages.md`) are in-character; the right edge never is. Typography alone, in one shared feed, was the earlier answer — space is the stronger signal, and it keeps the in-character history clean for investigation.

**The column is quiet when idle.** A new line appears, then fades after a few seconds; with nothing new, the column is empty — minimal chrome, per `main-hud.md` §1 rule 3. A key or hovering the column brings back recent lines to read. A player setting keeps it always visible instead, for those who want it.

**Every line carries its channel.** A short tag and colour per channel (OOC, LOOC, DEAD, ADMIN), so four channels in one column stay distinct. OOC and admin lines show the sender's **account name**, never their character, so OOC can't casually tie a player to who they're playing. LOOC shows both — "Marcus Voss (henk)" — since it's about the people around you.

**Admin messages can't be missed.** A message from an admin to you persists in the column until you dismiss it, with its own sound. Server and system notices (a round restarting, a bug-report confirmation) sit in the same column in neutral grey.

Each channel can be **filtered** out of the column by a player who'd rather not see it. The same channels also live in the **Chat tab** of the lobby and the Escape menu, for longer reading.

- **OOC** — out of character, server-wide. **Rate-limited**: a player can't post more than a few messages in a short window, so a playtest's OOC stays readable.
- **LOOC** — local out of character: reaches players within about **10 m** of the speaker. For "sorry, lagged" and "how do I open this?" without breaking the scene for the whole server.
- **Dead chat** — ghosts only (`observer.md` §4).
- **Admin help** — a two-way conversation between one player and the admins (`admin-tools.md` §3). Admins also have an admin-only channel among themselves.

- **Admins can mute OOC mid-round** from the server settings (`round-config.md` §2) — a common guard against out-of-character coordination. LOOC, dead chat and admin messages are never muted by it.

**Out-of-character messages use the same compose box** (§5): the channel tag and slash words route them, and the box takes the destination channel's **colour** as well as its tag, so "you are typing in OOC" is unmistakable before you send — typing in the wrong world is the mistake worth designing against. A dedicated "talk OOC" key exists for those who want one, unbound by default.

## 8. Communication modes

Shape, size, position, and icon carry the distinction between modes, not color alone — keeps it legible for colorblind players and avoids leaning on color for meaning the way the rest of this HUD already avoids doing.

- **Speak** — standard bubble, standard size and duration, three-tier distance/occlusion per §3.
- **Whisper** — privacy-preserving, not just quieter. No muffled tier: within a tight clear range (~1.5m) the listener reads it in full, outside that range they get nothing — the whole point of whispering is that content stays private. But fully invisible communication is exploitable (two people coordinating right next to a third with zero tell), so bystanders within a slightly wider notice range see a presence-only cue — a small ellipsis-style icon over the speakers, no text, enough to know something private is being said, never what.
- **Shout** — does double duty. Within an extended clear range, full text, same as speak but further out. Beyond that range, players still get a non-verbal directional ping (screen-edge indicator, no words) — the mechanical equivalent of hearing someone yell without catching the words, useful for calling for help across distance. Shouts also get priority in the crowd cap (§4): if the room's at capacity and someone shouts, it bumps a normal speaker rather than competing on proximity alone, since the entire point of shouting is not getting lost in noise.
- **Radio** — screen-space chip with a consistent channel icon, regardless of which department channel.
- **Station announcements** — a tagged line in the feed that every player receives, wherever they are, with its own sound so it's noticed. Announcements are rare enough in this pass (a nuclear device armed, `antagonist-content.md` §6; a round ending) that one treatment serves all of them. Separating urgent banners from routine notices is a later pass.

## 9. History / log

Everything heard gets logged, including muffled lines the player only partially caught — flagged as "(partially heard)" rather than silently dropped, the way a real half-caught sentence would stick with you. Whisper content a player wasn't in range for is not backfilled into their log (they didn't hear it; the presence-only cue is what they get). The history is the **feed's own scrollback**: expand the feed and scroll back through the round, timestamped and filterable by channel, with a rough location tag on local-speech entries — this is where the exact-quote, investigation, and coordination use cases that made SS13's chat box valuable actually live. Once the PDA exists (`pda.md`, deferred) it can offer the same history as a tab.

## 10. Voice compatibility

Nothing here assumes audio exists. If voice chat gets added later, it runs as an additional channel of the same speech event — the subtitle rendering, occlusion tiers, crowd cap, and log all keep working unchanged; voice supplies audio alongside the text that was already the source of truth.

## 11. Integration notes

| Comms element | Touches existing / needed system |
|---|---|
| Local speech, occlusion | Character positions, wall/collision geometry for the raycast |
| Crowd cap | Same restraint pattern as the alerts stack |
| Compose input | Intent module's modifier-chording and fading-hint pattern |
| Radio & channels | Headset and its issued access (`id-access.md`) |
| Admin help | `admin-tools.md` §3 |
| Dead chat | `observer.md` §4 |
| Out-of-character column placement | `main-hud.md` §4 |
| OOC mute | `round-config.md` §2 |
| Action lines in the feed and history | `action-messages.md` §5–§6 |
| Announcements | `antagonist-content.md` §6, `round-end.md` |
| History | The feed's scrollback; later a PDA tab (`pda.md`) |

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
- Separate urgent banners and routine notices for announcements (§8)
- Comms history on the PDA (§9)

