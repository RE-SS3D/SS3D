# SS13 DMM map import helpers

- `ss13_type_map.yaml` — prefix table used by the Play Mode importer (`SS3D/Map Import/Import DMM…`) and this CLI.
  Covers structural shell (floors/walls/windows/doors) plus infrastructure (cables, pipes,
  disposals, vents/scrubbers, APCs, lights). Pipe `so:` is a fallback; Unity resolves
  supply→`AtmosPipesSupply`, scrubbers→`AtmosPipesMixed`, waste/general/yellow→`AtmosPipesWaste`, else `piping_layer` 1–4.
- `dmm_gap_report.py` — list frequent unmapped type paths without Unity:

```bash
python3 Tools/map_import/dmm_gap_report.py /path/to/map.dmm
```

- `asset_ids.py` — the asset name → prefab id table, read from the asset files without Unity. Saved maps (schema 2)
  store that id beside each name. Run it on any map file written outside the game to stamp the ids:

```bash
python3 Tools/map_import/asset_ids.py Builds/Game/Data/StationTemplates/*.json Testing/fixtures/stations/*.json
```

- `classic_ministation.py` — the one-time build of the 2021 MiniStation (RE-SS3D #857) from git history.
  `Builds/Game/Data/StationTemplates/classic-ministation.json` is the source of truth now, edited in the
  map editor. The script only writes with `--overwrite-map-edits`, which loses those edits.

Do not commit full tgstation maps or converted Box/Meta station templates. Keep fixtures tiny under EditMode tests.
