> Implements: Documents/design/inventory-storage.md (all sections), Documents/design/main-hud.md §8
> Touches systems: inventory, main-hud, ui-shell, id-access
> Status: in-progress

# Inventory & storage redesign

Builds the "full inventory/backpack screen" `main-hud.md` §13 deferred and `inventory-storage.md`
formalizes: one shared `Container` primitive for every item-holding place (backpack, pockets, belt,
lockers, crates), and an on-demand panel UI opened per-container. Two Claude Design mockups
(`Backpack Storage Panel.dc.html`, `Looting Scene - Backpack and Locker.dc.html`, project
`Backpack storage panel design`) pin the exact visual/interaction target: header + weight bar + slot
grid, size-class rejection state, stacking badges, multiple panels open side by side, one level of
nested-container opening with an origin breadcrumb.

Existing `Assets/Scripts/SS3D/Systems/Inventory/` code cannot support this UI as-is — see
`Documents/architecture/systems/inventory.md` for the current-state map. This effort reworks the data
model, not just the presentation layer.

## Scope decisions

- **Locking:** minimal ID-gate only (`IdAccessSubSystem.CheckAccess`, same pattern every other
  consumer uses) for freestanding world containers. `hacking-interface.md` §3's FDU
  hacker-visible-bitmask exposure is **deferred** — hacking-interface has no architecture effort yet.
  Flagged as a follow-on integration point, same pattern `pda.md` used for its own unbuilt ahelp tab.
- **Slot addressing:** the existing `AttachedContainer` 2D grid (`Vector2Int Position`) carries no
  item-footprint data (nothing is ever wider than 1 cell) — collapsed to flat slot-index addressing
  matching the mockups' uniform grid. `ClothingContainers`' redundant string-keyed lookup is removed
  in favor of `ContainerType` as the single addressing scheme.
- **Drag-and-drop:** screen-space panel-to-panel slot dragging is a new UITK pointer-capture
  implementation, not a literal reuse of world-space `InteractionTier.Combine` (radial menu
  arm-cursor-then-click). `inventory-storage.md`'s "same Tier 3 combine grammar" language is a
  UX/feel parity claim; the code paths are necessarily different (screen-space panel vs. world
  raycast). Noting this explicitly so it isn't mistaken for a shared implementation later.
- **Out of scope:** exact slot counts/size-class ceilings/weights (balancing), uniform-specific
  pocket variance, storage item durability, bulk "loot everything," cross-round persistence, bag
  fullness silhouette, `cargo.md`/`disposal.md` code changes (conceptually already `Container`
  instances per design doc §2/§14, no functional change needed there), full hacking-interface FDU
  taxonomy (see locking above).

## Phases

1. **Purge condemned UI** — delete legacy uGUI container UI (`ContainerUi`, `ContainerDisplay`,
   `ItemGrid`, `ItemGridItem`, `ItemDisplay`, `DraggableWindow`, `InventoryView`, `DummySlot`,
   `SingleItemContainerSlot`, `InventoryDisplayElement`) per the replace-and-purge policy in
   [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md). `Human.prefab`
   references stripped via an Editor setup tool, not hand-edited YAML.
2. **Container primitive rework** — `SizeClass` + fit-check on `AttachedContainer`/`Item`; real
   recursive `Weight` (computed, never cached, matches design doc §2/§7); revived stacking
   (`StackableItem`); flat slot-index addressing; fix `StoreInteraction.Start()`'s whole-hand-dump
   bug; minimal ID-gated lock component for world containers.
3. **Networking** — verify/extend `ContainerViewer`'s per-client open/close RPC path for multiple
   simultaneously open containers.
4. **Storage panel UITK surface** — new `Assets/Scripts/SS3D/UI/StoragePanel/` following the
   established path-catalog pattern (`StoragePanelAssetPaths`/`StoragePanelAssetCatalog` + Editor
   rebuild menu, mirroring `MainHudAssetCatalog`/`MachineUiAssetCatalog`). `StoragePanelHost` manages
   N simultaneously open panel instances (unlike single-panel `MachineInterfaceHost`) anchored near
   their open origin; `StoragePanelView` renders header/weight bar/slot grid/nested breadcrumb per
   the mockups.
5. **Slot drag-and-drop** — new UITK pointer-capture manipulator per slot: floating ghost element,
   cross-panel hit-testing, valid/invalid-drop highlighting, server-validated transfer on drop.
6. **Wire entry points** — `HandsGearStrip` belt/ID/PDA/back click handlers (currently icon-only, no
   handler); world container open interactions repointed at `StoragePanelHost`; "take from another
   character" reuses the same open-panel path.
7. **Docs sync** — `update-system-docs`: `systems/inventory.md`, `INDEX.md` coverage table, this
   doc's `Status`, plan file todos.

## Related docs

- Design (read-only): [Documents/design/inventory-storage.md](../design/inventory-storage.md),
  [main-hud.md](../design/main-hud.md) §8
- System map: [systems/inventory.md](systems/inventory.md), [systems/ui-shell.md](systems/ui-shell.md),
  [systems/id-access.md](systems/id-access.md)
- Policy: [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md)
- Plan: [inventory_storage_redesign_9c3f21a4.plan.md](../plans/inventory_storage_redesign_9c3f21a4.plan.md)
