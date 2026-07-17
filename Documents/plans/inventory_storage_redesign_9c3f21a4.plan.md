---
name: Inventory Storage Redesign
overview: "Rebuild the inventory Container primitive (real weight, size-class fit-check, stacking, minimal ID-gated locks) and replace the condemned uGUI container UI with a UITK on-demand storage panel supporting multiple simultaneously open panels, nested-container opening, and slot-to-slot drag-and-drop, per Documents/design/inventory-storage.md and the imported Claude Design mockups."
todos:
  - id: docs-effort
    content: "Phase 0: Architecture effort doc + this plan file"
    status: completed
  - id: purge-ugui
    content: "Phase 0: Delete condemned uGUI container UI (ContainerUi, ContainerDisplay, ItemGrid, ItemGridItem, ItemDisplay, DraggableWindow, InventoryView, DummySlot, SingleItemContainerSlot, InventoryDisplayElement) + strip Human.prefab refs via Editor tool"
    status: pending
  - id: size-class
    content: "Phase 1: SizeClass enum on Item + max-size-class fit-check on AttachedContainer (SizeClassStorageCondition)"
    status: pending
  - id: recursive-weight
    content: "Phase 1: Real recursive AttachedContainer.Weight (computed, never cached) wired off Item._weight"
    status: pending
  - id: stacking
    content: "Phase 1: Revive stacking — StackableItem trait/component, AttachedContainer collapses identical stackable items into one slot + count"
    status: pending
  - id: flat-slots
    content: "Phase 1: Collapse 2D grid Position addressing to flat slot-index; remove ClothingContainers string-keyed lookup in favor of ContainerType"
    status: pending
  - id: store-bug
    content: "Phase 1: Fix StoreInteraction.Start() — dumps whole hand Container instead of moving the single item"
    status: pending
  - id: container-lock
    content: "Phase 1: Minimal ID-gated lock component on AttachedContainer for world containers, via IdAccessSubSystem.CheckAccess"
    status: pending
  - id: viewer-multi
    content: "Phase 2: Verify/extend ContainerViewer TargetRpc/ObserversRpc for multiple simultaneously open containers per client"
    status: pending
  - id: panel-catalog
    content: "Phase 3: StoragePanelAssetPaths/StoragePanelAssetCatalog + Editor rebuild menu (mirrors MainHudAssetCatalog/MachineUiAssetCatalog)"
    status: pending
  - id: panel-host
    content: "Phase 3: StoragePanelHost — multi-instance panel manager (dictionary keyed by container), anchored near open origin, closes on out-of-view"
    status: pending
  - id: panel-view
    content: "Phase 3: StoragePanelView — header/slot-count, weight bar (info/warning/danger thresholds), 3-col slot grid, stack badge, nested breadcrumb + close button"
    status: pending
  - id: slot-drag
    content: "Phase 4: Slot drag-and-drop UITK manipulator — ghost element, cross-panel hit-testing, valid/invalid-drop pulse states, server-validated transfer"
    status: pending
  - id: gear-strip-wire
    content: "Phase 5: HandsGearStrip belt/ID/PDA/back click handlers open StoragePanelHost panels"
    status: pending
  - id: world-container-wire
    content: "Phase 5: Repoint ViewContainerInteraction/OpenInteraction at StoragePanelHost; wire take-from-character path"
    status: pending
  - id: system-docs-sync
    content: "Phase 6: update-system-docs — systems/inventory.md, INDEX.md coverage table, architecture effort Status, plan todos"
    status: pending
  - id: verification
    content: "Verification: weight thresholds, size-class rejection, stacking, multi-panel + nested lockbox, drag-drop transfer, lock gating, two-client sync, Test Runner green"
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

(fill in as phases ship — record any divergence from the plan here)
