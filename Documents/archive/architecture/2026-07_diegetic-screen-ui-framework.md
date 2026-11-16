> Implements: Documents/design/main-hud.md §machine control surfaces
> Touches systems: machine-interface UI, furniture vending prefabs, FishNet snapshots
> Status: shipped

# Diegetic Screen UI Framework

## Goal

Introduce a reusable diegetic device shell and component library for in-world machine UIs, without replacing the existing APC/SMES modal panels. Ship vending machines as the first networked diegetic consumer.

## Scope

- Diegetic chassis/bezel shell (`DiegeticDeviceShell`) and shared components (status rows, panel sections, action log, steel buttons, inventory slots).
- `MachineInterfaceShellKind` to register modal vs diegetic layouts in `MachineInterfaceHost`.
- Action controls (`SetActionControl`) for discrete UI actions beyond bool/numeric toggles.
- Vending machine snapshot pipeline, binder, UXML template, and `VendingMachineController` with tray-based dispense flow.
- Dev harness scenario and editor preview window for diegetic components.

## Delivery order

1. Diegetic tokens, tones, and `[UxmlElement]` component set under `Assets/Content/Systems/UI/MachineInterface/`.
2. Host/subsystem/behaviour generalization: shell kind, action controls, Escape-to-close.
3. Vending snapshot → view model → binder → controller; migrate vendor prefabs from legacy `VendingMachine` interaction.
4. EditMode snapshot tests and styling fix (keep cloned `TemplateContainer` in hierarchy so UXML style sheets apply).

## Validation checklist

- Diegetic component preview opens from `SS3D → Machine Interface → Preview Diegetic Components`.
- Vending UI opens from world interaction and dev harness (`Alpha5`); chassis, bezel, and screen backgrounds render correctly.
- Vend-to-tray then take-from-tray flow updates snapshot and spawns items at the dispensing transform.
- APC and SMES modal panels remain unchanged.

## Implementation notes

- ID card reader is stubbed in v1 (`ReadId` logs unavailable).
- `VendingMachineController` lives in `Assets/Scripts/SS3D/UI/MachineInterface/` (not `Systems/Furniture/`); legacy `VendingMachine.cs` and `DispenseProductInteraction.cs` removed.
- Diegetic panels must mount the full cloned UXML `TemplateContainer` — detaching only `DiegeticDeviceShell` drops attached style sheets.
