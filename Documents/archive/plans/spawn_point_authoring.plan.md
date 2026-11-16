---
name: Spawn Point Authoring
overview: Map Editor spawn-point place/delete + station-template persistence (authoring only; runtime spawn deferred).
todos:
  - id: data-model
    content: Add SpawnPointRecord / Kind / AntagonistSpawnCategory + SpawnPointRegistry under Tile/SpawnPoints
    status: completed
  - id: persistence
    content: Add SpawnPointPersistenceContributor + DTOs; register in PersistenceSubSystem
    status: completed
  - id: catalog-ui
    content: Catalog spawn:job/antag keys; unblock SpawnPlacements grid; keep other Scripting stubs
    status: completed
  - id: commands-place-delete
    content: Place/ClearSpawnPoint commands, hologram place path, delete targeting, editor pin visuals
    status: completed
  - id: tests
    content: EditMode persistence round-trip (and command invertibility if feasible)
    status: completed
  - id: docs
    content: "update-system-docs: tile, persistence, creative-hooks, effort doc, INDEX, plan"
    status: completed
isProject: false
---

# Spawn Point Authoring (Map Editor + Save)

Implements [creative-mode.md](../../design/creative-mode.md) §8 **authoring + save only**. See architecture effort [2026-07_spawn-point-authoring.md](../architecture/2026-07_spawn-point-authoring.md).

## Implementation notes

- Job catalog tags are hardcoded to current `RoleData` names (`Assistant`, `Security`) via `MapEditorSpawnCatalog.DefaultJobNames` — extend that list when new roles ship.
- `TileMap.Clear` clears the spawn registry so templates without a `spawn-points` chunk cannot leave stale markers.
- EditMode filter `SpawnPointAuthoringTests` could not be batch-run while the Unity Editor held the project lock; tests are in place for the next EditMode run.
