---
name: Agent validation loop
overview: "Make develop → compile → test → build → multiplayer/visual validation runnable by an agent end to end, with no silent stalls and no manual checks by the owner."
todos:
  - id: guard-dirty-scene
    content: "Preflight shared by builds and pilots: refuse on dirty scenes, name leaked root objects (e.g. Chunk [x,z])"
    status: completed
  - id: dialog-watchdog
    content: "Testing/ui/watch-dialogs.sh: report and capture modal Unity windows (wmctrl + import)"
    status: completed
  - id: compile-check
    content: "Testing/ui/compile.sh: force refresh, wait for the compile, return all compiler + analyzer errors, nonzero on failure"
    status: completed
  - id: pilot-postconditions
    content: "Every pilot ends by asserting Game.unity is clean and no root Chunk [ objects exist"
    status: completed
  - id: validate-all
    content: "Testing/ui/validate.sh: compile → EditMode tests → visual → lifecycle → client build if stale → remote; one summary"
    status: completed
  - id: build-staleness
    content: "Stamp client builds with a source hash; remote pilot rebuilds or refuses when stale"
    status: completed
  - id: real-input
    content: "Real OS input smoke via xdotool on the windowed client (needs owner to install xdotool)"
    status: completed
  - id: parallel-builds
    content: "Optional: batchmode builds from a separate worktree so the interactive editor stays usable"
    status: cancelled
  - id: docs
    content: "Testing/ui/README.md + AGENTS.md pointer + update-system-docs"
    status: completed
  - id: playmode-step
    content: "PlayMode step in validate --full with Testing/ui/playmode-known-issues.txt expected-failures list"
    status: completed
  - id: server-build-step
    content: "Dedicated server build (stale-checked, source-hash stamped) alongside the client in the build step"
    status: completed
  - id: smoke-step
    content: "Multiplayer smoke (run_smoketest.sh CI set) as the last validate --full step"
    status: completed
isProject: false
---

# Agent validation loop

Goal: an agent can change code, then compile, test, build and validate the lobby/session and
UI work without the owner checking anything by hand. "Done" for a change is one command that
either passes or says exactly what failed, plus screenshots to review.

## Context (2026-10-30 session)

Already in place: the Unity MCP bridge, **Run Lobby UI Pilot** (visual, 122 checks),
**Run Lobby Lifecycle Pilot** (host session), **Run Lobby Remote Client Pilot** (editor Host + built
non-admin client running `Testing/multiplayer/scenarios/lobby-remote-client.txt`), `AutomationSubSystem`
UI opcodes, and `Testing/ui/README.md`. Reports land in `artifacts/ui/`.

These problems came up during that session (see the
[lobby effort](../architecture/systems/rounds-lobby.md)):

1. A player build waited ~10 min on Unity's "Scene(s) Have Been Modified" dialog. Over MCP the editor
   just looked idle. The owner clicked Save, which wrote leaked tile chunks into `Game.unity`
   (reverted).
2. `refresh_unity` returns before compilation finishes. A console filter on `error CS` missed
   the Coimbra analyzer error `COIMBRA0008`, so compile success was checked by DLL timestamp and
   by searching for UTF-16 strings in the DLL.
3. No single command ran everything. Each pilot was a menu item plus a hand-written wait loop,
   and EditMode tests were not run.
4. The client build must be rebuilt after any runtime/USS change. Nothing detects a stale build.
5. Pilots did not check side effects such as a dirty or polluted `Game.unity`.
6. A build blocks the editor's main thread: 13 min cold (URP shader variants), ~25 s warm.
7. Every UI check sends synthetic events. Real mouse/keyboard input is untested.

Unity runs under XWayland (`DISPLAY=:0`), so X11 tools work on its windows, but only with the
Bash sandbox disabled (same as `open-unity.sh`).

## Work order

Do 1–5 first. They are small, stay inside `Testing/` and `Assets/Editor/Automation/`, and close
the ways an agent gets stuck or misses failures. Then 6, 7 when the owner approves the install,
and 8 only if build frequency justifies it.

### 1. Dirty-scene preflight (`guard-dirty-scene`)

- Add an editor helper (e.g. `Assets/Editor/Automation/AutomationPreflight.cs`, in the
  `SS3D.Editor.Automation` asmdef) with `Check()`. It returns the dirty open scenes and any root
  objects matching known runtime leaks (`Chunk [`, `CharacterPreviewDummy`,
  `CharacterPreviewStaging`).
- Call it from a new non-interactive build entry (e.g. `AutomationBuild.BuildClient()`) before
  `ClientBuildScript.BuildClient`, and from the start of each pilot's `Run`. On failure, throw
  with the list. Do not save or discard on the agent's behalf; the owner decides.
- The build entry writes `artifacts/ui/client-build-result.json`
  (`status`, `seconds`, `error`, `sourceHash`) so callers can wait on a file instead of on a
  blocked MCP call.
- Accept: with a dirty `Game.unity`, a build or pilot fails within seconds and names the scene and
  the leaked objects.

### 2. Modal dialog watchdog (`dialog-watchdog`)

- `Testing/ui/watch-dialogs.sh`: find the editor PID (`pgrep -f "Unity -projectPath $REPO"`), list
  its windows with `wmctrl -l -p`, and ignore the main window and known progress windows
  ("Compiling shader variants", "Building Player", "Hold on"). For anything else, print the title and
  save a capture to `artifacts/ui/dialogs/<UTC>-<title>.png` via `import -window`. Exit nonzero if a
  dialog is present.
- `validate.sh` (item 5) runs it whenever a step exceeds its expected duration.
- Accept: with a "Scene(s) Have Been Modified" dialog open, the script reports it and saves a PNG
  within 5 s.

### 3. Compile check (`compile-check`)

- `Testing/ui/compile.sh`: record the `editor.log` line count, request a script refresh/compile
  (via `artifacts/unity-mcp/venv` MCP `refresh_unity`, as `lobby-pilot.py` does), then poll
  `mcpforunity://editor/state` until `is_compiling` is false *and* a domain reload or compile-finished
  timestamp is newer than the request.
- Report every `error ` line from `editor.log` after the marker, not only `error CS`
  (analyzers: `COIMBRA*`, `RCS*` errors, `SS3D*`). Also report
  `EditorUtility.scriptCompilationFailed` through a tiny `execute_code` call.
- Accept: introducing `Object.Destroy(gameObject)` (COIMBRA0008) makes the script exit nonzero
  with that file:line, and a clean tree exits 0 with "no errors".

### 4. Pilot postconditions (`pilot-postconditions`)

- In `LobbyUiPilot`, `LobbyLifecyclePilot` (both modes): after Play exits, and before the final
  report is written, run the preflight check from item 1. Record a dirty scene or leaked root
  objects as a failure.
- Accept: a leaked `Chunk [` in the edit scene after Play fails the pilot report.

### 5. One validation command (`validate-all`)

- `Testing/ui/validate.sh [--quick|--full]`, driven through the MCP venv like `lobby-pilot.py`.
  Steps:
  1. preflight (item 1) → 2. compile (item 3) → 3. EditMode tests (MCP `run_tests`, mode EditMode;
  fail on any failure) → 4. visual pilot → 5. lifecycle pilot → 6. client build if stale
  (item 6; `--quick` skips 6–7) → 7. remote client pilot.
- Each step has a timeout. On timeout, run the dialog watchdog and print the last 40 relevant
  `editor.log` lines.
- Output: one summary table (step, status, seconds, report path), the PNGs to review, exit
  nonzero on any failure. Write the summary to `artifacts/ui/validate-<UTC>/summary.md`.
- Accept: on a clean tree `--full` passes unattended; breaking a lobby button label makes it fail
  at the right pilot with the report path.

### 6. Build staleness (`build-staleness`)

- The build entry (item 1) computes a hash of the inputs, writes it to
  `Builds/Game/ss3d-build-hash.txt`, and stores it in the result JSON.
- Hash inputs: `git ls-files -s` plus working-tree content of `Assets/Scripts`, `Assets/Content/**/*.uss|uxml`,
  `Packages/manifest.json` and `ProjectSettings`. A cheap option: `git stash create`-style tree hash
  or hashing `git diff HEAD` plus `HEAD`.
- `RunRemoteClient` compares hashes: rebuild automatically when invoked from `validate.sh`,
  otherwise refuse with "client build is stale".
- Accept: editing a USS file makes the next remote pilot rebuild (or refuse) instead of testing the
  old client.

### 7. Real input smoke (`real-input`) — needs owner

- The owner installs `xdotool` (`sudo dnf install xdotool`; the agent may run it with `sudo -A` once
  the owner approves the dialog).
- Add an opcode such as `os_click <selector>`: the client resolves the element's `worldBound` to
  screen pixels, emits a `TestSignal` with the coordinates, and the host-side pilot calls
  `xdotool mousemove --window <id> x y click 1`. Also add `os_key <key>`.
- Scope: one Ready click and one keyboard shortcut in the remote pilot, to confirm hit testing and
  focus. The pilots otherwise stay synthetic.
- Accept: the remote pilot readies the client through a real click, and a covering overlay that
  intercepts the pointer would fail it.

### 8. Builds without blocking the editor (`parallel-builds`) — optional

- Build from a `git worktree` (separate `Library/`) with a batchmode Unity
  (`-batchmode -nographics -executeMethod SS3D.Editor.ClientBuildScript.BuildClient -customBuildPath …`).
  The first build of a fresh `Library` is slow; keep the worktree around.
- Only worth it if builds per session become frequent. Item 6 already avoids most unneeded builds.

### 9. Docs (`docs`)

- Document `compile.sh`, `validate.sh`, `watch-dialogs.sh` and the preflight in `Testing/ui/README.md`.
- In `AGENTS.md`, add one line under "After implementing a feature": run
  `Testing/ui/validate.sh` for UI/session work.
- Run `update-system-docs`; mark todos here.

## Out of scope

- Golden-image screenshot diffs (backgrounds and preview animation are live); keep human or agent
  review of PNGs.
- CI integration of the graphical pilots (headless CI cannot render the lobby); the smoke harness
  and EditMode remain the CI layer.
- Lobby feature work (Phase D jobs, chat, settings, E2, Phase F); it resumes on top of this loop.

## Owner decisions needed

- ~~Approve installing `xdotool` (item 7).~~ Approved and installed 2026-10-30.
- Whether `Assets/AddressableAssetsData/` and `Assets/Content/Addressables/Linux/` (created by
  client builds) should be committed or ignored.

## Implementation notes (2026-10-30)

Shipped items 1–7 and 9; item 8 cancelled (warm builds take ~30 s and the source hash skips
unneeded ones). Runbook: [Testing/ui/README.md](../../Testing/ui/README.md#validation-loop-agents-start-here).
Where the result differs from the plan:

- **Location.** The automation lives in `Assets/Scripts/SS3D/Editor/Automation/` (moved from
  `Assets/Editor/Automation/`): the EditMode suite's `FirstPartyAsmdefsLiveUnderScripts` failed on
  the old path. `AutomationBuild` sits in the Lifecycle asmdef, which references `SS3D.Editor`.
- **Compile check.** The completion marker is `artifacts/ui/compile-result.json`, written by a
  `CompilationPipeline` hook (`AutomationCompileReport`) with every error message; `editor.log`
  `error` lines after the request are merged in. `CompilerMessage` includes analyzer errors.
- **Readiness.** The MCP `editor/state` `ready_for_tools` flag stayed false (`stale_status`) on an idle,
  healthy editor, so `validate.py` asks the editor directly through `execute_code`.
- **delayCall.** In an idle, unfocused editor the `EditorApplication.delayCall` queue (Unity's own
  entries included) went unprocessed while `update` ticked. The build entry and the postcondition
  wait use one-shot `update` callbacks.
- **Postconditions** run 3 s after `EnteredEditMode`; reports pass through status `verifying`.
  Preflight refusals and stale-build refusals also write a failed `status.json`, so drivers don't
  wait out their timeout.
- **Build hash** is `Testing/ui/source_hash.py` (git blob ids of `Assets/`, `Packages/`,
  `ProjectSettings/` incl. working tree, minus `Editor/` folders and build-written Addressables
  output), shared by the C# build/pilot and the driver. Content-based, so reverting an edit
  makes an old build current. The stamp is taken after the build, because the build rewrites
  `UniversalRenderPipelineGlobalSettings.asset`.
- **EditMode** leaves scenes open/dirty until the Test Runner restores the setup after the job
  reports done; the step waits up to 60 s for the restore, then applies the preflight.
- **Real input.** `os_click`/`os_type`/`os_key` emit signals; the remote pilot performs them with
  xdotool. Scope: the Ready click plus clicking into chat, typing, and Return. There is no global
  lobby keyboard shortcut, so Return in the chat composer is the keyboard check. It found a real
  bug: the composer ignored a real Return (bubble-phase handler; fixed with `TrickleDown`).
- **Dialog watchdog** treats a bare `Unity` window title as a progress bar (Unity uses it for
  "Compiling Scripts").

Acceptance runs (2026-10-30): dirty `Game.unity` + `Chunk [3,4]` refused build/visual/lifecycle in
0–3 s naming both; "Scene(s) Have Been Modified" reported and captured in 0.07 s; COIMBRA0008 probe
failed `compile.sh` with file:line, clean tree exit 0; injected post-Play `Chunk [9,9]` failed a
122-check visual run (`20261030-170542-393`); `JOBS`→`Jobz` failed `--quick` at visual
(`20261030-172520-120`); a USS edit made the direct remote pilot refuse as stale
(`remote-20261030-172032-012`); a transparent overlay failed the remote pilot at Ready
(`remote-20261030-172640-741`).

Final: `validate.sh --full` run `validate-20261030-173633` passed unattended on the restored tree
(2093 EditMode, 122 visual, 28 lifecycle, build current, remote 17 checks).

## Implementation notes — 2026-10-30 (uinput)

Real OS input moved from xdotool to `Testing/ui/uinput_input.py serve` (stdlib-only virtual uinput
pointer+keyboard, one device per remote run). On GNOME 49 Wayland, xdotool's XTEST went through
Xwayland's libei RemoteDesktop portal and asked the owner for permission on every remote run. xdotool
remains only for the client window lookup. `/dev/uinput` access comes from the session's uaccess ACL
(Steam's udev rule on the owner's machine), so no new sudo rule was needed. Verified by
`validate-20261030-193923` (remote 17 checks, chat line typed, no portal dialog in the journal).

## Implementation notes — 2026-10-31 (PlayMode, server build, smoke)

`validate --full` is now compile → EditMode → PlayMode → visual → lifecycle → client+server build →
remote → multiplayer smoke. PlayMode tests could not start at all since Phase 3h; the bootstrap,
session-restart and dedicated-server-settle fixes that made them run, plus server-side round helpers
in `TestHelpers` (the uGUI lobby buttons they pressed were purged in Phase F), shipped with it. The
newly running suite exposed two real bugs, both fixed: a stop during Preparing/WarmingUp was dropped,
and a late joiner on a server that had already run a round never counted as spawned
(`ContainerViewedCondition` hit a destroyed object). The passing known-issue repros moved to
`PlayMode/RegressionTests/`; `playmode-known-issues.txt` lists nothing. The smoke step found an
intermittent late-join `Selectable.OnStart` NRE (`SelectionSubSystem` not yet spawned) — left open.
Runbook: [Testing/ui/README.md](../../Testing/ui/README.md#validation-loop-agents-start-here).
