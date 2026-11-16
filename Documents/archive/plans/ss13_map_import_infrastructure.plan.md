---
name: SS13 Map Import Infrastructure
overview: Extend the SS13 DMM importer to place cables, heuristically-layered gas pipes, disposals, vents/scrubbers, APCs, and lights as working SS3D prefabs.
todos:
  - id: kinds-yaml
    content: Add MapImportKind values + ss13_type_map.yaml prefixes for cable/pipe/disposal/vent/scrubber/APC/light
    status: completed
  - id: pipe-heuristic
    content: Implement ResolvePipeSo (supply→L1, scrubbers→L2, waste/general→L3, piping_layer 1–4 fallback)
    status: completed
  - id: planner-overlays
    content: Rewrite MapImportPlanner for structural + multi-layer overlays; FurnitureBase priority; wall-mount dirs
    status: completed
  - id: applier-area
    content: Verify/post-hook area rebuild after APC+cable import; keep adjacency refresh
    status: completed
  - id: tests-cli-docs
    content: tiny_infra fixture + planner tests; sync dmm_gap_report.py; architecture/plan/tile.md via update-system-docs
    status: completed
isProject: false
---

# SS13 DMM infrastructure import

See architecture effort [2026-07_ss13-map-import-infrastructure.md](../architecture/2026-07_ss13-map-import-infrastructure.md).

Extends the structural shell importer with multi-placement overlays and pipe-layer heuristic collapse onto `AtmosPipesL1`–`L4`.

## Implementation notes

- Pipe network type in the SS13 path beats `piping_layer`; generic pipes without either default to L3.
- FurnitureBase is single-occupant — Vent wins over Scrubber over DisposalBin over DisposalOutlet on the same cell.
- Applier uses `AreaSubSystem.BeginDeferredAreaFlood` / `EndDeferredAreaFlood` so APC flood-fill runs after all walls exist.
- Wall mounts (APC / lights) keep BYOND `dir` via `ResolveDirection`.
