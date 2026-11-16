> Implements: infrastructure — map scaffolding for tile / creative-mode authoring (not a design-doc slice)
> Touches systems: tile, area, electricity, atmospherics, disposal, persistence (save path only)
> Status: shipped

# SS13 DMM infrastructure import

## Goal

Extend the structural DMM importer so cables, heuristically-layered gas pipes, disposals, vents/scrubbers, APCs, and lights place as working SS3D prefabs — enough for Metastation imports to flood areas and light rooms after save.

## What shipped

- New `MapImportKind` values: Cable, Pipe, Disposal, DisposalTerminal, Vent, Scrubber, Apc, Light
- `MapImportPipeResolver` — supply→`AtmosPipesL1`, scrubbers→`AtmosPipesL2`, waste/general/yellow→`AtmosPipesL3`, else `piping_layer` 1–4 → L1–L4
- `MapImportDirection` — `/directional/…` path suffixes, `dir`/pixel vars; wall mounts offset onto wall tile facing into room; doors without dir inferred from walls (scan through door runs)
- Planner multi-placement per cell (Plenum + structural + overlays); FurnitureBase priority Vent > Scrubber > DisposalBin > DisposalOutlet
- Type-map prefixes in `Tools/map_import/ss13_type_map.yaml`; CLI gap report mirrors kinds
- `MapImportApplier` defers area flood (`BeginDeferredAreaFlood` / `EndDeferredAreaFlood`) around bulk place so APC seeds see complete walls
- EditMode fixture `tiny_infra.dmm` + planner/pipe resolver tests

## Follow-on (lattices / tables / SMES)

Shipped in-tree after this effort: `MapImportKind` Lattice/Table/Smes; lattice/catwalk replace Plenum (space+lattice cells import); wood→`TableWood`, other tables→`TableSteel`; SMES as FurnitureBase (priority above Vent). Fixture `tiny_furniture.dmm`. See [tile](../../architecture/systems/tile.md) Extension points / Pitfalls.

## Explicit non-goals

- Remaining furniture / machines / items / door access (chairs, racks, closets, etc.)
- Air alarms, solar, full atmos component set (binary pumps, etc.)
- Perfect SS13 `piping_layer` fidelity (network-type collapse)
- Distinct glass/poker/reinforced table SOs (non-wood tables collapse to `TableSteel`)
- Vendoring tgstation maps or committing converted Meta templates

## Key files

- `Assets/Scripts/SS3D/Systems/Tile/MapImport/` (planner, pipe resolver, applier, mapper)
- `Tools/map_import/ss13_type_map.yaml`
- `Assets/Scripts/Tests/EditMode/MapImport/`

## Related

- Prior shell: [2026-07_ss13-map-import.md](2026-07_ss13-map-import.md)
- Plan: [ss13_map_import_infrastructure.plan.md](../plans/ss13_map_import_infrastructure.plan.md)
