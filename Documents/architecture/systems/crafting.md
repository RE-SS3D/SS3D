> Code paths: Assets/Scripts/SS3D/Systems/Crafting/
> Entry points: CraftingSubSystem
> Status: stub
> Verified: a20853b1c — 2026-07-18

# Crafting

## Overview

Recipe-based crafting subsystem. **Obsolete — due for removal**, not extension. Do not invest in discover APIs, new recipes UI, or hand wiring beyond keeping Play Mode unblocked until purge.

**Condemned UI:** crafting menu uGUI — do not extend; replace per [crafting.md](../../design/crafting.md) ([agent-first composition](../2026-07_agent-first-composition.md)). Full system purge should remove `Craft` from hand prefabs and delete the menu path.

## Start here

- `Assets/Scripts/SS3D/Systems/Crafting/CraftingSubSystem.cs` — crafting subsystem
- `Assets/Scripts/SS3D/Systems/Crafting/Craft.cs` — hand/tool source extension (landmine until removed)
- `Assets/Scripts/SS3D/Data/Generated/CraftingRecipes.cs` — codegen recipe refs

## Extension points

None — system is obsolete. Prefer delete over new features.

## Pitfalls

- **Delete-debt / empty-hand outline pollution:** `Craft` on both hands used to discover `OpenCraftingMenu` for every hover target, so empty-hand outlines lit every `Selectable` while held-item (item as source, no `Craft`) looked fine. Discover is gated on `CanInteract` as a holdover bandage; the real fix is removing crafting + `Craft` from hands. See [interactions-framework](interactions-framework.md) § Architecture smells #1.

## Depends on / Used by

- **Depends on:** [inventory](inventory.md), [data-codegen](data-codegen.md), [interactions-framework](interactions-framework.md)

## Related docs

- Design (read-only): [Documents/design/crafting.md](../../design/crafting.md) — intended direction; code is not the target to extend
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [INDEX.md](../INDEX.md)
