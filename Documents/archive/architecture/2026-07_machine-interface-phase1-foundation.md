> Implements: Documents/design/main-hud.md §layout and interaction model
> Touches systems: UI Toolkit, machine-interface framework, editor tooling
> Status: shipped

# Machine Interface Phase 1 Foundation

## Goal

Create a standalone machine-interface foundation that can be developed and validated without network dependencies, then reused by APC and SMES implementations.

## Scope

- Introduce shared USS tokens and typography for machine panels.
- Build reusable UI Toolkit components (window shell, status rows, diagnostics, bars, channel controls).
- Add host/subsystem scaffolding for open/close/refresh flows in local simulation mode.
- Provide editor and play-mode harnesses for rapid iteration before server wiring.

## Delivery order

1. Add token and typography styles under `Assets/Content/Systems/UI/MachineInterface/Tokens/`.
2. Add reusable component classes and paired USS styles under `Assets/Scripts/SS3D/UI/MachineInterface/Components/` and `Assets/Content/Systems/UI/MachineInterface/Components/`.
3. Add `SS3D.UI.MachineInterface` assembly and local view-model/binder contracts.
4. Implement `MachineInterfaceHost` and `MachineInterfaceSubSystem` local-only open/refresh/close flow.
5. Add APC template (`ApcPowerController.uxml`/`.uss`) and wire binder.
6. Add tooling:
   - Editor preview window (`SS3D -> Machine Interface -> Preview APC Panel`)
   - Play-mode dev harness with state cycling shortcuts.

## Validation checklist

- Editor preview can switch between Nominal/Overload/Critical states.
- Panel window drag/close behavior works in local preview.
- Diagnostics and channel widgets update from view-model changes.
- No networking/FishNet dependencies required for Phase 1 iteration.

## Notes from recovered transcript plan

- This phase was explicitly intended as a hard boundary: no network RPC behavior, only UI foundation and mock state simulation.
- The local-only subsystem was planned to be upgraded in Phase 2 rather than replaced.
