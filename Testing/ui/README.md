# Unity MCP and lobby pilot

Pinned integration: CoplayDev Unity MCP 10.3.0, commit
`aa5fc638d623f56178d50329d9ad8c541a57fe66`, on Unity `6000.3.16f1`.
The Unity package is pinned in `Packages/manifest.json`; the Python environment is
local/ignored under `artifacts/unity-mcp/venv`. `requirements-mcp.txt` records the
tested Python 3.14 environment. No server or tooling is added to release builds by
the SS3D editor assembly.

## Run

From the repository root, start these long-lived processes separately:

```bash
Testing/ui/start-mcp.sh
Testing/ui/open-unity.sh
```

The first script installs the server if missing, then serves MCP on loopback
`http://127.0.0.1:8080/mcp` (override with `SS3D_MCP_URL`, e.g. `http://127.0.0.1:8095`, in the environment of all three: start-mcp.sh, open-unity.sh and the drivers). The second uses the exact project editor version and
connects the bridge. Set `SS3D_UNITY_EDITOR` if Unity is installed elsewhere. A
valid Unity license and graphical desktop are required. Do not launch a second
editor against an already-open checkout; use **SS3D → Testing → Connect Unity MCP**
in the existing editor instead. Script recompilation reconnects automatically
when launched with `open-unity.sh`.

Codex registration (already performed on Rutger's machine):

```bash
codex mcp add ss3d-unity --url http://127.0.0.1:8080/mcp
```

A newly registered server may need a new Codex session to appear as native tools.
The checked-in driver uses standard MCP immediately, including in an older chat:

```bash
artifacts/unity-mcp/venv/bin/python Testing/ui/lobby-pilot.py
```

It checks the selected editor's project path, invokes **SS3D → Testing → Run Lobby
UI Pilot**, waits for a completed report, and returns nonzero on failure/timeout.
Native MCP clients can call `execute_menu_item` with that same menu path. Inspect
`mcpforunity://editor/state` after `refresh_unity` before starting a run.

## Validation loop (agents: start here)

One command validates work in the open editor, unattended:

```bash
Testing/ui/validate.sh            # default: preflight, compile, EditMode
Testing/ui/validate.sh --steps compile,editmode,visual   # any subset, in order
Testing/ui/validate.sh --steps compile,editmode,scenarios:<category>   # PlayMode tests in one NUnit category
Testing/ui/validate.sh --steps compile,editmode,scenarios:Medical   # e.g. the medical scenarios; scenarios:Scenarios runs them all
Testing/ui/validate.sh --steps mppm:late-join   # multiplayer scenario in Multiplayer Play Mode (no build needed)
Testing/ui/validate.sh --steps mppm:reconnect
Testing/ui/validate.sh --steps mppm:breach   # breach wind on a remote owner: pushed body and streaks
Testing/ui/validate.sh --steps mppm:blast   # blast on a remote owner: shoved upright, then floored and thrown
Testing/ui/validate.sh --steps mppm:breach-budget   # atmos tick cost and traffic during a hull breach with loose items, logged as [atmos-budget]
Testing/ui/compile.sh             # = --steps compile
Testing/ui/validate.sh --nightly  # owner only: + PlayMode, client+server build if stale, multiplayer smoke
Testing/ui/validate.sh --weekly   # owner only: compile, visual, lifecycle, build, remote pilots
Testing/ui/validate.sh --steps compile,sandbox   # sandbox pilot on demand (not in --weekly)
Testing/ui/validate.sh --steps compile,round-reset # focused host atmos + decal reset regression
Testing/ui/validate.sh --steps compile,interaction-overlay # scaled CPR overlay visuals
```

`mppm:<scenario>` needs the `com.unity.multiplayer.playmode` package and MPPM enabled in the
editor (Window > Multiplayer Play Mode), with this project the only open SS3D editor. The step
starts from stopped clones and launches Player 2 itself (~30–60 s warm), then runs the scenario in
Play Mode and parks all players again. The clone's `PlayerState` can read `Launching` long after it
is up; the driver uses the clone process and its `Editor.log` instead. `/tmp` style details and the
scene-order pitfall are in
[networking-session-testing](../../Documents/architecture/systems/networking-session-testing.md#pitfalls).

Scenario categories (`scenarios:<category>`): `Medical`, `Atmos`, `Access`, `Environment`,
`Respawn`, `Speech`, `MapEditor`; `Scenarios` runs them all. They replaced four lifecycle pilot steps, which
`validate.sh` now refuses with the new name: `ragdoll-space` and `critical-reclaim` →
`scenarios:Environment`, `respawn-cleanup` → `scenarios:Respawn`, `critical-speech` →
`scenarios:Speech`.

Run it outside the agent's Bash sandbox (the dialog watchdog needs X11). It prints one `ok` line per
passed step and stops at the first failure, printing that step's details and report path. It lists
screenshots only when the `visual` step ran. Every run writes `artifacts/ui/validate-<UTC>/summary.md`
with the full table. Exit status is nonzero on any failure. Rough warm timings on tomnas: EditMode
~40 s, PlayMode ~6.5 min, visual ~70 s, lifecycle ~85 s, build ~30 s, remote ~50 s, smoke
~9 min; the sandbox pilot is ~4.5 min on demand. Agents run targeted steps, never `--nightly` or `--weekly` unless the owner
asks; see [2026-11_testing-philosophy.md](../../Documents/architecture/2026-11_testing-philosophy.md).

Both run unattended on TomNAS. The nightly (`multiplayer-smoke-test.yml`) runs PlayMode in a
batchmode editor (`Tools/run_playmode_tests.sh`, ~4–5 min) instead of through MCP, then builds and
runs the smoke set. The weekly (`weekly-graphical.yml`, Sunday) opens an editor on the runner's
checkout with `Tools/ci/editor_session.sh` (MCP on :8097) and runs `--weekly`; it refuses to start
while another editor is open, so `ss3d-dev stop` when you're done. A red run opens or updates a
`nightly-failure` / `weekly-failure` issue that the next green run closes.

| Step | What it checks |
|------|----------------|
| preflight | `AutomationPreflight.Check()`: no dirty open scene, no leaked runtime root objects (`Chunk [`, `CharacterPreviewDummy`, `CharacterPreviewStaging`). Never saves or discards. |
| compile | Refreshes, requests compilation, waits for `artifacts/ui/compile-result.json` (written by `AutomationCompileReport` from `CompilationPipeline`) newer than the request. Lists every compiler and analyzer error (`CS*`, `COIMBRA*`, `RCS*`) with file:line, plus `scriptCompilationFailed`. |
| editmode | All EditMode tests via MCP `run_tests`; `editmode.json` in the validate folder. Then waits for the Test Runner to restore the scene setup and fails if tests left a dirty/polluted scene. |
| playmode | `--nightly`. All PlayMode tests via MCP `run_tests`; failures are checked against `playmode-known-issues.txt` (see [PlayMode step](#playmode-step)). Same scene-restore check as editmode. |
| scenarios:&lt;category&gt; | The PlayMode tests in one NUnit category (MCP `run_tests` `category_names`), checked like `playmode`; `scenarios-<category>.json`. Fails if the category has no tests. |
| mppm:&lt;scenario&gt; | `late-join` or `reconnect` in Multiplayer Play Mode: MPPM tags the main editor with the host script and Player 2 with the client script, launches the clone, enters Play Mode, and waits for both players' `Temp/mppm-automation/*.json`. The host writes a `host-ready` marker once Game is loaded and the clone waits for it before connecting. Fails on anything but `ScriptComplete` or on any logged error (the smoke log gate). On demand only; the built `smoke` step stays the nightly gate. |
| visual / lifecycle | The two pilots below. Each refuses on a failed preflight (a failed `status.json` at once) and, after Play exits and 3 s of settling, re-runs the check as a postcondition: a leaked `Chunk [` fails the report (`postconditions` field). Reports pass through `verifying` before `passed`/`failed`. |
| build | Client and dedicated server, each skipped when its `Builds/{Game,GameServer}/ss3d-build-hash.txt` matches `python3 Testing/ui/source_hash.py`. Otherwise `AutomationBuild.BuildClient()` / `BuildServer()`: preflight, then the build on the next editor tick; the driver waits on `artifacts/ui/{client,server}-build-result.json` (`status`, `seconds`, `error`, `sourceHash`), since a build blocks the main thread and MCP. The server build restores the editor's standalone subtarget afterwards, so the editor never stays on UNITY_SERVER. Cold server build ~6.5 min on tomnas. |
| sandbox | On demand (`--steps sandbox`), not in `--weekly`: the reset loop is the nightly `sandbox-reset` smoke scenario, so the weekly only needs the graphical pilots. The pilot below (~4.5 min). |
| remote | The remote client pilot. Run directly with a stale build, it refuses with both hashes. |
| smoke | `--nightly`. `Testing/multiplayer/run_smoketest.sh` for each scenario in `Testing/multiplayer/smoke-set.txt` (late-join, reconnect, observer-sync, sandbox-reset, nukeops-round), the same set as `multiplayer-smoke-test.yml`. It runs a real headless dedicated server and clients from the current builds (refuses if either is stale). One log per scenario, `smoke-<scenario>.log` in the validate folder, which names the preserved `Testing/multiplayer/.runs/` directory. Runs every scenario, then fails if any failed. ~9 min on tomnas before `nukeops-round` (~3.5 min) was added. |

Each step has an expected duration and a timeout. Past the expected duration,
`Testing/ui/watch-dialogs.sh` runs every 30 s; a modal dialog fails the step at once. On
timeout the watchdog output and the last 40 `editor.log` lines are printed.

`watch-dialogs.sh` lists the editor's X11 windows (`wmctrl -l -p`), ignores the main window and
progress windows (`Compiling…`, `Building…`, `Hold on`, and the bare title `Unity`), and saves
any other window to `artifacts/ui/dialogs/<UTC>-<title>.png` (exit 1). Use it whenever the editor
sits at 0% CPU with no log progress.

The build hash covers `Assets/`, `Packages/` and `ProjectSettings/` by git blob id (working
tree included, staging/committing irrelevant), excluding `Editor/` folders and the Addressables
output a build writes. Reverting an edit makes an old build current again. A build rewrites
`UniversalRenderPipelineGlobalSettings.asset` (`m_RuntimeSettings`); the stamp is taken after
the build so this does not leave it stale.

Two editor behaviours the tooling works around:

- The MCP `editor/state` resource reports `ready_for_tools: false` with `stale_status` while an
  idle editor is fine. The driver asks the editor directly (`isCompiling`, `isUpdating`,
  Play Mode) through `execute_code`.
- An idle, unfocused editor can leave `EditorApplication.delayCall` unprocessed indefinitely while
  `EditorApplication.update` keeps ticking. Automation schedules one-shot `update` callbacks.

### PlayMode step

`playmode` (in `--nightly`) runs every PlayMode test through MCP `run_tests` (~6.5 min on
tomnas) and writes `validate-*/playmode.json`. Failures are checked against
[`playmode-known-issues.txt`](playmode-known-issues.txt), which lists expected failures:
- a listed test that fails is reported and does not fail the step;
- a listed test that passes is flagged, so its entry can be removed;
- any unlisted failure fails the step.

Known-issue fixtures (`Assets/Scripts/Tests/KnownIssueReproduction/`) are reported separately from
regular tests, in `playmode-known-issues.md` and in the summary. Keep the list's comments current: each
entry should say why it fails.

## Evidence and scope

Each visual run writes `artifacts/ui/<UTC timestamp>/status.json` and PNGs for the shell,
settings and Character Creator. Captures use a fixed 1920×1080 Game View preset. The selected
Game View size and Play Mode start scene are restored afterwards; Boot/Game assets are not edited.
The pilot checks that legacy lobby canvases are absent. Other startup/game UI can still appear.

The pilot opens the real lobby and creator, changes name and body sliders, rotates
the preview, navigates steps, saves, and returns. It checks visibility, control
presence, preview texture binding, angle label, saved name, updated dropdown/footer behavior, Saved confirmation,
local character switching and New Character reset. It fails on captured
Error/Exception/Assert logs. Fix the cause of a captured error; never allowlist one to
turn the report green. It does not claim that a bound RenderTexture contains
the correct model; inspect the PNGs too.

This is a **rendered smoke test**, not a visual golden-image test: animations and
the background are still live. Synthetic Click/NavigationSubmit events and field
changes exercise UI callbacks, but do not establish OS input, hit testing, focus
arbitration, multiplayer synchronization, or layout at other resolutions. Initial
startup messages before the EnteredPlayMode callback and teardown after the report
are outside its error collection window; consult `artifacts/ui/editor.log` too.

## Lobby lifecycle pilot

**SS3D → Testing → Run Lobby Lifecycle Pilot** boots a single editor Host and exercises
ready/unready, start/spawn/stop/return, latejoin, local disconnect/reconnect, character persistence
and appearance, player/server settings, map validation, and unsaved-map Stop/autosave choices.
Reports and screenshots live under `artifacts/ui/lifecycle-<UTC>/`; failures name the step and
session state. The pilot uses temporary map copies and restores edited server metadata.

Buttons use synthetic `NavigationSubmitEvent`; these checks do not prove OS input or a separate
client process. The [rounds/lobby map](../../Documents/architecture/systems/rounds-lobby.md) describes
current contracts and test coverage. Boot leaves the station unloaded; Start restores the selected
saved template. No current pass is implied by this runbook—use the report for the checkout tested.

## Sandbox pilot

**SS3D → Testing → Run Sandbox Pilot** applies the sandbox profile in Play Mode (the editor has no
`-sandbox` flag; `RoundSubSystem.ApplyServerSettingsForTesting` sets the same settings) and lets the
Host run rounds on its own: auto-start countdown without Ready, 1-minute warning, round end, station
wipe with no autosave, a second automatic round on a fresh station. It also checks the bug-report dialog and local bundle (report, screenshot, logs and current map with unsaved edits). Review `sandbox-bug-report-dialog.png` for readable text in both inputs. Reports and screenshots live
under `artifacts/ui/sandbox-<UTC>/`. Effort: [2026-11_sandbox-server](../../Documents/architecture/2026-11_sandbox-server.md).

## Lobby remote client pilot

**SS3D → Testing → Run Lobby Remote Client Pilot** needs a current client build at
`Builds/Game/SS3D.x86_64`. `validate.sh --steps build,remote` rebuilds it when stale; otherwise call
`SS3D.Editor.Automation.AutomationBuild.BuildClient()` through `execute_code` and wait on
`artifacts/ui/client-build-result.json` (not **SS3D → Build → Client (Linux)**, which opens a file
manager and has no preflight). The client is a separate player, not the editor's assemblies. The
first build compiles URP shader variants (~13 min), later ones ~30s; the editor is blocked meanwhile.

The pilot boots the editor Host, launches the client windowed as non-admin `remoteprobe` with
`-testscript=Testing/multiplayer/scenarios/lobby-remote-client.txt`, and drives Admin → Start/Stop
Round twice. The remote files a bug report with real keyboard input, waits for the server acknowledgement, and reopens the dialog during cooldown. The host checks the attributed JSONL and decodes the reduced JPEG. It writes `artifacts/ui/remote-<UTC>/status.json`, `client.log`, client
screenshots (`remote-0*.png` and `remote-bug-report-*.png`) and `host-after-remote.png`. A pass needs the client's
`ScriptComplete` signal and no errors in either log.

**Real OS input:** the client's Ready click, the Chat tab and chat-field clicks, the typed chat
line and Return, plus the bug-report menu button, fields, Send and Cancel, are real OS input. The client scenario's `os_click <selector>` resolves the element
to window pixels and emits an `OsClick` test signal; the pilot reads `client.log` and sends it to
`Testing/ui/uinput_input.py serve`, a virtual uinput pointer+keyboard that lives for the whole run
(`os_type`, `os_key` likewise). This moves the desktop pointer and types into the focused window
during the run; don't use the desktop meanwhile. A transparent overlay over the shell makes the
click miss and fails the pilot at the Ready step; synthetic events would have passed.

Requirements: write access to `/dev/uinput` for the session user (on Rutger's machine Steam's
`60-steam-input.rules` grants it via uaccess; otherwise add a udev rule such as
`KERNEL=="uinput", TAG+="uaccess"`), `xdotool` for the client window lookup only, and every monitor
at scale 1 (the absolute pointer spans the X root, which then equals GNOME's logical desktop).

Input constraint: on GNOME Wayland its XTEST events go through Xwayland's libei portal,
which shows an "allow remote interaction" dialog on every run. uinput sits below the compositor and
needs no dialog. Keep these constraints: udev tags the device as a keyboard only when keycodes 1–31 are all
declared (otherwise clicks work but typing silently does nothing), and keys from a second, freshly
created device did not reach the window the first device clicked, hence one device per run.

Windowed clients may stall during teardown after `ScriptComplete`; the pilot cleans up the
process and records that in the timeline. Completion still requires the signal and clean logs.

Before building, make sure `Game.unity` is not dirty: `BuildPlayer` opens a blocking
"Scene(s) Have Been Modified" dialog. If it is dirty after a playtest, look for root
`Chunk [x,z]` objects (leaked tile load, see the tile map) rather than saving blindly.

## Troubleshooting

- Missing license: check Unity Hub activation for the installed editor before running pilots.
- No instances: check the bridge and server logs; a healthy MCP server alone does
  not prove Unity connected. Keep it local to this machine.
- Compilation: automation has its own editor-only asmdef. `Assets` has a parent
  asmdef; merely placing scripts in an `Editor` folder does not isolate them.
- Blocking dialog after a catalog rebuild: the **SS3D → Data → Rebuild All UI Catalogs** menu
  opens a modal success dialog. Automation calls `UiCatalogRebuildAll.TryRebuildAll` from editor
  code instead of invoking the menu.
- Null UI root: a `UIDocument` may exist before its `rootVisualElement` does.
- UI events: set the pooled event's `target` before `SendEvent`.
- Game View sizing uses Unity's internal editor API, confined to one helper. If an
  editor upgrade changes it, repair that helper; don't silently capture another size.
- Validation step timed out: read the watchdog lines in the output. `busy: …` is a progress
  window; `DIALOG: …` names a modal and its PNG. Dismiss it deliberately (never Save `Game.unity`
  blindly; look for leaked `Chunk [x,z]` objects first).
- Shell width check on the first visual run: a slow first Play Mode can report a laid-out
  but not yet final shell. The pilot now waits up to 15 s, from the shell's first layout, for the
  reference geometry (panel = screen, 1180px window, 280px sidebar) before measuring. A real
  regression still fails, with the measured width in the message.
- No final report: the external driver times out after 180 seconds; the in-editor
  scenario times out after 120 seconds once Play Mode has started. Inspect the logs
  and exit Play Mode before retrying. A compiler-blocked editor cannot run the pilot.
