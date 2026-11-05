> Parent map: [networking-session.md](networking-session.md)
> Code paths: Assets/Scripts/SS3D/Systems/Testing/, Testing/multiplayer/
> Status: partial
> Verified: ae1a54ed1 — 2026-11-05

# Multiplayer testing

## Overview

The smoke harness launches a real Linux dedicated server and separate headless clients, drives
`AutomationSubSystem` scripts, and checks each process's logs. The load runner adds configurable
client count, soak duration and server metrics. For agent-local checks the same scenario scripts
also run inside Unity Multiplayer Play Mode (main editor hosts, virtual players join as clients)
through `validate.sh --steps mppm:<scenario>`. The built smoke remains the nightly gate because it
exercises the `UNITY_SERVER` code paths. Graphical UI/session pilots have a separate
[runbook](../../../Testing/ui/README.md).

## Start here

- `Tools/build_client_and_server.sh` — Unity batchmode Linux builds; installed Unity required, project Editor closed. Outputs `Builds/Game/SS3D.x86_64` and `Builds/GameServer/SS3D.x86_64`.
- `Testing/multiplayer/run_smoketest.sh` — `<scenario> [client-count]`; server script is `<scenario>.txt`, client scripts prefer `<scenario>-client-N.txt`, then `<scenario>-client.txt`
- `Testing/multiplayer/smoke-set.txt` + `run_smoke_set.sh` — the curated set the nightly, `develop-release`'s opt-in smoke and `validate.sh` run; `--skip-nightly-only` drops `sandbox-reset` and `nukeops-round`
- `Testing/multiplayer/run_loadtest.sh` — `--clients N --duration SECONDS --stagger SECONDS`; defaults 8, 300 and 0
- `Testing/multiplayer/lib/process.sh` — port allocation, build staging and process lifecycle
- `Testing/multiplayer/lib/logwait.sh` — signal waits and log failure gates
- `Testing/multiplayer/tools/triage_run.sh` — summarize a run by id/path or `latest`
- `Assets/Scripts/SS3D/Systems/Testing/AutomationSubSystem.cs` — code-bootstrapped DSL runner, enabled by `-testscript=<path>` or (in the editor) an MPPM `smoke:<name>` tag
- `Assets/Scripts/SS3D/Application/MultiplayerPlayModePlayer.cs` — which MPPM player this editor is (`Main` or `Player 2..4`), its `smoke:` script, and the result directory
- `Assets/Scripts/SS3D/Editor/Automation/MultiplayerPlayModeSmoke.cs` — tags players, launches/parks the clones, and lists their state through MPPM internals by reflection
- `Testing/multiplayer/mppm/` — host scripts that replace the dedicated-server side under MPPM; the clone runs a `scenarios/` client script, or an `mppm/` one when the scenario has no built smoke (`breach-budget-client`)
- `Testing/ui/validate.py` `MPPM_SCENARIOS` — maps each `mppm:<scenario>` to its host and client scripts; `MPPM_STATIONS` copies a `Testing/fixtures/stations/` map into `Builds/Game/Data/StationTemplates` for the run and removes it after
- `Assets/Scripts/SS3D/Systems/Testing/AutomationScript.cs` — instruction parsing
- `Assets/Scripts/SS3D/Systems/Testing/TestSignal.cs` — structured readiness/completion signals
- `.github/workflows/multiplayer-smoke-test.yml` — the nightly: PlayMode job, then build + smoke set, then the `nightly-failure` issue; also manual and labelled-PR

## Extension points

- Add server/client scenario text under `Testing/multiplayer/scenarios/`. Use `wait_connected`, `wait_clients` (server: N remote clients loaded their start scenes), `ready`, `start_round`, `wait_round`, `embark`, `assert_embarked`, `wait_players` (N bodies spawned, as this process sees them), `reconnect`, `console`, `wait_console` (retry a console line until it stops throwing), `wait_timeout`, `wait_seconds`, `assert_reset_health` (server: N lines in `reset-health.jsonl`), `assert_outcome <Trigger> [Winner] [antagonist]` (client: the last round outcome arrived with that trigger, and winner if given, and lists this player, as an antagonist if asked) and `disconnect`. Atmos breach checks: `use_station <map> <mode>` (host: the next round's map and mode; no spaces in the map name), `breach_hull` and `breach_beside_client` (server: open a hole to space), `scatter_items N` (server: loose items by the last `breach_hull`), `atmos_budget S` / `client_traffic S` (server tick cost and patch traffic / client received bytes, logged as `[atmos-budget]`) and `assert_wind_felt` (client: streaks drawn, its own body pushed, and wind and thinning air heard). `assert_internals_toggle` (client: the owned body offers internals, and toggling them from this process changes the server's breath). `wind_on_clients <speed> <seconds>` (server: blows reversing wind at remote bodies) and `assert_magboots_wind` (client: the wind moves the owned body, boots switched on from this process stop it, and off lets it move again). `{ckey}` in a `console`/`wait_console` line expands to the process's own ckey. A scenario's optional `scenarios/<name>.conf` (sourced) sets `SERVER_EXTRA_ARGS` and `CLIENT_TIMEOUT`; `embark` requests spawning, `assert_embarked` confirms control of a body.
- Add an MPPM scenario by registering it in `validate.py` `MPPM_SCENARIOS` (and `MPPM_STATIONS` if its host loads a fixture map with `use_station`).
- Expose deterministic assertions through `console` commands or DSL instructions. Emit distinctive signals and throw on failed assertions so the runner emits `ScriptFailed`.
- Graphical clients also accept `ui_wait`, `ui_text`, `ui_submit`, `os_click`, `os_type`, `os_key` and `screenshot`. `ui_submit` is synthetic navigation; the remote pilot services `os_*` through the OS-input bridge.
- Override builds with `SS3D_SERVER_BUILD_DIR`, `SS3D_CLIENT_BUILD_DIR`, `SS3D_SERVER_BIN_NAME` and `SS3D_CLIENT_BIN_NAME`. Both runners accept `SS3D_TILEMAP_FIXTURES` and `SS3D_STATION_FIXTURES` for map data.
- Use structured `Test signal` records for waits. Keep compact JSON file logging enabled; Unity exceptions also require checking `unity.log`.

## Pitfalls

- **Remote pilot clicks miss the client:** `os_*` inputs are real pointer events at screen coordinates. A maximized editor covers the client, and a client on default Borderless/native settings renders 1920×1080 inside a smaller window. Keep the editor windowed and give the client an isolated (`XDG_CONFIG_HOME`) windowed 1600×900 config; `Tools/ci/editor_session.sh` and `LobbyLifecyclePilot.LaunchClient` do this.
- **Round starts before a client loaded Game (MPPM):** the clone's client can finish connecting
  before it has loaded Game. Starting the round in that window makes FishNet drop the round's scene
  objects for it (`SceneId ... not found in SceneObjects`, then missing
  `TileSubSystem`/`SelectionSubSystem`). The MPPM host scripts wait with `wait_clients 1` after
  `wait_connected` before `start_round`.
- **Clones must not connect before the host has Game:** connecting earlier also drops Game's UI
  scene objects (`Console`, `PlayerCanvas`, `NetworkedView` — the `SceneId` errors) and the clone
  then fails `wait_lobby`. The MPPM host writes `Temp/mppm-automation/host-ready` after
  `wait_connected`, once Game is loaded (`AutomationSubSystem.MarkHostReady`); a clone waits for
  that marker before `StartNetworkSession` — the ordering the built smoke gets from `ServerReady`.
- **The host's Intro load raced FishNet's online scene:** the editor loaded Intro at Play Mode start
  while the automation started networking, and FishNet loads Game at the same time. When Intro won,
  the host stayed in Intro, the local client never got a ckey, and both connections timed out
  ("Server has timed out", intermittent). An editor automation host now skips Intro/Launcher and
  loads Game through FishNet alone, like a dedicated server.
- **MPPM `PlayerState` sticks at `Launching` on Linux:** the clone editor can be fully up and enter
  Play Mode while MPPM still reports `Launching` (2.0.2). `validate.py` treats the clone process
  plus a `Loaded scene` line in `Library/VP/<id>/Logs/Editor.log` as ready, not `PlayerState`.
- **MPPM's player control is internal:** `MultiplayerPlayModeSmoke` reaches `PlayMode.Editor.MultiplayerPlaymode`
  and `UnityPlayer` by reflection; an editor upgrade can rename them, and the step fails with
  `MPPM internals changed`. `CurrentPlayer.Tags` / `IsMainEditor` are the public API.
- **MPPM starts from the open scene:** a restarted Editor reopens its last scene, and any scene but Boot has no NetworkManager, so the host never starts and Player 2 sits at the launcher. `MultiplayerPlayModeSmoke.Prepare` opens Boot first. Hit 2026-11-05.
- **A clone's log follows the main editor's `-logFile`:** started through `open-unity.sh`, Player 2 logs to `artifacts/ui/editor.log-<virtual project>.txt` and `Library/VP/<virtual project>/Logs/Editor.log` goes stale, so waiting on that file alone times out at 420 s with a healthy clone. `clone_logs_by_name` takes the newer of the two. Hit 2026-11-05.
- **A clone opens the virtual project read-only:** `NetworkSettings` role/ckey overrides for a
  clone are in-memory only (`CommandLineArgsSubSystem.ApplyMultiplayerPlayModeRole`), never written
  to the shared asset.

- **Round start silently denied:** staged clients need Administrator permissions. Clear stale staged ServerMeta permissions before seeding `Config/permissions.txt`; the persistence envelope otherwise overrides it.
- **No station / zero atmos cells:** player build directories can lack CWD map data. Stage valid fixtures; content availability and validation requirements still apply to scripted starts.
- **Headless client crashes before game code:** Linux clients without DISPLAY need `SDL_VIDEODRIVER=dummy`; `process.sh` supplies it. Dedicated servers do not require it.
- **Headless UI instructions time out:** lobby documents skip bootstrap under batchmode/Null GFX. Use a graphical client for UI assertions and screenshots.
- **A completed windowed client never exits:** key success on `ScriptComplete`, then clean up leftovers; process termination alone is not the completion signal.
- **Noise classification hides a regression:** `known_unity_bad.patterns` is a failure gate. `known_unity_noise.patterns` is triage-only; never move pure-client SyncVar-write warnings into the noise list.
- **Disk usage jumps:** staging hardlinks immutable build files and copies writable Config/Data/Logs. Different filesystems require a full-copy fallback.

## Tests

Run the smoke set after building (or one scenario with `run_smoketest.sh`):

```bash
./Testing/multiplayer/run_smoke_set.sh
./Testing/multiplayer/run_smoketest.sh observer-sync 2
./Testing/multiplayer/tools/triage_run.sh latest
```

`late-join` (2 clients) covers connect/ready/start/embark, a console round trip and embarking
after Ongoing (it absorbed `basic-round`); `reconnect` checks reclaiming a body; `observer-sync`
(2 clients) has client 0 hurt itself, open the nearest airlock (`doordebug`), switch its area's
lights off (`lightdebug`) and heat its tile, while client 1 checks each change arrived with
`observesync` and `atmosclientstatus` — including the toolbox the Assistant loadout already puts
in client 0's hand (it replaced `atmos-client-sync` and the planned light-sync scenario); `sandbox-reset` (nightly only) runs a
`-sandbox` server with one-minute rounds and checks that a client that never readies joins two
automatic rounds and the reset writes `reset-health.jsonl`; `nukeops-round` (nightly only) runs a `-nukeops` server
with one-minute rounds and a one-player threshold: a client that never sent lobby preferences is drawn as an
operative by the default opt-in, gets a timer crew win naming it on the antagonist roster, and readies into the
next automatic round. The nightly runs the set after the
PlayMode job; PR label `test:multiplayer` runs both without the nightly-only scenarios. Budgets and
the `nightly-failure` issue: [testing philosophy](../2026-11_testing-philosophy.md#harness-contracts) D7 and D9.
GitHub disables scheduled workflows after 60 days without repo activity; check
`gh workflow list --all` if nightly runs stop.

The graphical remote pilot also files a bug report over client/server broadcasts, verifies the server-attributed JSONL and reduced JPEG, and checks the reopened dialog's cooldown; see the [UI runbook](../../../Testing/ui/README.md#lobby-remote-client-pilot).

With the Editor open, `Testing/ui/validate.sh --nightly` runs compile, EditMode, PlayMode, stale
builds and smoke; `--weekly` runs the visual, lifecycle and remote pilots (rebuilding stale builds); the sandbox pilot is on demand (`--steps sandbox`). Reports live under `artifacts/ui/`;
smoke logs are preserved under `Testing/multiplayer/.runs/`.

Agent-local multiplayer check without a build, in Multiplayer Play Mode (needs the
`com.unity.multiplayer.playmode` package and MPPM enabled in the editor; the main editor must be the
only open SS3D editor):

```bash
Testing/ui/validate.sh --steps mppm:late-join
Testing/ui/validate.sh --steps mppm:reconnect
Testing/ui/validate.sh --steps mppm:breach         # breach wind reaches a remote owner
Testing/ui/validate.sh --steps mppm:breach-budget  # atmos tick cost and traffic on atmostest, logged
```

The main editor hosts and runs `Testing/multiplayer/mppm/<scenario>-host.txt`; Player 2 joins as a
client and runs the matching `Testing/multiplayer/scenarios/` client script. The host writes
`Temp/mppm-automation/host-ready` once its start scenes are loaded, and the clone waits for it
before connecting, so the ordering matches the built smoke's `ServerReady` gate. Each player writes
`Temp/mppm-automation/<player>.json` (`signal`, `reason`, `errors`); the step fails on anything but
`ScriptComplete` or on any logged error, like the smoke log gate. Result copies land in the validate
folder as `mppm-<scenario>-<player>.json`. Rough warm timings: launch ~30–60 s, late-join ~1 min,
reconnect ~1 min. `breach-budget` has no thresholds; it only logs the numbers. These are on demand only; the built smoke stays the nightly gate.

Load example: `./Testing/multiplayer/run_loadtest.sh --clients 8 --duration 300 --stagger 2`.
Logs/`metrics.csv` live under `.runs/load-*`. Every 10 s the runner samples server RSS/CPU/threads
and host-wide loopback bytes/rates; run on an otherwise idle host. Embark counts are observed
signals, not continuous liveness. No performance thresholds or scheduled 24/7 soak are enforced.

Coverage gaps: rendered selection/drop/pickup, dedicated-client pocket/container round trips,
the complete Nuke Ops loop, and Windows clients. The headless runner cannot prove visual behavior.

## Depends on / Used by

- **Depends on:** [networking-session](networking-session.md), [permissions](permissions.md), [rounds-lobby](rounds-lobby.md) (round control, `-sandbox`/`-nukeops`), [atmospherics](atmospherics.md) (breach scenarios), Linux builds, `jq`
- **Used by:** release/smoke CI, [UI validation](../../../Testing/ui/README.md), multiplayer triage skills

## Related docs

- [CI build/release](../2026-07_ci-develop-release-pipeline.md), [self-hosted CI work](../2026-07_multiplayer-testing-self-hosted-ci.md)
- [Verification debt](../TECH_DEBT.md#110-play-mode--multiplayer-verification-is-optional-in-practice)
