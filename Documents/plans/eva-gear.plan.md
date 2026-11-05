---
name: EVA gear
overview: Hardsuit seal and breach, internals with gas tanks and suit storage, and magboots, serving MVP1 M8 and the operative kit
todos:
  - id: seal-breach
    content: "PR 1: outside-in armor order, SealedGearItemExtension, breach, pure exposure resolver, alerts and screen effects read effective exposure"
    status: completed
  - id: internals
    content: "PR 2: GasTankItemExtension, EvaGearController internals, SuitStorage slot, internals chip, tank content, crew pocket loadouts, oxygen treatment draws from the tank"
    status: pending
  - id: magboots
    content: "PR 3: SyndicateMagboots content with per-foot split, EvaGearController anchor, wind early-out, movement cost, chip, audio"
    status: pending
  - id: docs-sync
    content: "Each PR: update-system-docs for touched maps; M8 milestone row when the slice ships"
    status: pending
isProject: false
---

# EVA gear

Implements `Documents/design/armor.md` §1, §3, §6, `Documents/design/inventory-storage.md` §3 and `Documents/design/antagonist-content.md` first-pass operative kit. Serves [mvp1-nuke-ops](../milestones/mvp1-nuke-ops.md) M8 and M0.

## Decisions

**Delivery:** hardsuit PR first, then three PRs from `feature/eva-gear`, in the todo order.

**Armor order and breach:** `ApplyArmorAbsorption` resolves worn pieces outermost first (`ExoSuit`, then `Jumpsuit`). Breach is decided once after all layers: if any damage reaches the limb, the outermost sealed piece covering the zone is punctured. Absorption values stay as they are, so the first rifle round breaches; review in Playtest 1.

**Seal:** `SealedGearItemExtension` sits beside `ArmorItemExtension`, with a breached SyncVar and heat protection. The seal holds when the `ExoSuit` and `Head` slots both hold intact sealed pieces.

**Exposure:** `SampleEnvironmentAtBody` stays raw. A pure resolver applies seal and internals, and `HealthEnvironmentState` carries the result, so alerts and screen effects show only what is hurting the player. Protocol bump.

**Tanks:** `GasTankItemExtension` holds server-side moles per gas and syncs a fill fraction. There is no pressure model or atmos link. Breathing uses the tank's mix. No CO₂ is exhaled while on internals, so tanks never wake atmos cells.

**Body state:** one `EvaGearController` on `Human`, built by recipe, holds the internals and magboot toggles. The server turns internals off when the mask, helmet or tank goes away. Tank priority is suit storage, pocket, hand. Magboots need both feet, because shoes are one item per foot.

**Suit storage:** `ContainerType.SuitStorage` is appended to the enum and gated by `IStorageCondition`. A suit cannot be removed while storage holds a tank, following the belt and jumpsuit rule. `EquipmentGrid` shows the slot only while a suit is worn. Loadouts spawn it after the suit.

**Oxygen treatment:** each "Apply oxygen" draws from the tank and is blocked when it is empty.

**Magboots:** `Ragdoll.ServerApplyWind` returns early while anchored and upright, and existing wind fades by itself. Movement is owner-side, so it reads the SyncVar there, forces walking and applies the speed factor.

**Input:** two actions in the `Hotkeys` map, with default keys chosen from free ones during the build.

**Content:** the full tank becomes `Huge`, and `OxygenTank` and `PlasmaTank` gain the tank extension. The emergency tank is `Small`, using `TankOxygenEmergency` from `AirTanks.fbx`. Magboots use `SyndicateMagboots` and `SyndicateMagbootsItem` from `SyndicateHardsuit.fbx`, split into left and right worn meshes by bone weight, with a half mesh as the fallback for the folded form.

**Kits:** operatives get the hardsuit, a full tank in suit storage and magboots, and start with internals off, so spawn pads need air. Crew get an emergency tank and a mask in their pockets, through a `PocketAssets` list on `RoleLoadout`. Engineers get recoloured magboots when M0 adds the role. Crew EVA suits wait for art.

## Tuning

Starting values, reviewed after Playtest 1.

| Value | Start |
|---|---|
| Full tank | 45 mol (15 min at 0.05 mol/s) |
| Emergency tank | 9 mol (3 min) |
| Apply oxygen | 5 mol per use |
| Hardsuit heat and fire protection | 70% |
| Hardsuit weight | 8 |
| Full tank weight | 5 |
| Magboot speed factor | 0.6 |

## Deviations

- Magboots get a chip beside the internals chip. Deviates from `armor.md` §6, which names only the internals chip, because the boots need a visible on/off state.
- No milestone or design text relies on locker contents any more: `savedItemList` in station templates stores no container contents, so crew gear comes from loadouts.

## Implementation notes

The breach alert is a warning while the air is safe and critical once pressure or cold would reach the body. It clears when the breached piece comes off. Breach through `ApplyArmorAbsorption` is covered by a PlayMode scenario.

## Tests

EditMode first: resolver, breach order, tank drain and storage rules. A remote scenario through `run_smoketest.sh` covers toggle sync and magboot wind on a real client. Visual checks happen in the editor, because graphical release clients crash on tomnas after embark.
