> Code paths: Assets/Scripts/SS3D/Permissions/
> Entry points: PermissionSubSystem
> Status: partial
> Verified: db359c989 — 2026-11-06

# Permissions

## Overview

Admin permission checks for server-gated actions: round start/stop and server settings, map editor authorizers, comms channel roles, console command access levels. Permissions load from [persistence](persistence.md) server-meta envelope on boot, with legacy `Config/permissions.txt` fallback. Changes sync to clients via `UserPermissionsChangedEvent` and persist back to the envelope.

## Start here

- `Assets/Scripts/SS3D/Permissions/PermissionSubSystem.cs` — permission subsystem (`ImportUserPermissions`, `ExportUserPermissions`)
- `Assets/Scripts/SS3D/Permissions/AdminTokens.cs` — admin-name check at login. Names are self-declared (`-ckey=`), so on dedicated servers (or any server with `Config/admin-tokens.txt`) a remote login under an Administrator+ name needs a matching `-token=`; `PlayerSubSystem.ProcessAuthorizePlayer` refuses otherwise with `DisconnectCode.AdminToken`, which tells the client the name is an admin name (accepted on 2026-11-06); the main menu then offers a token field. The host's own connection is never checked. The tokens file is server-only and never synced
- `Assets/Scripts/SS3D/Systems/Persistence/PermissionsPersistenceContributor.cs`, `LegacyPermissionsMigrator.cs` — server-meta envelope load/save; both resolve the legacy path via the identical `Paths.GetPath(GamePaths.Config, true)/permissions.txt`
- `Builds/Game/Config/permissions.default.txt` — tracked seed for the gitignored live `permissions.txt`

## Extension points

- Check access: `PermissionSubSystem.IsAtLeast(ckey, ServerRoleTypes)`.
- Update role: `ChangeUserPermission` (writes legacy txt; the `UserPermissionsChangedEvent` it raises makes `PersistenceSubSystem` save the server-meta envelope).

## Pitfalls

- **`Config/permissions.txt` is live state, gitignored.** The tracked `Config/permissions.default.txt` seeds it: `LegacyPermissionsMigrator` reads the default when the live file is missing, and `PermissionSubSystem` copies it into place on first use. Edit the default to change who is admin on a fresh checkout or install; releases ship it as `permissions.txt`.
- **`HasLoadedPermissions` gates lobby admin controls:** `LobbyUiSubSystem.CanAdmin` (round start/stop, server settings) requires the SyncVar **and** `IsAtLeast(…, Administrator)`. That flag is set only by ServerMeta import / legacy load (`ImportUserPermissions` / `LoadPermissionsFromLegacyTxt`) — not by host `ChangeUserPermission`. If the lobby shows no admin controls despite a ServerOwner grant log, ServerMeta never imported (see [persistence](persistence.md)).
- On a fresh server with no saved envelope yet, `PermissionsPersistenceContributor.Restore` falls through to `LegacyPermissionsMigrator.TryLoadFromLegacyTxt` — **not** `PermissionSubSystem`'s own `LoadPermissionsFromLegacyTxt` fallback (that only fires if `HasLoadedPermissions` is still false by the time something calls `TryGetUserRole`, which the persistence contributor's restore beats it to on boot). Both read the same `Config/permissions.txt` path. An existing `Data/ServerMeta/permissions.json` wins and the legacy file is never consulted — smoke triage symptom: `User harness_… doesn't have Administrator` despite a seeded `permissions.txt`. `run_smoketest.sh` deletes the staged ServerMeta permissions file before seeding the txt so the migrator path runs.

- **Dedicated server rejects every admin name:** without `Config/admin-tokens.txt`, no remote login may use an Administrator/ServerOwner name, including the shipped `hostingUser`. Server log: `Rejected login from client … admin name with no line in admin-tokens.txt`. Harnesses that seed `permissions.txt` (`run_smoketest.sh`, `run_loadtest.sh`) must also seed tokens and pass `-token=` (a scenario's `CLIENT_NO_TOKEN=1` omits it to test the refusal). Any change to `UserAuthorizationMessage` (it carries `Token`) needs a `ProtocolVersion.Current` bump.

## Tests

- EditMode: `Assets/Scripts/Tests/EditMode/AdminTokensTests.cs`.

## Depends on / Used by

- **Depends on:** [persistence](persistence.md) (server-meta load/save)
- **Used by:** [networking-session](networking-session.md) login (`PlayerSubSystem` + `AdminTokens`), [map-editor-creative-hooks](map-editor-creative-hooks.md) (`MapEditorPermissions`, admin/open-build authorizers), [rounds-lobby](rounds-lobby.md) (round start/stop auth, `ServerSettings`, lobby admin UI), [chat-audio-screens](chat-audio-screens.md) (channel `RoleRequiredToUse`, OOC admin checks), [ingame-console](ingame-console.md) (`CommandsController` access level)

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: admin-tools (action logging, `Restricted` role)
- [persistence_architecture_design_2fe61864.plan.md](../../plans/persistence_architecture_design_2fe61864.plan.md)
