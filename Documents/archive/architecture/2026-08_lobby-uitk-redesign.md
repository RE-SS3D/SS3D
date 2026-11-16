> Implements: Documents/design/lobby.md
> Touches systems: rounds-lobby, ui-shell, electricity (preview light ownership), inventory (read-only worn-mesh sources)
> Status: shipped

**Progress:** Phases A–F; E2 for Human only. Deferred beyond this effort: non-Human species models, a gamemode consumer for antag opt-ins, OOC/LOOC in the in-round feed, chat moderation/history, runtime application of player Settings, per-server banner art.

# Lobby UITK redesign

Replace the condemned uGUI lobby with a UiShell Modal UITK Lobby Shell matching the attached
pre-round lobby design. Character Creator is a **separate** modal screen opened from the lobby
character preview.

## Phases

| Phase | Scope | Status |
|---|---|---|
| A | Lobby Shell layout + USS + mock data; catalog; preview placeholder click stub | **shipped** |
| B | Character Creator separate screen (guided-steps visual) | **shipped** (visual) |
| C | Wire Ready / round state / admin start-stop / player list / show-hide / chat | implemented (networked OOC/LOOC/Admin chat shipped); host lifecycle + remote non-admin client verified (Ready click and chat typing as real OS input; other steps synthetic) |
| D | Jobs H/M/L/N + antag prefs sync + resolution | **shipped** (round-start pass + latejoin; antag opt-ins stored, no gamemode consumer yet) |
| E1 | Live booth preview + local Save/Return draft (name) | **shipped** |
| E2 | Appearance apply/sync (hair/skin/catalog, network, spawn) | **shipped for Human** — name/body/hair/beard/eyebrows and skin/eye/style colors sync to spawned bodies; local presets persisted; other species disabled pending models |
| F | Purge condemned LobbyCanvas / uGUI views; docs finalize | **shipped** — see [Phase F](#legacy-ugui-purge-phase-f--2026-10-30) |

## Design divergences (do not edit design docs)

- Jobs UI uses **H/M/L/N priority chips** from the attached design, not `lobby.md` §3 drag-ranked list.
- Character Creator follows design option **1A Guided Steps** (Identity → Body → Style → Review).
- Character Creator is in scope for this effort (separate screen); `lobby.md` §6/§11 name-only identity is deferred only until Phase E2 lands.

## Entry points

- `Assets/Scripts/SS3D/UI/Lobby/LobbyUiSubSystem.cs` — DDOL bootstrap; shell + creator + booth lifecycle
- `Assets/Scripts/SS3D/UI/Lobby/LobbyShellView.cs` — full shell chrome
- `Assets/Scripts/SS3D/UI/Lobby/CharacterCreatorView.cs` — guided-steps Character Creator
- `Assets/Scripts/SS3D/Systems/Entities/Character/CharacterPreviewBooth.cs` — off-map RT booth (layer 23)
- `Assets/Scripts/SS3D/Systems/Entities/Character/HumanoidMorphApplier.cs` — Human.fbx body blend shapes (Female/Fat/Muscle/…)
- `Assets/Content/Systems/UI/Lobby/LobbyShell.uss` (+ `@import CharacterCreator.uss`) + `Resources/LobbyAssetCatalog.asset`
- Rebuild: **SS3D → Data → Rebuild All UI Catalogs**

## 2026-10-30 Character Creator refresh

Source: `SS3D Pre-round Lobby UI(1).zip`, `Character Creator - Options.dc.html`,
active option **1A Guided Steps**. The archive's older, removed lobby Character tab
is not the target. Implemented Species dropdown, removal of Uniform Color,
1360×820 maximum window with a 400px preview dock, independent step scrolling,
and one pinned footer action (Next → Save Character → Saved). Creator-local colors
and typography follow the supplied reference; shared lobby variables apply to both
sibling surfaces. Hair/beard labels strip internal prefab prefixes.

The owner explicitly chose to keep the six model-supported body controls
(Height/Female/Fat/Muscle/Chest/Jaw) rather than approximate the reference's
Belly/Upper Body/Lower Body/Waist/Skin Tone controls. Body numeric values use 0–1.
Species, eyes and eyebrow selections are local draft preferences; only the Human
model, body morphs, and catalog hair/beard/color are applied in the live booth.
The follow-up `SS3D Pre-round Lobby UI(2).zip` moves Hair Color off Body and places
a context-labelled palette below the Style tiles: Hair Color, Facial Hair Color,
and Eyebrow Color. Each color is retained independently in the session-local draft;
hair and beard use independent live tints. Eyebrow color is stored pending its mesh.

Species models, skin/eye tint, eyebrow meshes, disk persistence and networked
appearance remain deferred E2 work.

Local character slots now retain saved name/body/style/preferences, switching
restores the selected slot, New Character resets the form, and Return restores the
last explicitly saved draft. These slots live only for the current UI session.

## 2026-10-30 Player View shell refresh

Source: `SS3D Pre-round Lobby UI(2).zip`, `Lobby Shell - Player View.dc.html` plus
its bundled Button/ToggleButton and color/type tokens. Player tabs remain Server
Info, Jobs, Chat, Players and Settings; the removed Character tab stays removed.

- Shell uses 70% width clamped to 900–1180px, a 280px sidebar, 24px content padding,
  reference text/button colors (height subsequently increased by owner request below). Ready/Observe/Admin
  use bold titling rather than arcade glyphs. Sidebar starts in Waiting For Players,
  with the ready ratio beneath; its preview remains clickable to open the creator.
- Server Info uses a contained banner in a 21:9 well, an inset information card,
  220px Change Log column (outer scrolling subsequently removed by owner request below). Jobs uses compact names, a separate
  slot count, contiguous 20px priority segments and visible disabled locked-job controls.
- Settings has underlined Display/Sound/Keyboard subtabs and 420px forms. Display:
  resolution dropdown, Fullscreen checkbox, UI Scale and Brightness blue sliders with
  0–1 readouts. Sound: Mute All Audio checkbox and three percentage volume sliders;
  muting dims/disables the sliders. Keyboard: eight action rows, binding buttons,
  local next-key capture and Escape cancellation. Values survive subtab changes.

**Scope:** these are session-local UI interactions, matching the supplied prototype.
Resolution/fullscreen, UI scale/brightness, audio mixer levels and input-action
rebinding are not applied to the game or persisted. The reference keyboard labels
(including Tab inventory / T chat) are prototype data, not the fork's actual default
scheme; consult [inputs](../../architecture/systems/inputs.md) before runtime wiring. Ready/jobs/chat,
player actions, observer and admin round APIs retain their existing mock/stub status
(Phases C/D). Admin Server Settings is a later fidelity/wiring pass.

**Rendering constraint:** the tested Linux Unity path rendered the dropdown and
sprite preview white with rounded input/preview borders. Those two surfaces use
square corners to preserve their actual content; solid-color action buttons keep
3px radii. No design spec or prefab asset was changed.

## Lobby layout/startup follow-up — 2026-10-30

Owner requested a scroll-free Server Info pane, a live character from the first
lobby frame, and a gap above Ready. Shell height is now 840px (still capped at 92%
of the viewport). Server Info uses a plain card instead of a ScrollView; banner
height follows its actual sprite aspect and can shrink before the information
section. All four Change Log entries and Map/Mode remain visible at the tested size.
Ready keeps bottom alignment and 16px top padding.

`LobbyUiSubSystem` creates the booth during shell attachment, applies the current
draft and binds the RT immediately. `LobbyShellView` no longer substitutes a static
preview image. Early creation exposed `GameplayLightGuard` disabling booth lights
as orphans during `AreaSubSystem.CompleteSetup`; the guard now recognizes lights
owned by `CharacterPreviewBooth` independently of station `LightPower` fixtures.

## Default preview outfits — 2026-10-30

The booth now reads a curated list of existing clothing assets from
`LobbyAssetCatalog.PreviewOutfits`: civilian (grey + high-tops), engineering
(engineer + jackboots), botany (botany + high-tops), and security (security +
jackboots). The shared UI catalog builder supplies these references and checks
for a jumpsuit mesh and both shoe meshes. No clothing Item is instantiated or
network-spawned: `CharacterPreviewBooth.ApplyOutfit` applies the existing `Cloth`
mesh maps to the dummy's Jumpsuit/ShoeLeft/ShoeRight rig renderers, matching worn
presentation. Existing palette materials and animation bones are retained.

An outfit is chosen once per new session-local draft with an independent RNG and
stored as `OutfitIndex`. Body/style edits do not reroll it; Save, Return and character
switching restore that draft's selection. This is preview clothing, not authoritative
spawn equipment or job loadouts. No Human/clothing prefab assets or art imports
are changed.

Verified by run `20261030-144343-787`: 100 checks, zero captured errors, 14 PNGs.
Startup, creator and side/body-edit captures were inspected. The run checked active
jumpsuit/both-shoe renderers, unchanged outfit meshes during body edits, and restoration
of the same meshes after Save → other character → New Character → saved character.

## Visual QA

The [MCP lobby pilot](2026-10_unity-mcp-lobby-pilot.md) automates Identity/Body/Style/
Review, name/body edits, preview rotation, Save/Return, character switching and
New Character reset, plus all player tabs and Settings interactions. See [Testing/ui/README.md](../../../Testing/ui/README.md)
for commands and evidence. It does not establish physical-input or multiplayer correctness.

Play Mode: click character preview → Character Creator with live humanoid RT; Body sliders drive
Human.fbx shape keys (Female/Fat/Muscle/Chest/Jaw) + height scale; Front/Side/Back rotates the
dummy; **Save Character** updates lobby name; **Return to Lobby** restores the shell with the saved
RT appearance. The old uGUI lobby was removed in Phase F; the pilots assert it is absent.

## Related

- Design: [lobby.md](../../design/lobby.md)
- Maps: [rounds-lobby](../../architecture/systems/rounds-lobby.md), [ui-shell](../../architecture/systems/ui-shell.md)

## Preview framing — 2026-10-30

The shared booth camera now fits baked visible geometry after evaluating the initial pose, ignoring inactive renderers and empty equipment slots. This removes inflated animation bounds from the fit and enlarges both lobby and creator previews. Framing remains fixed during morph edits, with padding for movement, shoes and height changes.

Run `20261030-150133-789` passed 102 checks with zero captured errors. Rendered character occupancy measured 84% of preview height, with head and feet inside the frame. Lobby, Identity and Body side-view screenshots were visually reviewed.

## Session wiring — 2026-10-30

`LobbyUiSubSystem` reads replicated player, ready, entity and round snapshots every
200ms. Ready broadcasts to the existing server API; Ongoing switches the action to
Join Round via the existing late-spawn RPC. The server resolves ready requests from
the sending connection instead of trusting a supplied ckey. The shell and creator
hide on spawn/disconnect, returning with the saved draft when back in the lobby.
The preview booth is deactivated while hidden. Headless/batch sessions skip UI bootstrap.

Players and ready counts are live, with unavailable ping/account fields shown as
unknown. Administrator permission gates the Admin button and start/stop requests;
the server retains its permission check. Mock force-start/extend-timer controls are
removed. Observe is disabled until an observer backend exists. Legacy LobbyCanvas
roots are disabled at runtime, including after scene reload; asset purge remains Phase F.

Lobby chat and server metadata (both shipped 2026-10-31, below), settings application
and networked/persisted character appearance remain subsequent implementation slices.
The existing visual pilot now checks production legacy suppression instead of doing
the suppression itself. Runtime/multiplayer verification is required before Phase C
can be called shipped.

Validation: Unity 6000.3.16f1 `-batchmode -nographics -quit` completed successfully
with the runtime/editor assemblies and enforced analyzers. `git diff --check` passed.
The graphical editor exited during display initialization, so the rendered pilot
and host/client ready → round → spawn → return flow were not rerun in this pass. See the lifecycle verification below.

## Lifecycle verification — 2026-10-30

New **SS3D → Testing → Run Lobby Lifecycle Pilot** (`Assets/Scripts/SS3D/Editor/Automation/Lifecycle/`, moved from `Assets/Editor/Automation/`,
own asmdef because it references SS3D runtime assemblies and therefore the enforced
analyzers). Run `lifecycle-20261030-153833-081` **passed**: 28 checks, zero captured errors,
72s on a single editor Host. Covered: legacy suppression, ready/unready counts and labels,
round start while the creator is open (both surfaces and the booth hide on spawn), stop →
shell (not creator) with booth/RT and saved name restored, Admin → Start Round with an
unready player (stays in lobby, Join Round enabled), late join spawn/hide, second stop/return,
local client disconnect (hidden, booth off) and reconnect 70s after server start (shell, booth,
player list, Ready restored). The existing visual pilot also passed (run `20261030-154104-676`).

Fixes found by the runs:

- Legacy suppression never matched: `Game.unity` names the `LobbyCanvas` instance `Lobby`.
  Suppression and the visual pilot now key on the `LobbyScreen` component.
- Ready was disabled during Preparing/WarmingUp although the backend accepts it then; it now
  stays enabled until Ongoing (Join Round) and is disabled only while Ending.
- FishNet (vendored) seeded a new connection's `PacketTick` with a rejected update, leaving
  `LocalTick` 0: any connection opened 60s+ after server start could be timed out before its
  first packet (host reconnect was kicked). One-line `SS3D patch` in `NetworkConnection.Initialize`.
- Editor playtests now boot `ApplicationSettings.EditorStationTemplate` (`ministation`), not the
  most recent Metastation template: first round start dropped from ~72s to ~5s.

Not covered: a separate client process (no current client build), non-admin permission
gating, OS input. The pilot reconnects the Host's local client, retrying like the recovery
overlay because a start issued while Tugboat is still stopping is dropped.

## Remote client verification — 2026-10-30

**SS3D → Testing → Run Lobby Remote Client Pilot** boots the editor Host, launches a fresh Linux
client build (`Builds/Game`, ckey `remoteprobe`, 1600×900 windowed) running
`Testing/multiplayer/scenarios/lobby-remote-client.txt`, and drives the admin side. The client
asserts its own UITK lobby through new `AutomationSubSystem` opcodes (`ui_wait`, `ui_text`,
`ui_submit`, `screenshot`, `wait_timeout`). Run `remote-20261030-162400-944` **passed**: 15 host
checks plus the client script, zero errors on either side, 37s. The lifecycle pilot
(`lifecycle-20261030-162625-193`, 28 checks) and visual pilot (`20261030-162749-980`, 122
checks) were rerun green afterwards.

Covered from the remote side: shell visible/creator hidden on join, Admin disabled for a
non-admin, `0/2` → `1/2 ready`, a non-admin start request ignored by the server, Ready via the
button, spawn on admin Start Round with the unready host left in the lobby, return on Stop Round,
Join Round late spawn in a second round, return and disconnect. Client screenshots were reviewed.

Fixes found by the runs:

- The booth's Human dummy ran gameplay `Awake`s before stripping; on the client
  `HumanoidController.Setup` threw because `CameraSubSystem` did not exist yet. It is now
  instantiated inactive and stripped immediately.
- Disabled lobby buttons had no disabled style; a non-admin's Admin looked clickable.
- `AttachedContainer` threw on the server for every despawned body (`SyncList.Clear` passes
  default items).
- Unrelated but found while building: an async station load outliving Play Mode wrote
  `Chunk [x,z]` objects into the open `Game.unity` (saved once during this session, then
  reverted). `TileMap.CreateChunk` now cancels when its map is destroyed.

Not covered: OS mouse/keyboard input, a second machine, and the client after `ScriptComplete`
(a windowed player may stall in `Environment.Exit`; the pilot kills it). The remote client's first
post-spawn capture showed the camera over open space; not investigated. Header map/mode is
still the mock "Cerestation".

## Jobs and antagonist preferences (Phase D) — 2026-10-30

The Jobs tab now shows `RoleSubSystem`'s replicated `JobSlotInfo` list instead of mock departments:
role name, department (`IdAccessSubSystem.DepartmentForRole`), slots (0 = unlimited, shown as OPEN),
assigned and **interested** (online players ranking the job above Never, shown per row as live
demand, `lobby.md` §3). Today that is the two authored roles in `StartingRoles.asset`: Security (1)
and Assistant (unlimited). Departments without roles do not appear; Civilian uses Heroicons `UserGroup`.

- **Sync:** every chip/opt-in change sends a `JobPreferencesMessage` broadcast with the full list;
  `LobbyUiSubSystem` also resends once per connection after the local player record replicates. The
  server resolves the player from the sending connection, ignores unknown jobs/categories, and keeps
  preferences per ckey, so they survive reconnects within a server session (not persisted to disk).
- **Defaults:** every job Never and no antagonist opt-ins — an empty list is the explicit
  "no preference" and resolves to the fallback.
- **Resolution (`JobResolver`, pure + EditMode-tested):** `EntitySubSystem.SpawnReadyPlayers` calls
  `RoleSubSystem.ResolveRoundStartRoles` before spawning. All players' High jobs are tried before any
  Medium, then Low, in a fresh random order per tier with a random pick among a player's tied open
  jobs. Leftovers get `_fallbackRole` (Assistant), which ignores its cap. Latejoin
  (`GiveRoleLoadoutToPlayer` with no assignment) takes the highest ranked open job, else the fallback.
  Assignments clear on `Stopped`. The old assign-on-connect (first joiner → Security) is removed, as is
  the client-callable `ServerRpc` on `GiveRoleLoadoutToPlayer` (now `[Server]`).
- **Antagonists:** `AntagonistCategories` (Nuclear Operative, Traitor) replaces the mock list.
  `RoleSubSystem.IsAntagonistOptedIn(ckey, category)` is the server API; no gamemode selects from it yet
  (MVP1 M0 assignment).

Divergences from `lobby.md`: H/M/L/N tiers instead of a drag-ranked list (already recorded); latejoin
uses the same preference chips filtered by open slots rather than a direct pick list; no playtime
locks exist, so full jobs (not locked ones) render dimmed.

Verified by `Testing/ui/validate.sh --full` (`validate-20261030-182754`): 2101 EditMode tests incl.
8 `JobResolverTests`, visual 122 checks, lifecycle 38 checks (Security H chip → server interest 1 and
row label → round start assigns Security and slot 1/1 taken → stop clears → late join takes Security),
client rebuild and remote pilot 17 checks.

## Networked character (Phase E2 slice) — 2026-10-30

The saved lobby character now reaches the spawned body. `lobby.md` §6 requires only a name;
appearance goes beyond that as the effort's recorded Character Creator scope.

- **Sync:** `LobbyUiSubSystem` sends a `CharacterAppearanceMessage` broadcast on Save and once
  per connection (same pattern as job preferences). It carries the last *saved* draft (never
  unsaved creator edits), with name, the six body values, hair/beard indices and resolved hair/beard
  colors. `EntitySubSystem` resolves the player from the sending connection, runs
  `CharacterAppearance.Sanitized()` (name: no control chars or `<`/`>`, max 32; values clamped;
  colors opaque) and keeps it per ckey for the server session (not persisted).
- **Spawn:** `SpawnPlayer` sets `Mind.CharacterName` and `Human.SetAppearance` (one SyncVar).
  A Save affects the next spawn only, never a living body.
- **Apply:** `Human` applies on the server and on every client (`OnStartClient` + SyncVar change).
  Body morphs use `HumanoidMorphApplier`, but height scales the **armature** (`HumanoidRigReferences.ArmatureRoot`), not the
  networked root, so the movement capsule and root stay unit scale. The server applies it too so its
  bone hit colliders match clients. `ClothesDisplayer` re-applies morphs after a worn-mesh swap.
  Hair/beard prefabs resolve through `IHumanoidStyleSource`, which `LobbyAssetCatalog` implements and
  registers at UI bootstrap. Headless servers have none and spawn no style meshes. Like the booth,
  applying a style hides the baked Hair/Facial Hair/Eyebrows meshes.
- **Name:** `Mind.DisplayName` (character name, else ckey) replaces the ckey in speech bubbles,
  radio/announcement senders, crew records/ID cards and the late-join announcement. A ghost keeps
  its mind and therefore its name. The comms log now records the character name, not the ckey.

Still deferred in E2: species models, skin/eye tint, eyebrow meshes/color, disk persistence.
Index-based styles assume server and clients run the same build/catalog.

Verified by `Testing/ui/validate.sh --full` (`validate-20261030-184621`): 2110 EditMode tests incl. 9
`CharacterAppearanceTests`, visual 122 checks, lifecycle 58 checks (creator name/height/fat/first hair →
Save → round-start spawn and late-join spawn each carry the mind name, armature height with unit root
scale, the Fat shape key and one hair mesh; `spawned-appearance.png` reviewed), client rebuild, remote
pilot 17 checks. The remote client was not asserted on another client's appearance.

## Legacy uGUI purge (Phase F) — 2026-10-30

The condemned uGUI lobby is gone, not just suppressed. Nothing in it was live: `LobbyUiSubSystem` had disabled
the `LobbyScreen` root on every refresh, and the UITK shell already owned ready, round, admin and player-list behavior.

- **Scene (`Game.unity`):** removed the `Lobby` (LobbyCanvas) and `Character Customizer` canvases, `Static World Objects`
  (old customizer plane/camera writing `LobbyCameraRenderTexture`) and the `GameScreensController` object under
  `Lobby Subsystems`.
- **Networked View prefab:** kept only `GamemodeObjectiveView` (`GamemodeObjectivesView`, which feeds the in-round
  `Gamemode Objective Canvas`). Its legacy Player Username List / Lobby Ready / Change Round State / Round State
  children are gone.
- **Assets deleted:** `LobbyCanvas`, `CharacterCustomizer`, `CharacterCustomizerCanvas`, `PlayerNameUI`, `Settings/`
  (Category/Dropdown/Slider/Toogle), `StaticWorldObjects`, `LobbyCameraRenderTexture`, `GameScreenSettings.asset`
  (also dropped from Preloaded Assets). FishNet `DefaultPrefabObjects` was regenerated (three entries removed), so
  client and server builds from before this change will not connect to builds after it.
- **Scripts deleted:** `Systems/Lobby/UI/*` (LobbyView, LobbyTabsView, LobbyReadyView, ChangeRoundStateView,
  RoundStateView, PlayerUsernameListView, PlayerUsernameView) and the `GameScreens` system that only that canvas
  used (`GameScreen`, `GameScreens`, `GameScreensController`, `GameScreenSettings`, `ScreenType`, `LobbyScreen`,
  `ChangeGameScreenEvent`). The `Other.ToggleMenu` input action now has no listener. It used to re-open the
  legacy lobby over a spawned body, which already had no visible effect.
- **Kept:** `Canvas Template`, `Black Overlay`, `Player Canvas`, `Gamemode Objective Canvas` and the
  intro/server-connection canvases are not lobby UI. The generic `SS3D.UI` widgets (`GenericTabView`, `LabelButton`,
  `GlobalUiVisibilityControllerView`) remain for other users.
- **Pilots:** the runtime suppression loop and the "legacy exists and is suppressed" checks are replaced by
  "no root `Lobby`/`Character Customizer` canvas in the scene".

Validation: `Testing/ui/validate.sh --full` (`validate-20261030-191427`) passed: compile clean, 2083 EditMode tests
(27 fewer than before, all from `PrefabTests` running 3 cases for each of the 9 deleted prefabs), visual 122 checks, lifecycle
58 checks, client rebuilt for the new prefab list, remote pilot 17 checks. The scene has no missing scripts and
`GamemodeObjectivesView` still references `Gamemode Objective Canvas`. The pilots don't cover reconnect-to-existing-body
(reclaim); it relies on the same `IsPlayerSpawned(connection)` poll that was already the only live path.

## Playtest fixes — 2026-10-30

Owner-reported issues after Phase F:

- **B in lobby chat opened the map editor.** `Global` always enables `TileCreator/ToggleMenu`; lobby and creator
  text fields (chat, player search, character name) now hold `InputTextEntryScope` while focused and release it
  on tab/step rebuild, hide and detach.
- **Escape did nothing in-round.** Phase F removed the only `Other/Toggle Menu` listener (the legacy
  `GameScreensController`, which toggled the lobby over a spawned body). `LobbyUiSubSystem` now listens: while the
  local player is spawned, Escape opens/closes the shell as an overlay and pushes the new `InputContext.Menu`
  (only `Other` live, so movement/camera/interactions are masked). The action button reads **IN ROUND**, disabled.
  Escape is ignored before spawn, while an interaction is armed, while the map editor is open and during keybind capture.
- **Hair colour didn't change.** `HumanHair.mat` (Simple Toon) had `_AmbientCol: 0`, which makes the lit term use
  the light colour instead of `_Color`, and the tint wrote `_BaseColor`, which the shader lacks. The material now
  uses `_AmbientCol: 1` like `HumanSkin.mat`, and `HumanoidStyleApplier` sets `_Color` (plus `_BaseColor`). This
  also fixes spawned bodies.
- **Character selector showed a placeholder.** `CharacterPreviewBooth.TryCapturePortrait` blits a head crop of the
  Front-angle RT into a 128px per-character portrait. `LobbyUiSubSystem` recaptures after style/body/outfit/angle
  changes, and the creator uses it for the header thumb and menu rows. Characters never shown at Front (the mock
  slots before first selection) keep the old thumbnail.
- **Assertions at boot.** The booth's stripped dummy kept `ContainerItemDisplay`, whose `Awake` asserts its
  (destroyed, networked) `AttachedContainer`. It is now stripped too.

Verified: `validate.sh` compile + EditMode + visual (122 checks) + lifecycle `lifecycle-20261030-201405-665`
(69 checks, new: B in focused chat leaves the map editor closed, leaving the tab releases text entry, Escape
opens the menu over the spawned body with movement masked, Escape closes it and restores movement). Synthetic
Input System key events, not OS input. Red/blue hair/beard tint and the portrait crop were checked from booth captures.


## 2026-10-30 Server Settings

Source: attached `SS3D Pre-round Lobby UI(2).zip`, Server Settings section of
`Lobby Shell - Player View.dc.html`. Replaced the static admin rows with a max-640px
form: inset Round State card, ready ratio, green Start, amber outline Force Start /
Extend Timer / Cancel Countdown, stacked map/mode selectors, paired numeric inputs,
and four rule checkboxes. Stop Round is an in-round extension of the pre-round reference.

The existing round subsystem owns validated, server-session configuration. Every admin
read/write/action checks the requesting connection's role on the server. Individual edits
merge one field into authoritative data, so stale UI snapshots cannot overwrite another edit. Configuration
snapshots target authorized admins only, and the active gamemode is no longer a SyncVar.
Late-join policy and station load state/name replicate publicly. Cap admission is checked at player authorization;
late spawn checks sender ownership and the server policy. Round length ends the round at
the configured elapsed duration; countdown time is reset when the round begins.

Map options are actual saved templates, not prototype fixture names. The lobby starts without
a restored station. Every Start restores the selected template asynchronously before readiness
and countdown; changing selection while stopped never gets stuck behind a prior loaded map.
Modes are Secret (the one available antagonist mode), Extended (no objectives), and Nuclear
Emergency (existing implementation). Unsupported Traitor/Deathmatch are omitted. Random
Events, Friendly Fire policy and Cloning are visible disabled controls with explanations.
Manual start replaces the prototype's unimplemented autostart threshold caption.

This is the attached prototype's direct-selection UI. The 2026-10 design review adopted it as
the first pass ([round-config.md](../../design/round-config.md) §2); weighted pools are its later
pass (§7). History UI, the map-fits-mode check (§5), autostart and disk persistence remain
deferred.

Harness: lifecycle pilot covers authorized snapshots, edits, validation, waiting/countdown/rules
screenshots, saved-map restore, extension, cancellation, Force Start, an Extended round with
server-enforced late-join denial, active-round edit rejection and automatic one-minute expiry; the remote scenario probes unauthorized
reads/writes/actions in addition to its real Ready/chat input. Numeric bounds have EditMode tests.

Verification: `validate-20261030-203641` passed compile, lifecycle (96 checks, zero captured
errors), rebuild and remote (19 host checks, zero captured errors). EditMode (2089 completed)
and visual (122 checks) passed in `validate-20261030-203255`; the subsequent runtime rerun
verified the per-field merge correction. Server Settings waiting/countdown/rules PNGs were
visually reviewed. See [test runbook](../../../Testing/ui/README.md#lobby-lifecycle-pilot).


## 2026-10-30 Deferred round-map loading

Owner follow-up: remove automatic station loading from session boot. `TileSubSystem.Setup`
now creates an empty map container only (needed by authoring/AOI); it does not read station
saves. Start Round waits for the tile resource loader, restores the selected saved template,
initializes world systems and then starts the warmup countdown. Every new start restores the
selection, including after cancellation or a previous round. The selector has no ambiguous
Current station option; it lists actual saves. Default preference is saved ministation, otherwise
the first save; no available map disables Start and a direct start request reports failure.

Replicated station state/name drives both the player sidebar and admin map status: No map
loaded, Loading map, Initializing station, Map ready or load failed. Round/load transitions bypass
the ordinary 200ms snapshot throttle. Start and Stop are mutually exclusive; active rounds show
only Stop, while countdown shows Force Start / Extend / Cancel. Preparation locks configuration.
A failed restore returns to Stopped for retry, and round operations cancel during server teardown.

Regression coverage asserts empty startup, loading/ready indicators, a temporary second saved
map selected after countdown cancellation and restored on the next Start, a fresh readiness
epoch, and exclusive actions in host and remote-client pilots. Temporary map fixtures are removed
when the pilot finishes. No scene or prefab wiring changed.


Deferred-loading verification: full `validate-20261030-205054` passed compile,
2089/2089 EditMode tests, visual (122 checks), lifecycle (107 checks), rebuild and remote
(21 checks). After the long-name status layout adjustment, `validate-20261030-205721`
passed compile, visual (122), lifecycle (108), rebuilt client and remote (21). Loading,
waiting, countdown and running screenshots were reviewed; the running form shows Stop
without Start, and the long map status no longer displaces the sidebar actions.


## Remaining-change review — 2026-10-30

Reviewed the uncommitted playtest fixes: lobby/creator focus scopes, the Escape menu input
context and lifecycle checks, hair tint via Simple Toon `_Color` / `_AmbientCol`, head-crop
selector portraits and stripping `ContainerItemDisplay` before preview activation. Portrait
capture now restores the previous `RenderTexture.active`; the lifecycle pilot checks both a
rendered selector texture and preservation of a sentinel active target.

Review also traced renderer asset churn to startup setup destroying a healthy Selection Pick
feature. Setup now reuses the feature, updates its recovery-ID map, and saves only the renderer
asset when a change is needed. Repeated setup is covered by an EditMode identity/map regression.
No lighting/render-quality policy was changed. Dynamic font atlas data, neutral URP default/build
serialization, regenerated prefab IDs and runtime null catalog entries are generated state;
they are excluded from the authored-work commit. Linux Addressables build/profile caches and
Python bytecode are ignored. Generated tracked diffs are backed up before cleanup.


Review verification: `validate-20261030-211254` passed preflight, compile, all 2090 EditMode
tests (including renderer setup identity/recovery mapping), 122 visual checks, 110 lifecycle
checks (including portrait target preservation and Escape/focus behavior), client rebuild and
21 remote checks. Selector portrait and in-round menu captures were visually reviewed.


## Local character presets — 2026-10-30

Save Character now writes all saved slots and the last saved slot ID to a versioned local
profile at `Application.persistentDataPath/Lobby/characters.json`. Startup restores it before
creating the lobby and booth. Body morphs are stored by key alongside identity, style/color
indices and preview outfit; unsaved edits and rendered portraits are not persisted. A temporary
file plus atomic replacement protects the existing save during writes. Unreadable or newer-schema
profiles fall back to defaults in memory and remain untouched on subsequent Save calls.

This is a client-owned preset library within the effort's expanded creator scope, separate
from server-owned player meta in [persistence-save.md](../../design/persistence-save.md) §5.
No account identity, job preferences, moderation data or in-round crew records are stored here.
Style indices retain the existing matching-catalog assumption.

EditMode tests cover fresh stores, multiple slots, selection/body/style round trips, unsaved
edits, replacement writes and corrupt/newer profile preservation. The lifecycle pilot reloads
the saved character from disk and checks name, height, fat and hair. Editor pilots route
profiles into their per-run artifact folders to avoid reading or overwriting personal presets.
Validation: `validate-20261030-213057` passed compilation, all 2096 EditMode tests and
122 visual checks. Its driver was interrupted during lifecycle; the independent rerun
`validate-20261030-213523` passed all 112 lifecycle checks, including the two disk-profile
assertions. Visual and lifecycle captures were reviewed. `validate-20261030-213728` rebuilt the client and passed all 21 remote checks; remote
captures were reviewed. Generated font/prefab/URP serialization was backed up and removed
from the authored diff after validation.


## Human skin, eyes and eyebrows — 2026-10-30

Body now provides eight skin tones alongside the existing eye-color palette. Style → Eyebrows
uses the five authored prefabs (Bold, Circle, Classic, Dainty, Default) plus None, with an
independent hair-palette color. These update the live booth and portraits, persist with the
saved slot, and travel in the sanitized `CharacterAppearance` SyncVar for spawning/latejoin.
Old eyebrow preset names map to current catalog IDs. Skin defaults to the authored HumanSkin
color for profiles saved before skin selection existed.

`HumanoidStyleApplier.ApplyFace` writes per-material property blocks only for HumanSkin and
HumanEye slots, preserving shared materials, clothing and cut surfaces. Eyebrows attach to
an animated head socket with the same imported-mesh correction as hair. The catalog builder
discovers existing eyebrow prefabs; no scene/mega-prefab edits or new art imports are needed.

Human is the only selectable species. Skrell and Vox remain visible disabled menu items with
an unavailable caption. Older nonhuman presets open as Human, and Save records Human. Unity 6
keeps DropdownField's menu factory internal, so pointer/submit interception opens a
GenericDropdownMenu with disabled entries while the field's selectable list contains only Human.

Validation covers per-character/per-material tint isolation, server color/style sanitization,
legacy preset mapping, local profile round trips, the disabled species menu, preview tints and
spawned/latejoin appearance.

Runtime verification found two bugs, both fixed:

- **Colors were truncated on the wire.** FishNet's default `Color` serializer writes each channel
  as `(byte)(value * 100)`, so a spawned body got e.g. skin (0.54, 0.34, 0.23) instead of the
  palette's (0.549, 0.341, 0.235). `CharacterAppearance` colors are now `Color32` (sent
  byte-exact); the palettes are byte-defined anyway. This also affected hair/beard colors.
- **Eyebrow/hair tint was wiped on spawned bodies.** `Selectable` cleared any non-empty
  renderer-wide property block on mesh renderers to remove stale selection colors, which also
  removed style tints. It now clears only blocks that carry the selection color. Booth dummies
  strip `Selectable`, so the preview never showed this.

The pilot's species and swatch steps also needed fixes. A navigation event sent with no target
goes to the focused element, and each swatch click rebuilds the step, so the next element is not
laid out yet. Validation `validate-20261030-225941` passed: 2112 EditMode tests, 122 visual,
227 lifecycle and 22 remote checks.


## Lobby chat — 2026-10-31

The Chat tab now uses real networked OOC/LOOC/Admin channels instead of mock lines. These are
the non-diegetic channels from [comms.md](../../design/comms.md) §7. The UI is in the lobby
shell, which is also the in-round Escape menu. There is no HUD feed for OOC yet.

- **Transport:** FishNet broadcasts in a `CommsSubSystem` partial (`CommsSubSystem.Ooc.cs`), so
  no new NetworkObject or scene edit is needed. The client sends `OocChatSendMessage`. The server
  takes the sender's ckey from the connection, normalizes the text (control characters, whitespace,
  256-character cap), rate-limits each sender to 5 messages per 5 s and stamps the role and UTC time.
- **Channels:** OOC goes to every authenticated client. Admin is sent to and readable by
  Administrators only; the tab is hidden for everyone else. LOOC needs a spawned body and reaches
  spawned players within 10 m. Without a body the sender gets a notice instead. Round start/end
  are posted as OOC system lines.
- **History:** the server keeps the last 200 OOC and Admin lines for this server session (not
  saved to disk). LOOC is not kept. Each line has a per-session id. Once its player record exists,
  each connection requests the lines after its last id. The client log orders and dedupes by id,
  so a replay that arrives after live lines still works.
- **Rendering:** labels have rich text disabled, so player text cannot inject markup. The log
  rebuilds only when its version changes and follows new lines only while scrolled to the
  bottom. Transcript lines go to `Logs/Comms.txt` on the server.

Verification: EditMode tests for normalization, rate limiting and history ordering. Lifecycle
pilot: a host line round-trips with rich text shown literally, an Administrator sees the Admin
channel, and unspawned LOOC is refused. Remote pilot: a non-admin client types an OOC line with
real OS input, and the host receives it attributed to `remoteprobe`.

Deferred: OOC/LOOC in the in-round comms feed (§7), mute/ban moderation, persistent history.


## Server Info metadata — 2026-10-31

The Server Info tab and header no longer use mock data. The owner chose these sources:

- **Server name and MOTD are admin-editable.** They are fields in Server Settings (Server Info
  section): the name commits on Enter or focus loss, the MOTD on focus loss. Unlike map/mode they
  can be edited in any round state. The server sanitizes them (single-line name, at most 64
  characters; MOTD at most 1000 characters, keeping paragraph breaks) and stores them in
  `ServerMeta/server-info.json` through `LocalStorage`. They load in `RoundSubSystem.OnStartServer`.
  A whole-form settings save cannot roll them back.
- **The public mode is the configured mode.** `PublicMode` replicates the admin's pick. Secret
  stays secret because it only resolves at round start.
- **Build and uptime are replicated** from the server process: `BuildInfo.Label` (app version
  plus commit) and the server start time. The client formats the uptime.
- **The change log comes from git.** `BuildInfoGenerator` (editor) writes the last 6 commit
  hashes, dates and subjects into a gitignored `Resources/BuildInfo.json`. It regenerates on
  editor load when HEAD changes and before every player build. The shell shows 4 entries, with
  subjects shortened to 40 characters and the full text in a tooltip, so the pane stays
  scroll-free. Without git, as in a container build, the file is kept or absent, and the label
  falls back to the app version with "No change log in this build". The change log describes the
  client's own build; the build label shows the server's.

Name, MOTD and change log labels have rich text disabled. Unused mock server/player data was
removed from `LobbyMockData`; only the placeholder character name and the player-row type remain.

Verification: `ServerInfoTests` covers sanitization and build-info parsing. In the lifecycle
pilot, an admin edits the name and MOTD during an active round. The pilot checks the saved file,
the replicated mode/build/start time, and the Server Info tab: live name, literal tags in the
MOTD, server build, and no mock text. It then restores the original values, because the file is
real server data. `validate-20261031-081305` passed the full loop (EditMode, visual, lifecycle,
build, remote); the change log row limit was then rechecked in lifecycle `validate-20261031-081828`.

Deferred: per-server banner art, ping/account-age player fields, and settings application.

