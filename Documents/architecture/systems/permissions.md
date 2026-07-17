> Code paths: Assets/Scripts/SS3D/Permissions/
> Entry points: PermissionSubSystem
> Status: partial
> Verified: b7b7dcf3 — 2026-07-17

# Permissions

## Overview

Admin permission checks for server-gated actions (e.g. TileMap Creator RPCs). Permissions load from [persistence](persistence.md) server-meta envelope on boot, with legacy `Config/permissions.txt` fallback. Changes sync to clients via `UserPermissionsChangedEvent` and persist back to the envelope.

## Start here

- `Assets/Scripts/SS3D/Permissions/PermissionSubSystem.cs` — permission subsystem (`ImportUserPermissions`, `ExportUserPermissions`)
- `Assets/Scripts/SS3D/Systems/Persistence/PermissionsPersistenceContributor.cs`, `LegacyPermissionsMigrator.cs` — server-meta envelope load/save; both resolve the legacy path via the identical `Paths.GetPath(GamePaths.Config, true)/permissions.txt`

## Extension points

- Check access: `PermissionSubSystem.IsAtLeast(ckey, ServerRoleTypes)`.
- Update role: `ChangeUserPermission` (writes legacy txt + triggers envelope save via persistence).

## Pitfalls

- On a fresh server with no saved envelope yet, `PermissionsPersistenceContributor.Restore` falls through to `LegacyPermissionsMigrator.TryLoadFromLegacyTxt` — **not** `PermissionSubSystem`'s own `LoadPermissionsFromLegacyTxt` fallback (that only fires if `HasLoadedPermissions` is still false by the time something calls `TryGetUserRole`, which the persistence contributor's restore beats it to on boot). Both read the same `Config/permissions.txt` path, so seeding that file before server start (see `Testing/multiplayer/run_smoketest.sh`) still works either way, but if you're debugging why a hand-edited `permissions.txt` isn't taking effect, check whether a server-meta envelope already exists first — an existing envelope wins and the legacy file is never consulted at all.

## Depends on / Used by

- **Depends on:** [persistence](persistence.md) (server-meta load/save)
- **Used by:** [tile](tile.md) TileMap Creator, [rounds-lobby](rounds-lobby.md) (round start/stop auth)

## Related docs

- [persistence_architecture_design_2fe61864.plan.md](../../plans/persistence_architecture_design_2fe61864.plan.md)
