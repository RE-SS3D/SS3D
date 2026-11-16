> Implements: infrastructure — session/scene lifecycle & world readiness; realizes [2026-07_agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md) follow-on (a) (code bootstrap + `NetworkSystemsHub`); session/reconnect behavior per [design/networking.md](../../design/networking.md) §2–§5
> Touches systems: networking-session, scene-management, application, core-subsystems, tile, area, electricity, atmospherics, rounds-lobby, player-control, persistence, disposal
> Status: shipped

# Session & world lifecycle

Server/client init, disconnect handling, and round start are fragile because **prerequisites are
never modeled**. This effort replaces the ad-hoc init/reconnect bandages with two explicit contracts —
a **session lifecycle FSM** and a **world readiness graph** — and lands them on the code-bootstrap
foundation that [2026-07_agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md) follow-on
**(a)** and [TECH_DEBT.md](../../architecture/TECH_DEBT.md) §1.7 name.

Sequenced in three phases; all shipped 2026-07-23.

**Update 2026-07-23 (Phase 3h):** Boot Persistent Systems and Game Systems SubSystems emptied via
`SS3D/Bootstrap/Phase 3h — Rebuild Hub + Strip Boot & Game`. Process-wide services live in
`SystemsBootstrap` (DDOL); world/session `NetworkSubSystem`s live on `NetworkSystemsHub` Resources
prefab (spawned Online). Empty parent roots (`Persistent Systems` / `Systems` + EventSystem) may
remain in scenes as launch pads only.

**Post-ship hardening (same branch):** hub spawn exposed Awake/`OnStartServer` vs Unity `Start` and
hub-before-Game ordering — Persistence now owns `LoadServerMeta` (contributors in `OnAwake`);
`CameraSubSystem.PlayerCamera` lazy-resolves after Game’s MainCamera exists; do **not** re-run the
Phase 3h “Rebuild Hub + Strip” menu (Game no longer has SerializeField sources). TECH_DEBT §1.7 /
§1.16 are in [TECH_DEBT.md](../../architecture/TECH_DEBT.md) §6 Resolved.

**Residual (not Phase 3 scope):** Boot/Game *systems roots* are empty, but some **content prefabs**
still carry `SubSystem` components that register when Game loads — before `NetworkSystemsHub` is
Online (e.g. `PlayerCamera` + `SelectionCamera`, Radial/Armed overlay prefabs, MapEditor canvas).
Smoke hardening on this branch uses consumer `TryGet` / lazy resolve (same pattern as PlayerCamera)
and starts `SetSuppressMissingErrors` on **Disconnecting** so hub/device teardown Gets stay silent.
Relocating those leftover prefab SubSystems onto `SystemsBootstrap` / the hub is follow-on cleanup,
not a contradiction of Phase 3.

## 1. Problem & non-goals

One architectural hole with two faces.

**Face A — session/scene lifecycle.** FishNet `DefaultScene` (config in `Boot.unity:281-288`) uses
**offline = Boot**, online = Game. On any disconnect, `DefaultScene.LoadOfflineScene()` reloads Boot as
a *Single* scene while the `NetworkManager` is DontDestroyOnLoad → duplicate managers +
`ApplicationInitializerSubSystem.OnStart()` re-fires the whole
`ApplicationPreInitializing → Initializing → Initialized` chain (the "Boot storm"). Leaked
`ApplicationInitializing` listeners then re-drive `IntroUIHelper`/`SkipIntro` → repeated
`StartNetworkSession` → "Failed to start… already starting/started".

**Update 2026-07-23 — the storm symptom is now fixed, landed independently of this doc via PR #36
(`cursor/client-light-fixture-sync` → `develop`).** New `Networking/ClientConnectionRecovery.cs` — a
DontDestroyOnLoad component on the `NetworkManager` — arms `Empty.unity` as the FishNet offline scene
after the **first successful connection**, so a later disconnect no longer reloads Boot; it also owns
an OnGUI Retry/Quit fallback for the case where Boot (and its UI) is already gone. Re-entrant
`StartNetworkSession()` calls are now guarded by `NetworkSessionSubSystem.CanStartNetworkSession`
(skips while the client is Starting/Started/Stopping — the exact condition that used to storm). The
`ServerConnectionView` Retry button (for the pre-first-connect / Intro-still-loaded case) now really
calls `StartNetworkSession()` again instead of only replaying the loading animation. The previously
harness-only redirect (`AutomationSubSystem.RedirectOfflineSceneToEmpty`) is now belt-and-suspenders on
top of the universal fix rather than the only mitigation. See
[networking-session.md](../../architecture/systems/networking-session.md) Pitfalls for the updated account.

**Update (same day, implementation branch):** Phase 1 also landed the named `SessionState` FSM on
`ClientConnectionRecovery`, `Scenes.Empty` / `Scenes.EmptyPath` codegen, Cold-only Intro auto-join, and
silent `SubSystems.Get` while `WaitingForServer`. See §2.

**Face B — world readiness.** Subsystems register in Unity `Awake` (`SubSystem`/`NetworkSubSystem.OnAwake`
→ `SubSystems.Register`) but do real setup later in `OnStartServer`/`OnStartClient` and only become
*functional* after the map is actually loaded. **Registration ≠ readiness.** Historically three
incompatible idioms coexisted (`IsSetUp`/`OnSystemSetUp`, UniTask polls on `CurrentMap != null`, raw
`OnMapCreated`/`OnMapLoaded`). Round start was time-driven (`PrepareRound` fixed 500 ms), so
`SpawnReadyPlayersEvent` could fire before flood-fill/atmos init. **§3b shipped** the general
`IWorldReady` / `WorldReadinessSubSystem` graph and gates `PrepareRound` on `WorldReady` — see §3b.
Obsolete `IsSetUp` shims remain on Area/Electricity for call sites not yet migrated.

The specific client/host divergence this doc originally called out — flood-fill and `TileChunk._areaIds`
being server-only and never replicated, plus non-`BufferLast` lighting RPCs, so a client's `LightPower`
couldn't resolve area or lighting state — was the bug class `cursor/client-light-fixture-sync` was
chasing. **It shipped 2026-07-23** (PR #36); see §3a for what landed and why it validates this doc's
client-parity contract (orthogonal to the readiness graph).

**Still open (deferred elsewhere):** reconnect exponential backoff; UiShell/MainHud/StoragePanel
self-bootstraps (follow-on **(b)**); `Human.prefab` decomposition (follow-on **(d)**).

**Non-goals.**
- No prediction/rollback/lag-compensation/interpolation ([design/networking.md](../../design/networking.md) §9 keeps
  netcode-level concerns out of scope).
- Do **not** encode a full dependency DAG into `SubSystems.Get` — the fix is *don't call `Get` until
  ready*, and make missing-during-`WaitingForServer` silent (as quit already is).
- Do **not** fix every domain race with a one-off SyncVar — SyncVars are for *client view of server
  truth*; readiness is for *when simulation may run*.
- No server browser / matchmaking / capacity-queue ([design/networking.md](../../design/networking.md) §5, §9); capacity is a
  plain reject, deferred here.

## 2. Phase 1 — Session lifecycle FSM (standalone shippable slice)

**Status: shipped, 2026-07-23.** Behavioral contract met via PR #36 (Empty offline + retry guards);
named FSM + Empty codegen landed on the same-day implementation pass (`cursor/session-world-lifecycle`).

```
Cold → Connecting → Online → Disconnecting → WaitingForServer → Connecting → …
```

Owned by `ClientConnectionRecovery` (`SessionState` enum). Rules:

- **Cold start** may load Boot/Intro **once**. `SkipIntro`/`IntroUIHelper` auto-join fires **only** when
  `SessionState.Cold`, never on reconnect.
- **After any join attempt, offline is never Boot.** CCR arms `Scenes.EmptyPath` after first Online;
  `AutomationSubSystem` redirect never restores Boot offline.
- **Reconnect** via manual Retry (CCR OnGUI when Boot is gone; `ServerConnectionView` while Intro
  remains). **Not done:** exponential backoff. Never via Intro/`SkipIntro`.
- **Online** loads Game; **Disconnecting** → `WaitingForServer` with `SubSystems.SetSuppressMissingErrors`
  so locator misses stay silent.
- Prefer branching on `SessionState` / `ClientConnectionRecovery.Instance` over inferring session from
  subsystem presence.

Reconnect-to-body unchanged: `PlayerSubSystem.ProcessAuthorizePlayer` →
`EntitySubSystem.TryReclaimEntity` ([design/networking.md](../../design/networking.md) §3).

**Key files:** `Networking/SessionState.cs`, `Networking/ClientConnectionRecovery.cs`,
`Networking/NetworkSessionSubSystem.cs`, `Networking/ServerConnectionView.cs`,
`Systems/Intro/IntroUIHelper.cs`, `Systems/Testing/AutomationSubSystem.cs`,
`Data/Generated/Scenes.cs` (`Empty` / `EmptyPath` / `BootPath`), FishNet `DefaultScene` in `Boot.unity`.

## 3. Phase 2 — World readiness graph

Separate **registered** from **ready**. Two pieces: **client-parity** (§3a, PR #36) and the
**general world-readiness graph** (§3b, shipped on the implementation branch).

### 3a. Client parity contract — shipped instance (PR #36)

Pure clients never run flood-fill; area ids used to live only in server `TileChunk._areaIds` and
lighting RPCs were not buffered, so a client's `LightPower` could not resolve area or lighting state.
**Fixed 2026-07-23**, independently of this doc, on `cursor/client-light-fixture-sync`:

- `LightPower._fixtureVisual` became a server-authoritative **SyncVar** — FishNet auto-replicates
  SyncVar values to late joiners, which is exactly the "buffered" property this contract needs; the
  server computes Off/Normal/Emergency and clients only apply it (never re-derive from
  `AreaSubSystem.IsSetUp`, which stays `false` on pure clients).
- `AreaSubSystem`'s per-area lighting RPC was replaced with a **BufferLast full-snapshot**
  `RpcSyncAreaLighting` (the old non-buffered per-area RPC only retained state for the *last* area
  touched — a late joiner or a client that missed one area's transition never caught up).
- New `AreaDeviceTileResolver` + `AreaFloorVisualCache.TryGetAreaIdForWorldGrid` let a client resolve
  "which area is this device in" from its own local floor-cache snapshot, without needing the host's
  live flood-fill registry at all.

This validates the contract shape this doc asked for (buffered server truth + a client-side resolver
that doesn't depend on host-only state) with a real, narrower implementation than a general
"world epoch N" broadcast — worth keeping as the reference pattern for any future domain that hits the
same host/client divergence. Detail: [area.md](../../architecture/systems/area.md) Pitfalls ("Client fixtures stay stuck
on/off"), [electricity.md](../../architecture/systems/electricity.md) Pitfalls ("Client light fixtures ignore APC / wall-switch
toggles").

**Key files (as shipped):** `Systems/Electricity/LightPower.cs`, `Systems/Area/AreaSubSystem.cs`,
`Systems/Area/AreaDeviceTileResolver.cs`, `Systems/Area/AreaFloorVisualCache.cs`.

### 3b. General world-readiness graph — shipped

**Status: shipped, 2026-07-23** (`cursor/session-world-lifecycle`). One contract replaces the old idioms:

- `IWorldReady` / `IsReady` / `WhenReady` on world subsystems + UniTask `WaitUntilAsync` on
  `WorldReadinessSubSystem`, phases:

  ```
  TileMapLoaded → AreasFlooded → ElectricityReady / AtmosReady / DisposalReady → WorldReady
  Persistence OnBeforeRestore → ResetEpoch; OnAfterRestore → TileMapLoaded (+ domain re-notify)
  ```

- Tile gate is end-of-restore / post-flood — **not** `OnMapCreated`. Atmos/Disposal await
  `TileMapLoaded` then notify their gates (no `CurrentMap != null` poll).
- Area notifies `AreasFlooded` after deferred flood; Electricity awaits AreasFlooded then notifies.
  Area/Electricity keep obsolete `IsSetUp` → `IsReady` shims.
- `RoundSubSystem.PrepareRound` awaits `WorldReadyPhase.WorldReady` (no fixed 500 ms delay).
- Station restore: `PersistenceSubSystem` calls `NotifyStationTemplateRestoreBeginning` directly
  (WorldReadiness is DDOL; Persistence is hub-spawned — event-only binding is unreliable).

**Key files:** `Core/WorldReadiness/{IWorldReady,WorldReadyPhase}.cs`,
`Systems/WorldReadiness/WorldReadinessSubSystem.cs`; Area / Electricity / Atmos / Disposal SubSystems;
`Systems/Persistence/PersistenceSubSystem.cs`; `Systems/Rounds/RoundSubSystem.cs`.

## 4. Phase 3 — Thin scenes + code bootstrap + `NetworkSystemsHub`

Realizes [2026-07_agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md) follow-on **(a)**;
pays down [TECH_DEBT.md](../../architecture/TECH_DEBT.md) §1.7 / §1.12.

**Status: shipped.**

- `SystemsBootstrap` — DDOL: NetworkSession / CommandLineArgs (via type name), Scene, Input,
  WorldReadiness, ScreenEffects, Automation, Vision, ApplicationInitializer (last so listeners Awake first).
- `NetworkSystemsHub` Resources prefab holds world/session SubSystems (edit-time components;
  `SS3D/Bootstrap/Rebuild NetworkSystemsHub Prefab` copies SerializeFields from Game then strips).
- Boot Persistent Systems + Game Systems SubSystem GOs removed; EventSystem left on Game.
- Hub despawn on network stop → `OnDestroyed` → `Unregister` (no extra reverse-order teardown needed).
- `SubSystems.Get` FindObject fallback skipped while quitting **or** `WaitingForServer`.
- CCR OnGUI recovery keys off Intro/Boot/Launcher loaded (NetworkSession is DDOL and always present).

**Key files:** `Systems/Bootstrap/SystemsBootstrap.cs`, `Networking/NetworkSystemsHub.cs`,
`Assets/Resources/NetworkSystemsHub.prefab`, `Editor/Bootstrap/SessionWorldLifecycleEditorMenus.cs`,
`Core/Subsystems.cs`, `Assets/Content/Scenes/{Boot,Game}.unity`.

## 5. Sequencing & acceptance criteria

1. **Phase 1** — session FSM + never re-enter Boot after first join. **Shipped.**
2. **Phase 2** — world-ready + client parity. **Shipped** (§3a + §3b).
3. **Phase 3** — code bootstrap + hub + empty Boot/Game. **Shipped.**

Per-phase acceptance:

- **Phase 1 — met.** Disconnect does not reload Boot after first Online; manual Retry reconnects;
  named `SessionState`; Intro auto-join Cold-only. No backoff yet (deferred).
- **Phase 2, §3a — met.** Late-join pure client lights fixtures via SyncVar + floor-cache area resolve.
- **Phase 2, §3b — met.** Domains notify gates; `PrepareRound` awaits `WorldReady`; restore resets epoch
  via direct Persistence → WorldReadiness notify.
- **Phase 3 — met.** No gameplay SubSystem GameObjects under Boot/Game systems roots; new domains use
  bootstrap list or hub rebuild menu. UI host `RuntimeInitializeOnLoad` remains (follow-on **(b)**).

Verification: Editor Play Mode host cold-start; `Testing/multiplayer/run_smoketest.sh` reconnect
scenarios; EditMode via `.github/workflows/editmodetestrunner.yml`.

## 6. Related docs

- Composition target: [2026-07_agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md) (follow-on (a))
- Debt register: [TECH_DEBT.md](../../architecture/TECH_DEBT.md) §1.7 (bootstrap), §1.12 (`Get` FindObject), §1.16 (lifecycle)
- Design (read-only): [design/networking.md](../../design/networking.md) §2–§5 (disconnect/reconnect/capacity)
- System maps: [networking-session](../../architecture/systems/networking-session.md), [scene-management](../../architecture/systems/scene-management.md),
  [application](../../architecture/systems/application.md), [core-subsystems](../../architecture/systems/core-subsystems.md),
  [player-control](../../architecture/systems/player-control.md), [tile](../../architecture/systems/tile.md), [area](../../architecture/systems/area.md),
  [electricity](../../architecture/systems/electricity.md), [atmospherics](../../architecture/systems/atmospherics.md), [disposal](../../architecture/systems/disposal.md),
  [persistence](../../architecture/systems/persistence.md), [rounds-lobby](../../architecture/systems/rounds-lobby.md)
