> Implements: Documents/architecture/TECH_DEBT.md#19-god-classes-forming-in-hot-uiinteraction-code
> Touches systems: interactions runtime, interactions framework, combat, entities, examine, inventory
> Status: shipped

# InteractionController decomposition (Jul 2026)

## Goal

Turn `InteractionController` from a god-class (~2.3k lines) into a thin player-owned input/policy
router. Domains own network and feedback. Preserve the shipped framework contract
([interactions-framework](../../architecture/systems/interactions-framework.md)): `IInteraction`, `InteractionPipeline`,
Discover/`HasPoint`, `InteractionIdentifier`, Harm-never-falls-through.

Closes TECH_DEBT §1.9 for **interactions only** (`MainHudSubSystem` remains open).

## Shipped

| Phase | Deliverable |
|-------|-------------|
| 0 | This effort doc + TECH_DEBT / map links |
| 1 | `InteractionDiscovery` + `InteractionDispatch` + EditMode `InteractionDispatchTests` |
| 2 | `InteractionOutlineDriver` + `DelayedInteractionTracker` |
| 3 | `CombatInteractionNetwork` + `CombatInteractionNetworkPrefabSetup` (Human recipe) + integrity test |
| 4 | Thin `InteractionController` router (world/inventory/examine RPCs stay) |
| 5 | System maps / INDEX / TECH_DEBT update |

Approximate sizes after ship: `InteractionController` ~1.2k lines; `CombatInteractionNetwork` ~820 lines.

Follow-up fix: `CombatEditorAssembly.asmdef` must reference `SS3D.Core` (`ac35e3e8e`) so recipes can `AddComponent` NetworkActor-derived types.

## Locked decisions (retained)

- Sibling `NetworkBehaviour` for combat on the Human `NetworkObject`.
- World / inventory / examine-take RPCs stay on `InteractionController`.
- No Discover rewrite, no ScriptableObject interaction registry.
- Human wiring via tier-B recipe on `HumanPrefabRecipes` (TestHuman is stripped — no IC — skipped).

## Out of scope (still open)

- Splitting world/inventory/examine RPCs onto more NetworkBehaviours
- `MainHudSubSystem` decomposition
- RPC rate limiting, SO registry, Discover rewrite

## Related

- [interactions-runtime.md](../../architecture/systems/interactions-runtime.md)
- [interactions-framework.md](../../architecture/systems/interactions-framework.md)
- [combat.md](../../architecture/systems/combat.md)
- [TECH_DEBT.md](../../architecture/TECH_DEBT.md) §1.9
- Prior: [2026-07_interaction-system-hardening.md](2026-07_interaction-system-hardening.md)
