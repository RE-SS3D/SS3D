> Implements: Documents/design/main-hud.md §machine control surfaces, Documents/design/area.md §power infrastructure behavior
> Touches systems: SMES UI, machine-interface registry/plumbing, FishNet RPC shape, snapshot mapping/tests
> Status: shipped

# Machine Interface Phase 3 SMES and Generalization

## Goal

Expand the machine-interface stack from APC-only to multi-machine support, beginning with SMES, while reducing duplicate host/binder/network plumbing.

## Shipped

- SMES interface models, snapshots, binders, and UI component set.
- `SmesController` integrated with networked machine interface open/refresh lifecycle.
- Registry-driven machine interface registration and shared binder flow.
- APC overlap diagnostic and grid-in display (cached per-tick draw from electricity subsystem).
- SMES input/output controlled only via machine interface; world toggle interaction removed from SMES prefab.
- SMES warnings panel removed; status conveyed via status banner and power-flow rows.
- SMES networked interface and prefab/scene wiring were implemented and committed.
- Follow-up fixes addressed FishNet codegen constraints (notably nested network behavior rejection and concrete RPC typing).
- Additional generalization/refactor commits introduced registry-driven plumbing and shared binder flow.

## Validation checklist

- APC and SMES interfaces can both open from world interactions.
- Snapshot transport remains deterministic for both machine types.
- Shared host/subsystem path handles open/refresh/close without machine-specific breakage.
- No nested `NetworkBehaviour` patterns (FishNet ILPP compatibility requirement).

## Follow-ups (out of scope for this effort)

- Consolidate duplicated diagnostics behavior across APC/SMES binders.
- Explicit integration test coverage for multi-viewer mixed machine sessions.
- Additional machine types via registry extension pattern documented in [diegetic screen UI framework](2026-07_diegetic-screen-ui-framework.md) and the updated [machine-interface](systems/machine-interface.md) system map.
