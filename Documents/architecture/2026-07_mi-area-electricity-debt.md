> Implements: Documents/design/area.md §power infrastructure behavior, Documents/design/main-hud.md §machine control surfaces
> Touches systems: machine-interface, electricity, area
> Status: shipped

# Machine UI + Area/Electricity debt refactor

## Goal

Close authenticity and performance debt introduced with machine interface and area-scoped electricity, then thin registration and peel APC domain helpers without crossing asmdef cycles.

## Shipped

1. Removed unvalidated `CmdRequestOpen`; open only via interaction `ServerHandleOpenRequest`.
2. Per-APC consumer index on `ElectricitySubSystem`; invalidate from area APC register/unregister/rebuild; reduced LINQ on area power path.
3. Unified `PowerGate` (`NullConsumerPolicy` Allow/Deny) with migrated furniture/visual/UI call sites.
4. `IMachineOptimisticControlHandler` + `MachineUiCatalog` thins Host/SubSystem growth.
5. `PowerStorageMath`, `ApcStatusDeriver` / power-state enums in Systems; namespace renamed `System.Electricity` → `SS3D.Systems.Electricity`; `ApcController` remains MI façade.

## Related

- System maps: [machine-interface](systems/machine-interface.md), [electricity](systems/electricity.md), [area](systems/area.md)
- Prior: [2026-07_machine-interface-phase3-smes-generalization](2026-07_machine-interface-phase3-smes-generalization.md), [2026-07_area-foundation](2026-07_area-foundation.md)
