> Code paths: Assets/Scripts/SS3D/UI/MachineInterface/, Assets/Content/Systems/UI/MachineInterface/
> Entry points: MachineInterfaceSubSystem, MachineInterfaceHost, MachineInterfaceRegistry, MachineUiAssetCatalog
> Status: shipped
> Verified: add2ad2c9 — 2026-07-18

# Machine interface UI

## Overview

UI Toolkit panels for station machines, networked via FishNet snapshots. Templates and styles load from a committed `MachineUiAssetCatalog` (`Resources.Load`) rebuilt from `MachineUiAssetPaths` — not Game.unity SerializeFields. APC and SMES use the diegetic `DiegeticDeviceShell` with full-screen engineering ID access gates; atmospheric devices use an inline ID reader row. Both variants share server-side credential checks and expose idle, scanning, granted, and denied UI states. Vending uses the diegetic shell without an access gate. Shared view-model/binder pattern with `MachineUiCatalog` registration and `IMachineOptimisticControlHandler` for client optimistic controls. Open is server-only via validated interaction (`ServerHandleOpenRequest`); there is no open ServerRpc. Air alarm panels discover real area vents/scrubbers, apply preset modes server-side, and read the turf cell in front of the wall mount. Scrubber panels persist per-gas filter toggles into `ScrubberController` simulation state. Vent target pressure is enforced in `VentController`. Pump panels control `AtmosPumpController` directly — pumps are not area-linked. Diegetic open dims the overlay scrim and softens the 3D world via `ScreenEffectsSubSystem.SetUiBackdropBlur` (UITK chassis stays sharp), with a DOTween bring-up/dismiss (opacity, scale, translate). Close awaits the dismiss tween before disabling the `UIDocument`.

## Start here

- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceSubSystem.cs` — open/close/refresh; `InterfaceOpened` / `InterfaceClosed`; awaits host close anim
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceHost.cs` — panel host; catalog; diegetic backdrop + DOTween open/close
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineUiAssetPaths.cs` — path constants for templates/styles
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineUiAssetCatalog.cs` — ScriptableObject catalog type
- `Assets/Content/Systems/UI/MachineInterface/Resources/MachineUiAssetCatalog.asset` — committed runtime catalog
- `Assets/Scripts/SS3D/Editor/MachineUiAssetCatalogBuilder.cs` — `SS3D → Machine Interface → Rebuild Asset Catalog`
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineUiCatalog.cs` — registers UI templates/binders into `MachineInterfaceRegistry`
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceBehaviour.cs` — networked open/refresh/close base
- `Assets/Scripts/SS3D/UI/MachineInterface/AccessGatedMachineInterfaceBehaviour.cs` — server ID scan; syncs access states
- `Assets/Scripts/SS3D/UI/MachineInterface/OpenMachineInterfaceInteraction.cs` — open panel (server-validated only)
- `Assets/Scripts/SS3D/UI/MachineInterface/Components/DiegeticDeviceShell.cs` — diegetic chassis shell
- `Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tokens.uss` — diegetic design tokens

## Extension points

**New diegetic/modal machine checklist:**
1. Add id in `MachineInterfaceIds`; control ids in `MachineInterfaceControlIds` if needed.
2. Snapshot + FishNet serializer + view model + mapper + binder + UXML/USS.
3. Prefab controller subclassing `MachineInterfaceBehaviour` (concrete TargetRpc snapshot types — FishNet does not support generic RPC parameters). Prefer Editor setup tools over hand-editing machine prefab YAML ([agent-first composition](../2026-07_agent-first-composition.md)).
4. Add paths to `MachineUiAssetPaths` + entry in `MachineUiCatalog.RegisterAll`; run **SS3D → Machine Interface → Rebuild Asset Catalog** and commit the catalog asset (no Game.unity template wiring).
5. Register snapshot in `MachineInterfaceNetworkRegistry`.
6. Add `IMachineOptimisticControlHandler` for client optimistic Apply* (register in `MachineOptimisticControlRegistry.EnsureRegistered`).
7. Optional: dev harness scenario / editor preview.

**Engineering ID gate (two UI variants):** subclass `AccessGatedMachineInterfaceBehaviour`; wire `ReadId` action in binder. Full-screen: `AccessGatedRegion` + `AccessGatePanel` + `AccessStrip` (APC/SMES). Inline: `AtmosIdReaderRow` in a `panel-section` (pump/vent/scrubber/air alarm). Snapshots must include `AccessGranted`, `AccessScanning`, and `AccessDenied`.

**UI Toolkit masking:** never combine `border-radius` and `overflow: hidden` on the same `VisualElement` (renders as a flat white block). Split painted and clipping layers — see `DiegeticDeviceShell` (`_screen` vs `_screenContent`) and `PanelSection` (`panel-section` vs `panel-section__clip`).

Dev harness: `MachineInterfaceDevHarness.cs`; editor previews via `SS3D → Machine Interface` menu.

## Pitfalls

- **Missing catalog in builds:** host loads `Resources/MachineUiAssetCatalog`. If the asset was never rebuilt/committed, Play Mode and builds fail at awake with an explicit error — not a silent null at panel open (that was the old `EnsureEditorAssets` trap). Main HUD now has the same trap/fix class (`MainHudAssetCatalog`); do not grow a third copy — see [ui-shell](ui-shell.md) § Future work.
- **New UXML without rebuild:** adding paths in C# without running **Rebuild Asset Catalog** leaves the committed SO stale; Editor Play Mode uses the SO, not `AssetDatabase` path strings.
- **Editor asmdef:** `MachineUiAssetCatalogBuilder` needs `SS3D.Core` referenced from `SS3D.Editor` (so `MachineInterfaceHost` / `View` resolve for scene cleanup). Without it, `FindObjectsByType<MachineInterfaceHost>` fails to compile.
- **No UITK backdrop-filter:** USS cannot blur the 3D world behind a panel. Diegetic focus uses a dark overlay scrim plus Dual Kawase fullscreen blur (`UiBackdropBlurRendererFeature` via [screen-effects](screen-effects.md) `SetUiBackdropBlur`); the Screen Space Overlay chassis stays sharp on top. URP Gaussian DoF is too weak for this — do not reintroduce DoF for UI focus.
- **Close must await dismiss tween:** disabling `UIDocument` mid-DOTween kills the tree. `MachineInterfaceHost.Close(onComplete)` teardowns only after the sequence; SubSystem keeps input blocked / `IsOpen` until then. Starting the close tween must not clear `_pendingCloseComplete` — that skipped `FinishClose` and left `InputContext.MachineUI` stuck (no movement).
- **Do not hide Main HUD / storage from MI:** chrome visibility is owned by [inventory](inventory.md) `MainHudSubSystem` and `StoragePanelHost` observing `InterfaceOpened` / `InterfaceClosed` (asmdef is MainHud/StoragePanel → MI; reverse would cycle).
- **Open interface from across the room on air alarm:** prefab lacked a collider; unresolved interaction point made `RangeCheck` pass everywhere — see [interactions-framework](interactions-framework.md). AirAlarm now has a BoxCollider.

## Depends on / Used by

- **Depends on:** [electricity](electricity.md), [area](area.md), [atmospherics](atmospherics.md), [id-access](id-access.md), [interactions-framework](interactions-framework.md), [selection](selection.md), [inventory](inventory.md), [screen-effects](screen-effects.md) (diegetic backdrop blur)
- **Used by:** `ApcController`, `SmesController`, `VendingMachineController`, `AirAlarmInterfaceController`, `ScrubberInterfaceController`, `VentInterfaceController`, `PumpInterfaceController`; Main HUD observes open/close for suppress

## Related docs

- Architecture efforts: [phase 1](../2026-07_machine-interface-phase1-foundation.md), [phase 2](../2026-07_machine-interface-phase2-apc-networking.md), [phase 3](../2026-07_machine-interface-phase3-smes-generalization.md), [diegetic screen UI](../2026-07_diegetic-screen-ui-framework.md), [mi-area-electricity debt](../2026-07_mi-area-electricity-debt.md), [area foundation](../2026-07_area-foundation.md), [agent-first composition](../2026-07_agent-first-composition.md), [mi path catalog](../2026-07_mi-path-catalog.md)
- Target shell: [ui-shell](ui-shell.md)
- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md)
- Design (read-only): [Documents/design/id-access.md](../../design/id-access.md), [Documents/design/main-hud.md](../../design/main-hud.md)
