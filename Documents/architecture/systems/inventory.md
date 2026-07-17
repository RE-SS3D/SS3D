> Code paths: Assets/Scripts/SS3D/Systems/Inventory/, Assets/Scripts/SS3D/UI/MainHud/, Assets/Scripts/SS3D/UI/StoragePanel/
> Entry points: ItemSubSystem, MainHudSubSystem, StoragePanelHost
> Status: partial
> Verified: b342d874 — 2026-07-17

# Inventory

## Overview

Items, containers, hands, identification cards (`IDCard`, `PDA`), and the on-demand storage panel UI. ID cards bind to server-side crew records via [id-access](id-access.md); spawn-time binding in `RoleSubSystem`.

**Container primitive:** `AttachedContainer` — position-grid slots (`Size.x × Size.y`), a `SizeClass` fit ceiling (`MaxSizeClass`), recursive computed `Weight` (own items' weight × stack count, plus nested containers' own recursive weight — never cached), a `MaxWeight` display ceiling for the panel's readout, and stacking (`Item.MaxStackSize`/`StackCount`, merged on add via `AddStoredItem`; splitting a stack back apart is not built). An optional `AttachedContainerLock` component ID-gates world containers (lockers/crates) via `IdAccessSubSystem.CheckAccess` — personal worn containers never get one. See [inventory-storage.md](../../design/inventory-storage.md).

**Storage panel UI (new):** `StoragePanelHost` (self-bootstrapped `SubSystem`, own `UIDocument`) manages any number of simultaneously open `StoragePanelView` instances — a worn container, a world locker, and a nested lockbox opened from that locker can all be visible at once. Opened via the gear strip (`HandsGearStrip` belt/ID/PDA/back clicks) or a world container's existing `ViewContainerInteraction`; both paths converge on `ContainerViewer.ShowContainerUI`, which `StoragePanelHost` listens to directly. Slot-to-slot drag-and-drop is a **new UITK pointer-capture implementation** (`StorageSlot`), not a reuse of the world-space `InteractionTier.Combine` grammar — see the architecture effort doc's "Scope decisions" for why. Drop-state highlighting is a static class toggle, not an animated pulse (UI Toolkit has no CSS `@keyframes`).

**Condemned UI (partially purged):** the old uGUI container UI scripts (`ContainerUi`, `ContainerView`, `ItemGrid`, `SingleItemContainerSlot`, etc.) are deleted. The prefab assets are not — `HumanoidInventory.prefab` is nested inside `Assets/Content/Systems/UI/Lobby/Canvas/PlayerCanvas.prefab`; removing that nested instance and the now-orphaned leaf prefabs needs the Unity Editor (see [2026-07_inventory-storage-redesign.md](../2026-07_inventory-storage-redesign.md)). Hands wiring on `Human.prefab` remains prefab composition debt ([agent-first composition](../2026-07_agent-first-composition.md)).

**Main HUD (partial):** `SS3D.UI.MainHud` shows worn equipment (incl. gloves), gear strip, hands, and intent. Hold-to-self-examine from [main-hud.md](../../design/main-hud.md) §5 is **not** in the HUD — deferred to the general examine surface. Hand-well clicks call `HumanInventory.ActivateHand`; gear-strip clicks open a `StoragePanelHost` panel for that slot's container. The HUD `UIDocument` is registered with [InputInterface](inputs.md) so pointer-over-HUD blocks world clicks. Visibility binds on `LocalPlayerObjectChanged` / `SpawnedPlayersUpdated` with a catch-up path so pure clients that miss the first mind-sync event still show the HUD. Styles/icons/`PanelSettings` load from a committed `MainHudAssetCatalog` via `Resources.Load` (rebuild: **SS3D → Main HUD → Rebuild Asset Catalog**) — Editor-only `AssetDatabase` is fallback only; standalone builds need the catalog asset. `MainHudSubSystem` also owns handing `StoragePanelHost` the local player's `ContainerViewer` at bind/unbind time (see Pitfalls — this indirection exists to avoid a circular assembly reference).

## Start here

- `Assets/Scripts/SS3D/Systems/Inventory/Items/ItemSubSystem.cs` — item subsystem entry point
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/AttachedContainer.cs` — container primitive (weight/size-class/stacking/lock hook)
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/AttachedContainerLock.cs` — minimal ID-gated lock for world containers
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/HumanInventory.cs` — on-person containers and hands
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/ContainerViewer.cs` — server-authoritative per-client open/close (Systems layer; no UI dependency)
- `Assets/Scripts/SS3D/Systems/Inventory/Items/Identification/IDCard.cs` — physical ID token
- `Assets/Scripts/SS3D/Systems/Inventory/Items/Identification/PDA.cs` — PDA with internal ID slot
- `Assets/Scripts/SS3D/UI/MainHud/MainHudSubSystem.cs` — Main HUD overlay bootstrap + bind; also binds `StoragePanelHost` to the local player's `ContainerViewer`
- `Assets/Scripts/SS3D/UI/StoragePanel/StoragePanelHost.cs` — multi-panel manager, drag-and-drop, nested-container opening
- `Assets/Scripts/SS3D/UI/StoragePanel/StoragePanelView.cs` — one panel (header/weight bar/slot grid/breadcrumb)
- `Assets/Content/Systems/UI/MainHud/Resources/MainHudAssetCatalog.asset` — committed UITK refs for builds
- `Assets/Content/Systems/UI/StoragePanel/Resources/StoragePanelAssetCatalog.asset` — committed UITK refs for builds (must be generated via the Editor rebuild menu — not yet built in this remote session, see Pitfalls)

## Extension points

- **New Main HUD stylesheet/icon:** add a path in `MainHudAssetPaths`, assign on `MainHudAssetCatalog`, run **SS3D → Main HUD → Rebuild Asset Catalog**, commit the SO (do not rely on Editor `AssetDatabase` in `MainHudSubSystem`). Do not invent a third Resources-catalog stack — shared helper is deferred under [ui-shell](ui-shell.md) § Future work.
- **New storage panel stylesheet:** add a path in `StoragePanelAssetPaths`, run **SS3D → Storage Panel → Rebuild Asset Catalog**, commit the SO.
- **New container-opening entry point:** call `ContainerViewer.ShowContainerUI(container)` (server-authoritative, same path gear strip and world containers already use) — do not invent a second open path.

## Pitfalls

- **`StoragePanelAssetCatalog` was never generated:** this effort was implemented with no local Unity Editor. `StoragePanelHost` will log an explicit error and disable itself until **SS3D → Storage Panel → Rebuild Asset Catalog** is run once and the resulting `.asset` is committed — same failure mode as `MainHudAssetCatalog`/`MachineUiAssetCatalog` before their first rebuild, not a silent null.
- **`HumanoidInventory.prefab` still nested in `PlayerCanvas.prefab`:** the condemned UI *scripts* are deleted (their components on that prefab now show "Missing Script" — expected, harmless) but the *prefab asset* itself was left alone rather than hand-editing `PlayerCanvas.prefab`'s nested-`PrefabInstance` YAML blind. Needs a one-time Editor cleanup: remove the nested instance, then delete the now-orphaned prefabs under `Assets/Content/Systems/UI/Systems/Containers/`.
- **`ContainerViewer` must never reference `SS3D.UI.*` types:** the real asmdef chain is `SS3D.Systems` → `SS3D.UI` → ... → `SS3D.UI.MachineInterface` → `SS3D.UI.StoragePanel` → `SS3D.UI.MainHud` (confirmed by grepping GUID cross-references between `.asmdef` files, not by trusting folder layout). `StoragePanelHost` cannot discover its own local player the way `MainHudSubSystem` does — it's handed the `ContainerViewer` reference by `MainHudSubSystem` at bind/unbind time instead. Don't "fix" this by adding a reference from `SS3D.Systems` to a UI assembly — that's the circular reference this indirection avoids.
- **Stack-merge and drop-highlight math must stay in sync:** `AttachedContainer.CanContainItemAtPosition` (drives the drag-drop valid/invalid highlight) special-cases an occupied slot as valid when it holds a mergeable stack — this must mirror `AddStoredItem`'s actual merge condition (`CanMergeWith` + remaining `MaxStackSize` room) or the highlight will lie about what a drop will do.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [inputs](inputs.md) (UITK document registry / pointer-over-UI), [id-access](id-access.md) (container locks)
- **Used by:** [examine](examine.md), [player-control](player-control.md), [id-access](id-access.md)
- **Catalog pattern:** [ui-shell](ui-shell.md), [machine-interface](machine-interface.md)

## Related docs

- Design (read-only): [Documents/design/inventory-storage.md](../../design/inventory-storage.md), [main-hud.md](../../design/main-hud.md)
- [2026-07_inventory-storage-redesign](../2026-07_inventory-storage-redesign.md) — this effort (Status: in-progress — verification/prefab cleanup pending)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md), [2026-07_mi-path-catalog](../2026-07_mi-path-catalog.md)
- [INDEX.md](../INDEX.md)
