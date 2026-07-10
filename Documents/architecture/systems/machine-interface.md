> Code paths: Assets/Scripts/SS3D/UI/MachineInterface/, Assets/Content/Systems/UI/MachineInterface/
> Entry points: MachineInterfaceSubSystem, MachineInterfaceHost, MachineInterfaceRegistry
> Status: shipped

# Machine interface UI

## Overview

Diegetic UI Toolkit panels for station machines (APC, SMES), networked via FishNet snapshots. Shared component library, view-model/binder pattern, and registry-driven machine type registration. `MachineInterfaceHost` disables `UIDocument` when closed to avoid interfering with the [selection](selection.md) pick pass.

## Start here

- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceSubSystem.cs` — open/close/refresh subsystem
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceHost.cs` — per-machine host; toggles UIDocument visibility
- `Assets/Scripts/SS3D/UI/MachineInterface/MachineInterfaceRegistry.cs` — machine type → binder registration
- `Assets/Scripts/SS3D/UI/MachineInterface/IMachineInterfaceBinder.cs` — binds snapshot to UI Toolkit tree
- `Assets/Scripts/SS3D/UI/MachineInterface/NetworkSnapshotHandler.cs` — FishNet snapshot sync
- `Assets/Scripts/SS3D/UI/MachineInterface/ApcInterfaceSnapshot.cs` — APC snapshot fields (includes `MultipleApcsInArea` overlap flag)
- `Assets/Scripts/SS3D/UI/MachineInterface/Bindings/ApcPowerControllerBinder.cs` — APC panel binder
- `Assets/Scripts/SS3D/UI/MachineInterface/Bindings/SmesUnitBinder.cs` — SMES panel binder
- `Assets/Scripts/SS3D/UI/MachineInterface/OpenMachineInterfaceInteraction.cs` — interaction to open panel

## Extension points

- New machine type: add view-model, binder, UXML/USS under `Assets/Content/Systems/UI/MachineInterface/`, register in `MachineInterfaceRegistry`.
- Implement `IMachineInterfaceProvider` on the world object; wire `MachineInterfaceBehaviour`.
- Dev harness: `MachineInterfaceDevHarness.cs`; editor preview via `SS3D → Machine Interface` menu.

## Depends on / Used by

- **Depends on:** [electricity](electricity.md), [area](area.md), [interactions-framework](interactions-framework.md), [selection](selection.md)
- **Used by:** APC and SMES controllers (`ApcController`, `SmesController`)

## Related docs

- Architecture efforts: [phase 1](../2026-07_machine-interface-phase1-foundation.md), [phase 2](../2026-07_machine-interface-phase2-apc-networking.md), [phase 3](../2026-07_machine-interface-phase3-smes-generalization.md), [area foundation](../2026-07_area-foundation.md)
- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md)
- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md), [Documents/design/comms.md](../../design/comms.md)
