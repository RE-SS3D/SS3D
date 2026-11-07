---
name: Item size and weight balance
overview: Replace the default Normal/1 on every handheld, wearable and material item with SS13-style size tiers and kg-scale weights, and re-balance bag grids, ceilings and weight readouts, driven by one recipe table
todos:
  - id: table-recipe
    content: "Add ItemSizeWeightRecipe (static path -> size, weight table) and register it on Run Content Prefab Recipes"
    status: completed
  - id: fold-recipes
    content: "Make GasTankSetup and SyndicateHardsuitSetup read the table instead of their own constants"
    status: completed
  - id: containers
    content: "Extend StorageContainerPrefabSetup: backpack/toolbelt/toolbox/medkit grids, ceilings, MaxWeight; lockers to Huge-max"
    status: completed
  - id: seeded-contents
    content: "Audit prefab-seeded contents, role loadouts and map placements against the smaller grids and new ceilings"
    status: completed
  - id: tests
    content: "EditMode ItemSizeWeightTableTests: coverage, prefab == table, tier ordering, fit rules, loadout bands"
    status: completed
  - id: run-and-verify
    content: "Run the recipe in the Editor, commit the prefab diffs, run Testing/ui/validate.sh --steps compile,editmode"
    status: completed
  - id: docs-sync
    content: "Add the tier rubric to inventory.md; update-system-docs for map, Verified stamp and pitfalls"
    status: completed
isProject: false
---

# Item size and weight balance

Implements `Documents/design/inventory-storage.md` §4 (size tiers and flat fit), §10 (weight feeds stamina) and §14 (exact slot counts, ceilings and weights are a balancing pass). Touches [inventory](../architecture/systems/inventory.md) and [stamina](../architecture/systems/stamina.md) (encumbrance consumer). Disposal reads the same tier ([disposal.md §2](../design/disposal.md)) and is not changed. No milestone gate is tied to this; it is content balance.

Settled in a gameplay-free implementation grill. Nothing here deviates from a design doc.

## Problem

- 55 of the 58 in-scope item prefabs carry the default `Normal` / weight `1`. The exceptions are the hardsuit (8), helmet (2), magboots (2) and the full tanks (5 / Huge).
- Backpack, toolbelt and pockets cap at `Small`, yet toolbox, medkit, backpack and toolbelt are `Normal`, so a medkit cannot go in the player's own backpack.
- Medkit and toolbox have 6×6 grids, bigger than the backpack that carries them. The backpack is 3×2.
- Lockers are `Bulky`-max, so a full gas tank (`Huge`) cannot be stored in any locker.
- Weights are hard-coded in three places (`GasTankSetup`, `SyndicateHardsuitSetup`, prefabs), so any hand edit is silently reverted by a recipe run.

## Decisions

**Altitude and premises:** five tiers, flat fit (tier at or below the ceiling and a free cell), weight never gates fit, weight only feeds stamina (`inventory-storage.md` §4, §10).

**Weight unit:** roughly kilograms. `StoragePanelView` already prints `x.x / y KG`, so weights are multiples of 0.1 (one displayed decimal). Stamina is full at 40 (`StaminaController.ReferenceEncumbranceWeight`), and targets are everyday kit about 10%, EVA kit about 45%, only deliberate hauling near 100%.

**Footprints:** none. One cell per item whatever its tier; capacity is the grid cell count. Footprints stay a known gap.

**Stacks:** weight is per unit. No `maxStackSize` change; stack caps for sheets, rods, planks and tiles are parked.

**Scope:** 57 item prefabs under `Assets/Content/WorldObjects/Items`. Organs, body parts (22 prefabs under `Entities/Humanoids/Human`) and `Doodad` are untouched.

**Bags:** backpack and toolbelt are `Bulky`, so neither fits a backpack (no bag-in-bag loops). A Normal medkit still nests in a backpack per §7; a Bulky toolbox does not.

**Source of truth:** a static C# table in a new tier-B recipe, applied through `SerializedObject` on each run. The recipe owns the values; a comment in the table says "edit the table, not the prefab". A new `Item` prefab must be added to the table or the exempt list or the test fails.

### Containers

| Container | Grid now | Grid | Max tier | MaxWeight (readout) |
|---|---|---|---|---|
| Backpack | 3×2 | 4×3 | Normal (was Small) | 25 (was 20) |
| Toolbelt | 4×1 | 5×1 | Small | 6 (was 10) |
| Toolbox | 6×6 | 4×2 | Bulky | 12 (was 20) |
| Medkit | 6×6 | 3×2 | Normal | 6 (was 20) |
| Lockers (3 prefabs) | 4×4 | 4×4 | Huge (was Bulky) | 80 |
| Pockets, PDA | 2×2 | unchanged | unchanged | unchanged |

Toolbox and medkit are not in `StorageContainerPrefabSetup.Targets` today; they join it. The toolbox's own ceiling stays `Huge` today and is set to `Bulky` here, so a toolbox cannot swallow another toolbox.

### Item table

Tier, then weight in kg-ish units. Paths are relative to `Assets/Content/WorldObjects/Items/`.

| Prefab | Tier | Weight |
|---|---|---|
| Functional/Generic/Identification/IDCard, SecurityIDCard | Tiny | 0.1 |
| Functional/Tools/Command/NuclearAuthenticationDisk | Tiny | 0.1 |
| Consumable/Chemical/Medications/BrutePatch, BurnPatch, MedicalPatch | Tiny | 0.1 |
| Consumable/Food/General/Snacks/DonkPocket | Tiny | 0.2 (displayed 0.2; 0.15 rounds on the readout) |
| Functional/Materials/GlassShard | Tiny | 0.2 |
| Functional/Generic/Poster | Small | 0.1 |
| Clothing/Sunglasses | Small | 0.1 |
| Cosmetic/BikeHorn/BikeHorn | Small | 0.2 |
| Clothing/Headset | Small | 0.2 |
| Clothing/TruckerCap | Small | 0.2 |
| Clothing/GloveInsulated | Small | 0.3 |
| Functional/Storage/Substances/Mug | Small | 0.3 |
| Consumable/Food/Drinks/Soda/SodaCanGeneric | Small | 0.4 |
| Functional/Generic/Identification/PDA, SecurityPDA | Small | 0.4 |
| Clothing/FacewearGasMask | Small | 0.5 |
| Functional/Generic/Tanks/EmergencyOxygenTank | Small | 0.8 |
| Functional/Tools/Engineering/ScrewdriverBlue | Small | 0.2 |
| Functional/Tools/Engineering/Wirecutters, Multitool | Small | 0.3 |
| Functional/Tools/Generic/FlashlightBlue, Medical/HealthScanner | Small | 0.4 |
| Functional/Tools/Engineering/Wrench, Welder; Kitchen/KitchenKnife | Small | 0.5 |
| Functional/Materials/ElectricalCable, ElectricalWire | Small | 0.3 |
| Functional/Materials/SteelRod | Small | 0.5 |
| Functional/Parts/MachineParts/MicroLaser | Small | 0.2 |
| Functional/Parts/MachineParts/MatterBin | Small | 0.4 |
| Functional/Parts/PowerCells/PowerCell | Small | 0.7 |
| Functional/Tools/Engineering/Crowbar | Normal | 1.0 |
| Functional/Tools/Botany/Hatchet | Normal | 1.2 |
| Functional/Storage/Objects/Medkits/Medkit | Normal | 0.6 |
| Clothing/Backpack | Bulky | 0.6 |
| Clothing/Toolbelt | Bulky | 0.5 |
| Clothing/JumpsuitGrey | Normal | 1.0 |
| Clothing/ShoeHiTops | Normal | 0.8 |
| Clothing/ShoeJackboots | Normal | 1.2 |
| Clothing/SyndicateMagboots | Normal | 2.0 |
| Clothing/SyndicateHardsuitHelmet | Normal | 1.5 |
| Functional/Generic/FloorTile | Normal | 0.6 |
| Functional/Materials/GlassSheet | Normal | 1.0 |
| Functional/Materials/GlassReinforcedSheet | Normal | 1.5 |
| Functional/Materials/SteelSheet | Normal | 1.5 |
| Functional/Materials/SteelReinforcedSheet | Normal | 2.0 |
| Functional/Materials/WoodPlank | Normal | 1.0 |
| Weapons/SyndicateMiniBomb, Taser, C4, X4 | Normal | 0.5, 0.6, 0.8, 1.2 |
| Weapons/IonRifle, LaserRifle | Normal | 2.5 |
| Weapons/M4 | Normal | 3.5 |
| Functional/Storage/Objects/ToolBoxes/ToolboxBlue | Bulky | 2.0 |
| Functional/Generic/Boombox | Bulky | 3.0 |
| Functional/Materials/WoodLog | Bulky | 4.0 |
| Clothing/SyndicateHardsuit | Bulky | 6.0 |
| Functional/Generic/Tanks/OxygenTank, PlasmaTank | Huge | 5.0 |

Two numbers moved by 0.05 from the earlier proposal so every weight is a multiple of 0.1 and the panel never shows a rounded value: DonkPocket 0.15 became 0.2 and SodaCan 0.35 became 0.4. Backpack and toolbelt are `Bulky` items; the backpack's `Normal` *ceiling* in the container table is what lets a medkit, crowbar or boots go inside it.

### Load check

Everyday kit (jumpsuit 1.0, boots 0.8, backpack 0.6, belt 0.5, ID 0.1, PDA 0.4, headset 0.2) is 3.6 or 9% of 40. Adding a few tools and a medkit puts it in the 10-15% band. EVA kit (hardsuit 6, helmet 1.5, magboots 2, tank 5, jumpsuit 1, mask 0.5, gloves 0.3, backpack 0.6) is 16.9 or about 42% before cargo. These two sums become test assertions so a later rebalance cannot silently leave the band.

## Work

1. **Recipe.** `ItemSizeWeightRecipe` in `Assets/Scripts/SS3D/Systems/Inventory/Items/Editor/` holds the table and the exempt list (organs, body parts, `Doodad`). It loads each prefab with `PrefabUtility.LoadPrefabContents`, sets `_sizeClass` and `_weight` through `SerializedObject`, and saves. Register it on the **Run Content Prefab Recipes** aggregator (`InventoryContentPrefabRecipes`). No new permanent menu item (`2026-07_editor-tooling-tiers.md`).
2. **Fold hard-coded values.** `GasTankSetup` (`FullTankWeight`, `EmergencyTankWeight`) and `SyndicateHardsuitSetup` (`_weight` 8 / 2 / 2 at the hardsuit, helmet and magboot lines) must read the table, or their recipes would revert it. `SuitStoragePrefabSetup` already uses `Small` and stays.
3. **Containers.** Add the toolbox and medkit to `StorageContainerPrefabSetup.Targets`; update backpack, toolbelt and the three lockers per the container table. The recipe sets `_size`, `_maxSizeClass` and `_maxWeight`.
4. **Seeded contents audit.** Search prefab-seeded `AttachedContainer` contents, `RoleSubSystem` loadouts and station templates (`Builds/Game/Data/StationTemplates/`) for items that no longer fit: anything Bulky or Huge inside a backpack or belt, or more than 6 items in a medkit or 8 in a toolbox. Fix by moving the item to hand, belt or the world, not by weakening the ceilings. Container contents are not persisted yet, so no `PersistenceEnvelope` bump.
5. **Tests.** EditMode `ItemSizeWeightTableTests`:
   - every `Item` prefab under `Items/` is in the table or the exempt list;
   - each prefab's `SizeClass` and `Weight` equal the table;
   - every weight is a multiple of 0.1 and positive;
   - the tier ladder holds: Tiny weights at most 0.2, Small at most 0.8, Normal 0.5-2.0, Bulky at least 0.5, Huge 5;
   - fit rules: pockets and toolbelt accept nothing above Small; the backpack takes Normal and rejects Bulky and Huge, including itself and the toolbelt; lockers take every tier;
   - the everyday and EVA kit weights stay within their bands against `ReferenceEncumbranceWeight`.
6. **Run and verify.** Run **Run Content Prefab Recipes** in the Editor and commit the resulting prefab diffs. Then `Testing/ui/validate.sh --steps compile,editmode`. No `visual` step: the storage panel layout changes only by grid size, but read one screenshot of a 4×3 backpack panel if the Editor is open.
7. **Docs.** Add the tier rubric (what each tier means with examples, plus its weight band) to the Extension points of [inventory.md](../architecture/systems/inventory.md), correct its container line (backpack 4×3 Normal-max, toolbelt 5×1, lockers Huge-max), note the recipe-owns-values pitfall, and run `update-system-docs`.

## Parked for implementation

- Stack caps for sheets, rods, planks and floor tiles (crafting and cargo).
- Organ and body-part weights (surgery).
- Item footprints (separate effort; already a known gap in the inventory map).

## Risks

- **Silent revert.** Any prefab hand-edit reverts on the next recipe run. Mitigated by the test comparing prefab to table, and by the table comment.
- **Grid shrink strands items.** Shrinking the medkit and toolbox from 36 cells to 6 and 8 can leave a seeded or placed medkit over capacity. Covered by the audit in step 4.
- **Bulky bags and lockers.** A Bulky backpack fits a locker (Huge-max after this change) but no backpack. Playtest the "put my bag in the locker" flow once.

## Implementation notes

Written without a Unity Editor. **Verified 2026-11-07** (Unity 6000.3.16f1): Run Content Prefab Recipes changed no values (only Unity re-serialising, discarded), and `validate.sh --steps compile,editmode` passed 3451/3451. The checked-in prefab values were applied to the YAML by script from the same table and cross-checked against `ItemSizeWeightTable.cs` row by row (57 rows, 0 mismatches), so a first recipe run should produce no diff apart from Unity re-serialising.

Differences from the plan above:

- **Location:** the table and recipe are in `Containers/Editor/` beside `GasTankSetup` (same assembly), not `Items/Editor/`. The EditMode test assembly now references `ContainerEditorAssembly`.
- **Medkit and toolbox are not in `StorageContainerPrefabSetup.Targets`.** That recipe forces `_isOpenable` and `_onlyStoreWhenOpen` off, which would break both. Their grid, ceiling and readout are `ItemSizeWeightTable.Containers`, applied by `ItemSizeWeightRecipe`.
- **Prefab variants** (12: three jumpsuits, seven soda cans, `HeadsetSecurity`, `PowerCellAdmin`) have no row of their own. None overrides weight or size, so they inherit their base; the coverage test accepts a variant only when its base has a row and it does not override.
- **Seeded-contents audit found nothing to fix.** No prefab or station template stores container contents. Loadouts fill only equipment slots and pockets; the pocket items (emergency tank, gas mask) are Small, which pockets still take.
- **System map** `inventory.md` was edited by hand for the touched sections (rubric, container line, pitfalls, tests). Its `Verified` stamp was not bumped because the rest of the map was not re-checked; run `update-system-docs` for that.
- **Weapons stay Normal (2026-11-07, owner).** Develop's #174 added the energy weapons and explosives plus `TileMapTests.EveryWeaponFitsTheBackpack`. The owner chose that rule over Bulky rifles, so the M4 moved from Bulky to Normal and weapons may exceed the 2 kg Normal band (up to 3.5).
