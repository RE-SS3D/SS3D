> Code paths: Assets/Scripts/SS3D/Data/Persistence/, Assets/Scripts/SS3D/Systems/Persistence/
> Entry points: PersistenceSubSystem, IPersistenceContributor, EnvelopePersistenceStore
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Persistence

## Overview

Layered contributor-based disk persistence for station templates and server meta. `PersistenceSubSystem` orchestrates ordered save/load via `IPersistenceContributor` implementations and wraps payloads in versioned `PersistenceEnvelope` files. Station templates split tilemap structure, area metadata (including authored and APC-less flags), and authored spawn markers; server meta currently covers admin permissions (imported from `Config/permissions.txt`, or the tracked `permissions.default.txt` when no live file exists yet), append-only round history JSONL and the lobby server name/MOTD (`ServerMeta/server-info`, owned by `RoundSubSystem`). `ServerMeta/` also holds JSONL written by other domains: `reset-health` (round observability), `bugs` and `bug-screenshots/` (bug reports). Round-config map pools and round snapshots are planned.

## Start here

- `Assets/Scripts/SS3D/Data/Persistence/PersistenceEnvelope.cs` — envelope wrapper and schema version
- `Assets/Scripts/SS3D/Data/Persistence/EnvelopePersistenceStore.cs` — JSON file I/O via `LocalStorage`
- `Assets/Scripts/SS3D/Data/Persistence/PersistencePaths.cs` — `StationTemplates/`, `ServerMeta/` (permissions, round-history, server-info, reset-health, bugs), legacy `Tilemaps/`
- `Assets/Scripts/SS3D/Systems/Persistence/PersistenceSubSystem.cs` — orchestrator, lifecycle events; station restore resets world-readiness epoch and notifies `TileMapLoaded` after contributors + deferred area flood; large templates use `LoadStationTemplateAsync` (time-sliced place + bulk mutation mute). Successful saves/loads raise `StationTemplateSaved` / `StationTemplateLoaded` (the map editor's save point); a second restore is refused while one runs, and saves fail after a partial restore until a full load succeeds. `CaptureStationTemplateJson` returns the current map as JSON without writing (bug-report bundles)
- `Assets/Scripts/SS3D/Data/Persistence/MapFileSharing.cs` — map sharing: `TryExport` zips a template into `Data/Exports/<name>.zip`; `ImportDroppedArchives` unpacks zips dropped into `StationTemplates/` (run by `ListStationTemplates`; name clashes get ` (2)`; processed zips move to `StationTemplates/ImportedZips/`); `Reveal` opens the OS file manager
- `Assets/Scripts/SS3D/Systems/Persistence/TileMapPersistenceContributor.cs` — tilemap + items chunk
- `Assets/Scripts/SS3D/Systems/Persistence/AreaPersistenceContributor.cs` — area metadata chunk
- `Assets/Scripts/SS3D/Systems/Persistence/SpawnPointPersistenceContributor.cs` — spawn markers chunk (`spawn-points`, load order 110)
- `Assets/Scripts/SS3D/Systems/Tile/SpawnPoints/SavedSpawnPointRecord.cs` — spawn DTO payload
- `Assets/Scripts/SS3D/Systems/Persistence/GasMarkerPersistenceContributor.cs` — gas markers chunk (`gas-markers`, load order 115)
- `Assets/Scripts/SS3D/Systems/Persistence/PermissionsPersistenceContributor.cs` — admin permissions chunk
- `Assets/Scripts/SS3D/Systems/Persistence/RoundHistoryStore.cs` / `RoundHistoryEntry.cs` — append-only round history JSONL and its row
- `Assets/Scripts/SS3D/Systems/Persistence/LegacyTileMapMigrator.cs` — flat tilemap JSON → envelope
- `Assets/Scripts/SS3D/Systems/Persistence/LegacyPermissionsMigrator.cs` — `permissions.txt` → payload
- `Assets/Scripts/SS3D/Data/Persistence/SavedPermissionsPayload.cs` — permissions envelope DTO
- `Assets/Scripts/Tests/EditMode/PersistenceFrameworkTests.cs` — envelope round-trip, legacy tilemap migration, load order
- `Assets/Scripts/Tests/EditMode/SpawnPointAuthoringTests.cs` — spawn registry / DTO / command invertibility
- `Assets/Scripts/Tests/EditMode/ServerMetaPersistenceTests.cs` — permissions migration and round-history append
- `Assets/Scripts/Tests/EditMode/MapFileSharingTests.cs` — export / dropped-zip import

## Extension points

- Authored areas: tile chunk `areaIds` are restored as saved when area records carry `authored`; APC-less records carry `noApc`. The area payload also stores a map-level `authored` flag so deleting all rooms does not regenerate them on load; legacy flat saves carry `hasAuthoredAreas` through migration. Missing flags keep legacy APC flood defaults. See [area-authoring](area-authoring.md).

- New domain: implement `IPersistenceContributor`, register in `PersistenceSubSystem.RegisterBuiltInContributors()` or call `RegisterContributor` at startup; add a `DeserializePayload` arm for the DTO type.
- Station template save/load: `SaveStationTemplate` / `LoadStationTemplate` / `LoadMostRecentStationTemplate`.
- Server meta: `LoadServerMeta` (owned by `PersistenceSubSystem.OnStart` when server), `SaveServerMeta` (on `UserPermissionsChangedEvent`).
- Round history: `AppendRoundHistory` — hooked from `RoundSubSystem.ProcessEndRound` with the fixed `RoundOutcome`: mode, map, player count, duration, `trigger`, `winner`, `operativeCkeys` and `fallbackMap` (true only when P2's Extended fallback ran).
- **Deferred:** `RoundConfigPersistenceContributor` (blocked on round-config), round snapshot contributors (Phase 2), round-start `LoadStationTemplate(mapId)` from config pool.

- Placed tile objects and items (schema 2) save `id`, the prefab's `ObjectAssetReference.Id`, beside the name; loading resolves by id, then by name through `RenamedTileObjects` (`SavedAssetLookup`). A map file written outside the game (`Tools/map_import/`, fixtures) gets its ids from `python3 Tools/map_import/asset_ids.py <files>`; `SavedAssetLookupTests.ShippedMaps_StoreAnIdMatchingEachName` fails on a shipped map without them.

## Pitfalls

- **Station template restore blocks FishNet if sync:** MetaStation-scale `TileMap.Load` without yields freezes Host. Play Mode / Map Editor use `LoadStationTemplateAsync` → `LoadRoutine` + `BeginBulkMutation`. Sync `LoadStationTemplate` remains for EditMode tests only. Hit 2026-07-28.
- **Empty `Data/Tilemaps` (and no StationTemplates) logs `No station templates found to load`.** Fresh Unity player builds have neither tree; loadable releases must ship the fixtures in `Builds/Game/Data/` (`StationTemplates/`, legacy `Tilemaps/`) next to the binary (see [data-codegen](data-codegen.md) Paths pitfall / [CI develop-release](../2026-07_ci-develop-release-pipeline.md)). Smoke seeds the same path when staging.
- **Contributor registration vs LoadServerMeta:** register built-in contributors in `OnAwake`, not `OnStart`. Hub spawn can run other systems' `OnStartServer` before Unity `Start`; LoadServerMeta itself runs from Persistence `OnStart` after all Awakes, so `PermissionSubSystem` is already registered for restore. Do not call `LoadServerMeta` from Tile or other domains.
- **Missing spawn chunk leaves stale markers:** tilemap restore calls `TileMap.Clear`, which clears `TileSubSystem.SpawnPoints`. Do not remove that clear — templates without `spawn-points` must start empty.
- **Station restore epoch:** `RestoreStationTemplate` calls `WorldReadinessSubSystem.NotifyStationTemplateRestoreBeginning` directly (Persistence is hub-spawned; WorldReadiness is DDOL — do not rely on OnBeforeRestore subscription alone).

## Depends on / Used by

- **Depends on:** [data-codegen](data-codegen.md) (`LocalStorage`), [permissions](permissions.md), [tile](tile.md), [area](area.md), [rounds-lobby](rounds-lobby.md), [gamemodes-roles-traits](gamemodes-roles-traits.md) (`CurrentGamemodeName` for round history)
- **Used by:** [tile](tile.md) (save/load backend), [permissions](permissions.md) (import/export), [rounds-lobby](rounds-lobby.md) (round-end history)

## Related docs

- Plan: [persistence_architecture_design_2fe61864.plan.md](../../plans/persistence_architecture_design_2fe61864.plan.md), [spawn_point_authoring.plan.md](../../archive/plans/spawn_point_authoring.plan.md)
- Effort: [2026-07_spawn-point-authoring](../../archive/architecture/2026-07_spawn-point-authoring.md)
- Effort: [2026-07_session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md)
- Design (read-only): [persistence-save.md](../../design/persistence-save.md) (the four-layer spec this system implements); [round-config.md](../../design/round-config.md) (blocks map pool contributor); [creative-mode.md](../../design/creative-mode.md) §9
