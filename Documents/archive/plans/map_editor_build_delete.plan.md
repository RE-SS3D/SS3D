---
name: Map editor build delete
overview: Hard-block map editor placement via BuildChecker with toastable reasons; Delete targets the current object-library subcategory (wall-mount face via hologram rotation).
todos:
  - id: preview-reasons
    content: Add BuildFailReason + thread through BuildChecker / TryPreviewTile / PreviewResult; EditMode tests
    status: completed
  - id: hard-block-place
    content: Stop skipBuildCheck in map editor commands; block PlaceOnHolograms on fail; toast + hover hint
    status: completed
  - id: delete-targeting
    content: MapEditorDeleteTargeting from subcategory (+ Direction for wall mounts)
    status: completed
  - id: delete-preview
    content: Delete-tool cursor ghost, scoped clear, drag erase, R face for mounts
    status: completed
  - id: docs
    content: Update tile.md pitfalls + plan notes after ship
    status: completed
isProject: false
---

# Map editor placement validation and scoped delete

Shipped. See [Documents/architecture/systems/tile.md](../../architecture/systems/tile.md) Pitfalls.

## Implementation notes

- `BuildChecker.Evaluate` returns `BuildFailReason[]`; `PreviewResult` carries `Failures` + `PrimaryMessage`.
- Map editor place (`PlaceTileCommand`, `MoveTileCommand`, `RpcPlaceObject`) uses `skipBuildCheck: false`. Client `PlaceOnHolograms` pre-checks and skips invalid cells; toasts primary reason / skipped count.
- Delete / eraser clears only targets from `MapEditorDeleteTargeting` for `CurrentSubcategory` (wall mounts filtered by hologram `Direction`). Overlays clear floor decals; Items mode still raycasts items.
- Delete tool keeps a cursor ghost (prefab of first target when present, else a tinted marker) and supports line / Shift+rectangle drag erase.
