# SS13 DMM map import helpers

- `ss13_type_map.yaml` — prefix table used by the Play Mode importer (`SS3D/Map Import/Import DMM…`) and this CLI.
  Covers structural shell (floors/walls/windows/doors) plus infrastructure (cables, pipes,
  disposals, vents/scrubbers, APCs, lights). Pipe `so:` is a fallback; Unity resolves
  supply→`AtmosPipesL1`, scrubbers→`L2`, waste/general/yellow→`L3`, else `piping_layer` 1–4.
- `dmm_gap_report.py` — list frequent unmapped type paths without Unity:

```bash
python3 Tools/map_import/dmm_gap_report.py /path/to/map.dmm
```

- `classic_ministation.py` — rebuilds the 2021 MiniStation (RE-SS3D #857) from git history as
  `Builds/Game/Data/StationTemplates/classic-ministation.json`. Stand-ins, dropped assets and added
  power/spawns live in tables at the top of the script. Run it with `--check` to verify the template.

Do not commit full tgstation maps or converted Box/Meta station templates. Keep fixtures tiny under EditMode tests.
