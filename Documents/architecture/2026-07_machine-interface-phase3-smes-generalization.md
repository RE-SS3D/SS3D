> Implements: Documents/design/main-hud.md §machine control surfaces, Documents/design/area.md §power infrastructure behavior
> Touches systems: SMES UI, machine-interface registry/plumbing, FishNet RPC shape, snapshot mapping/tests
> Status: in-progress

# Machine Interface Phase 3 SMES and Generalization

## Goal

Expand the machine-interface stack from APC-only to multi-machine support, beginning with SMES, while reducing duplicate host/binder/network plumbing.

## Scope

- Add SMES interface models, snapshots, binders, and UI component set.
- Integrate SMES controller into networked machine interface behavior.
- Refactor host/subsystem/behavior contracts toward machine-type-agnostic registration.
- Keep APC behavior functioning during generalization.

## Delivery order

1. SMES data model and view-model definitions.
2. SMES snapshot serializer/mapper pipeline.
3. SMES UI templates and interaction widgets (exterior + engineer-oriented view elements).
4. `SmesController` integration with machine interface open/refresh lifecycle.
5. Generalization pass:
   - Registry for machine interface types.
   - Shared binder flow and reduced APC/SMES branching in host/subsystem.
6. Test pass:
   - snapshot serializer round-trip checks
   - mapper/unit validation
   - smoke checks for open/close/toggle flows.

## Validation checklist

- APC and SMES interfaces can both open from world interactions.
- Snapshot transport remains deterministic for both machine types.
- Shared host/subsystem path handles open/refresh/close without machine-specific breakage.
- No nested `NetworkBehaviour` patterns (FishNet ILPP compatibility requirement).

## Current state recovered from transcripts

- SMES networked interface and prefab/scene wiring were implemented and committed.
- Follow-up fixes addressed FishNet codegen constraints (notably nested network behavior rejection and concrete RPC typing).
- Additional generalization/refactor commits introduced registry-driven plumbing and shared binder flow.

## Remaining work to close phase

- Consolidate duplicated diagnostics behavior across APC/SMES binders.
- Add explicit integration test coverage for multi-viewer mixed machine sessions.

Registry extension pattern for new machine UIs is documented in [diegetic screen UI framework](2026-07_diegetic-screen-ui-framework.md) and the updated [machine-interface](systems/machine-interface.md) system map.
