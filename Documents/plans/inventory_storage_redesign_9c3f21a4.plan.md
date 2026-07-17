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

**Not yet started:** Phases 0, 2, 3, 4, 5 (uGUI purge, ContainerViewer multi-panel networking, the
UITK storage panel surface itself, slot drag-and-drop, gear-strip/world-container wiring). See
architecture doc "Execution environment constraint" — no Unity Editor was available in the
implementing session; Phase 1 could be written and reasoned about as plain C#, but the remaining
phases involve UXML/USS assets, ScriptableObject catalog instances, and (for Phase 0) mega-prefab
surgery that per policy requires an Editor tool, none of which can be authored with confidence without
compiler/Editor feedback in one continuous session. Resume here.
