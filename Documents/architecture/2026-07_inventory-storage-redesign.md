> Implements: Documents/design/inventory-storage.md (all sections), Documents/design/main-hud.md §8, Documents/design/stamina.md (Phase 7a core)
> Touches systems: inventory, main-hud, ui-shell, id-access, stamina, health
> Status: in-progress (clean-slate code shipped; Play Mode / Editor verification pending)

# Inventory & storage redesign

Builds the "full inventory/backpack screen" `main-hud.md` §13 deferred and `inventory-storage.md`
formalizes: one shared `Container` primitive, on-demand multi-panel UI, Main HUD as the only
equip/storage surface, and Phase 7a-core stamina (weight → regen/cap, push-past-empty → oxy debt).
Obsolete uGUI inventory + stamina bar purged — no dual container UI.

## Scope decisions

- **Locking:** minimal ID-gate only for world containers; FDU bitmask deferred.
- **Slot addressing:** kept 2D grid; no item footprints.
- **Drag-and-drop:** UITK pointer-capture for panel↔panel and panel↔HUD (`HudDropTarget`); not world Combine.
- **Stamina:** obsolete pool/bar replaced (Phase 7a core). Combat drains / winded FX deferred.
- **Crafting/chemistry:** may break; no compatibility shims for old container UI.
- **Out of scope:** balancing numbers, pocket variance, stack split, bulk loot, persistence, bag silhouette, FDU locks, search-other-player polish.

## Clean-slate completion (this pass)

1. **Purge** — removed nested `HumanoidInventory` / `StaminaBar` from `PlayerCanvas.prefab`; deleted
   `Assets/Content/Systems/UI/Systems/Containers/` prefab tree and `StaminaBar` prefab; deleted
   `StaminaBarView.cs`; cleared settings toggle UnityEvent for `ToogleStaminaBar`;
   `ToggleInternalClothing` now opens pocket containers via `ContainerViewer`.
2. **Main HUD ownership** — equipment doll click/drag; gear/hand drag peers; `StoragePanelHost.SetHudDropTargets`.
3. **Carried weight** — `HumanInventory.CarriedWeight` + `OnCarriedWeightChanged`.
4. **Stamina rewrite** — health-modulated regen, weight encumbrance, sprint drain, `ApplyOxyDebt`,
   `ExertionPenalty` on movement; no permanent bar.
5. **Catalog** — committed `StoragePanelAssetCatalog.asset` under Resources.

## Earlier phases (data model + panel)

Container primitive (size-class, recursive weight, stacking, lock), multi-open `ContainerViewer`,
`StoragePanelHost` / `StoragePanelView`, gear-strip open path — see prior plan notes.

## Execution environment constraint

Implemented without Unity Editor Play Mode / Test Runner. Owner must compile in Editor, confirm
catalogs, and run verification before merge. Prefab YAML for `PlayerCanvas` was surgically stripped
of the two nested PrefabInstances (inventory + stamina bar) rather than left dangling.

## Related docs

- Design (read-only): [inventory-storage.md](../design/inventory-storage.md), [main-hud.md](../design/main-hud.md) §8,
  [stamina.md](../design/stamina.md)
- System maps: [systems/inventory.md](systems/inventory.md), [systems/stamina.md](systems/stamina.md),
  [systems/ui-shell.md](systems/ui-shell.md), [systems/id-access.md](systems/id-access.md),
  [systems/health.md](systems/health.md)
- Plan: [inventory_storage_redesign_9c3f21a4.plan.md](../plans/inventory_storage_redesign_9c3f21a4.plan.md)
- Clean-slate plan: `.cursor/plans/inventory_clean_slate_43796029.plan.md`
