> Code paths: Assets/Scripts/SS3D/UI/Shell/
> Entry points: UiShellSubSystem, UiLayer, IUiSurface, UiShellAssetCatalog
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# UI shell

## Overview

Composition root for player-facing UI Toolkit surfaces. `UiShellSubSystem` self-bootstraps one shared `UIDocument`, builds one child `VisualElement` per `UiLayer` (`Hud`, `Overlay`, `Diegetic`, `Modal`, `Debug`) and applies the global token stylesheets once at the root. Surfaces attach into a layer instead of owning a private document. Policy: [agent-first composition](../2026-07_agent-first-composition.md). Effort: [ui-shell-consolidation](../2026-07_ui-shell-consolidation.md), whose non-goals list the later migration phases.

**On the shell today:**

| Surface | Layer | Owner map |
|---|---|---|
| Radial interaction menu, armed overlay | Overlay | [interactions-runtime](interactions-runtime.md) |
| Examine overlay (hover/detail + character paperdoll) | Overlay | [examine](examine.md) |
| Local-speech chips/compose (`LocalSpeechBubbleController`) | Overlay | [chat-audio-screens](chat-audio-screens.md) |
| Comms feed (`CommsFeedController`) | Hud | [chat-audio-screens](chat-audio-screens.md) |
| Lobby shell + Character Creator (`LobbyUiSubSystem`) | Modal | [rounds-lobby](rounds-lobby.md) |
| Bug report dialog + toast (`BugReportSubSystem` / `BugReportView`) | Modal | [rounds-lobby](rounds-lobby.md) (report records) |
| Round-end summary + banner (`RoundEndUiSubSystem` / `RoundEndOverlay`) | Overlay | [rounds-lobby](rounds-lobby.md) |
| Main menu (`MainMenuSubSystem` / `MainMenuView`) | Modal | [main menu](ui-shell-main-menu.md) |

**Not migrated yet.** Each still has its own `UIDocument` and path catalog, all rebuilt by **SS3D → Data → Rebuild All UI Catalogs**:

| Surface | Paths + SO | Owner map |
|---|---|---|
| Machine UI | `MachineUiAssetPaths` / `MachineUiAssetCatalog` | [machine-interface](machine-interface.md) |
| Main HUD | `MainHudAssetPaths` / `MainHudAssetCatalog` | [inventory](inventory.md) |
| Storage Panel | `StoragePanelAssetPaths` / `StoragePanelAssetCatalog` | [inventory](inventory.md) |

The F1 developer window (`UI/MainHud/Dev/DebugMenuView.cs`) also owns a private DDOL `UIDocument`; it uses Main HUD PanelSettings and has not migrated to `UiLayer.Debug`.

The shell's own catalog (`UiShellAssetPaths` / `UiShellAssetCatalog`), the lobby's (`LobbyAssetPaths` / `LobbyAssetCatalog`) and the round-end stylesheet catalog (`RoundEndAssetPaths` / `RoundEndAssetCatalog`) use the same rebuild. `ExamineOverlayAssetCatalog` is a lighter one-off: no `PanelSettings` or document of its own, not a `UiAssetCatalogBase` derivative.

**Bug report dialog** ([sandbox-server.md](../../design/sandbox-server.md) §7): `Systems/BugReports/BugReportSubSystem` self-bootstraps on clients (not batch mode) and attaches `BugReportView`, built from `Content/Systems/UI/BugReport/BugReport.uxml` / `.uss` via `BugReportAssetCatalog` (umbrella rebuild). The warmer v2 colours are scoped to this window, not the shared tokens. F8, a bare `/bug` and the menu's **Report a bug (F8)** open it; the context, round, position and screenshot are frozen first (one frame plus end-of-frame, so the closed Escape menu isn't in the shot). A client sends `BugReportSubmitMessage` and keeps the dialog open until `BugReportResultMessage` arrives (rate-limit refusals keep the text). An own host (or an offline player) saves a zip to `Data/Exports/` (`BugReportBundle`) and gets the saved screen with **Open Report Folder** / **Open GitHub Issue** (`BugReportIssueLink` pre-fills `Bug.yml` or `MapBug.yml`). **SS3D → UI → Preview Bug Report** drives the same view with sample data for each design state.

**Main menu:** [main menu](ui-shell-main-menu.md) replaces the old Launcher UI. `MainMenuSubSystem` attaches it whenever the Launcher scene loads and detaches it when Game loads.

## Start here

- [Main menu](ui-shell-main-menu.md) — controller, view, server status query, update button
- `Assets/Scripts/SS3D/UI/Shell/UiShellSubSystem.cs` — `TryGetLayer(UiLayer, out VisualElement)` (preferred) / `GetLayer`
- `Assets/Scripts/SS3D/UI/Shell/UiLayer.cs`, `IUiSurface.cs` — layer enum; surface contract implemented by the radial, armed, examine and lobby views
- `Assets/Scripts/SS3D/UI/Shell/Catalog/UiAssetCatalogBase.cs`, `UiCatalogRuntimeLoader.cs` — shared catalog base
- `Assets/Scripts/SS3D/UI/Shell/Animation/PanelAnimator.cs` — shared panel animation
- `Assets/Scripts/SS3D/UI/Shell/Binding/UiBinderBase.cs` — scaffolded binder base with no consumers yet
- Tokens: `Assets/Content/Systems/UI/Tokens/`, `Assets/Content/Systems/UI/MachineInterface/Tokens/`
- `Assets/Scripts/SS3D/Systems/BugReports/BugReportSubSystem.cs` (host: entry points, capture, send/save), `BugReportView.cs` (presentation), `BugReportModel.cs` (frozen context, metadata rows), `BugReportBundle.cs` (local zip, issue link)
- `Assets/Scripts/SS3D/Editor/BugReportPreviewWindow.cs` — editor preview of `BugReportView` with sample data
- `Assets/Scripts/SS3D/UI/Shell/BugReport/BugReportAssetPaths.cs` — bug report asset paths and Resources name

## Extension points

- New UI is UITK only. Resolve the shell with `SubSystems.Get<UiShellSubSystem>()`, attach via `TryGetLayer`, and implement `IUiSurface`. The effort doc's agent checklist covers the steps.
- A surface that needs its own asset catalog derives from `UiAssetCatalogBase` and registers with `UiCatalogBuilderKit` and the umbrella rebuild menu ([editor-tooling-tiers](../2026-07_editor-tooling-tiers.md)), not a per-surface MenuItem.
- `UiShellSubSystem` creates the document itself, so Boot and Game scenes need no `UIDocument` host.
- A screen-space host with its own `PanelSettings` calls `PlayerSettingsSubSystem.RegisterUiPanel` or UI Scale never reaches it; `LobbyUiSubSystem` registers the shell's shared panel.
- Debug tools: register a named tool through `Core/Debugging/DebugTools.Get(DebugToolNames.X)`, subscribe/unsubscribe `Invoked`, and set `IsOn` for toggles. The F1 window lists available tools in Editor/development builds; do not add per-tool keys. Remaining host work: [TECH_DEBT §1.13](../TECH_DEBT.md#113-debug-tools-centralized-ui-host-consolidation-remains).

## Pitfalls

- **UI Toolkit font assets are static atlases.** A dynamic atlas rewrites its asset with every new glyph drawn, which churned git after each play session. `SS3D → Build → Bake Static UI Fonts` (`UiFontBaker`) bakes every `FontAsset` under `Assets/Content/Systems/UI` with `UiFontFallbacks.Characters` and records its source font; at runtime `UiFontFallbacks` adds an unsaved dynamic fallback for anything else (chat in other scripts). After adding a UI font asset or a character your UI uses, run the bake and commit the result.
- USS does not support `:first-child` / `:last-child`; sibling spacing requires explicit modifier classes. Unsupported selectors are ignored and leave unwanted edge margins.
- **`Application` inside `SS3D.*` namespaces resolves to the `SS3D.Application` namespace**, not `UnityEngine.Application`, in assemblies that reference it (`SS3D.Systems`, `SS3D.Editor`). Write `UnityEngine.Application.…` there.
- **A modal surface's scrim must take picks.** The bug report scrim was `picking-mode="Ignore"` in the fixture; on the shared document that lets clicks fall through to the lobby shell underneath.

- **Per-frame FishNet lookups on the main menu:** `InstanceFinder` logs and rescans scenes on every miss when no `NetworkManager` exists yet. Surfaces that poll each frame (bug report, lobby) check `NetworkManager.Instances.Count > 0` first.
- **White text-input rectangles on Linux:** rounded input borders can render the clipped contents solid white even when their resolved background colours are correct. Keep bug-report inputs square (`border-radius: 0`); see the [lobby rendering pitfall](rounds-lobby.md#pitfalls). Check runtime screenshots, including typed text.

## Tests

- Bug report: `BugReportTests` (EditMode: rate limit, text, bundle, issue link, log tail); the sandbox pilot opens the dialog in the host, checks badge/round/map option, saves a bundle and checks its entries (screenshots `sandbox-bug-report-dialog`, `sandbox-bug-report-saved`). The graphical remote pilot types and sends a report through the real client/server broadcasts, checks the server-attributed JSONL and reduced JPEG, then reopens the dialog to check cooldown and draft entry. **SS3D → UI → Preview Bug Report** covers each design state; its actions are inert.

## Depends on / Used by

- **Depends on:** [inputs](inputs.md) (`InputInterface` document registration), `PlayerSettingsSubSystem` (UI Scale; [2026-10_player-settings](../2026-10_player-settings.md))
- **Used by:** every surface in the tables above; [machine-interface](machine-interface.md) and the Main HUD once migrated

## Related docs

- [2026-07_ui-shell-consolidation.md](../2026-07_ui-shell-consolidation.md)
- [2026-07_agent-first-composition.md](../2026-07_agent-first-composition.md)
- [mi-path-catalog](../../archive/architecture/2026-07_mi-path-catalog.md) (archived) — the first path-catalog wedge
- Design (read-only): [main-hud.md](../../design/main-hud.md), [lobby.md](../../design/lobby.md)
