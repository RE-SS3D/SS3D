> Code paths: Assets/Scripts/SS3D/Systems/PlayerControl/
> Entry points: PlayerSubSystem
> Status: partial
> Verified: b0df80aa9 — 2026-11-02

# Player control

## Overview

Player subsystem — connect/authorize/disconnect flow for `Player` (the persistent
per-ckey identity object, distinct from the `Entity` body it controls), input routing,
round join ordering.

Connect/disconnect lifecycle, server-side:

1. `ServerManager.OnRemoteConnectionState` fires nothing on connect; `SceneManager.OnClientLoadedStartScenes` → `HandleClientLoadedStartScenes` → `ProcessPlayerJoin` spawns a temporary `UnauthorizedPlayer` for the new connection.
2. The client's `UnauthorizedPlayer.Setup()` immediately broadcasts `UserAuthorizationMessage` with its ckey (no real login — see TODO in `ProcessAuthorizePlayer`).
3. `ProcessAuthorizePlayer` rejects a new online identity when the configured Server Settings player cap is reached, then finds-or-creates the `_serverPlayers[ckey]` `Player` object, gives the connection ownership of it, adds it to `_onlinePlayers`, **reconnects it to any existing body** via `EntitySubSystem.TryReclaimEntity` if `IsPlayerSpawned(player)` is true (i.e. they disconnected mid-round and are rejoining), then despawns the `UnauthorizedPlayer`.
4. `ServerManager.OnRemoteConnectionState` → `Stopped` → `ProcessPlayerDisconnect` calls `RemoveOwnership()` on every `NetworkObject` the connection owns (the `Player`, and separately the controlled `Entity`/body if one was spawned) and removes the ckey from `_onlinePlayers`. The body itself is **not** despawned here — it's left ownerless in the world until either the owning player reconnects (step 3) or the round ends (`EntitySubSystem.DestroySpawnedPlayers`).

## Start here

- `Assets/Scripts/SS3D/Systems/PlayerControl/PlayerSubSystem.cs` — player subsystem; `ProcessPlayerJoin`/`ProcessAuthorizePlayer`/`ProcessPlayerDisconnect` own the connect/disconnect lifecycle above.
- `Assets/Scripts/SS3D/Systems/PlayerControl/UnauthorizedPlayer.cs` — temporary NOB that broadcasts `UserAuthorizationMessage`
- `Assets/Scripts/SS3D/Systems/Entities/EntitySubSystem.cs` — `TryReclaimEntity` re-links a reconnecting player's `Player` to the body they left behind.

## Extension points

- **Diverges from `networking.md`** (design doc landed after this reconnect flow was built — not yet reconciled):
  - §3 ("reconnecting hands control straight back, no prompt") is what `TryReclaimEntity` does — this part matches.
  - §2's diegetic tell (idle animation slackens/stops while disconnected) is **not implemented** — a disconnected body currently looks identical to an idle player-controlled one; nothing currently signals "unpiloted" on the model itself.
  - §5's capacity check is **partially implemented**: `ProcessAuthorizePlayer` rejects a new online ckey when `_onlinePlayers.Count >= RoundSubSystem.PlayerCap`; already-online keys bypass that check. A full-server reason is not sent to the client.
  - The `cryogenics.md` §4 grace-period handoff this doc's §3 window is supposed to expire into does not exist yet (`cryogenics` has no architecture/system map — see INDEX.md coverage table) — so today a reconnect window never actually closes; a body left unclaimed sits ownerless until round end, not until some grace period elapses.
- No real authentication (`ProcessAuthorizePlayer` trusts the client-supplied ckey wholesale) — flagged in-code as a TODO, out of scope here.

## Pitfalls

- **Reclaiming a collapsed body must reapply ragdoll authority:** the ownership callback runs without a presentation SyncVar change. `Ragdoll` reapplies the current collapse/death state and preserves animator startup defaults; see [entities](entities.md).

- **Rejoin shows lobby + HUD:** `TryReclaimEntity` restores ownership without mutating `SpawnedPlayers` / Mind SyncVar. HUD binds via `Mind.player.IsLocalConnection`; lobby used to leave only on `SpawnedPlayersUpdated` with Owner match. Fire `LocalPlayerObjectChanged` from `Entity.OnOwnershipClient` (HUD). `GameScreensController` was deleted with the legacy lobby (Phase F); the UITK lobby polls `EntitySubSystem.IsPlayerSpawned(connection)`, which matches a reclaimed body via `Mind.player.Owner`. Hit 2026-07-26.
- **`OnClientLoadedStartScenes` must gate on `asServer`:** host also fires the client-side (`asServer=false`) callback before `LoadedStartScenes(true)` is set. Spawning there warns and can create a duplicate UnauthorizedPlayer that later fails despawn ("already deinitializing"). Match FishNet `PlayerSpawner`.
- **Despawn UnauthorizedPlayer carefully:** snapshot `conn.Objects.ToArray()` and skip non-spawned objects before `ServerManager.Despawn` (`IsDeinitializing` is FishNet-internal).

- **Late client SyncDictionary values may be null before the Player NetworkObject spawns.** `ServerPlayers` / `OnlinePlayers`, sync callbacks and lookups filter/skip unresolved values. Preserve these guards; dictionary membership alone does not prove the Player exists (`18d28be1e`, `f1221aaae`, PR #92).

## Depends on / Used by

- **Depends on:** [entities](entities.md), [inputs](inputs.md)
- **Used by:** [interactions-runtime](interactions-runtime.md), [rounds-lobby](rounds-lobby.md)

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: networking (full-server reason), observer (Observe button)
- Design (read-only): [Documents/design/networking.md](../../design/networking.md) — the connection-lifecycle spec this domain now implements (partially — see Extension points)
- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md)
- Coming back after death (a different mechanism from network disconnect): ghosts never return to their old body by defib ([observer.md](../../design/observer.md) §6); the ways back are cloning and the respawn setting ([death-cloning-respawn.md](../../design/death-cloning-respawn.md) §5, §6)
- Regression coverage for the disconnect/reconnect ownership flow: `Testing/multiplayer/scenarios/reconnect{,-client}.txt`, see [Multiplayer testing](networking-session-testing.md)
