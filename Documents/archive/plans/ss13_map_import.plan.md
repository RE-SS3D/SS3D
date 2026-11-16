---
name: SS13 Map Importer
overview: Build a v1 SS13 `.dmm` importer that places floors, walls, windows, and doors into the live tilemap via the Map Editor, saves as a normal station template, and emits an unmapped-type gap report.
todos:
  - id: arch-doc
    content: Add Documents/architecture/2026-07_ss13-map-import.md + Documents/plans entry linking tile/persistence
    status: completed
  - id: dmm-parser
    content: Implement DmmParser + tiny fixture; EditMode parse tests
    status: completed
  - id: type-map
    content: Ship ss13_type_map.yaml (floors/walls/windows/doors) + TypeMapper + unmapped report
    status: completed
  - id: placement-plan
    content: Build placement plan DTO (Plenum + SO + dir per cell); EditMode plan tests
    status: completed
  - id: playmode-import
    content: "Tier-A Map Import entry: file pick, optional bbox, place via TileMap, log summary"
    status: completed
  - id: docs-sync
    content: Update tile.md Extension points/Pitfalls; update-system-docs on ship
    status: completed
isProject: false
---

# SS13 DMM structural map importer

See architecture effort [2026-07_ss13-map-import.md](../architecture/2026-07_ss13-map-import.md).

Implements floors/walls/windows/doors import from SS13 `.dmm` into the live tilemap; gap report drives progressive content. Persistence save path unchanged (Map Editor Save).

## Implementation notes

- Type-map `so:` values must be `GenericObjectSo.NameString` (prefab name), e.g. `FancyCarpetRed` not `TileCarpet`.
- Play Mode entry is `SS3D/Map Import/Import DMM…` (`MapImportWindow`).
- EditMode: `DmmParserTests`, `MapImportPlannerTests` + `Fixtures/tiny_box.dmm`.
- Infrastructure follow-on: [ss13_map_import_infrastructure.plan.md](ss13_map_import_infrastructure.plan.md) / [2026-07_ss13-map-import-infrastructure.md](../architecture/2026-07_ss13-map-import-infrastructure.md).
