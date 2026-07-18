> Code paths: Assets/Scripts/SS3D/Systems/Inventory/, Assets/Scripts/SS3D/UI/MainHud/, Assets/Scripts/SS3D/UI/StoragePanel/, Assets/Scripts/SS3D/Systems/Stamina/
> Entry points: ItemSubSystem, MainHudSubSystem, StoragePanelHost, StaminaController
> Status: partial
> Verified: 5aa81e431 — 2026-07-18

# Inventory

## Overview

Items, containers, hands, identification cards (`IDCard`, `PDA`), on-demand storage panel UI, and Main HUD equip/storage UX. ID cards bind to server-side crew records via [id-access](id-access.md); spawn-time binding in `RoleSubSystem`.

**Container primitive:** `AttachedContainer` — position-grid slots (`Size.x × Size.y`), `SizeClass` fit ceiling, recursive computed `Weight`, `MaxWeight` panel readout, stacking (`Item.MaxStackSize`/`StackCount`, merge-on-add; split not built). Optional `AttachedContainerLock` ID-gates world containers. See [inventory-storage.md](../../design/inventory-storage.md).

**Carried weight:** `HumanInventory.CarriedWeight` sums every inventory container's recursive `AttachedContainer.Weight` (hands included). Fires `OnCarriedWeightChanged`. Feeds [stamina](stamina.md) encumbrance (Phase 7a).

**Storage panel UI:** `StoragePanelHost` manages N simultaneous `StoragePanelView` panels. Opened via gear strip, world `ViewContainerInteraction`, pocket hotkey (`ToggleInternalClothing` → `ContainerViewer.ShowContainerUI`), or nested click. Panel width follows container columns (1×1 / 2×2 / 3×3…); header drag uses UITK pointer capture (`MachineWindow` pattern) with the host registered on `InputInterface`. Slot drag is UITK pointer-capture; HUD equipment/gear/hand slots register as `HudDropTarget` peers for panel↔HUD transfers. Drop highlight is a static class toggle (no UITK `@keyframes`).

**Main HUD:** equipment doll click = `ClientInteractWithContainerSlot` (equip/unequip vs active hand); gear strip click opens that container's panel; hands select active hand. Cross-surface drag goes through `StoragePanelHost.BeginHudDrag` / `EndHudDrag`. Visibility is owned by `MainHudSubSystem.ApplyVisibility`: local spawned body + in-game round, and **suppressed while** [machine-interface](machine-interface.md) is open (`InterfaceOpened` / `InterfaceClosed`). Show/hide uses the same DOTween bring-up as diegetic MI. Styles/icons/`PanelSettings` load from a committed `MainHudAssetCatalog` via `Resources.Load` (rebuild: **SS3D → Main HUD → Rebuild Asset Catalog**).

**Legacy uGUI purged:** condemned container UI scripts and prefabs under `Systems/UI/Systems/Containers/` removed; `HumanoidInventory` / `StaminaBar` stripped from `PlayerCanvas.prefab`. Hands wiring on `Human.prefab` remains prefab composition debt ([agent-first composition](../2026-07_agent-first-composition.md)).

## Start here

- `Assets/Scripts/SS3D/Systems/Inventory/Items/ItemSubSystem.cs` — item subsystem entry point
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/AttachedContainer.cs` — container primitive
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/AttachedContainerLock.cs` — ID-gated world lock
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/HumanInventory.cs` — on-person containers, `CarriedWeight`
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/ContainerViewer.cs` — server-authoritative open/close
- `Assets/Scripts/SS3D/UI/MainHud/MainHudSubSystem.cs` — HUD bind + equip/gear/hands + `StoragePanelHost` viewer bind
- `Assets/Scripts/SS3D/UI/StoragePanel/StoragePanelHost.cs` — multi-panel manager, HUD drop targets, drag-drop
- `Assets/Content/Systems/UI/StoragePanel/Resources/StoragePanelAssetCatalog.asset` — committed UITK refs

## Extension points

- **New container-opening entry point:** `ContainerViewer.ShowContainerUI(container)` only — do not invent a second open path.
- **New HUD drop peer:** register via `StoragePanelHost.SetHudDropTargets` from Main HUD bind/refresh.
- **New storage panel stylesheet:** path in `StoragePanelAssetPaths`, run **SS3D → Storage Panel → Rebuild Asset Catalog**.

## Pitfalls

- **`ContainerViewer` must never reference `SS3D.UI.*`:** MainHudSubSystem hands the viewer to `StoragePanelHost` at bind/unbind. Do not add Systems→UI asmdef refs.
- **Stack-merge highlight must match `AddStoredItem`:** `CanContainItemAtPosition` treats mergeable occupied stacks as valid — keep in sync with merge room checks.
- **Spawn/round catch-up can re-show HUD over MI:** always route through `ApplyVisibility()` (includes `_machineUiOpen`). Do not call bare `SetVisible(true)` from bind/round handlers.
- **UITK white block:** never put `border-radius` and `overflow: hidden` on the same element — split painted outer vs clip inner (`storage-panel` / `storage-panel__clip`, weight track same). Same rule as [machine-interface](machine-interface.md).
- **Gear-strip panels open above the anchor:** `StoragePanelHost.PositionPanel` flips above when the anchor is near the bottom (belt/ID/PDA/back). Do not set `style.top = gearBound.y` without that clamp — the strip sits on the screen edge.
- **Play Mode / Editor verification still required** for this clean-slate pass (catalog present; compile/Play Mode not run in implementing session).

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [inputs](inputs.md), [id-access](id-access.md), [stamina](stamina.md) (encumbrance consumer), [machine-interface](machine-interface.md) (open/close suppress)
- **Used by:** [examine](examine.md), [player-control](player-control.md), [id-access](id-access.md), [stamina](stamina.md)
- **Catalog pattern:** [ui-shell](ui-shell.md), [machine-interface](machine-interface.md)

## Related docs

- Design (read-only): [inventory-storage.md](../../design/inventory-storage.md), [main-hud.md](../../design/main-hud.md)
- [2026-07_inventory-storage-redesign](../2026-07_inventory-storage-redesign.md)
- [INDEX.md](../INDEX.md)
