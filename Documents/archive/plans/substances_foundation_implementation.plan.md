---
name: Substances foundation plan
overview: "Clean-slate rewrite of the substances runtime per Documents/architecture/2026-07_substances-foundation.md: purge legacy types, ship shared ReagentDefinition + networked containers + pour/transfer, then reaction resolution with container heat and hazard hooks — chemistry gameplay deferred to MVP2."
todos:
  - id: phase0-purge-contract
    content: "Phase 0: Delete legacy SS3D.Substances types; add ReagentDefinition/Registry/RecipeDefinition under SS3D.Systems.Substances; retarget GasDefinition.LinkedSubstance; hub rebuild"
    status: completed
  - id: phase0-prefab-recipes
    content: "Phase 0: Tier-B recipes — strip Human SubstanceContainer; migrate mug/soda/tanks InitialMixture to new container"
    status: completed
  - id: phase1-container-transfer
    content: "Phase 1: SyncList SubstanceContainer + Add/Remove/TransferVolume; Tier 2 transfer + Tier 3 Combine pour; free-property examine provider"
    status: completed
  - id: phase1-tests
    content: "Phase 1: Rewrite SubstanceContainerTests for volume-first capacity/lock/proportional transfer"
    status: completed
  - id: phase2-resolver-heat
    content: "Phase 2: ReactionResolver (indexed) + container ambient/decay/thermal writeback + flash to Ignition; HazardKind event stub"
    status: completed
  - id: phase2-content-slice
    content: "Phase 2: Author minimal reagent/recipe vertical slice (success, heat-gated, incompatible)"
    status: completed
  - id: docs-sync
    content: "On ship: update-system-docs — effort status, substances map, INDEX, plan todos"
    status: completed
isProject: false
---

# Substances foundation — implementation plan

Authority: [Documents/architecture/2026-07_substances-foundation.md](../architecture/2026-07_substances-foundation.md).

## Implementation notes

Shipped on branch `cursor/substances-foundation`. Unity batch recipe run was blocked by an open Editor instance; registry assets + prefab migrations were applied via content YAML and tier-B recipe code remains for re-run (`SS3D/Substances/Run Content Prefab Recipes`).

Examined prefabs may still need `SubstanceContainerExaminable` added in-Editor via the content recipe if missing after YAML migration.
