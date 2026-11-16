# Networking — design document

> Status: active

Formalizes the connection lifecycle as the player experiences it: joining a server, staying
connected across rounds, dropping out mid-round, and coming back. What a disconnected character
*is* while nobody's driving it has only ever lived in code; this doc writes it down.

## 1. Design philosophy

**This isn't fixing a BYOND artifact — it's formalizing decisions that have only lived in code.**
Unlike most of what this project resolves, SS13's own client-server model isn't the thing to test
against; this project already runs on a real client-server architecture, with one dedicated server
players connect to directly.

**A disconnected character is a real, visible fact, not a silent vanishing or a suspended
non-entity.** The same "let the world carry the information" discipline that puts wounds on a body
and lighting state on a room applies here: nobody needs a UI element to learn a nearby character
stopped responding, because the character itself shows it.

**Reconnecting is resuming, not rejoining.** The body never paused or moved anywhere; it just sat
there unpiloted. Getting control back is instant, not a lobby screen.

**The server outlives the round.** Players connect to a server, not to a round. When a round ends,
everyone stays connected and goes back to the lobby together (`round-end.md` §6).

## 2. Disconnection — what the body does

**The character stays exactly where it was, fully physically simulated.** It can still be pushed,
damaged, dragged, or examined — no special ragdoll state, no despawn, no invulnerability. Nothing
about the world's physics or interaction rules cares that nobody's controlling it, the same way
nothing changes about an unconscious character's physical presence either.

**A real, diegetic tell.** The character's idle animation changes — posture slackens, head-tracking
and idle fidgets stop — and examining them says they're **asleep and unresponsive** (`examine.md`
§5). Anyone looking at a disconnected character can tell something's wrong without consulting a
device. This is SS13's "SSD" state, made visible.

**No control input reaches the character from anyone.** Whatever caused the disconnect, nobody else
can drive that body in the interim. Physical interaction *with* it (searching, dragging,
restraining) works exactly as it would on anyone else — the character being unpiloted doesn't grant
or remove any access, item, or state it didn't already have.

## 3. Reconnecting — resuming, not rejoining

**The body waits for the rest of the round.** As in SS13, a disconnected character stays where it
is, asleep, for as long as the round lasts. There is no timer after which it's taken away.

**Reconnecting any time during the same round hands control straight back.** No prompt, no lobby
screen, no latejoin interface — the same player, the same character, wherever and however it is
now. The tell from §2 clears the instant control resumes. If the character died in the meantime,
the player comes back as its ghost (`observer.md` §2).

**Moving a long-gone player's body somewhere safe** — a cryo pod that stores the character and
frees their job slot — is `cryogenics.md`'s job, deferred. Until it exists, the body simply stays.

## 4. Joining fresh vs. reconnecting — two different doors

**Lobby's job interface (`lobby.md` §7) is for players who don't already have a character in this
round** — a fresh join, or latejoin into a currently-open slot. Reconnecting to an existing
character (§3) never touches that interface at all.

## 5. Who can connect

**A matching game version.** A client running a different build from the server is turned away at
the door with a message saying both versions, so the player knows to update rather than seeing an
unexplained failure or a broken session.

**A real player cap.** A server has a fixed maximum connected-player count, set by its admins
(`round-config.md` §2). A connection attempt beyond that is rejected with **a clear reason** — "the
server is full" — not a silent disconnect. No queueing system: a queue is a real feature with its
own UX (position, estimated wait, cancel) that this pass doesn't need.

## 6. Losing the server

If the client loses its connection to the server — the server restarts, or the network goes — the
player sees a clear message and a choice: **retry** the connection, or **quit** to the main menu.
Retrying into the same round lands them back in their character per §3.

## 7. HUD & touchpoints

No new permanent chrome.

- The disconnected-character tell (§2) renders on the model itself and on examine — no icon, no HUD
  overlay.
- A rejected connection (§5) and a lost server (§6) are legible messages at that moment, not silent
  failures.
- Admin observation (`admin-tools.md` §4) can confirm a suspicious idle character is genuinely
  disconnected rather than just standing still on purpose; no new admin surface is designed here.

## 8. Worked examples

**A — A brief connection hiccup:**

| Step | What happens | State |
|---|---|---|
| 1 | A player's connection drops for a few seconds | Character's tell appears (§2); no one else can control the body |
| 2 | Connection re-establishes | Control resumes instantly, no prompt |
| 3 | The tell clears | Nothing about the character's state, inventory, or position changed |

**B — Gone for twenty minutes:**

| Step | What happens | State |
|---|---|---|
| 1 | A player disconnects and doesn't return for a while | Their character sleeps where it stood; examine says "asleep and unresponsive" |
| 2 | Another crew member drags the body to medbay to keep it safe | Ordinary physical interaction |
| 3 | The player reconnects, same round | Back in control, in medbay |

**C — Wrong version / server full:**

| Step | What happens | State |
|---|---|---|
| 1 | A player on last week's build tries to connect | Turned away with both version numbers shown |
| 2 | They update, but the server is now at its cap | Turned away with "the server is full" |

## 9. Integration notes

| Networking element | Touches existing / needed system |
|---|---|
| Disconnected-character tell | "Body is the doll" precedent (`main-hud.md` §5); examine (`examine.md` §5) |
| Long-term storage of a gone player's body | `cryogenics.md` (deferred) |
| Fresh join / latejoin boundary | `lobby.md` §7 |
| Player cap | `round-config.md` §2 |
| Staying connected between rounds | `round-end.md` §6 |
| Deeper inspection if needed | `admin-tools.md` §4 |

## 10. Out of scope for this pass

- **Netcode-level concerns** — prediction, rollback, lag compensation, interpolation. These are
  implementation, not a gameplay decision this doc is positioned to make.
- **Voice chat networking** — `comms.md` §10's own territory, unextended here.
- **Server browser, matchmaking, or cross-server features** — this project targets direct-connect to
  a single dedicated server.
- **A connection queue for a full server** — reject is the whole mechanic this pass commits to (§5).
- **Who a player is** — identity and authentication are `player-accounts.md`'s job.
- **Anti-cheat specifics** — every other doc in this project already assumes server authority over
  outcomes.
- **Exact maximum-player-count value** — a hosting decision, not fixed here.
