> Implements: Documents/design/chemistry.md §2–§6 (reagent record, containers/transfer, reactions, thermodynamics, bad-mix resolution hooks); Documents/design/atmospherics.md §3 (shared substance record fields — data only)
> Touches systems: substances, interactions, examine, inventory, atmospherics (definition contract only)
> Status: shipped

# Substances foundation

## Goal

Clean-slate rewrite of the liquid substance runtime: data-driven reagent definitions, networked containers, pour-triggered reaction resolution, and container temperature. Ships the **physical primitives** [`chemistry.md`](../../design/chemistry.md) and later atmospherics both need. Does **not** ship chemistry gameplay (bloodstream, delivery tools, analyzer knowledge, chem machines, HUD alerts) — that is a separate MVP2 effort.

## Shipped

1. **Purge** — removed legacy `SS3D.Substances` enum/SO/`ProcessContainer`/radial dispenser stack; namespace is `SS3D.Systems.Substances`.
2. **Data contract** — `ReagentDefinition` / `ReagentRegistry` / `RecipeDefinition` / `MixtureEntry` (ml); atmos §3 molar mass + boil/freeze fields present; `GasDefinition.LinkedReagent`.
3. **Container** — SyncList mixture + capacity/lock/temperature SyncVars; server-only `InitialMixture` seed; Tier 2 transfer + Tier 3 Combine pour; free-property examine provider.
4. **Reactions** — indexed `ReactionResolver` (success / near-miss / incompatible); thermal writeback; flash → Ignition; `HazardOccurred` stub logger.
5. **Content** — `CoreReagentRegistry` + vertical slice (Product C, heat-gated recipe, volatile incompatible pair); mug/soda/tanks migrated; Human empty container stripped.
6. **Editor** — `SubstancesContentPrefabRecipes` + Human strip registered on `HumanPrefabRecipes`.
7. **Tests** — EditMode mixture math + resolver cases in `SubstanceContainerTests`.

## Deferred (chemistry / MVP2)

Track as a future effort (suggested name `2026-07_chemistry-gameplay.md` when commissioned):

- Per-mind reagent knowledge + chemical analyzer ([chemistry.md](../../design/chemistry.md) §7)
- Bloodstream metabolism + sedation via `IHealthEffectModifier` (§8)
- Syringe / patch / pill / ingest / IV delivery (§9)
- Chem dispenser, grinder, pill press as MI surfaces (§10)
- HUD organ-readout extension + OD/sedation alerts (§11)
- Full §6 hazard consumers (atmos gas inject, structural small blast, AoE foam/frost)
- Powered heater furniture; cryogenics consuming container temperature ([cryogenics.md](../../design/cryogenics.md))
- Gas/solid reagent states; IV furniture; botanics/food as reagent sources (§13)

## Documented notes

- Identity discovery is observer knowledge, not a container `Analyzed` flag (architecture interpretation of chemistry §7).
- Gas tanks remain on `SubstanceContainer` until dedicated gas cylinders exist.

## Related docs

- System map: [systems/substances.md](../../architecture/systems/substances.md)
- Plan: [substances_foundation_implementation.plan.md](../plans/substances_foundation_implementation.plan.md)
- Design (read-only): [chemistry.md](../../design/chemistry.md), [atmospherics.md](../../design/atmospherics.md) §3
- Composition policy: [2026-07_agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md)
