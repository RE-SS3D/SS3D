> Code paths: Assets/Scripts/SS3D/Systems/Rounds/, Assets/Scripts/SS3D/UI/Lobby/
> Entry points: RoundSubSystem, ReadyPlayersSubSystem, RoundSubSystemBase, LobbyUiSubSystem
> Status: partial
> Verified: dde6992c8 — 2026-11-03

# Rounds / lobby

## Overview

Round lifecycle state machine with single-flight `CancellationTokenSource` (prevents double start/stop and embark-during-ending races). States: `Stopped → Preparing → WarmingUp → Ongoing → Ending → Ended`. `PrepareRound` awaits `WorldReadinessSubSystem` `WorldReady` (not a fixed 500 ms delay).

**UITK lobby:** `LobbyUiSubSystem` owns the shell and guided Character Creator on
`UiLayer.Modal`. Ready/latejoin, player readiness/counts, round timer, admin controls and job
preferences are networked. Jobs use H/M/L/N chips; latejoin resolves those preferences against
open slots rather than a direct job picker. Playtime locks and a gamemode consumer for antagonist
opt-ins are absent.

The saved Human character (name, body morphs, hair/beard/eyebrows and independent colors) persists
client-locally and is sent to the server for application to the next spawned body. Other species
are disabled until models exist. Creator body controls are Height/Female/Fat/Muscle/Chest/Jaw,
matching the model's available controls. Character slots and the last saved selection survive
restarts; unsaved drafts do not overwrite saved slots.

The shell is also the in-round Escape menu (`InputContext.Menu`). Its Display, Audio and Controls
tabs apply through `PlayerSettingsSubSystem`; controls use real binding overrides and conflict
rules ([player settings](../2026-10_player-settings.md), [inputs](inputs.md)). The Chat tab carries
OOC/LOOC/Admin chat ([comms](chat-audio-screens.md)). Server Info shows persisted server name/MOTD,
configured mode, server build/uptime and git-generated change log. Per-server banner art and
ping/account-age fields are absent.

The UITK shell is the sole lobby UI. Do not restore the removed uGUI lobby/customizer or
`GameScreens`; the round simulation remains live ([composition policy](../2026-07_agent-first-composition.md)).

**Round end** ([2026-11_round-end-loop](../2026-11_round-end-loop.md) P1, [round-end.md](../../design/round-end.md) §2, §5–§6): every trigger (timer, gamemode condition via `GamemodeSubSystem.EndRound`, admin stop) calls `RoundSubSystem.RequestRoundEnd(RoundEndRequest)`, which fixes a `RoundOutcome` on the spot: trigger, winner, configured vs. resolved mode, reason, duration, and every player who took a body (name, job, alive; a ghost counts as dead). `Nuke.Detonate` ends as an Operatives win with the station destroyed: the server announces it station-wide and every client plays `StationDetonationPresentation` (white-out, shake); the summary opens once the white-out is over. A gamemode gives a timer or admin end its winner through `GamemodeSubSystem.ResolveEnd` (Nuke Ops: [gamemodes-roles-traits](gamemodes-roles-traits.md) `NukeOpsRules`). Default round length is 30 minutes (Decision 20). Ending posts the result to OOC, appends round history, sends the outcome to every client (buffered `ObserversRpc`, so latecomers get it) and runs a 30 s beat (`RoundEndBeat`) that an admin can hold and resume from Server Settings. The world keeps running during the beat. `RoundEndUiSubSystem` paints the summary over living and ghost players on the shell's Overlay layer; Tab or the chevron collapses it to a banner. The lobby shell keeps the outcome as a **Last Round** tab until the next round ends. `AutoStart` defaults on for a dedicated server (`NetworkType.DedicatedServer`) and waits for `MinimumReady` ready players; the admin form edits both.

**Stopped** despawns the crew (`EntitySubSystem`) and then `RoundSubSystem.UnloadStation`
clears the station (`TileMap.Clear`, load state `NotLoaded`); the next Start restores the template.
The lobby blurs the world (`SetMenuBlur`) whenever it is visible, not only as the Escape menu.
The sidebar has a two-click **Quit to menu** button (`QuitRequested` → `NetworkSessionSubSystem.LeaveToMainMenu`): it leaves the session (ending it when hosting) and opens the [main menu](ui-shell-main-menu.md). The Server Info **Change Log**
lists first-parent changes (merged PRs by title, `#N` links) grouped under the `v*` release tag that
shipped them ("In development" before a tag) and links to the GitHub releases page. One starter
character ships (`CharacterCreatorMockData.Loadouts`).

Server Settings now has the reference state card, countdown actions, map/mode selectors,
player cap/round-length inputs and rule checkboxes. `ServerSettings.cs` extends the existing
round subsystem: permission-checked ServerRpcs and admin-only TargetRpc snapshots. Late-join policy and station load state/name are publicly
replicated; gamemode identity stays protected. Configuration is editable while stopped, retained
across rounds in memory, and not yet persisted to disk. Map options come from saved station
templates. Session boot creates an empty tile container without restoring any station; Start
always restores the selected template and awaits world readiness before the countdown.
The map selector stays editable while stopped, even if a prior round left a station in memory.
Public and admin indicators distinguish no map, loading, initializing, ready and failed.
Only Stop is visible while a round runs; Start is visible only while stopped/ended.
Secret resolves to the sole available antagonist mode; Extended suppresses objectives;
Nuclear Emergency uses the existing mode. Random Events, Friendly Fire and Cloning controls
are disabled with explanations because their configurable gameplay consumers do not exist.
The waiting card says Manual start unless `AutoStart` is on (then the auto countdown, or that it is held).
Start validates the restored template before world readiness/countdown: every offered job needs an authored marker;
Secret (currently the nuclear mode) and Nuclear Emergency also need an operative marker, placed `Nuke` and placed `NukeCard`.
Failures return to Stopped/Failed with the complete actionable list in the admin settings status; all start paths share the gate.
An empty job configuration or unsupported mode fails closed. Missing/unreadable templates retain the existing load-failure path.

**Sandbox profile** ([2026-11_sandbox-server](../2026-11_sandbox-server.md)): `-sandbox` overlays the in-memory settings at boot (`SandboxProfile`): Extended, 20 min (`-sandbox-round-minutes=N`), cap 16, and the rule fields `AutoStart`, `AllConnectedReady`, `RespawnEnabled`/`RespawnCooldownSeconds`/`RemoveCorpseOnRespawn`, `MapPersistence` (sandbox removes the previous corpse after successful respawn), which the admin form doesn't edit except `AutoStart` (`WithRulesOf` keeps them). `AutoStart` (also on by default for a dedicated server) runs a 20 s idle countdown (`RoundAutoStart.cs`) once `MinimumReady` players are ready, then the normal Start; an admin Cancel or a failed load holds it until an admin Start. `AllConnectedReady` spawns everyone online. Every timed round warns at 5 and 1 minutes left. Each round gets a per-process `RoundId`; after Stopped, `ServerMeta/reset-health.jsonl` gains a line, and `/bug <note>` or the bug report dialog writes `ServerMeta/bugs.jsonl` (`RoundObservability.cs`): the server stamps time, round, ckey, alive/ghost/lobby and position, and keeps the dialog's summary, details, severity, crash flag, client context and log tail; its reduced JPEG goes to `ServerMeta/bug-screenshots/`. Both paths share one report per ckey per minute (`BugReportRateLimiter`). `RoundId` is a SyncVar so clients can name the round. The dialog itself: [ui-shell](ui-shell.md). `-heartbeat-file=` and `-exit-after-rounds=` (`ServerLifetime.cs`) serve an unattended server.

Unsaved map edits across a round stop (A4): the map editor runs during an Ongoing round, and
Stop unloads the station, so an admin Stop with unsaved editor commands first opens
**Save and stop / Discard and stop / Cancel** in the lobby shell. The confirmation is bound to the editor
revision and template; a stale one asks again. Saving a map with no template name needs a fresh name.
Any other round end (timer, gamemode) autosaves unsaved edits to `<template>-autosave` (never the
original) and tells admins in Admin chat. Unloading resets the editor's history and template identity.

## Start here

- `Assets/Scripts/SS3D/UI/Lobby/LobbyUiSubSystem.cs` — UITK shell + Character Creator host; attaches into Modal
- `Assets/Scripts/SS3D/UI/Lobby/LobbyShellView.cs` — Player View tabs/sidebar, Chat tab (`SetChat` / `ChatSubmitted`) + Display/Audio/Controls intents
- `Assets/Scripts/SS3D/UI/Lobby/CharacterCreatorView.cs` — guided-steps Character Creator, local character slots + pinned Save footer
- `Assets/Scripts/SS3D/UI/Lobby/CharacterProfileStore.cs` — versioned local presets under `Application.persistentDataPath/Lobby/characters.json`; atomic replacement, unreadable/newer file preservation, pilot-isolated profiles
- `Assets/Scripts/SS3D/UI/Lobby/CharacterCreatorDraft.cs` — local name/species/body/style/preferences + separate hair/beard/brow colors + stable outfit index
- `Assets/Scripts/SS3D/Systems/Entities/Character/CharacterAppearance.cs` — replicated character + server sanitization; `Human.cs` applies it, `Mind.DisplayName` is the in-world name
- `Assets/Scripts/SS3D/Systems/Entities/Character/CharacterPreviewBooth.cs` — off-map RT booth (layer 23, own lights), worn jumpsuit/shoe mesh application
- `Assets/Scripts/SS3D/UI/Lobby/LobbyAssetCatalog.cs` / `Assets/Scripts/SS3D/Editor/LobbyAssetCatalogBuilder.cs` — curated preview outfit references; umbrella catalog rebuild
- `Assets/Scripts/SS3D/Systems/Roles/RoleSubSystem.cs` / `JobResolver.cs` — replicated job slots, `JobPreferencesMessage` handling, round-start + latejoin assignment ([roles map](gamemodes-roles-traits.md))
- `Assets/Scripts/SS3D/UI/Lobby/LobbyServerSettingsView.cs` — admin form and live enabled states
- `Assets/Scripts/SS3D/Systems/Tile/Validation/` — shared map requirements/report and tracked-map content adapter
- `Assets/Scripts/SS3D/Systems/Rounds/SandboxProfile.cs`, `RoundAutoStart.cs`, `RoundObservability.cs`, `ServerLifetime.cs` — sandbox profile, auto-start/warnings, round ID/reset-health/`/bug`, heartbeat/exit-after-N
- `Assets/Scripts/SS3D/Systems/Rounds/ServerSettings.cs` — validated configuration, admin RPCs, countdown commands
- `Assets/Scripts/SS3D/Systems/Rounds/MapEditorRoundStop.cs` — Stop confirmation for unsaved map edits, autosave before `UnloadStation`; `UI/Lobby/LobbyMapStopPrompt.cs` renders the choices
- `Assets/Scripts/SS3D/Systems/Rounds/ServerInfo.cs` — public name/MOTD/mode/build/start-time SyncVars, `ServerInfoText` sanitizers, `ServerMeta/server-info` load/save
- `Assets/Scripts/SS3D/Systems/Rounds/BuildInfo.cs` / `Assets/Scripts/SS3D/Editor/BuildInfoGenerator.cs` — game version + git commit + change log resource (gitignored, generated on editor load and pre-build; stamps the player version). `GameVersion.cs` formats versions — rules in [2026-10_game-versioning](../2026-10_game-versioning.md). Use `BuildInfo.Version`, not `Application.version` (an `0.0.0-editor` placeholder outside player builds)
- `Assets/Scripts/SS3D/Systems/Rounds/RoundSubSystem.cs` — concrete round subsystem; `PrepareRound` →
  `WaitUntilAsync(WorldReady)`; on Ongoing fires welcome via `CommsSubSystem.SendAnnouncement`
  (`StationWelcome` follow-up clip)
- `Assets/Scripts/SS3D/Systems/Rounds/RoundSubSystemBase.cs` — state machine base with generation-tracked CTS
- `Assets/Scripts/SS3D/Systems/Rounds/RoundEnd.cs` — `RequestRoundEnd`, outcome build, buffered outcome RPC, summary beat and admin hold
- `Assets/Scripts/SS3D/Systems/Rounds/Outcome/` — `RoundOutcome`/`RoundEndRequest` (record + triggers), `RoundEndBeat` + `RoundOutcomeBuilder`, `RoundSummaryModel` (viewer-relative fate/rows); pure, EditMode-tested
- `Assets/Scripts/SS3D/UI/Lobby/RoundEnd/` — `RoundEndUiSubSystem` (client host, Tab), `RoundEndOverlay` (panel + banner), `RoundSummaryView` (shared ledger builder), `LobbyShellView.LastRound.cs` (Last Round tab); styles `Content/Systems/UI/RoundEnd/RoundEnd.uss` via `RoundEndAssetCatalog` (umbrella rebuild)
- `Assets/Scripts/SS3D/Systems/Rounds/ReadyPlayersSubSystem.cs` — player ready tracking; spawns on `Ongoing`
- `Assets/Scripts/SS3D/Systems/WorldReadiness/WorldReadinessSubSystem.cs` — readiness gate for prepare

- `Assets/Scripts/SS3D/Editor/Automation/LobbyUiPilot.cs` — **SS3D → Testing → Run Lobby UI Pilot**, screenshot/check report under `artifacts/ui/`
- `Assets/Scripts/SS3D/Editor/Automation/Lifecycle/LobbyLifecyclePilot.cs` — **SS3D → Testing → Run Lobby Lifecycle Pilot**, host session lifecycle report under `artifacts/ui/lifecycle-*`; **Run Lobby Remote Client Pilot** launches `Builds/Game` as non-admin `remoteprobe` (report + client screenshots under `artifacts/ui/remote-*`); performs the client's `os_*` requests as real OS input through `Testing/ui/uinput_input.py serve` (virtual uinput device)
- `Testing/multiplayer/scenarios/lobby-remote-client.txt` — the remote client's half (UI and `os_*` opcodes in `AutomationSubSystem`)
- `Testing/ui/validate.sh` — preflight → compile → EditMode → both pilots → stale-build rebuild → remote; `Testing/ui/README.md` — runbook

## Extension points

- Round transitions: extend `RoundSubSystemBase` state handlers and messages in `Messages/`.
- Ending a round: call `RoundSubSystem.RequestRoundEnd` (or `GamemodeSubSystem.EndRound(request)`) with the winner and wording; never `RequestRoundStateChange(false)` directly, or the outcome falls back to an admin end. Nuke Ops rules (P3) build the request; antagonist rows set `RoundOutcomePlayer.Antagonist`.
- Spawn flow: `SpawnReadyPlayersEvent` after Ongoing (world must already be ready from PrepareRound).
- Lobby UITK: Session snapshots/actions live in `LobbyUiSubSystem` (including job preference sends); `LobbyShellView` renders `RoleSubSystem.Jobs`; Character Creator is a separate Modal screen.

## Pitfalls

- **Round-end typography must use explicit line boxes.** Web `line-height: 1`/`1.1` is not the fonts' natural TextCore height; leaving result/reason and roster labels unconstrained bloats the result and clips row text. Reset theme Label padding locally, constrain those labels, and keep the roster flexible. The overlay switches to 680 px wide with 16/84 px top/bottom clearance at panel heights ≤720.
- **The legacy `OdetoIdleGamingSDF` contains Spessman face/glyph data.** Round-end uses `RoundEndArcade.asset`, generated from the real Ode font by `RoundEndAssetCatalogBuilder`; don't replace it with the legacy asset. TextCore tracking uses percent-of-font-size units: the reference's `0.12em` requires `letter-spacing: 12px`, while badges/statuses use zero tracking.

- **Every round end now takes 30 s longer.** Ending runs the summary beat before Stopped, so pilot steps that wait on Stopped after a stop need ≥60 s timeouts (the lifecycle and sandbox pilots were raised). A stop during Preparing/WarmingUp still skips Ending.
- **Don't auto-switch lobby tabs under the player.** The Last Round tab opens itself once per round, except from Server Settings (pilots and admins wait on its buttons) or Chat (focus loss mid-line). Scenarios that need it select `#tab-last-round` explicitly.

- **Surface decals are client-local round state:** clearing the tilemap does not clear blood/bullet projectors held by static spawners. `SyncRoundState(Stopped)` clears both spawners on server/host and pure clients, including their instance materials.

- **The empty change log needs its own label style.** `lobby-shell__changelog-text` has `flex-basis: 0` for the row layout; reused on the lone "No change log in this build." label in the ScrollView's column it collapses to ~0 px and only the tops of the glyphs show (v0.1.0, every build without `BuildInfo.json`). The editor always has the file, so only player builds show it. Use `lobby-shell__changelog-empty`.
- **Portrait capture must restore `RenderTexture.active`:** `Graphics.Blit` changes the global active target. Save/restore it around the head crop so later rendering/readback does not inherit the portrait target; the lifecycle pilot uses a sentinel target to check this.

- **Long map status displaced Ready over the preview:** wrapping a full save name made the fixed sidebar too tall. Keep status labels single-line with ellipsis; the lifecycle pilot uses a long temporary map name to check this.

- **Rapid settings edits lost earlier values:** UI snapshots can lag later callbacks. The UI sends a field key; the server merges that one field into current authoritative settings. Never submit the whole cached form for an individual edit. The lifecycle pilot changes cap/length together to cover this race.

- **Admin state refresh:** ordinary player snapshots refresh every 200ms, but round/load-state changes bypass that throttle so loading and action visibility update promptly. Pilots still wait on enabled UI and completed layout before the next action.
- **Selected-map start:** enter Preparing before asynchronous restore, locking configuration and rejecting duplicate starts before cancelling any active operation. A stop (or Cancel Countdown) during Preparing/WarmingUp cancels the start via `CancelCountdown()` and goes straight to Stopped, skipping Ending; `ServerSettings` also resets a station load left at Loading/Initializing so Start re-enables.

- **Legacy lobby purged:** the uGUI lobby is deleted (Phase F), not suppressed. Before that, the suppression keyed on a name and silently matched nothing because `Game.unity` renamed the `LobbyCanvas` instance to `Lobby`, so the old lobby stayed live under the UITK shell. The pilots now assert there is no root `Lobby`/`Character Customizer` canvas. `GamemodeObjectivesView` still lives on the `Networked View` prefab; don't delete that prefab as lobby debris.
- **Ready window:** the server accepts ready toggles in every state except Ongoing/Ending and spawns whoever is ready on Ongoing, so Ready stays enabled through Preparing/WarmingUp (countdown). Ongoing switches it to Join Round (late spawn RPC).
- **Pilot clicks:** a synthetic `ClickEvent` does not fire `Button.clicked` (its `Clickable` listens to pointer/submit). Drive buttons with `NavigationSubmitEvent`; keep `ClickEvent` for `RegisterCallback<ClickEvent>` surfaces such as the preview. UI snapshots lag subsystem state by the 200ms refresh — wait on the UI, not only the backend.
- **No boot restore:** `TileSubSystem.Setup` creates only an empty container, never a saved station. The first Start waits for tile resources, restores the selected map and then awaits `WorldReady`. A default selection prefers saved `ministation`, otherwise the first saved template; it is not loaded until Start.
- **Session lifetime:** the lobby host survives network teardown. Read replicated subsystem snapshots with `TryGet`, hide both surfaces/booth offline or when the local player is spawned, and restore the saved draft on return.
- **Jobs prefs:** shell uses H/M/L/N chips (attached design), not drag-ranked lists (`lobby.md` §3) — an accepted presentation deviation. Jobs come only from `StartingRoles.asset`; a job needs a `RoleData` there to appear. Job resolution must run before `SpawnPlayer` (loadout reads the assignment); anything spawning players outside `SpawnReadyPlayers` falls through to latejoin assignment.
- **Preview booth:** live preview uses `CharacterPreviewBooth` on layer **23** with its own camera/lights/RT at (-250, -250, 250). Never share layer 22 or (-250, -250, -250) with `RuntimePreviewGenerator`: sharing put the dummy in every item icon — (the old `LobbyCameraRenderTexture`/`StaticWorldObjects` customizer was deleted in Phase F). Dispose the booth on host destroy; deactivate (don’t destroy) on Return to avoid respawn hitch.
- **E1 vs E2:** Save stores local character slots on disk and sends the saved slot as `CharacterAppearanceMessage` (also once per connection); `EntitySubSystem` sanitizes/stores it per ckey and stamps `Mind.CharacterName` + `Human.SetAppearance` at spawn. Save never changes a living body. Skin/eye tint and catalog-backed eyebrows render in the booth and on spawned bodies; account/server player-meta persistence is separate from local presets.
- **Spawned height:** scale the armature (`HumanoidRigReferences.ArmatureRoot`), never the networked root (capsule, NetworkTransform). The server applies morphs too so bone hit colliders match. Clothing mesh swaps reset shape keys — `ClothesDisplayer` calls `Human.ReapplyBodyMorphs`.
- **Style indices:** hair/beard replicate as `LobbyAssetCatalog` indices resolved via `HumanoidStyleSource.Current` (registered by the lobby UI); a headless server spawns no style meshes, and mismatched client/server catalogs would show the wrong style. Eyebrows replicate by lowercase catalog ID (legacy preset keys normalize in `CharacterAppearance`); skin/eye tint writes per-material property blocks so clothing and cut surfaces retain their materials. Body sliders map to Human.fbx blend shapes via `HumanoidMorphApplier` (Height is root scale — there is no Height shape key).

- **Preview stripping order:** remove combat/speech/movement dependents and the non-networked `HumanoidBodyStateBridge` before the generic network pass; prefab component order is not dependency order and Unity refuses required-component removal.
- **Preview orientation:** Human faces +Z at yaw 0 and the booth camera sits on +Z; a 180° base yaw silently swaps Front/Back. Keep base yaw at 0; verify the face in screenshots, not just the RenderTexture binding.

- **Creator colors:** lobby and creator are sibling Modal surfaces; variables declared only on `.lobby-shell` do not reach the creator. Scope shared lobby variables to both roots; reference text colors are overridden locally on `.char-creator`.
- **Character drafts:** live preview callbacks must not mutate the saved draft; commit on Save only, or switching/New Character silently overwrites the saved body/style. The loadout popup is the window's last absolute child so the step form cannot paint over it.

- **Slider fill:** Unity 6's Slider has no `unity-base-slider__fill` element; styling that selector silently leaves an empty track. Add a non-picking fill under the tracker and update its percentage width with the value.

- **Linux rounded inputs/images:** rounded borders on the settings dropdown and sprite preview rendered solid white in the tested editor. Keep those surfaces square; screenshot review is necessary even when all UI assertions pass.
- **Settings toggles:** use `Toggle.text` for check-before-label layout; forcing its input to `flex-grow: 0` collapses the text. Keep the input growing with an automatic basis.
- **Settings tests:** a `ScrollView` contains internal `Slider`s for its scrollers. Query `lobby-shell__settings-slider` when counting or checking enabled volume controls.

- **Startup preview goes black:** `AreaSubSystem.CompleteSetup` runs the orphan-light guard after an early booth is created. The guard must exempt `CharacterPreviewBooth`-owned lights; delaying the booth until creator open only hides the ownership bug.

- **Preview clothes:** use `Cloth.GetClothMesh` on catalog prefab assets and activate the dummy's existing Jumpsuit/ShoeLeft/ShoeRight renderers. Do not instantiate folded Item prefabs or route offline preview through server-only `ClothesDisplayer`. Pick an outfit once per draft, not on each appearance callback.
- **Remote client booth threw in `HumanoidController.Setup`:** instantiating the active Human prefab ran every gameplay `Awake` before stripping; on a remote client the booth is built before `CameraSubSystem` exists. The dummy is now instantiated under an inactive staging parent and stripped with `DestroyImmediate` before activation. Host-only pilots never saw this — run the remote pilot after booth changes.
- **Disabled lobby buttons looked clickable:** the fill-colour button rules had no `:disabled` state, so a non-admin's Admin (and unwired Observe) rendered like live buttons; `ui_wait … disabled` still passed. `.lobby-shell__btn:disabled` dims them. UI renders in linear colour space, so opacity 0.35 reads closer to 60% — judge the screenshot, not the number.
- **Server name/MOTD are not round config:** they bypass the idle-only settings gate, and a whole-form save keeps the current values. Pilots that edit them must restore the originals, because the file is the real `ServerMeta` data.
- **Appearance colors are `Color32`:** FishNet's default `Color` serializer truncates channels to hundredths. Keep replicated colors as `Color32`.
- **Chat Return ignored in real play:** a real Return keydown targets the `TextField`'s inner text element, which consumes it before it bubbles; the composer's bubble-phase `KeyDownEvent` handler never ran, while synthetic events sent to the field itself passed. Register Return/submit handlers on text fields with `TrickleDown.TrickleDown`; found by the remote pilot's real keyboard input.
- **Remote pilot asked for permission every run:** xdotool's XTEST input goes through Xwayland's libei portal on GNOME Wayland, which shows an "allow remote interaction" dialog per run. OS input now comes from a uinput virtual device (no dialog). Two silent failures on the way: without keycodes 1–31 all declared, udev doesn't tag the device `ID_INPUT_KEYBOARD`, so clicks worked and typing did nothing; and keys from a second, newly created device did not reach the window the first device clicked. Keep one device for the whole run (`serve`). See [Testing/ui/README.md](../../../Testing/ui/README.md).
- **Catalog rebuild over MCP:** SS3D → Data → Rebuild All UI Catalogs ends in a modal "UI Catalogs" OK dialog that blocks MCP and ignores xdotool under XWayland; prefer `LobbyAssetCatalogBuilder.RebuildCatalogBatch` via `execute_code`.
- **Pilot assembly stays reference-free:** adding `SS3D.Core`/`SS3D.Systems` to `SS3D.Editor.Automation.asmdef` turns on the project's Roslynator rules as errors for the whole pilot (≈25 RCS errors in `LobbyUiPilot.cs`). Reach runtime state through Unity APIs (`Resources.Load`, the pilot's output files) or reflection, as the Sound-tab settings checks do.
- **Players persist offline:** `PlayerSubSystem.GetPlayer(ckey)` still returns a disconnected player. Use the online count or the connection, not a null record, to detect a remote leaving.
- **Unloading the station is server-only and after despawn:** `UnloadStation` runs in `StopRound` after `RoundState = Stopped`, whose SyncVar callback already despawned players. Map editor sessions close themselves once the round leaves `Ongoing` ([tile](tile.md)), so no editor camera is left over a cleared map.

## Tests

- `Testing/ui/validate.sh --steps compile,round-reset` checks blood/bullet decal cleanup and atmos reseeding on an immediate host stop/restart, without the full sandbox timer or UI suite.

- `Testing/ui/validate.sh --weekly` runs these graphical pilots. `LobbyUiPilot` covers shell/creator visuals; `LobbyLifecyclePilot` covers host lifecycle, saved characters and settings; the remote pilot checks a built non-admin client. Review screenshots as well as assertions; OS-input Ready/chat checks do not imply every pilot action uses real input.
- **Run Sandbox Pilot** (`LobbyLifecyclePilot.Sandbox.cs`, `validate.sh --steps sandbox` or `--weekly`) applies the profile in Play Mode (`ApplyServerSettingsForTesting`) and runs two automatic rounds: countdown without Ready, death → ghost editing → Respawn, `/bug`, the F8 dialog saving a local bundle, 1-minute warning, wipe without autosave, reset-health lines with no leftover tile objects/items and flat NetworkObject counts. `SandboxServerTests` covers the pure rules.
- **Round end** (tier per the [testing philosophy](../2026-11_testing-philosophy.md)): `RoundEndLoopTests` (EditMode) covers the outcome per trigger, respawned players listed once, the viewer's fate and row, the beat and hold, countdown text, minimum-ready auto-start and the dedicated default; `SquadDrawTests` covers the draw. `validate.sh --steps lifecycle`: one host stop checks the overlay over a living player, the admin hold reaching the UI, return to the lobby and the Last Round tab (`round-end-summary.png`, `round-end-held.png`, `round-end-banner.png`, `lobby-last-round.png`). The nightly `sandbox-reset` smoke asserts that the outcome reaches a remote client (`assert_outcome Timer`). PlayMode tests and pilots run a 3 s beat (`ServerHelpers.TestEndBeatSeconds`, `RoundSubSystem.SetEndBeatSecondsForTesting`); only the pilot's summary step runs the real 30 s.
- `MapValidationTests` covers mode/job requirements, combined failures, repair, warnings and the scrollable save report.
- The lifecycle pilot creates temporary invalid/valid station copies, checks a failed Start lists all missing jobs without countdown/spawn, then repairs the file and retries. Remote smoke uses the valid copy; original templates are untouched. `server-settings-validation.png` captures the failure list.
- The lifecycle pilot edits a spawn during the first round, checks that Stop opens the prompt (`map-stop-unsaved.png`) and that Cancel keeps the round and edits, then that the timed round end autosaves. Later rounds check a player stop request with Discard and stop (no autosave) and Save and stop (template overwritten). `LobbyMapStopPromptTests` covers the name draft and choice submission.

## Depends on / Used by

- **Depends on:** [entities](entities.md), [player-control](player-control.md),
  [gamemodes-roles-traits](gamemodes-roles-traits.md), [persistence](persistence.md),
  [chat-audio-screens](chat-audio-screens.md) (round-start announce), [ui-shell](ui-shell.md),
  world readiness (tile/area/electricity/atmos/disposal)
- **Used by:** All in-round gameplay

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: lobby, round-end, round-config (autostart loop, opt-in list, Secret leak, fallback, `fallbackMap` bug)
- Design (read-only): [Documents/design/lobby.md](../../design/lobby.md), [round-config.md](../../design/round-config.md)
- [2026-07_session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md)
- [2026-07_comms-non-diegetic-feed](../../archive/architecture/2026-07_comms-non-diegetic-feed.md)
- [INDEX.md](../INDEX.md)

- **Preview framing:** authored skinned-renderer bounds include animation padding; empty equipment slots can report large bounds despite having no mesh. Fit the shared lobby/creator camera to baked visible geometry after evaluating the initial pose, skipping inactive renderers and null meshes. Keep framing fixed during morph edits so Height remains visible; padding leaves room for shoes and walking.
