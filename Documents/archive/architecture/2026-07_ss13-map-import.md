> Implements: infrastructure — map scaffolding for tile / creative-mode authoring (not a design-doc slice)
> Touches systems: tile, persistence (save path only)
> Status: shipped

# SS13 DMM structural map import

## Goal

Import an SS13 `.dmm` as a walkable SS3D shell (plenum + floors + walls/windows + doors) via Play Mode Map Editor, save as a normal station template, and emit an unmapped-type gap report for progressive content backfill.

## What shipped

- Runtime-safe `DmmParser` + `Ss13TypeMapper` + `MapImportPlanner` under `Assets/Scripts/SS3D/Systems/Tile/MapImport/`
- Editable prefix table `Tools/map_import/ss13_type_map.yaml`
- Tier-A menu `SS3D/Map Import/Import DMM…` (requires Play Mode + `TileSubSystem`)
- EditMode tests + synthetic `tiny_box.dmm` fixture
- Optional CLI gap report: `Tools/map_import/dmm_gap_report.py`

## Explicit non-goals (at ship time)

- SS14 `.yml` maps
- Furniture, machines, items (beyond infrastructure follow-on)
- Access on doors
- Vendoring full tgstation maps or committing converted Box/Meta templates

**Follow-on (shipped):** cables, pipes, disposals, vents/scrubbers, APCs, lights — [2026-07_ss13-map-import-infrastructure.md](2026-07_ss13-map-import-infrastructure.md).

## Key files

- `Assets/Scripts/SS3D/Systems/Tile/MapImport/`
- `Assets/Scripts/SS3D/Systems/Tile/MapImport/Editor/MapImportWindow.cs`
- `Tools/map_import/ss13_type_map.yaml`
- `Assets/Scripts/Tests/EditMode/MapImport/`
