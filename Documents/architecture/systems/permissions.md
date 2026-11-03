> Code paths: Assets/Scripts/SS3D/Permissions/
> Entry points: PermissionSubSystem
> Status: partial
> Verified: 7c8e6b6d7 — 2026-11-01

# Permissions

## Overview

Admin permission checks for server-gated actions (e.g. TileMap Creator RPCs). Permissions load from [persistence](persistence.md) server-meta envelope on boot, with legacy `Config/permissions.txt` fallback. Changes sync to clients via `UserPermissionsChangedEvent` and persist back to the envelope.

## Start here

- `Assets/Scripts/SS3D/Permissions/PermissionSubSystem.cs` — permission subsystem (`ImportUserPermissions`, `ExportUserPermissions`)
- `Assets/Scripts/SS3D/Permissions/AdminTokens.cs` — admin-name check at login. Names are self-declared (`-ckey=`), so on dedicated servers (or any server with `Config/admin-tokens.txt`) a remote login under an Administrator+ name needs a matching `-token=`; `PlayerSubSystem.ProcessAuthorizePlayer` disconnects otherwise. The host's own connection is never checked. The tokens file is server-only and never synced
- `Assets/Scripts/SS3D/Systems/Persistence/PermissionsPersistenceContributor.cs`, `LegacyPermissionsMigrator.cs` — server-meta envelope load/save; both resolve the legacy path via the identical `Paths.GetPath(GamePaths.Config, true)/permissions.txt`

## Extension points

- Check access: `PermissionSubSystem.IsAtLeast(ckey, ServerRoleTypes)`.
- Update role: `ChangeUserPermission` (writes legacy txt + triggers envelope save via persistence).

## Pitfalls

- **`Config/permissions.txt` is live state, gitignored.** The tracked `Config/permissions.default.txt` seeds it: `LegacyPermissionsMigrator` reads the default when the live file is missing, and `PermissionSubSystem` copies it into place on first use. Edit the default to change who is admin on a fresh checkout or install; releases ship it as `permissions.txt`.
- **`HasLoadedPermissions` gates lobby start:** `ChangeRoundStateView` requires the SyncVar **and** `IsAtLeast(…, Administrator)`. That flag is set only by ServerMeta import / legacy load (`ImportUserPermissions` / `LoadPermissionsFromLegacyTxt`) — not by host `ChangeUserPermission`. If lobby says permissions not loaded despite a ServerOwner grant log, ServerMeta never imported (see [persistence](persistence.md)).
- On a fresh server with no saved envelope yet, `PermissionsPersistenceContributor.Restore` falls through to `LegacyPermissionsMigrator.TryLoadFromLegacyTxt` — **not** `PermissionSubSystem`'s own `LoadPermissionsFromLegacyTxt` fallback (that only fires if `HasLoadedPermissions` is still false by the time something calls `TryGetUserRole`, which the persistence contributor's restore beats it to on boot). Both read the same `Config/permissions.txt` path. An existing `Data/ServerMeta/permissions.json` wins and the legacy file is never consulted — smoke triage symptom: `User harness_… doesn't have Administrator` despite a seeded `permissions.txt`. `run_smoketest.sh` deletes the staged ServerMeta permissions file before seeding the txt so the migrator path runs.

- **Dedicated server rejects every admin name:** without `Config/admin-tokens.txt`, no remote login may use an Administrator/ServerOwner name, including the shipped `hostingUser`. Server log: `Rejected login from client … admin name with no line in admin-tokens.txt`. Harnesses that seed `permissions.txt` (`run_smoketest.sh`, `run_loadtest.sh`) must also seed tokens and pass `-token=`. Changing `UserAuthorizationMessage` bumped `ProtocolVersion.Current` to 7.

## Depends on / Used by

- **Depends on:** [persistence](persistence.md) (server-meta load/save)
- **Used by:** [networking-session](networking-session.md) login (`AdminTokens`), [tile](tile.md) TileMap Creator, [rounds-lobby](rounds-lobby.md) (round start/stop auth)

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: admin-tools (action logging, `Restricted` role)
- [persistence_architecture_design_2fe61864.plan.md](../../plans/persistence_architecture_design_2fe61864.plan.md)
