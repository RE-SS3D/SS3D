> Code paths: Assets/Scripts/SS3D/Data/Persistence/, Assets/Scripts/SS3D/Systems/Persistence/
> Entry points: PersistenceSubSystem, IPersistenceContributor, EnvelopePersistenceStore
> Status: partial

# Persistence

## Overview

Layered contributor-based disk persistence for station templates and server meta. `PersistenceSubSystem` orchestrates ordered save/load via `IPersistenceContributor` implementations and wraps payloads in versioned `PersistenceEnvelope` files. Station templates split tilemap structure and area metadata; server meta currently covers admin permissions (with legacy `permissions.txt` import) and append-only round history JSONL. Round-config map pools and round snapshots are planned.

## Start here

- `Assets/Scripts/SS3D/Data/Persistence/PersistenceEnvelope.cs` — envelope wrapper and schema version
- `Assets/Scripts/SS3D/Data/Persistence/EnvelopePersistenceStore.cs` — JSON file I/O via `LocalStorage`
- `Assets/Scripts/SS3D/Data/Persistence/PersistencePaths.cs` — `StationTemplates/`, `ServerMeta/`, legacy `Tilemaps/`
- `Assets/Scripts/SS3D/Systems/Persistence/PersistenceSubSystem.cs` — orchestrator, lifecycle events
- `Assets/Scripts/SS3D/Systems/Persistence/TileMapPersistenceContributor.cs` — tilemap + items chunk
- `Assets/Scripts/SS3D/Systems/Persistence/AreaPersistenceContributor.cs` — area metadata chunk
- `Assets/Scripts/SS3D/Systems/Persistence/PermissionsPersistenceContributor.cs` — admin permissions chunk
- `Assets/Scripts/SS3D/Systems/Persistence/RoundHistoryStore.cs` — append-only round history JSONL
- `Assets/Scripts/SS3D/Systems/Persistence/LegacyTileMapMigrator.cs` — flat tilemap JSON → envelope
- `Assets/Scripts/SS3D/Systems/Persistence/LegacyPermissionsMigrator.cs` — `permissions.txt` → payload
- `Assets/Scripts/SS3D/Data/Persistence/SavedPermissionsPayload.cs` — permissions envelope DTO
- `Assets/Scripts/Tests/EditMode/PersistenceFrameworkTests.cs` — envelope round-trip, legacy tilemap migration, load order
- `Assets/Scripts/Tests/EditMode/ServerMetaPersistenceTests.cs` — permissions migration and round-history append

## Extension points

- New domain: implement `IPersistenceContributor`, register in `PersistenceSubSystem.RegisterBuiltInContributors()` or call `RegisterContributor` at startup.
- Station template save/load: `SaveStationTemplate` / `LoadStationTemplate` / `LoadMostRecentStationTemplate`.
- Server meta: `LoadServerMeta` (server boot via `TileSubSystem`), `SaveServerMeta` (on `UserPermissionsChangedEvent`).
- Round history: `AppendRoundHistory` — hooked from `RoundSubSystem.ProcessEndRound`.
- **Deferred:** `RoundConfigPersistenceContributor` (blocked on round-config), round snapshot contributors (Phase 2), round-start `LoadStationTemplate(mapId)` from config pool.

## Depends on / Used by

- **Depends on:** [data-codegen](data-codegen.md) (`LocalStorage`), [permissions](permissions.md), [tile](tile.md), [area](area.md), [rounds-lobby](rounds-lobby.md), [gamemodes-roles-traits](gamemodes-roles-traits.md) (`CurrentGamemodeName` for round history)
- **Used by:** [tile](tile.md) (save/load backend), [permissions](permissions.md) (import/export), [rounds-lobby](rounds-lobby.md) (round-end history)

## Related docs

- Plan: [persistence_architecture_design_2fe61864.plan.md](../../plans/persistence_architecture_design_2fe61864.plan.md)
- Design (read-only): [round-config.md](../../design/round-config.md) (blocks map pool contributor)
