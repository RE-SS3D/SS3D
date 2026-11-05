> Parent map: [entities.md](entities.md)
> Code paths: Assets/Scripts/SS3D/Systems/Entities/Editor/, Assets/Scripts/SS3D/Systems/Inventory/Containers/Editor/
> Status: shipped
> Verified: e2f86cf20 — 2026-11-05

# Entity prefab recipes

## Overview

`HumanPrefabRecipes` maintains Human and nested body-part wiring through Unity `PrefabUtility`.
Run **SS3D → Entities → Run All Human Prefab Recipes**, or use the batch entry point
`SS3D.Systems.Entities.Editor.HumanPrefabRecipes.RunAllBatch` with Unity `-executeMethod`.
Close the project's Editor before starting a separate batchmode process.

The aggregator removes dev hacks, strips head/torso world-container interactions, wires hands,
ensures local speech and combat networking, strips the obsolete Human substance container, wires
the ExoSuit slot and worn renderer, and resyncs nested body-part instances last. It is scoped to Human/body-part prefabs; storage-item and
hand-tool recipes belong to their own domain aggregators.

## Start here

- `Assets/Scripts/SS3D/Systems/Entities/Editor/HumanPrefabRecipes.cs` — menu and batch aggregators
- `Assets/Scripts/SS3D/Systems/Entities/Editor/HumanPrefabHygiene.cs` — dev-hack removal, speech emitter, nested-instance resync, FishNet behaviour collection
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/Editor/BodyPartContainerInteractiveStrip.cs` — head/torso root interaction strip
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/Editor/HandsPrefabSetup.cs` — Left/Right hand order and `HandSearchExtension`
- `Assets/Scripts/SS3D/Systems/Combat/Editor/CombatInteractionNetworkPrefabSetup.cs` — combat network sibling wiring
- `Assets/Scripts/SS3D/Systems/Substances/Editor/HumanSubstanceContainerStrip.cs` — obsolete root substance-container strip
- `Assets/Scripts/SS3D/Systems/Entities/Editor/SuitStoragePrefabSetup.cs` — suit storage slot and its two storage conditions, `EvaGearController` on Human, and the pocket size limit on HumanTorso (run after ExoSuitPrefabSetup)
- `Assets/Scripts/SS3D/Systems/Entities/Editor/ExoSuitPrefabSetup.cs` — ExoSuit container/filter (without the jumpsuit belt lock) and type-addressed worn Overwear renderer (requires inventory content assets first)
- `Assets/Scripts/Tests/EditMode/HumanPrefabIntegrityTests.cs` — committed prefab integrity assertions

## Extension points

- Add a repeatable `PrefabUtility` static for a domain concern; register it in **both** `RunAllMenu` and `RunAllBatch`. Do not add a permanent per-recipe MenuItem or hand-edit mega-prefab YAML.
- Rebuild FishNet's serialized behaviour list when adding/removing `NetworkBehaviour`s. Preserve nested body-part behaviours; stop collection only at genuine separate spawn roots.
- Keep `ResyncNestedPrefabInstances` last after body-part mutations.
- `Hands.PlayerHands` must contain Left then Right; the first entry is the initial active hand.
- In-body organ meshes are rigged visual children. `HumanOrgans` item prefabs represent removed organs and carry networking/physics; do not substitute those item prefabs into the body's visual rig.

## Pitfalls

- **Dangling stripped mirrors after body-part edits:** changing a nested prefab does not refresh Human's serialized mirror automatically. Resave/resync Human after all recipes run.
- **Body-part behaviours disappear from FishNet's list:** flat-prefab collectors stop at every child `NetworkObject`. Human collection must traverse children marked nested and stop only at separate spawn roots.
- **Worn clothing renderer must not carry its own `NetworkObject`:** clothing is addressed by `ClothType`, and a new empty nested Nob on a renderer like `Overwear` does not resolve reliably. `ExoSuitPrefabSetup` removes it and rebuilds the root's behaviour list; do the same for any new worn slot.
- **Hand Search is missing despite correct hand order:** the hand recipe also ensures `HandSearchExtension` on both hand assets; run the aggregator rather than editing only `PlayerHands`.

## Tests

- `HumanPrefabIntegrityTests` checks script GUID resolution, dev-hack exclusion, head/torso root `ContainerInteractive` absence, Left/Right hand order, and combat-network presence.
- These assertions cover specific contracts; they do not impose a general component budget or validate all domain wiring.
- After recipe changes, inspect Human/TestHuman for missing-script warnings and exercise affected runtime paths (movement, equip/drop in both hands, health, combat dummy, examine, speech). Source inspection is not a runtime pass.

## Depends on / Used by

- **Depends on:** Unity Editor `PrefabUtility`, FishNet nested-prefab serialization
- **Used by:** [entities](entities.md), [inventory](inventory.md), [combat](combat.md), [health](health.md)

## Related docs

- [Composition policy](../2026-07_agent-first-composition.md), [Editor tooling tiers](../2026-07_editor-tooling-tiers.md)
- Remaining composition work: [TECH_DEBT §1.1](../TECH_DEBT.md#11-humanprefab-mega-prefab)
