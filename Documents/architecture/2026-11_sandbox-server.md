> Implements: Documents/design/sandbox-server.md §2–§7; Documents/design/lobby.md §7 (self-starting round); Documents/design/death-cloning-respawn.md §6 (respawn setting)
> Touches systems: rounds-lobby, map-editor-creative-hooks (tile), entities, networking-session, persistence, ui-shell, inputs
> Status: in-progress

**Progress:** P1–P4 built and pilot-tested in the editor (`Run Sandbox Pilot`), plus the bug report dialog (below); P5's server-build half (heartbeat, exit after N rounds) is in, and the server is live on TomNAS, deployed and maintained outside this repo (no `Tools/sandbox-server/` files); P6 waits on session data. Advances [test-server.md](../milestones/test-server.md) T7.

# Sandbox server (Nov 2026)

A separate dedicated instance where everyone can build and fight, with a 20-minute round that always resets to a clean map ([sandbox-server.md](../design/sandbox-server.md)). It doubles as a live playtest: every reset is a repeatable stability test, and players' bugs and abuse are the output. Scope was settled in an implementation grill on 2026-11-01.

## Starting state (2026-11-01, `17c1009db`)

- **`-openbuild`** (`OpenBuildMapEditorAuthorizer`) already lets every player edit, throttled by `MapEditorRateLimiter`, with own-edit undo, and `MapEditorPermissions.MayReplaceMapFile` stops overwriting other players' saves. Admin names need a `-token` from `Config/admin-tokens.txt` on dedicated servers.
- **Round length** is enforced: `RoundSubSystem` requests a round end at `ServerSettings.RoundLengthMinutes`. Nothing warns before it.
- **No automatic start.** A round only starts from an admin's `ChangeRoundStateMessage` (`RoundSubSystemBase.HandleRequestStartRound`); [lobby.md](../design/lobby.md) §7's self-starting countdown is unbuilt. After a round, the server sits in Stopped.
- **Ready is cleared** once a round spawns its players (`ReadyPlayersSubSystem.InvokeSpawnReadyPlayers`).
- **A ghost counts as a spawned player.** `Human` swaps the ghost into `EntitySubSystem._spawnedPlayers` via `TryTransferEntity`, so `SpawnLatePlayer` refuses ghosts: there is no respawn path.
- **Editor server RPCs check only the authorizer**, never for a body. The client's `MapEditorSubSystem.CanOpenEditor` asks `IsPlayerSpawned`, which is true for a ghost; ghost editing is untested at runtime.
- **Player invincibility** is a per-client toggle in `MapEditorSettings` (`EntityInvincibility.SetServer`), authorized with `MapEditorPermissions.TryAuthorize` — anyone on `-openbuild`. It persists after closing the editor until disconnect.
- **Persistence calls** are separate server RPCs on `MapEditorSubSystem`: `RpcSaveMap`, `RpcExportMap`, `RpcLoadMap`, `RpcDeleteMap`, `RpcNewMap`. Round-end autosave lives in `MapEditorRoundStop`.
- **Spawn points** have their own command kinds (`PlaceSpawnPoint`, `ClearSpawnPoint` in `MapEditorCommandKind`).
- **TomNAS** already runs Docker and user systemd with linger for the CI runner ([2026-07_multiplayer-testing-self-hosted-ci](2026-07_multiplayer-testing-self-hosted-ci.md)).

## Decisions

| # | Decision |
|---|---|
| 1 | One **`-sandbox`** flag. It implies `-openbuild` and applies the profile **in memory** at boot: Extended, `RoundLengthMinutes` 20, latejoin on, `PlayerCap` 16, respawn on, persistence off, auto-start on. It never writes the saved server settings. Each rule is its own setting in code so other servers or a later gamemode can reuse it. Admins can still extend or end rounds. |
| 2 | Auto-start is the general [lobby.md](../design/lobby.md) §7 mechanism behind an **`AutoStart`** server setting: a countdown starts once at least 1 player is ready. The sandbox is its first user. |
| 3 | On the sandbox, **every connected player who isn't observing counts as ready** at round start. Ready flags are not remembered across rounds. |
| 4 | Respawn is general: **`RespawnEnabled`** and **`RespawnCooldownSeconds`** (sandbox: on, ~30 s). A **Respawn** button with countdown on the ghost screen calls the server, which despawns the ghost and runs the normal `SpawnPlayer` path: always a fresh body at a spawn point ([sandbox-server.md](../design/sandbox-server.md) §4). The corpse remains during the cooldown; `RemoveCorpseOnRespawn` (sandbox: on, otherwise off) removes it after a successful respawn. The cooldown starts at death. |
| 5 | **Anyone authorized may edit as a ghost** — admins anywhere, everyone on `-openbuild`. No sandbox-only check. Work is a runtime check of the ghost path, then fixing what breaks (camera handover, `CanOpenEditor` after the body swap). |
| 6 | Invincibility is **refused server-side and hidden** for `IsSandboxPlayer` connections. Admins and the host keep it. |
| 7 | One server-side gate (e.g. `MapEditorPermissions.PersistenceAllowed`) blocks Save, Export, Load, Delete, **New** and the round-end autosave. Load and New are blocked because they replace the station for everyone. A replicated flag hides those buttons. |
| 8 | A **code-side denylist** for sandbox players: the `PlaceSpawnPoint`/`ClearSpawnPoint` command kinds and the nuclear device and disk asset IDs. Refused in `RpcExecuteCommands`, filtered from the palette. Area painting stays allowed. |
| 9 | **5-minute and 1-minute warnings** for every timed round, in `RoundSubSystem`'s tick. The MOTD carries the sandbox rules text. |
| 10 | Round ID: an incrementing counter per server lifetime, stamped on round start/end log lines and on every record below. |
| 11 | **`reset-health.jsonl`** in the server's `Data/`, one line per Stopped: round ID, player count, spawned NetworkObjects, GameObject count, managed heap, total allocated memory, tile objects and items still registered after unload. Separate from `RoundHistoryEntry`. |
| 12 | **`/bug <note>`** appends ckey, round ID, UTC time, position, alive/ghost and the note to `Data/bugs.jsonl`, and confirms in chat. No Discord webhook until the server is public. |
| 13 | Hosting: a **Docker container** on TomNAS, `restart: unless-stopped`. A main-thread heartbeat writes a timestamp file every 5 s; the healthcheck fails when it goes stale. The server **exits after N rounds** (~12, about 4 h) with code 0; a crash exits nonzero. `Data/` sits on the container volume. |
| 14 | The build is the Linux dedicated server from the manual CI prerelease ([2026-07_ci-develop-release-pipeline](2026-07_ci-develop-release-pipeline.md)), deployed by hand. Auto-deploy stays deferred per the milestone. |
| 15 | **Caps** come last, with numbers from `reset-health.jsonl` and the first session: total placed items, items per asset, total placed tile objects, and a NetworkObject ceiling as backstop. A refused build gets a chat message with the reset time and a server log line. Gas/atmos caps are out. |
| 16 | The **first invite-only Discord session** runs after P1–P5, without caps. Invite-only plus the watchdog make that acceptable, and it produces the cap numbers. |

## Shape

- **Profile.** A small server-only `SandboxProfile` reads `-sandbox` (next to `OpenBuildMapEditorAuthorizer.RequestedOnCommandLine`) and, on server start, overlays the in-memory `ServerSettings` and the new flags (`AutoStart`, `RespawnEnabled`, `RespawnCooldownSeconds`, `PersistenceAllowed`). Everything else reads those settings, never the flag, so a test can set them directly.
- **Map editor gates** stay in `MapEditorPermissions`: `PersistenceAllowed`, the denylist check (beside `IsSandboxPlayer`), and the invincibility refusal in `MapEditorSettings`. Client UI reads a replicated capability snapshot rather than re-deriving rules.
- **Round cycle** in `RoundSubSystem`/`ReadyPlayersSubSystem`: an auto-start countdown while Stopped with `AutoStart` on, the sandbox all-ready rule at spawn time, and warnings in the Ongoing tick.
- **Respawn** in `EntitySubSystem`: a server RPC that validates `RespawnEnabled`, the cooldown and that the caller's entity is a ghost, then replaces the ghost in `_spawnedPlayers` with a fresh `SpawnPlayer` body. The ghost-screen button follows the UiShell view/subsystem split.
- **Observability** is a server-only service writing JSONL beside the existing round history in `PersistenceSubSystem`'s data folder; `/bug` registers through the existing chat command path.
- **Hosting** files live under `Tools/sandbox-server/` (Dockerfile, compose file, README runbook).

## Phases

### P1 — Profile and lockdown — **built**

- `-sandbox` flag and in-memory profile (Decision 1).
- Persistence gate on all six paths, buttons hidden (Decision 7).
- Denylist for spawn-point commands and nuke/disk assets, palette filtered (Decision 8).
- Invincibility refused and hidden for sandbox players (Decision 6).
- Tests: EditMode for the persistence gate, denylist and invincibility refusal.

### P2 — Round cycle — **built**

- `AutoStart` countdown while Stopped, minimum 1 ready (Decision 2).
- Sandbox all-ready at round start (Decision 3).
- 5/1-minute warnings for every timed round; sandbox MOTD (Decision 9).
- Tests: EditMode for auto-start conditions; `LobbyLifecyclePilot` `-sandbox` run with 1-minute rounds through two automatic cycles.

### P3 — Respawn and ghost editing — **built**

- `RespawnEnabled` / `RespawnCooldownSeconds`, ghost-screen Respawn button, server respawn RPC (Decision 4).
- Runtime check of the editor as a ghost; fix what breaks (Decision 5).
- Tests: EditMode for the cooldown; pilot covers death → ghost → edit → respawn → fresh body at a spawn point.

### P4 — Observability — **built**

- Round ID in logs (Decision 10), `reset-health.jsonl` (Decision 11), `/bug` (Decision 12).
- Tests: multiplayer smoke step where a remote client survives one reset and `reset-health.jsonl` gains a line.

### P5 — TomNAS hosting — **in-progress** (server-build half done)

- Heartbeat writer and exit-after-N-rounds in the server build (Decision 13).
- `Tools/sandbox-server/` Dockerfile, compose with healthcheck and volume, runbook.
- Manual deploy of the CI Linux server build (Decision 14).
- Exit: first invite-only Discord session (Decision 16).

### P6 — Caps — **planned**

- Counters and limits from session data, chat refusal message, log line (Decision 15).
- Tests: EditMode for each counter and the refusal path.

Each phase runs `Testing/ui/validate.sh --full` before merge.

## Implementation notes

Where the built code differs from the plan above, or adds to it:

- **Settings.** The rules are `ServerSettings` fields: `AutoStart`, `AllConnectedReady`, `RespawnEnabled`, `RespawnCooldownSeconds`, `RemoveCorpseOnRespawn`, `MapPersistence`. The admin form doesn't edit them; `ServerSettings.WithRulesOf` keeps them through a whole-form save. `SandboxProfile` (`Systems/Rounds/`) applies them at boot; `RoundSubSystem.ApplyServerRules` pushes `MapPersistence` into `MapEditorPermissions.PersistenceAllowed`. `-sandbox-round-minutes=N` overrides the 20-minute length (tests).
- **Persistence gate** applies to everyone, admins included; a Stop with unsaved edits doesn't prompt when it's off. The editor learns what to hide from a per-connection `MapEditorCapabilities` TargetRpc sent when it opens, not a SyncVar.
- **Denylist** (`MapEditorSandboxRules`): spawn-point command kinds and the `NukeCart` / `NuclearAuthenticationDisk` assets, also in `RpcMoveObject`. Applies to every `IsSandboxPlayer`, so plain `-openbuild` servers get it too.
- **Auto-start**: 20 s idle countdown (`AutoStartCountdown`), then the normal Start (station load, warmup). An admin Cancel (warmup or idle countdown) or a failed station load **holds** auto-start until an admin Start; the hold is announced in Admin chat. Warnings go out as an announcement banner and an OOC line (`RoundEndWarnings`); rounds shorter than 5 or 1 minute skip that warning.
- **All-ready** counts every online player; observers don't exist yet, so none are left out.
- **Respawn** (`EntitySubSystem.Respawn.cs`, `RespawnPrompt`): the ghost's Mind moves to the new body. The sandbox profile removes the prior corpse after success, indexed by the server-attributed player ckey; a refusal keeps it. The prompt is a code-built panel on the UiShell HUD layer; a reconnecting ghost isn't re-offered a respawn until it dies again.
- **Ghost editing** needed no fix: `CanOpenEditor` passes for a ghost and the camera hand-over works (pilot screenshot `sandbox-ghost-editing.png`).
- **Leaks fixed on the way** (found by the pilot's reset checks): corpses left `_spawnedPlayers` at death and survived every reset; every spawn's `Mind` was never despawned (+1 NetworkObject per player per round). Death also logged a `SetFirstObject` error (`SwapMinds` on the corpse's invalid owner) and a refused `InteractionController` removal (now disabled instead).
- **Files**: `reset-health.jsonl` and `bugs.jsonl` sit in `Data/ServerMeta/` next to `round-history.jsonl`. `/bug` is intercepted in the server's OOC handler (lobby/Escape chat) and forwarded from the in-round compose box.
- **P5 server half**: `-heartbeat-file=<path>` (main-thread timestamp every 5 s) and `-exit-after-rounds=<N>` (`ServerLifetimeOptions`).
- **Bug report dialog** ([sandbox-server.md](../design/sandbox-server.md) §7, revised 2026-11-01; built on top of Decision 12): F8 (`InputSubSystem.OpenBugReport`), a bare `/bug`, or **Report a bug (F8)** in the lobby/Escape menu opens it. A client sends the report to the server, which appends it to `bugs.jsonl` with `source: "dialog"` and saves the reduced screenshot to `ServerMeta/bug-screenshots/`. An own host saves a zip in `Data/Exports/` instead (report, logs, full screenshot, optional current map via `PersistenceSubSystem.CaptureStationTemplateJson`). One report per ckey per minute, shared with `/bug <note>`; the chat confirmation now names the round. `RoundId` became a SyncVar and the dialog adds broadcasts, so `ProtocolVersion` went to 8. "Map ships with the game" is a hard-coded list (`ministation`, `Metastation v2`) in `BugReportSubSystem`. The sandbox pilot runs the local-bundle path; the graphical remote pilot sends a report over the network, checks the server-attributed record and reduced JPEG, and checks cooldown on reopening.
- **Open finding**: plain GameObjects grow by about 8 per reset in the editor Host (336 → 344) while NetworkObjects stay flat. Watch it in `reset-health.jsonl` on the real server before calling it a leak.

## Parked

Discord webhook for `/bug` · vote-kick, extend and reset-now votes · public default server target in release builds · auto-deploy to TomNAS · purpose-built sandbox map and map rotation · gas/atmos caps · client-side export of a build · a real Sandbox/Builder gamemode ([creative-mode.md](../design/creative-mode.md) §15).
