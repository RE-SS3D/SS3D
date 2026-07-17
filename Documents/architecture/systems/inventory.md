> Code paths: Assets/Scripts/SS3D/Systems/Inventory/, Assets/Scripts/SS3D/UI/MainHud/
> Entry points: ItemSubSystem, MainHudSubSystem
> Status: partial
> Verified: 624906c2b — 2026-07-18

# Inventory

## Overview

Items, containers, hands, and identification cards (`IDCard`, `PDA`). ID cards bind to server-side crew records via [id-access](id-access.md); spawn-time binding in `RoleSubSystem`.

**Condemned UI:** inventory / hands / intent uGUI — do not extend; replace per [main-hud.md](../../design/main-hud.md) and [inventory-storage.md](../../design/inventory-storage.md) with Phase 0 purge. Domain items/containers may remain until that redesign. Hands wiring on `Human.prefab` is prefab composition debt ([agent-first composition](../2026-07_agent-first-composition.md)).

**Main HUD (partial):** `SS3D.UI.MainHud` shows worn equipment (incl. gloves), gear strip, hands, and intent. Hold-to-self-examine from [main-hud.md](../../design/main-hud.md) §5 is **not** in the HUD — deferred to the general examine surface. Hand-well clicks call `HumanInventory.ActivateHand` (same as legacy slots). The HUD `UIDocument` is registered with [InputInterface](inputs.md) so pointer-over-HUD blocks world clicks. Visibility is owned by `MainHudSubSystem.ApplyVisibility`: local spawned body + in-game round, and **suppressed while** [machine-interface](machine-interface.md) is open (`InterfaceOpened` / `InterfaceClosed`). Show/hide uses the same DOTween bring-up as diegetic MI (opacity, scale, lift). Fork divergence from design (screens “on top” of HUD): chrome hides for diegetic focus. Styles/icons/`PanelSettings` load from a committed `MainHudAssetCatalog` via `Resources.Load` (rebuild: **SS3D → Main HUD → Rebuild Asset Catalog**) — Editor-only `AssetDatabase` is fallback only; standalone builds need the catalog asset.

## Start here

- `Assets/Scripts/SS3D/Systems/Inventory/Items/ItemSubSystem.cs` — item subsystem entry point
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/HumanInventory.cs` — on-person containers and hands
- `Assets/Scripts/SS3D/Systems/Inventory/Items/Identification/IDCard.cs` — physical ID token
- `Assets/Scripts/SS3D/Systems/Inventory/Items/Identification/PDA.cs` — PDA with internal ID slot
- `Assets/Scripts/SS3D/UI/MainHud/MainHudSubSystem.cs` — Main HUD overlay bootstrap + bind
- `Assets/Content/Systems/UI/MainHud/Resources/MainHudAssetCatalog.asset` — committed UITK refs for builds

## Extension points

- **New Main HUD stylesheet/icon:** add a path in `MainHudAssetPaths`, assign on `MainHudAssetCatalog`, run **SS3D → Main HUD → Rebuild Asset Catalog**, commit the SO (do not rely on Editor `AssetDatabase` in `MainHudSubSystem`). Do not invent a third Resources-catalog stack — shared helper is deferred under [ui-shell](ui-shell.md) § Future work.

## Pitfalls

- **HUD works in Editor Play Mode, missing in player builds:** `MainHudSubSystem` self-bootstraps with no SerializeFields; Editor used to fill via `AssetDatabase`. Builds need `Resources/MainHudAssetCatalog` — run **SS3D → Main HUD → Rebuild Asset Catalog** and commit the asset (same pattern as Machine UI; second copy of that stack).
- **Spawn/round catch-up can re-show HUD over MI:** always route through `ApplyVisibility()` (includes `_machineUiOpen`). Do not call bare `SetVisible(true)` from bind/round handlers. Do not hide HUD from `MachineInterfaceHost` — MainHud observes MI events (asmdef direction).

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [inputs](inputs.md) (UITK document registry / pointer-over-UI), [machine-interface](machine-interface.md) (open/close suppress)
- **Used by:** [examine](examine.md), [player-control](player-control.md), [id-access](id-access.md)
- **Catalog pattern:** [ui-shell](ui-shell.md), [machine-interface](machine-interface.md)

## Related docs

- Design (read-only): [Documents/design/inventory-storage.md](../../design/inventory-storage.md), [main-hud.md](../../design/main-hud.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md), [2026-07_mi-path-catalog](../2026-07_mi-path-catalog.md)
- [INDEX.md](../INDEX.md)