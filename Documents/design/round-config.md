# Round Configuration & Gamemode Selection — design document

> Status: active

Decides which gamemode and which map a round runs, who gets to choose, and how much players are told. Lobby's antagonist opt-in (`lobby.md` §4) and the round's length and end (`round-end.md` §2) both read from what this doc configures. Same session/meta carve-out as lobby (§1 there): no world yet, so the test is native-vs-leftover, not diegetic-vs-panel.

## 1. Design philosophy

**The native-vs-leftover test applies again, to a different screen.** Lobby's job-select redesign targeted the player-facing BYOND browser panel. This doc targets its sibling: the admin-facing config screen. Same artifact, same fix — the same visual system as the rest of this project applied to a real settings surface, not a checkbox-laden webpage.

**Start with an admin's choice, grow into a draw.** A full SS13-style config — weighted pools of gamemodes and maps, each gated by player count, drawn at random — only earns its complexity once a server has several antagonist modes and several maps to choose between. This pass has one antagonist mode and few maps. So the first pass is direct: an admin picks. The weighted pool (§7) is the later pass, and the screen is shaped so it can grow into one.

**Secrecy is a feature this doc preserves, not an artifact it fixes.** "You don't know the round's real gamemode" isn't a BYOND limitation — it's a deliberate design choice that holds up independent of engine. What this doc commits to is that the secrecy is real: when the mode is secret, the server never tells a client which mode it is (§4).

## 2. What an admin configures

A **Server Settings** screen, open to Administrators (`admin-tools.md` §2), holds the settings that shape a round. They persist across rounds as server meta (`persistence-save.md` §4).

- **Gamemode** — one of:
  - **Secret** — the server picks among the enabled antagonist modes, and players are told only "Secret".
  - **Extended** — no antagonists; a quiet station round.
  - **A named mode** (e.g. Nuclear Emergency) — that mode, announced openly.
- **Map** — which station the next round loads.
- **Round length** — the hard time limit for a round (`round-end.md` §2). A server setting, the same for every mode in this pass; a gamemode may override it in a later pass.
- **Player cap** — how many players can be connected at once (`networking.md` §5).
- **Latejoin** — on or off (`lobby.md` §7).
- **OOC chat** — on by default; an admin can mute global OOC mid-round, without a restart (`comms.md` §7). LOOC, dead chat and admin messages are unaffected.
- **Respawn** — off by default; when on, a ghost may latejoin as a new character after a cooldown (`death-cloning-respawn.md` §6).

## 3. Choosing the mode at round start

The mode is settled **as the lobby countdown ends**, before job resolution (`lobby.md` §5):

- **Secret** draws one of the enabled antagonist modes.
- The mode then picks its antagonists **only from players who opted into its roles** (`lobby.md` §4, `antagonist-content.md` §2).
- **Not enough opt-ins → Extended.** Every antagonist mode has a minimum (Nuclear Operatives needs enough operatives to be a team). If the opted-in players fall short, the round runs as Extended instead, and admins get a notice saying so. It's never a silent substitution, and it never forces an antagonist role on someone who didn't opt in.

## 4. Secrecy

**Secret stays secret.** If the admin chose Secret, every player-facing surface — the lobby, the Server Info tab, the round itself — says "Secret" until the round-end reveal (`round-end.md` §4). The real mode stays on the server.

**A named mode is public by choice.** If an admin picks Nuclear Emergency directly, players see it. That's the admin deciding to announce it, not a leak.

**Secret means little with one mode.** While Nuclear Operatives is the only antagonist mode, Secret can only become Nuclear Operatives (or Extended, §3). The secrecy becomes real as more modes are added; the rule is stated now so it's already right when they are.

## 5. The settings screen

The settings from §2 shown as plain, editable fields in the project's visual language. A **round history** — the last N rounds' mode, map, and whether §3 fell back to Extended — sits alongside, so an admin can see what has actually been running.

**A map has to fit the mode.** Before a round can start, the chosen map is checked against the chosen mode — every job has somewhere to spawn, and the mode's own needs are on the map (Nuclear Operatives needs a nuke, a disk and an operative spawn). A failing check blocks the start and says what's missing (`creative-mode.md` §8), rather than a round failing halfway through starting.

## 6. What this doesn't decide

- **Which specific crew member becomes which antag** within a chosen gamemode — that's the mode's own business, designed in `antagonist-content.md` §2.
- **Round end conditions** — `round-end.md` §2.

## 7. Later pass: weighted pools

Once there are several antagonist modes and several maps, Secret grows into a draw:

- **A pool** per kind (gamemodes, maps): enabled entries, each with a relative weight and preconditions — chiefly a min/max connected-player range.
- **The draw** happens as the countdown ends, against the **live** player count, so players leaving during the lobby are accounted for.
- **Fallback** to Extended when no entry's preconditions hold, logged for admins like §3's fallback.
- **An optional, advisory map poll** players can vote in, feeding the weights rather than binding them.

Nothing in §2–§5 has to change shape for this; Secret just draws from a weighted pool instead of from every enabled mode equally.

## 8. Integration notes

| Element | Touches existing / needed system |
|---|---|
| Antagonist roles on offer | `lobby.md` §4 — the enabled modes' roles |
| Antagonist assignment and minimums | `antagonist-content.md` §2 |
| Round length | `round-end.md` §2 |
| Mode reveal | `round-end.md` §4 |
| Settings persistence | `persistence-save.md` §4 |
| Who may change settings, and the log of changes | `admin-tools.md` §1, §2 |
| Map-fits-mode check | `creative-mode.md` §8 |
| OOC mute | `comms.md` §7 |

## 9. Worked examples

**A — Secret round:**

| Step | What happens | Round config state |
|---|---|---|
| 1 | Admin leaves the gamemode on Secret | Lobby and Server Info show "Secret" |
| 2 | Countdown ends; Nuclear Operatives is drawn | Stored on the server only |
| 3 | Five players opted into Nuclear Operative — above the minimum | The mode picks its squad from those five |
| 4 | The round ends | The summary reveals "Nuclear Operatives" for the first time (`round-end.md` §4) |

**B — Too few opt-ins:**

| Step | What happens | Round config state |
|---|---|---|
| 1 | Secret draws Nuclear Operatives, but only one player opted in | Below the mode's minimum |
| 2 | The round falls back | Runs as Extended; admins get a notice; nobody is forced into a role |

**C — Map doesn't fit:**

| Step | What happens | Round config state |
|---|---|---|
| 1 | Admin picks Nuclear Emergency on a map with no disk | The start is blocked with "this map has no nuke disk" |
| 2 | Admin picks a different map, or Extended | The check passes; the countdown can run |

## 10. Out of scope for this pass

- Weighted pools, preconditions and the map poll (§7)
- Dynamic/threat-budget-style meta-gamemodes
- Per-gamemode round length
- The exact antagonist minimums and ratios (a balancing decision)
- The full admin toolkit — `admin-tools.md`
