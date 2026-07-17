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
- **Slot addressing:** kept the existing `AttachedContainer` 2D grid (`Vector2Int Position`) as the
  storage-layer representation rather than refactoring to flat slot-index addressing — since no item
  ever occupies more than one cell, a `Size.x × Size.y` grid already behaves identically to a flat
  slot count for capacity purposes, so collapsing it is pure churn for no behavior change. The new
  UITK panel view computes its own 3-column visual layout from slot count; the storage layer is
  untouched. Revisited during implementation — trimmed from the original plan to reduce risk.
  `ClothingContainers`' redundant string-keyed lookup was also left alone for the same reason: it's
  used by clothing/armor rendering call sites beyond this effort's scope, and swapping its addressing
  scheme without compiler verification (see below) isn't worth the blast radius for a cosmetic cleanup.
- **Drag-and-drop:** screen-space panel-to-panel slot dragging is a new UITK pointer-capture
  implementation, not a literal reuse of world-space `InteractionTier.Combine` (radial menu
  arm-cursor-then-click). `inventory-storage.md`'s "same Tier 3 combine grammar" language is a
  UX/feel parity claim; the code paths are necessarily different (screen-space panel vs. world
  raycast). Noting this explicitly so it isn't mistaken for a shared implementation later.
- **Out of scope:** exact slot counts/size-class ceilings/weights (balancing), uniform-specific
  pocket variance, storage item durability, bulk "loot everything," cross-round persistence, bag
  fullness silhouette, `cargo.md`/`disposal.md` code changes (conceptually already `Container`
  instances per design doc §2/§14, no functional change needed there), full hacking-interface FDU
  taxonomy (see locking above). **Stack splitting** is also out of scope this pass — merging identical
  incoming items into an existing compatible stack is implemented, but pulling part of a stack back out
  is left for the Tier 3 combine grammar design doc §5 already points at, not built here.

## Execution environment constraint

This effort was implemented in a remote session with **no local Unity Editor** — no compilation, Play
Mode, or Test Runner access. C#/UXML/USS source was written and reviewed by careful reading, not
compiler feedback. Per [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md),
mega-prefabs are never hand-edited as YAML regardless of environment. In practice `Human.prefab` and
the item prefabs turned out fine — they only ever held a null `AttachedContainer.ContainerUi` field
reference, not an attached condemned-UI component, so nothing there needed surgery. The real instance
of this constraint turned out to be `Assets/Content/Systems/UI/Lobby/Canvas/PlayerCanvas.prefab`,
which nests the old `HumanoidInventory.prefab` as a child `PrefabInstance` — that nested reference is
left in place; see "Purge" below and the plan file's `purge-prefabs-followup` todo. The owner must:
open the project in Unity Editor, resolve any compile errors this effort's diff introduces, do that
manual prefab cleanup, run the Editor rebuild-menu tools this effort adds, and carry out the
Verification section below before merging.

## Phases

1. **Purge condemned UI** — deleted the legacy uGUI container UI scripts (`ContainerUi`,
   `ContainerView`, `ContainerDisplay`, `ItemGrid`, `ItemGridItem`, `ItemDisplay`, `DraggableWindow`,
   `InventoryView`, `DummySlot`, `SingleItemContainerSlot`, `InventoryDisplayElement`,
   `ToggleInternalClothingUI`, `ToggleBodyTargetUI`) per the replace-and-purge policy in
   [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md), and cleaned up every call
   site (`HumanInventory`, `Hands`, a PlayMode test). Left the **prefab assets** themselves in place —
   `HumanoidInventory.prefab` is nested inside `PlayerCanvas.prefab`; deleting the asset without the
   Editor would leave a dangling nested-prefab reference. Manual Editor follow-up required (plan file).
2. **Container primitive rework** — `SizeClass` + fit-check on `AttachedContainer`/`Item`; real
   recursive `Weight` (computed, never cached, matches design doc §2/§7) plus a new `MaxWeight`
   display ceiling; revived stacking (`Item.MaxStackSize`/`StackCount`, merge-on-add in
   `AttachedContainer.AddStoredItem`); fixed `StoreInteraction.Start()`'s whole-hand-dump bug; minimal
   ID-gated `AttachedContainerLock` component for world containers.
3. **Networking** — turned out to be a no-op: `ContainerViewer`'s existing `_displayedContainers` list
   and per-container `TargetRpc` open/close already supports any number of simultaneously open
   containers.
4. **Storage panel UITK surface** — new `Assets/Scripts/SS3D/UI/StoragePanel/` (own
   `SS3D.UI.StoragePanel.asmdef`) following the established path-catalog pattern
   (`StoragePanelAssetPaths`/`StoragePanelAssetCatalog` + Editor rebuild menu, mirroring
   `MainHudAssetCatalog`/`MachineUiAssetCatalog`). `StoragePanelHost` manages N simultaneously open
   panel instances (unlike single-panel `MachineInterfaceHost`); `StoragePanelView` renders
   header/weight bar/slot grid/nested breadcrumb per the mockups. **Assembly layering note:** placing
   this required tracing the real asmdef dependency chain (`SS3D.Systems` → `SS3D.UI` → ... →
   `SS3D.UI.MachineInterface` → `SS3D.UI.StoragePanel` → `SS3D.UI.MainHud`, confirmed via GUID
   cross-references between `.asmdef` files) — `ContainerViewer` (Systems layer) cannot reference
   `StoragePanelHost` directly without a circular assembly reference. Fixed by having
   `MainHudSubSystem` (which already owns local-player lifecycle tracking) hand `StoragePanelHost` the
   `ContainerViewer` reference at bind/unbind time, rather than the panel host discovering its own
   local player.
5. **Slot drag-and-drop** — new UITK pointer-capture manipulator per slot (`StorageSlot`): floating
   ghost element, cross-panel hit-testing, valid/invalid-drop state. Shipped as a **static class
   toggle**, not the mockup's pulsing border — UI Toolkit has no CSS `@keyframes`. Also click-to-open
   for nested container items. Fixed a real bug found while wiring this: `AddStoredItem`'s "position
   occupied" check ran before the stack-merge check, so dropping onto an existing stack (the actual
   drag-drop case) would have failed instead of merging.
6. **Wire entry points** — `HandsGearStrip` belt/ID/PDA/back now fire click events;
   `MainHudSubSystem` opens the panel anchored at the slot. World containers needed no interaction-file
   changes — `ViewContainerInteraction` already calls `ContainerViewer.ShowContainerUI`, which
   `StoragePanelHost` now listens to directly (falls back to a cascading position, no precise
   click-anchor plumbing this pass).
7. **Docs sync** — this doc + system map + INDEX + plan file, done as part of shipping this pass.

## Related docs

- Design (read-only): [Documents/design/inventory-storage.md](../design/inventory-storage.md),
  [main-hud.md](../design/main-hud.md) §8
- System map: [systems/inventory.md](systems/inventory.md), [systems/ui-shell.md](systems/ui-shell.md),
  [systems/id-access.md](systems/id-access.md)
- Policy: [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md)
- Plan: [inventory_storage_redesign_9c3f21a4.plan.md](../plans/inventory_storage_redesign_9c3f21a4.plan.md)
