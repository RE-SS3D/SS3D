> Code paths: Assets/Scripts/SS3D/UI/MachineInterface/, Assets/Content/Systems/UI/MachineInterface/
> Entry points: MachineInterfaceSubSystem, MachineInterfaceHost, MachineInterfaceRegistry
> Status: shipped

# Machine interface UI

## Overview

UI Toolkit panels for station machines, networked via FishNet snapshots. APC and SMES use the diegetic `DiegeticDeviceShell` with full-screen engineering ID access gates; atmospheric devices use an inline ID reader row. Both variants share server-side credential checks and expose idle, scanning, granted, and denied UI states. Vending uses the diegetic shell without an access gate. Shared view-model/binder pattern and registry-driven registration. Air alarm panels discover real area vents/scrubbers, apply preset modes server-side, and read the turf cell in front of the wall mount. Scrubber panels persist per-gas filter toggles into `ScrubberController` simulation state. Vent target pressure is enforced in `VentController`. Pump panels control `AtmosPumpController` directly — pumps are not area-linked.

## Start here

- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceSubSystem.cs` — open/close/refresh; bool, numeric, and action controls
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceHost.cs` — panel host; `MachineInterfaceShellKind` routing
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceRegistry.cs` — interface id → UI registration
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceBehaviour.cs` — networked open/refresh/close base
- `Assets/Scripts/SS3D/UI/MachineInterface/IMachineInterfaceBinder.cs` — binds snapshot to UI Toolkit tree
- `Assets/Scripts/SS3D/UI/MachineInterface/NetworkSnapshotHandler.cs` — FishNet snapshot sync
- `Assets/Scripts/SS3D/UI/MachineInterface/ApcInterfaceSnapshot.cs` — APC snapshot fields (includes `MultipleApcsInArea` overlap flag)
- `Assets/Scripts/SS3D/UI/MachineInterface/ApcStatusDeriver.cs` — APC nominal/overload/critical derivation from circuit stats
- `Assets/Scripts/SS3D/UI/MachineInterface/SmesController.cs` — SMES panel; input/output enable and rate limits
- `Assets/Scripts/SS3D/UI/MachineInterface/Bindings/ApcPowerControllerBinder.cs` — APC panel binder (diegetic shell, access gate)
- `Assets/Scripts/SS3D/UI/MachineInterface/Bindings/SmesUnitBinder.cs` — SMES panel binder (diegetic shell, access gate)
- `Assets/Scripts/SS3D/UI/MachineInterface/AccessGatedMachineInterfaceBehaviour.cs` — server ID scan; syncs `AccessGranted` / `AccessScanning` / `AccessDenied`
- `Assets/Scripts/SS3D/UI/MachineInterface/Components/AccessGatePanel.cs` — full-screen APC/SMES gate (idle, scanning, denied)
- `Assets/Scripts/SS3D/UI/MachineInterface/Components/AtmosIdReaderRow.cs` — inline atmos gate with lock glyph and danger button on deny
- `Assets/Scripts/SS3D/UI/MachineInterface/Components/AccessGatedRegion.cs` — gate + controls subtree; server-driven via `ApplyAccessState`
- `Assets/Scripts/SS3D/UI/MachineInterface/Components/GlanceableStatusChip.cs` — compact status headline + badge row
- `Assets/Scripts/SS3D/UI/MachineInterface/Components/ToggleSwitch.cs` — slider toggle for channel and rate controls
- `Assets/Scripts/SS3D/UI/MachineInterface/Bindings/VendingMachineBinder.cs` — vending panel binder
- `Assets/Scripts/SS3D/UI/MachineInterface/VendingMachineController.cs` — vending networked controller
- `Assets/Scripts/SS3D/UI/MachineInterface/AirAlarmInterfaceController.cs` — air alarm UI; live device discovery and preset dispatch
- `Assets/Scripts/SS3D/UI/MachineInterface/ScrubberInterfaceController.cs` — scrubber UI; filter state in snapshots
- `Assets/Scripts/SS3D/UI/MachineInterface/VentInterfaceController.cs` — vent UI; target pressure stored on `VentController`
- `Assets/Scripts/SS3D/UI/MachineInterface/PumpInterfaceController.cs` — pump unit UI; power/target outlet pressure on `AtmosPumpController` (not area-linked)
- `Assets/Scripts/SS3D/UI/MachineInterface/Bindings/PumpInterfaceBinder.cs` — pump panel binder (inlet/outlet readouts, ID gate)
- `Assets/Scripts/SS3D/UI/MachineInterface/OpenMachineInterfaceInteraction.cs` — interaction to open panel
- `Assets/Scripts/SS3D/UI/MachineInterface/Components/DiegeticDeviceShell.cs` — diegetic chassis shell
- `Assets/Scripts/SS3D/Editor/DiegeticComponentsPreviewWindow.cs` — editor component preview
- `Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tokens.uss` — diegetic design tokens

## Extension points

**New modal machine (legacy `MachineWindow` still available for simple panels):** snapshot, serializer, mapper, view model, binder, UXML/USS; register in `MachineInterfaceHost.RegisterUiEntries` with `ShellKind = ModalWindow`; subclass `MachineInterfaceBehaviour`; register snapshot in `MachineInterfaceNetworkRegistry`.

**New diegetic machine (APC/SMES/vending/gas pump pattern):** same pipeline plus diegetic components under `Components/`; set `ShellKind = DiegeticDevice`; reference component USS in UXML `<ui:Style>`. Host mounts the full cloned `TemplateContainer` so styles stay attached.

**Engineering ID gate (two UI variants):** subclass `AccessGatedMachineInterfaceBehaviour`; wire `ReadId` action in binder. Full-screen: `AccessGatedRegion` + `AccessGatePanel` + `AccessStrip` (APC/SMES). Inline: `AtmosIdReaderRow` in a `panel-section` (pump/vent/scrubber/air alarm). Snapshots must include `AccessGranted`, `AccessScanning`, and `AccessDenied`.

**UI Toolkit masking:** never combine `border-radius` and `overflow: hidden` on the same `VisualElement` (renders as a flat white block). Split painted and clipping layers — see `DiegeticDeviceShell` (`_screen` vs `_screenContent`) and `PanelSection` (`panel-section` vs `panel-section__clip`).

**Action controls:** add IDs in `MachineInterfaceControlIds`, handle in binder (`ActionControlChanged`) and controller (`ApplyActionControl`).

Dev harness: `MachineInterfaceDevHarness.cs`; editor previews via `SS3D → Machine Interface` menu.

## Depends on / Used by

- **Depends on:** [electricity](electricity.md), [area](area.md), [atmospherics](atmospherics.md), [id-access](id-access.md), [interactions-framework](interactions-framework.md), [selection](selection.md), [inventory](inventory.md)
- **Used by:** `ApcController`, `SmesController`, `VendingMachineController`, `AirAlarmInterfaceController`, `ScrubberInterfaceController`, `VentInterfaceController`, `PumpInterfaceController`

## Related docs

- Architecture efforts: [phase 1](../2026-07_machine-interface-phase1-foundation.md), [phase 2](../2026-07_machine-interface-phase2-apc-networking.md), [phase 3](../2026-07_machine-interface-phase3-smes-generalization.md), [diegetic screen UI](../2026-07_diegetic-screen-ui-framework.md), [area foundation](../2026-07_area-foundation.md)
- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md)
- Design (read-only): [Documents/design/id-access.md](../../design/id-access.md), [Documents/design/main-hud.md](../../design/main-hud.md)
