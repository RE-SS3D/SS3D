# Main Menu — design document

> Status: active

The screen the game opens to: where a player says who they are, finds a server, joins it, plays locally, changes settings or updates. It replaces the habit of launching the game through scripts that ask for an IP address. The bar is the one a stranger sets: download, open the game, and be playing on a server without editing a file or looking up an address. Settled in a grill on 2026-11-02.

## 1. Design philosophy

**The game has a front door.** Every route into or out of a session passes through one screen. Starting the game lands here. Leaving a server lands here (`networking.md` §6, "quit to the main menu"), and so does a refused connection. Nothing about joining lives in launch scripts or text files.

**Paste once, then one click.** Server addresses travel through the project's Discord (`sandbox-server.md` §2), so the menu can't know them in advance. A player gives an address once and the menu remembers it from then on. Servers the project chooses to list publicly appear with no setup at all.

**Tell the player before they try.** Whether a server is up, full or on another version is shown next to it, before the player clicks. A refusal never fails silently; it comes back to the menu with the reason (`networking.md` §5).

**The world is the best background.** A menu shown over a live station blurs the real station, because that shows something true. Artwork fills only the moments when there is no world to show.

## 2. What the menu offers

- **Join a server**: recent servers, a public list, and an address field (§4).
- **Play locally**: host your own session (§6).
- **Settings**: the same Settings screen as the in-game Escape menu, so audio, display and controls can be set before joining.
- **Quit**.
- A **version label**, and a **Discord link**, since that's where server addresses and help live.

There is no option to run a dedicated server. Running one is an operator task, not a player one.

## 3. Who you are

One **name** field, filled with the computer's user name the first time and remembered afterwards. The player can change it at any time before joining. The name is a claim, not a verified identity; it stays that way until `player-accounts.md` replaces it with a platform identity, and this field is replaced along with it.

## 4. Finding a server

**Three sources, one list.**

- **Recent servers**: the last five joined, newest first. Each can be renamed or removed. The most recent one sits at the top, so coming back is one click.
- **Public servers**: a list the project maintains outside the game, so adding or removing a server needs no new release. It starts empty. A server joins it when the project decides it is public; the sandbox does once it has proven itself (`sandbox-server.md` §2).
- **Direct connect**: paste `address` or `address:port`. A successful join adds it to recent servers.

**What each entry shows:** name, players and cap, whether it runs a compatible version, and ping. An incompatible server stays visible but can't be joined, and says which version it needs. An unreachable server is marked offline. If the public list can't be fetched, recent servers and direct connect still work.

Round state and gamemode are not shown. The lobby shows them after joining (`lobby.md`), and showing them here would tie the menu to round rules.

**Admin token.** A saved server can carry an optional admin token, entered in that server's details. An admin joins their own server the same way everyone else does.

**No automatic join.** The game always opens on the menu, even with a recent server. Joining automatically would skip the update banner and the server status exactly when they matter.

## 5. Joining and failing

- Joining shows a **Connecting** screen with **Cancel**.
- Every refusal (version mismatch, server full, unreachable, timed out) returns to the menu, with the reason shown next to that server.
- Losing a server mid-session keeps the **Retry / Quit** choice of `networking.md` §6. Quit returns to the menu rather than closing the game.

## 6. Playing locally

**One click** starts a local session, hosted by the player, and drops them into their own lobby. This is the path for building maps (`creative-mode.md` §9) and for playing with friends on the same network. Changing the port is available but tucked away.

While hosting, the session's server info shows the address friends on the same network can join with, so nobody has to look up their own IP.

## 7. Staying up to date

When a newer release than the one running exists, the menu shows a **"new version available"** banner with **Update and restart**. It updates the game the way the bundled updater does, keeping the player's maps and settings, then starts the new version. A version mismatch is the most likely reason a new player fails to join; this turns "refused" into a fix.

## 8. Look

- The same visual language as the lobby and HUD (`main-hud.md`): flat surfaces, hairline borders, muted colours. The SS3D logo is prominent.
- Behind it, **one wallpaper per launch**, chosen at random from a curated set of the project's own wallpaper art: around ten to fifteen, all in the same rendered style, with no gore and no near-duplicates. The Halloween wallpapers appear only in October.
- The menu panel keeps its text readable over any wallpaper; the wallpaper is atmosphere, not content.
- A footer line credits the art: *Wallpapers: RE:SS3D art contributors*, linking to the art repository. It becomes a credit per image once each artist is known.

**Inside a session**, a blurred wallpaper sits behind the lobby only when there is no world to show: before the first round, between rounds, and while connecting or loading. While a round runs, the lobby and Escape menu keep blurring the real station.

## 9. Worked examples

**A — A stranger's first game:**

| Step | What happens | State |
|---|---|---|
| 1 | They download the release and start the game | The main menu, name filled in from their computer |
| 2 | They paste the server address from Discord and join | Connecting, then the server's lobby |
| 3 | Next day they start the game again | That server is at the top of recent servers; one click joins |

**B — Out of date:**

| Step | What happens | State |
|---|---|---|
| 1 | A player on last week's release opens the menu | A "new version available" banner; their usual server shows "needs v0.X" |
| 2 | They click Update and restart | The game updates, keeping maps and settings, and reopens on the menu |
| 3 | They join | The server accepts them |

**C — Building a map with a friend:**

| Step | What happens | State |
|---|---|---|
| 1 | A tester clicks Play locally | Their own lobby; server info shows "friends can join at 192.168.1.20" |
| 2 | Their friend pastes that address into Direct connect | Joined the tester's session |

**D — Server full:**

| Step | What happens | State |
|---|---|---|
| 1 | The list shows 16/16 for a server | The player can see it's full before trying |
| 2 | They try anyway as the last slot fills | Back on the menu, "the server is full" next to it |

## 10. Integration notes

| Main menu element | Touches existing / needed system |
|---|---|
| Quit after losing the server | `networking.md` §6 |
| Version and full-server refusals | `networking.md` §5; `round-config.md` §2 (player cap) |
| Sandbox listing and address sharing | `sandbox-server.md` §2 |
| Name as a claimed identity | `player-accounts.md` §1, §3 |
| Play locally, building maps | `creative-mode.md` §9 |
| Lobby after joining | `lobby.md` |
| Visual language | `main-hud.md` |
| Settings screen | Shared with the in-game Escape menu |

## 11. Out of scope for this pass

- **Join links** that open the game from a click in Discord. A downloaded zip has no installer to register them.
- A full **server browser** with discovery, filtering and favourites beyond recent servers.
- Verified **accounts** and login (`player-accounts.md`).
- **Per-image art credits**, until the artists are identified.
- An animated or live-rendered **station background** behind the menu.
- Showing round state, gamemode or MOTD before joining.
