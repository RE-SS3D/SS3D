> Code paths: Assets/Scripts/SS3D/UI/MachineInterface/, Assets/Content/Systems/UI/MachineInterface/
> Entry points: MachineInterfaceSubSystem, MachineInterfaceHost, MachineInterfaceRegistry
> Status: shipped

# Machine interface UI

## Overview

UI Toolkit panels for station machines, networked via FishNet snapshots. APC and SMES use the diegetic `DiegeticDeviceShell` with full-screen engineering ID access gates; atmospheric devices use an inline ID reader row. Both variants share server-side credential checks and expose idle, scanning, granted, and denied UI states. Vending uses the diegetic shell without an access gate. Shared view-model/binder pattern with `MachineUiCatalog` registration and `IMachineOptimisticControlHandler` for client optimistic controls. Open is server-only via validated interaction (`ServerHandleOpenRequest`); there is no open ServerRpc. Air alarm panels discover real area vents/scrubbers, apply preset modes server-side, and read the turf cell in front of the wall mount. Scrubber panels persist per-gas filter toggles into `ScrubberController` simulation state. Vent target pressure is enforced in `VentController`. Pump panels control `AtmosPumpController` directly — pumps are not area-linked.

## Start here

- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceSubSystem.cs` — open/close/refresh; delegates optimistic Apply* to handlers
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceHost.cs` — panel host; `MachineInterfaceShellKind` routing; asset mount
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineUiCatalog.cs` — registers UI templates/binders into `MachineInterfaceRegistry`
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineOptimisticControlHandlers.cs` — per-machine optimistic control handlers
- `Assets/Scripts/SS3D/UI/MachineInterface/IMachineOptimisticControlHandler.cs` — optimistic control handler contract + registry
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceRegistry.cs` — interface id → UI registration
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceBehaviour.cs` — networked open/refresh/close base
- `Assets/Scripts/SS3D/UI/MachineInterface/IMachineInterfaceBinder.cs` — binds snapshot to UI Toolkit tree
- `Assets/Scripts/SS3D/UI/MachineInterface/NetworkSnapshotHandler.cs` — FishNet snapshot sync
- `Assets/Scripts/SS3D/UI/MachineInterface/ApcInterfaceSnapshot.cs` — APC snapshot fields (includes `MultipleApcsInArea` overlap flag)
- `Assets/Scripts/SS3D/Systems/Electricity/ApcStatusDeriver.cs` — APC nominal/overload/critical derivation from circuit stats
- `Assets/Scripts/SS3D/UI/MachineInterface/SmesController.cs` — SMES panel; input/output enable and rate limits
- `Assets/Scripts/SS3D/UI/MachineInterface/Bindings/ApcPowerControllerBinder.cs` — APC panel binder (diegetic shell, access gate)
- `Assets/Scripts/SS3D/UI/MachineInterface/Bindings/SmesUnitBinder.cs` — SMES panel binder (diegetic shell, access gate)
- `Assets/Scripts/SS3D/UI/MachineInterface/AccessGatedMachineInterfaceBehaviour.cs` — server ID scan; syncs `AccessGranted` / `AccessScanning` / `AccessDenied`
- `Assets/Scripts/SS3D/UI/MachineInterface/OpenMachineInterfaceInteraction.cs` — interaction to open panel (server-validated only)
- `Assets/Scripts/SS3D/UI/MachineInterface/Components/DiegeticDeviceShell.cs` — diegetic chassis shell
- `Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tokens.uss` — diegetic design tokens

## Extension points

**New diegetic/modal machine checklist:**
1. Add id in `MachineInterfaceIds`; control ids in `MachineInterfaceControlIds` if needed.
2. Snapshot + FishNet serializer + view model + mapper + binder + UXML/USS.
3. Prefab controller subclassing `MachineInterfaceBehaviour` (concrete TargetRpc snapshot types — FishNet does not support generic RPC parameters).
4. Assign templates on `MachineInterfaceHost`; add entry in `MachineUiCatalog.RegisterAll`.
5. Register snapshot in `MachineInterfaceNetworkRegistry`.
6. Add `IMachineOptimisticControlHandler` for client optimistic Apply* (register in `MachineOptimisticControlRegistry.EnsureRegistered`).
7. Optional: dev harness scenario / editor preview.

**Engineering ID gate (two UI variants):** subclass `AccessGatedMachineInterfaceBehaviour`; wire `ReadId` action in binder. Full-screen: `AccessGatedRegion` + `AccessGatePanel` + `AccessStrip` (APC/SMES). Inline: `AtmosIdReaderRow` in a `panel-section` (pump/vent/scrubber/air alarm). Snapshots must include `AccessGranted`, `AccessScanning`, and `AccessDenied`.

**UI Toolkit masking:** never combine `border-radius` and `overflow: hidden` on the same `VisualElement` (renders as a flat white block). Split painted and clipping layers — see `DiegeticDeviceShell` (`_screen` vs `_screenContent`) and `PanelSection` (`panel-section` vs `panel-section__clip`).

Dev harness: `MachineInterfaceDevHarness.cs`; editor previews via `SS3D → Machine Interface` menu.

## Depends on / Used by

- **Depends on:** [electricity](electricity.md), [area](area.md), [atmospherics](atmospherics.md), [id-access](id-access.md), [interactions-framework](interactions-framework.md), [selection](selection.md), [inventory](inventory.md)
- **Used by:** `ApcController`, `SmesController`, `VendingMachineController`, `AirAlarmInterfaceController`, `ScrubberInterfaceController`, `VentInterfaceController`, `PumpInterfaceController`

## Related docs

- Architecture efforts: [phase 1](../2026-07_machine-interface-phase1-foundation.md), [phase 2](../2026-07_machine-interface-phase2-apc-networking.md), [phase 3](../2026-07_machine-interface-phase3-smes-generalization.md), [diegetic screen UI](../2026-07_diegetic-screen-ui-framework.md), [mi-area-electricity debt](../2026-07_mi-area-electricity-debt.md), [area foundation](../2026-07_area-foundation.md)
- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md)
- Design (read-only): [Documents/design/id-access.md](../../design/id-access.md), [Documents/design/main-hud.md](../../design/main-hud.md)
