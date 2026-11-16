> Implements: Documents/design/creative-mode.md §8 (authoring + save only)
> Touches systems: tile/map-editor, persistence
> Status: shipped

# Spawn Point Authoring

## Goal

Map Editor Scripting → Spawn Placements can place job- and antagonist-tagged markers; they persist in station templates. Runtime role→spawn resolution stays on `EntitySubSystem._spawnPoint` for a follow-on.

## What shipped

- `SpawnPointRegistry` on `TileSubSystem` (one marker per tile; multiple markers per tag)
- Catalog keys `spawn:job:{RoleName}` / `spawn:antag:{Category}` (Assistant/Security + Traitor/Malf/Nuke Op)
- Undoable `PlaceSpawnPoint` / `ClearSpawnPoint` commands (plenum required)
- `SpawnPointPersistenceContributor` (`spawn-points` chunk, load order 110)
- Editor pin visuals while Map Editor is open
- EditMode: `SpawnPointAuthoringTests`

## Explicit non-goals (still open)

- `EntitySubSystem` job-aware spawn pick
- Round-config map-pool coverage validation
- RandomSpawners / Triggers / Atmospherics Scripting rails

## Key files

- `Assets/Scripts/SS3D/Systems/Tile/SpawnPoints/`
- `Assets/Scripts/SS3D/Systems/Tile/MapEditor/MapEditorSpawnCatalog.cs`
- `Assets/Scripts/SS3D/Systems/Persistence/SpawnPointPersistenceContributor.cs`
