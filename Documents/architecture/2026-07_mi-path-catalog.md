> Implements: Documents/architecture/2026-07_agent-first-composition.md follow-on (b) wedge — MI path catalog only
> Touches systems: machine-interface, ui-shell
> Status: shipped

# Machine UI path catalog

Replaces `MachineInterfaceHost` per-template SerializeFields and Editor-only `EnsureEditorAssets` with path constants plus a committed `MachineUiAssetCatalog` ScriptableObject loaded via `Resources.Load`. New machine panels no longer require Game.unity edits; player builds resolve templates without Editor-only loads.

## What shipped

- `MachineUiAssetPaths` — stable path constants for templates, shared tokens, component USS lists, panel settings
- `MachineUiAssetCatalog` — committed asset under `Assets/Content/Systems/UI/MachineInterface/Resources/`
- Editor: `SS3D → Machine Interface → Rebuild Asset Catalog` (+ batch `RebuildCatalogAndClearHost`)
- `MachineInterfaceHost` loads the catalog at runtime; scene host keeps `UIDocument` (+ `_document`) only
- Game.unity stripped of obsolete template SerializeFields

## Agent checklist (new machine UI)

1. Add UXML/USS under `Content/Systems/UI/MachineInterface`
2. Add paths to `MachineUiAssetPaths` and registration in `MachineUiCatalog.RegisterAll`
3. Run **SS3D → Machine Interface → Rebuild Asset Catalog** and commit the catalog asset
4. Snapshot / binder / controller / network registry as before — no Game.unity wiring for templates

## Explicit non-goals (still deferred)

- Full UiShell / folding radial + armed
- Subsystem bootstrap / moving MI host off Game.unity
- Addressables
- **Shared catalog helper** — Main HUD later copied this Paths + Resources SO + rebuild-menu stack (`MainHudAssetCatalog`). Unifying MI + Main HUD (and blocking a third copy) is follow-on (b) work, documented under [ui-shell.md](systems/ui-shell.md) § Future work — not part of this MI-only wedge.

## Implementation notes

- Initial catalog asset was authored from path/guid data when Unity batchmode could not open the project (another Editor instance held the lock). Prefer **Rebuild Asset Catalog** in the Editor after any path change.
- `SS3D.Editor` must reference `SS3D.Core` for `MachineUiAssetCatalogBuilder` scene cleanup (`MachineInterfaceHost` extends `View`).

## Related docs

- Policy: [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md)
- Map: [systems/machine-interface.md](systems/machine-interface.md), [systems/ui-shell.md](systems/ui-shell.md), [systems/inventory.md](systems/inventory.md) (sibling Main HUD catalog)