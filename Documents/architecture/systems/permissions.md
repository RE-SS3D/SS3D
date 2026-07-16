> Code paths: Assets/Scripts/SS3D/Permissions/
> Entry points: PermissionSubSystem
> Status: partial

# Permissions

## Overview

Admin permission checks for server-gated actions (e.g. TileMap Creator RPCs). Permissions load from [persistence](persistence.md) server-meta envelope on boot, with legacy `Config/permissions.txt` fallback. Changes sync to clients via `UserPermissionsChangedEvent` and persist back to the envelope.

## Start here

- `Assets/Scripts/SS3D/Permissions/PermissionSubSystem.cs` — permission subsystem (`ImportUserPermissions`, `ExportUserPermissions`)

## Extension points

- Check access: `PermissionSubSystem.IsAtLeast(ckey, ServerRoleTypes)`.
- Update role: `ChangeUserPermission` (writes legacy txt + triggers envelope save via persistence).

## Depends on / Used by

- **Depends on:** [persistence](persistence.md) (server-meta load/save)
- **Used by:** [tile](tile.md) TileMap Creator, [rounds-lobby](rounds-lobby.md) (round start/stop auth)

## Related docs

- [persistence_architecture_design_2fe61864.plan.md](../../plans/persistence_architecture_design_2fe61864.plan.md)
