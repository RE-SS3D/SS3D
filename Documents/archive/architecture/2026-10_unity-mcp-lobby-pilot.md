> Implements: infrastructure — agent-operated Unity inspection and lobby verification
> Touches systems: rounds-lobby, ui-shell (inspection only)
> Status: shipped (tooling pilot; lobby baseline passes after preview fixes)

# Unity MCP and rendered lobby pilot

Unity MCP 10.3.0 is pinned by commit in the project manifest. The local HTTP server
and Codex registration provide editor control, compilation refresh, console access
and inspection. The first scenario is the existing lobby/character creator, driven
through the standard MCP `execute_menu_item` tool.

## Entry points

- [Testing/ui/README.md](../../../Testing/ui/README.md) — setup, commands, evidence, limitations and observed baseline defects.
- `Testing/ui/start-mcp.sh` / `open-unity.sh` — local server and exact-version editor launch.
- `Testing/ui/lobby-pilot.py` — project identity check, MCP invocation, bounded result wait, failure exit status.
- `Assets/Scripts/SS3D/Editor/Automation/UnityMcpBootstrap.cs` — explicit bridge connection, editor-only.
- `Assets/Scripts/SS3D/Editor/Automation/LobbyUiPilot.cs` — Play Mode scenario, fixed Game View resolution, screenshots, checks and error report.
- `Assets/Scripts/SS3D/Editor/Automation/Lifecycle/LobbyLifecyclePilot.cs` — single-editor Host session lifecycle (ready → round → spawn → return, late join, reconnect) and the remote client pilot; own asmdef because it references SS3D runtime assemblies.
- `Assets/Scripts/SS3D/Editor/Automation/AutomationPreflight.cs`, `AutomationCompileReport.cs`, `Lifecycle/AutomationBuild.cs` — dirty-scene/leak guard, compile result file, non-interactive stamped client build.
- `Testing/ui/validate.sh` / `validate.py`, `compile.sh`, `watch-dialogs.sh`, `source_hash.py` — the [agent validation loop](../../plans/agent_validation_loop.plan.md).

The automation moved from `Assets/Editor/Automation/` on 2026-10-30: `AssetTaxonomyTests.FirstPartyAsmdefsLiveUnderScripts` requires first-party asmdefs under `Assets/Scripts/`.

## Validation

On 2026-10-27, Unity 6000.3.16f1 on Linux imported the pinned package and compiled the
automation. MCP connected, refreshed scripts across domain reloads, and ran the full
scenario. The fixed-resolution baseline produced six 1920×1080 PNGs and passed 18 UI
assertions. Its overall result remains **failed** because preview construction logs
three component-dependency errors; see the runbook. Images were inspected separately
and reveal preview orientation and background/overlapping UI concerns.

On 2026-10-30, run `20261030-130709-268` passed all 18 UI checks with zero captured
errors and six 1920×1080 screenshots. `CharacterPreviewBooth.StripNetworking` now
removes required-component dependents before the generic network pass, including
the non-networked body bridge. Base yaw is 0° for the +Z booth camera; Front and Side
captures were visually inspected and Front now shows the face. Background/overlap
and raw style-label concerns remain outside this fix. A `TileMap`
`MissingReferenceException` in `TileSubSystem.WaitForResourcesLoad` was observed
after the report during teardown, outside the scenario error collection window.

The updated Guided Steps creator passed run `20261030-133826-904` with **40 checks**,
zero captured errors and six 1920×1080 captures. New coverage exercises the
Species dropdown, removal of Uniform Color, one pinned Save/Saved footer,
character switching, fresh-draft reset and saved name/body restoration. Targeted
visual review confirms the wider preview, local reference colors, readable style
labels and red slider fills. The agreed model-supported body controls and remaining
appearance limits are recorded in the [lobby effort](../../architecture/2026-08_lobby-uitk-redesign.md#2026-10-30-character-creator-refresh).

The palette-move follow-up (`SS3D Pre-round Lobby UI(2).zip`) passed run
`20261030-134939-028` with **60 checks**, zero captured errors and six PNGs.
Checks now cover hair color absent from Body, the three context-labelled Style
palettes below the tiles, independent selections and per-character color restoration.
The Style capture was visually inspected. Hair/beard tints are independent; eyebrow
color is retained as a preference while eyebrow rendering remains deferred.

The Player View fidelity pass passed run `20261030-140336-222` with **84 checks**,
zero captured errors and **14** 1920×1080 PNGs. Eight new captures cover Server Info,
Jobs, Chat, Players and the three Settings subtabs (including muted Sound).
Assertions exercise local Display/Sound state, mute disabling/readouts, next-key
capture and value retention before the existing creator scenario. Screenshot review
caught white rounded image/input output and collapsed toggle text despite passing
checks; square image/input corners and a growing toggle input fixed them. Settings
remain local UI state; see the [effort scope](../../architecture/2026-08_lobby-uitk-redesign.md#2026-10-30-player-view-shell-refresh).

The owner-requested scroll-free/live-startup follow-up passed run
`20261030-142251-600` with **89 checks**, zero captured errors and 14 PNGs.
Startup asserts a live preview RT, active booth lights, no outer Server Info
ScrollView, information bounds and spacing above Ready. Images confirm the full
information card and a lit character before creator interaction. The first attempt
exposed the orphan-light guard disabling the early booth; see the
[lobby follow-up](../../architecture/2026-08_lobby-uitk-redesign.md#lobby-layoutstartup-follow-up--2026-10-30).

Default preview outfits passed run `20261030-144343-787` with **100 checks**, zero
captured errors and 14 PNGs. New checks cover active jumpsuit/left-shoe/right-shoe
renderers, outfit mesh stability during morph edits, and restoration after switching
characters. Lobby, creator and side captures show the selected clothing correctly.
Outfits are chosen once per new draft from catalog-backed existing clothing sources;
this does not grant gameplay equipment. See the [outfit implementation](../../architecture/2026-08_lobby-uitk-redesign.md#default-preview-outfits--2026-10-30).

## Boundaries and follow-up

- No Boot/Game or Human prefab changes, no new gameplay UI, no design changes.
- Uses the actual boot flow and asserts the legacy uGUI lobby canvas is absent (purged in Phase F);
  this is not an isolated UI scene. The lifecycle pilot covers a single-editor Host session only,
  not a separate client process.
- Synthetic UI events cover callback wiring, not physical input/hit testing. The remote pilot adds real OS input (uinput virtual device) for Ready and chat only.
- Current captures are review evidence; animation timing is not deterministic and
  pixel baseline comparison is not enabled.
- Follow-up: additional resolutions, deterministic captures, then rendered-client CI coverage.
- No playable milestone slice is completed by this tooling work.

## Related docs

- [rounds-lobby](../../architecture/systems/rounds-lobby.md)
- [ui-shell](../../architecture/systems/ui-shell.md)
- [Lobby redesign](../../architecture/2026-08_lobby-uitk-redesign.md)
- [Existing multiplayer harness](../../architecture/2026-07_multiplayer-test-harness.md)

## Enlarged preview verification — 2026-10-30

Run `20261030-150133-789` passed **102 checks**, with zero captured errors and 14 screenshots. A new rendered-output check downsamples the preview RT and measures foreground rows against its solid background: the character must fill at least 72% of the height without touching the top or bottom edge. This run measured 84%. Lobby, creator Identity and Body side-view captures were visually reviewed.
