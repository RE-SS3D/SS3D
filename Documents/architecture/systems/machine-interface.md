> Code paths: Assets/Scripts/SS3D/UI/MachineInterface/, Assets/Content/Systems/UI/MachineInterface/
> Entry points: MachineInterfaceSubSystem, MachineInterfaceHost, MachineInterfaceRegistry
> Status: shipped

# Machine interface UI

## Overview

UI Toolkit panels for station machines, networked via FishNet snapshots. APC and SMES use modal `MachineWindow` shells; newer diegetic devices use `DiegeticDeviceShell` (chassis/bezel/screen). Shared view-model/binder pattern and registry-driven registration. `MachineInterfaceHost` disables `UIDocument` when closed to avoid interfering with the [selection](selection.md) pick pass.

## Start here

- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceSubSystem.cs` — open/close/refresh; bool, numeric, and action controls
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceHost.cs` — panel host; `MachineInterfaceShellKind` routing
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceRegistry.cs` — interface id → UI registration
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceBehaviour.cs` — networked open/refresh/close base
- `Assets/Scripts/SS3D/UI/MachineInterface/Components/DiegeticDeviceShell.cs` — diegetic chassis shell
- `Assets/Scripts/SS3D/UI/MachineInterface/Bindings/VendingMachineBinder.cs` — vending panel binder
- `Assets/Scripts/SS3D/UI/MachineInterface/VendingMachineController.cs` — vending networked controller
- `Assets/Scripts/SS3D/Editor/DiegeticComponentsPreviewWindow.cs` — editor component preview
- `Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tokens.uss` — diegetic design tokens

## Extension points

**New modal machine (APC/SMES pattern):** snapshot, serializer, mapper, view model, binder, UXML/USS; register in `MachineInterfaceHost.RegisterUiEntries` with `ShellKind = ModalWindow`; subclass `MachineInterfaceBehaviour`; register snapshot in `MachineInterfaceNetworkRegistry`.

**New diegetic device:** same pipeline plus diegetic components under `Components/`; set `ShellKind = DiegeticDevice`; reference component USS files in UXML `<ui:Style>` tags. Host mounts the full cloned `TemplateContainer` so styles stay attached — do not add only the inner shell to the overlay.

**Action controls:** add IDs in `MachineInterfaceControlIds`, handle in binder (`ActionControlChanged`) and controller (`ApplyActionControl`).

Dev harness: `MachineInterfaceDevHarness.cs`; editor previews via `SS3D → Machine Interface` menu.

## Depends on / Used by

- **Depends on:** [electricity](electricity.md), [interactions-framework](interactions-framework.md), [selection](selection.md), [inventory](inventory.md) (vending dispense)
- **Used by:** `ApcController`, `SmesController`, `VendingMachineController`

## Related docs

- Architecture efforts: [phase 1](../2026-07_machine-interface-phase1-foundation.md), [phase 2](../2026-07_machine-interface-phase2-apc-networking.md), [phase 3](../2026-07_machine-interface-phase3-smes-generalization.md), [diegetic screen UI](../2026-07_diegetic-screen-ui-framework.md)
- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md), [Documents/design/comms.md](../../design/comms.md)
