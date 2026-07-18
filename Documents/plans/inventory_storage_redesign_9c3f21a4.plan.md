---
name: Inventory Storage Redesign
overview: "Rebuild the inventory Container primitive (real weight, size-class fit-check, stacking, minimal ID-gated locks) and replace the condemned uGUI container UI with a UITK on-demand storage panel supporting multiple simultaneously open panels, nested-container opening, and slot-to-slot drag-and-drop, per Documents/design/inventory-storage.md and the imported Claude Design mockups."
todos:
  - id: docs-effort
    content: "Phase 0: Architecture effort doc + this plan file"
    status: completed
  - id: purge-ugui
    content: "Phase 0: Deleted condemned uGUI container UI scripts (ContainerUi, ContainerView, ContainerDisplay, ItemGrid, ItemGridItem, ItemDisplay, DraggableWindow, InventoryView, DummySlot, SingleItemContainerSlot, InventoryDisplayElement, ToggleInternalClothingUI, ToggleBodyTargetUI) + cleaned up call sites (HumanInventory, Hands, IssueReproduction test). Human.prefab/item prefabs never actually referenced these as attached components (only a since-renamed null AttachedContainer.ContainerUi field) so no prefab surgery was needed there. Prefab ASSETS themselves were NOT deleted — see 'purge-prefabs-followup' below."
    status: completed
  - id: purge-prefabs-followup
    content: "Phase 0 follow-up: PlayerCanvas stripped of HumanoidInventory + StaminaBar PrefabInstances; Containers UI prefab tree + StaminaBar prefab deleted; StaminaBarView deleted."
    status: completed
  - id: size-class
    content: "Phase 1: SizeClass enum on Item + max-size-class fit-check on AttachedContainer.CanContainItem"
    status: completed
  - id: recursive-weight
    content: "Phase 1: Real recursive AttachedContainer.Weight (computed, never cached) wired off Item.Weight + StackCount"
    status: completed
  - id: stacking
    content: "Phase 1: Revive stacking — Item.MaxStackSize/StackCount, AttachedContainer.AddStoredItem merges into an existing compatible stack instead of taking a new slot (splitting deferred, see architecture doc)"
    status: completed
  - id: flat-slots
    content: "Phase 1 (dropped): kept existing 2D grid Position addressing — behaves as flat slot count already since no item occupies >1 cell; ClothingContainers string-keyed lookup left untouched (out of blast radius). See architecture doc scope decisions."
    status: completed
  - id: store-bug
    content: "Phase 1: Fix StoreInteraction.Start() — was calling Container.Dump() (whole hand); now RemoveItem(item)"
    status: completed
  - id: container-lock
    content: "Phase 1: AttachedContainerLock component + AttachedContainer.IsAccessibleBy, wired into ViewContainerInteraction/StoreInteraction/TakeFirstInteraction/OpenInteraction CanInteract"
    status: completed
  - id: viewer-multi
    content: "Phase 2: Verified — ContainerViewer's existing _displayedContainers list + per-container TargetRpc open/close already supports multiple simultaneously open containers; no change needed."
    status: completed
  - id: panel-catalog
    content: "Phase 3: StoragePanelAssetPaths/StoragePanelAssetCatalog + Editor rebuild menu (SS3D → Storage Panel → Rebuild Asset Catalog), new SS3D.UI.StoragePanel asmdef (Systems ⟶ UI ⟶ MachineInterface ⟶ StoragePanel ⟶ MainHud dependency chain — see architecture doc)"
    status: completed
  - id: panel-host
    content: "Phase 3: StoragePanelHost — multi-instance panel manager (dictionary keyed by container), cascade-position fallback + anchored-near-click positioning, bound/unbound by MainHudSubSystem's existing local-player lifecycle (not self-discovered, to keep Systems→UI layering intact)"
    status: completed
  - id: panel-view
    content: "Phase 3: StoragePanelView — header/slot-count, weight bar (info/warning/danger thresholds off new AttachedContainer.MaxWeight), slot grid (StorageSlot : InventorySlot), nested breadcrumb + close button"
    status: completed
  - id: slot-drag
    content: "Phase 4: Slot drag-and-drop UITK manipulator — ghost element, cross-panel hit-testing, valid/invalid-drop state (static class toggle, not the mockup's pulse animation — UITK has no @keyframes), server-validated transfer via HumanInventory.ClientTransferItem. Also added click-to-open for nested container items (lockbox-in-locker)."
    status: completed
  - id: gear-strip-wire
    content: "Phase 5: HandsGearStrip belt/ID/PDA/back now fire GearSlotClicked; MainHudSubSystem resolves the container and calls StoragePanelHost.RequestOpenNear anchored at the slot's worldBound"
    status: completed
  - id: world-container-wire
    content: "Phase 5: No interaction-file changes needed — ViewContainerInteraction.Start() already calls ContainerViewer.ShowContainerUI, which StoragePanelHost now listens to directly; world containers open via the cascade-position fallback (no precise click-anchor plumbing this pass). Take-from-character reuses the same path, untested."
    status: completed
  - id: clean-slate-hud
    content: "Clean slate: Equipment doll click/drag; HUD↔panel HudDropTarget drag; pockets via ToggleInternalClothing→ContainerViewer; StoragePanelAssetCatalog committed"
    status: completed
  - id: clean-slate-stamina
    content: "Clean slate: HumanInventory.CarriedWeight; stamina Phase 7a rewrite (health regen, weight, overdraw→oxy); StaminaBar purged; StaminaTests updated"
    status: completed
  - id: system-docs-sync
    content: "Phase 6: update-system-docs — inventory + stamina maps, INDEX, architecture effort, health 7a, this plan"
    status: completed
  - id: verification
    content: "Verification: NOT RUN in implementing session — owner must compile in Unity Editor and Play-Mode verify equip/panels/weight/stamina/oxy before merge."
    status: pending
isProject: false
---

# Inventory Storage Redesign

## Context

**Design specs (read-only):**

- [Documents/design/inventory-storage.md](../design/inventory-storage.md) — Container primitive, equip
  slots, size class/fit, stacking, storage panel UI, nested containers, locked storage, weight/encumbrance
- [Documents/design/main-hud.md](../design/main-hud.md) §8 — gear strip as the panel's entry point

**Architecture effort:** [2026-07_inventory-storage-redesign.md](../architecture/2026-07_inventory-storage-redesign.md)

**Imported reference (Claude Design project `Backpack storage panel design`,
`69c40665-15fd-401f-9cd9-b3751bd12bdb`):** `Backpack Storage Panel.dc.html` (single panel: header,
weight bar, 3-col slot grid, stack badge, oversized-item rejection state) and
`Looting Scene - Backpack and Locker.dc.html` (multiple panels open side by side, nested lockbox panel
with origin breadcrumb, drag-in-transit ghost with path line).

**Current-state map (pre-redesign):** [Documents/architecture/systems/inventory.md](../architecture/systems/inventory.md)

**Scope decisions:** see architecture effort doc "Scope decisions" — minimal ID-gate locking only
(hacking-interface FDU bitmask deferred), flat slot-index addressing (no item footprints exist
today), panel drag-drop is a new UITK implementation (not literal `InteractionTier.Combine` reuse).

## Implementation notes

**Phase 1 (data model), shipped this pass:** `SizeClass` enum, `Item.Weight`/`SizeClass`/
`MaxStackSize`/`StackCount` fields, `AttachedContainer.Weight` (recursive), `AttachedContainer.
CanContainItem` size-class check, stack-merge in `AddStoredItem` (`TryFindMergeableStack` +
`DespawnMergedItem`), `StoreInteraction.Start()` hand-dump bug fix, `AttachedContainerLock` +
`AttachedContainer.IsAccessibleBy` wired into the four container interactions' `CanInteract`. Deleted
dead `Stackable.cs` (fully commented out, unused). Two items **trimmed from the original plan** during
implementation (see architecture doc "Scope decisions"): flat slot-index addressing refactor and
`ClothingContainers` string-key cleanup — both judged unnecessary churn/risk for no behavior change,
given no compiler was available to verify a wider refactor. Stack **splitting** (pulling part of a
stack back out) is explicitly not implemented — only merging on add.

**Phases 0/2/3/4/5, shipped this pass (still unverified — no Editor):**

- **Phase 0:** deleted the condemned scripts; left the .prefab assets alone (see
  `purge-prefabs-followup` — `HumanoidInventory.prefab` is nested inside `PlayerCanvas.prefab`,
  genuine Editor-only surgery).
- **Phase 2:** turned out to be a no-op — `ContainerViewer` already supported N simultaneously open
  containers.
- **Phase 3:** `Assets/Scripts/SS3D/UI/StoragePanel/` — new `SS3D.UI.StoragePanel.asmdef`. Placing the
  panel code required working out the real asmdef dependency chain (`SS3D.Systems` → `SS3D.UI` →
  ... → `SS3D.UI.MachineInterface` → `SS3D.UI.StoragePanel` → `SS3D.UI.MainHud`, confirmed by
  grepping GUID cross-references between `.asmdef` files) — an earlier attempt put the panel host
  where `ContainerViewer` would need to reference it directly, which would have been a circular
  assembly reference. Fixed by having `MainHudSubSystem` (which already tracks the local player)
  hand `StoragePanelHost` the `ContainerViewer` reference, instead of the panel host discovering its
  own local player.
- **Phase 4:** drag-and-drop shipped without the mockup's pulsing valid/invalid border — UI Toolkit
  has no CSS `@keyframes`; used a static class-toggle highlight instead. Also found and fixed a real
  bug while wiring this: `AttachedContainer.AddStoredItem`'s "position occupied" check ran *before*
  the stack-merge check, so dropping directly onto an existing stack (the actual drag-drop use case)
  would have failed instead of merging — reordered, and `CanContainItemAtPosition` updated to match
  so the drop-highlight preview agrees with what actually happens.
- **Phase 5:** gear-strip wiring shipped; world-container wiring needed no changes (already routes
  through the now-panel-connected `ContainerViewer`).

**Still needed:** Phase 6 (docs sync — this note is part of it), the `purge-prefabs-followup` manual
Editor step, and the entire Verification section — nothing in this pass was compiled or run. See
architecture doc "Execution environment constraint."
