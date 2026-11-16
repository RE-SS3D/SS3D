# Sandbox Server — design document

> Status: active

A public "fuck around" server: a separate instance where anyone who joins can build, fight, set up atmospherics and break things together, and the whole station resets to a clean map every 20 minutes. It exists for two reasons at once. Players get a low-stakes multiplayer playground. The project gets a live playtest that finds what breaks under real players, real abuse and real load. This doc defines how that server plays. It is the first, lighter step toward the Builder gamemode `creative-mode.md` §15 left out of scope.

## 1. Design philosophy

**A server profile, not a gamemode.** The sandbox runs ordinary Extended rounds (no antagonists) with a fixed set of server settings: everyone can build, respawn is on, nothing is saved, rounds are short. Each rule is a setting an operator could flip on its own, so nothing here adds a mode that has to be maintained next to the real ones. It becomes a gamemode only if it grows into a product of its own.

**Breaking it is the point.** Most servers protect the round. This one invites players to stress it: spam objects, flood rooms with gas, test every weapon on each other. Every crash, leak and stall it surfaces is a bug fixed before a real round meets it. The rules below therefore protect *the session* (one player can't end everyone's 20 minutes), but not *the round* (anything may happen inside it).

**Nothing carries over.** Every round starts from the same untouched map, and nothing built in one round survives into the next. Serious map-making happens locally (`creative-mode.md` §9); the sandbox is for playing with what's there. A clean start each round also makes each reset a repeatable test: any difference between round one and round fifty is a bug.

**Sandbox exceptions stay in the sandbox.** Several rules below (ghosts building, no god mode, everyone editing) break principles other docs hold firmly. They're justified only because nothing is at stake here, and they don't apply to any other server.

## 2. Who plays and how they find it

- A **separate instance** with its own settings, alongside whatever server runs real rounds.
- The address is shared through the project's Discord, not baked into release builds as the default server. It goes fully public once the reset loop and caps have survived some unattended weekends.
- **Player cap: 16.** Enough for chaos; raised once the server has shown it holds up.
- On joining, the **MOTD** and a short message at round start state the rules: *everything is allowed, the server resets every 20 minutes, nothing is saved, report bugs with `/bug`.*

## 3. The round loop

- **Fixed 20-minute rounds** on a real station (the same kind of map real rounds use), so real atmospherics and power load get exercised.
- **Warnings** at about 5 minutes and 1 minute before the reset.
- At the end, everyone returns to the lobby for a short countdown, their ready state carries over, and they spawn onto a fresh copy of the map. The lobby's self-starting countdown (`lobby.md` §7, `round-end.md` §6) drives this; no operator has to be online.
- **No vote to extend.** An admin can still extend or end a round through the usual tools (`admin-tools.md` §6).

## 4. Death and respawn

**Respawn is on** (`death-cloning-respawn.md` §6), with a short cooldown of about 30 seconds.

**Latejoin always gives you a new body at a spawn point.** On other servers, latejoin can return you to your own body or leave you a ghost. Here atmospherics damage is real and bodies are easy to lose for good (vented, burned, spaced), so a returning player never depends on their old body being recoverable. Respawning is a new character, as §6 of the death doc already says.

**Ghosts can build.** A dead player can open the Map Editor and keep building without a body. This deliberately breaks `observer.md` §1 (no path from ghosts into the living world): a ghost can drop a grenade on the player who killed them. On the sandbox that's fine; it's what the server is for.

## 5. Building

**Everyone can use the Map Editor** (`creative-mode.md` §2), not just admins.

**The palette is open except for what breaks the server's own loop:**
- **No spawn-point or marker tools.** A deleted spawn point would break respawn for everyone.
- **No nuclear device or authentication disk.**
- Everything else, including weapons and explosives, is available. Getting items and weapons from the palette is the intended way to test them; there's no separate spawn menu and no console for players.

**No god mode while editing.** A living player who opens the editor stays vulnerable. If they die, they carry on building as a ghost (§4). This removes "open the editor to become immortal mid-fight" entirely.

**Anyone can erase anything.** Builds aren't protected per owner; the world is wiped every 20 minutes anyway. Undo still only reverts your own edits.

**Nothing is saved, loaded or exported** on this server. A build is gone at reset.

## 6. Limits

**Per-player throttling** keeps one player from editing faster than the server can follow.

**Server-wide caps** limit how much of each kind of thing can exist on the station at once. They're set high: they exist to keep the session alive, not to restrict play. When a cap is reached, building that kind of thing is refused and the player gets a chat message saying so and when the next reset is (*"Server build limit reached for grenades. Resets in 7 min."*). Everything else keeps working. A cap being hit is also a finding: it marks where the game's real limits sit.

**Vote-kick and a "reset now" vote** wait until the server is fully public (§2).

## 7. Reporting bugs

Bug reports are the server's main output, so filing one must cost seconds and must never interrupt the round.

**Two ways in, one report.** `/bug <note>` in chat files instantly, with no dialog, the moment a player sees something. **F8**, a bare `/bug`, or **Report a bug** in the Escape menu opens the bug report dialog. It works everywhere: lobby, in round, as a ghost, in the map editor. Both paths produce the same report; the dialog only adds detail.

**The moment is frozen, not the game.** Nothing pauses; other players keep playing. When the dialog opens, the round ID, round time, position and a screenshot are captured as they were *before* the dialog appeared, so the report describes what the player saw.

**What the player fills in:**

- **Summary**, required, up to 80 characters.
- **What happened**, optional, prompting for steps: what they were doing, what they expected.
- **Severity**, optional, with nothing selected by default: *Minor* (cosmetic, easy to ignore), *Major* (breaks a mechanic or task), *Blocker* (can't keep playing). Blank means a developer triages it. A preselected default would make most reports look Minor.
- **The game crashed or froze**, one checkbox. There is no category picker: players guess categories badly, and the context below says more.
- **Screenshot**, attached by default and removable in one click, since it can show chat and player names.

**What is logged automatically**, listed in the dialog so the player can see it: game version, round ID and time, map, context (lobby, alive, ghost, or map editor and active tool), position, network role, player count, frame rate, OS and graphics card, player name, and the tail of the player's own log (warnings and errors), because many bugs only happen on the player's machine.

**Limits.** One report per player per minute; the dialog shows when the next one is allowed. Screenshots are sent at reduced size. No per-round cap until the first invite-only session shows one is needed.

**After sending**, the dialog closes and a short notice confirms the report was saved, naming the round.

**The same dialog in a local session.** When the player is their own host, such as a tester building a map (`creative-mode.md` §9), there is no remote server to receive the report. The dialog then saves a report bundle on the player's machine: the report, their logs, the screenshot and, if they choose, the **current map** as it is now, including unsaved edits, without touching the saved map file. The map option only appears locally, and it is on by default when the map editor was used this round or the map is not one the game ships with. After saving, the player gets buttons to open the report folder and to open a pre-filled bug report on the project's issue tracker, plus a reminder that the bundle can be posted in Discord instead.

## 8. Worked examples

**A — Testing atmospherics, dying, coming back:**

| Step | What happens | State |
|---|---|---|
| 1 | Player builds a sealed room with the editor and floods it with plasma | Real gas, real damage |
| 2 | They walk in to check it | They die; their body is now in a plasma-filled room |
| 3 | As a ghost, they open the editor and add a vent to fix the room | Ghost building (§4) |
| 4 | Cooldown ends; they latejoin | New body at a spawn point, not the dead one |

**B — Someone spams objects:**

| Step | What happens | State |
|---|---|---|
| 1 | A player rectangle-fills the station with grenades | Per-player throttle slows them |
| 2 | The server-wide grenade cap is reached | Further grenades refused, with a chat message and the reset time |
| 3 | Other players keep building and fighting | The session stays playable |
| 4 | The round resets | A clean station; the cap hit is logged as a finding |

**C — A normal cycle:**

| Step | What happens | State |
|---|---|---|
| 1 | 5-minute warning, then 1-minute warning | Players wrap up |
| 2 | Round ends; everyone goes to the lobby | Ready state kept |
| 3 | Countdown runs out | Fresh copy of the map; nothing from last round remains |

## 9. Integration notes

| Element | Touches |
|---|---|
| Building with the editor, palette | `creative-mode.md` §2, §5 |
| Nothing saved, no autosave | `creative-mode.md` §9 (deliberately not applied) |
| Respawn setting and cooldown | `death-cloning-respawn.md` §6 |
| Ghost building | `observer.md` §1 (sandbox exception) |
| Self-starting lobby loop | `lobby.md` §7, `round-end.md` §6 |
| MOTD, round length, player cap | `round-config.md` §2 |
| Admin extend / end | `admin-tools.md` §6 |
| Nuclear device and disk excluded | `antagonist-content.md` §6 |

## 10. Out of scope for this pass

- A real Sandbox/Builder **gamemode** (`creative-mode.md` §15).
- **Votes**: extend the round, reset now, vote-kick.
- **Exporting** a build to take home.
- **Protecting** a player's build from others.
- A purpose-built **sandbox map** with demo rooms (open space, an atmos setup, a power loop), or **rotating** between maps.
- Making the sandbox the **default server** in public release builds.
