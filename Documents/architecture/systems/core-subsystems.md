> Code paths: Assets/Scripts/SS3D/Core/, Assets/Scripts/SS3D/Systems/Bootstrap/, Assets/Scripts/SS3D/Systems/WorldReadiness/, Assets/Scripts/SS3D/Networking/NetworkSystemsHub.cs
> Entry points: SubSystem, NetworkSubSystem, SubSystems, SystemsBootstrap, WorldReadinessSubSystem, NetworkSystemsHub
> Status: partial
> Verified: 77ce6acb3 — 2026-11-13

# Core / SubSystems

## Overview

Base actor/subsystem pattern and runtime service locator. All gameplay domains expose a `*SubSystem` registered via `SubSystems.Get<T>()`. `NetworkSubSystem` extends FishNet `NetworkActor` for networked subsystems.

Process-wide services: `SystemsBootstrap` (DDOL, `AfterSceneLoad`; creates network session, command-line, player settings, scene, input, world readiness, screen effects, ambience/personal audio, automation, vision, server-status publisher, client session (`ClientSessionSubSystem`), structural damage, bug reports (not in batch mode), the airlock proximity service, the floor-decal and area-stripe views, the debug-build-only local-speech and screen-effects debug tools, client main menu, then `ApplicationInitializerSubSystem` last). Systems types do not self-bootstrap with `AfterSceneLoad`; add new process-wide services here so the Test Runner guard covers them. World/session networked systems: `NetworkSystemsHub` Resources prefab (spawned Online). The hub also owns transient [napalm surface fire](combat-napalm.md), so it survives its shooter and clears on world reset. Boot/Game do not place per-system SubSystem GameObjects ([session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md)).

## Start here

- `Assets/Scripts/SS3D/Core/Behaviours/SubSystem.cs` — non-networked subsystem base (bootstrap-owned)
- `Assets/Scripts/SS3D/Core/Behaviours/NetworkSubSystem.cs` — networked subsystem base (hub prefab only)
- `Assets/Scripts/SS3D/Core/Subsystems.cs` — locator; FindObject fallback skipped while quitting / WaitingForServer, and logs a warning on every hit (removed once none are seen)
- `Assets/Scripts/SS3D/Core/SimulationClock.cs` — one clock on FishNet `TimeManager.OnTick` with named fixed-rate channels (`atmos`, `electricity`, `health`); `WorldSimSuspension` pauses the channels that opt in
- `Assets/Scripts/SS3D/Core/SceneRegistry.cs` — `SceneRegistry<T>`: components register on enable (or spawn), so gameplay never scans the scene
- `Assets/Scripts/SS3D/Core/BlittableEquality.cs` — allocation-free `IEquatable<T>` for plain-data `[SyncVar]` structs
- `Assets/Scripts/SS3D/Core/WorldReadiness/IWorldReady.cs` + `WorldReadyPhase.cs` — readiness contract
- `Assets/Scripts/SS3D/Systems/WorldReadiness/WorldReadinessSubSystem.cs` — phase coordinator
- `Assets/Scripts/SS3D/Systems/Bootstrap/SystemsBootstrap.cs` — DDOL process-wide services
- `Assets/Scripts/SS3D/Networking/NetworkSystemsHub.cs` + `Assets/Resources/NetworkSystemsHub.prefab` — Online hub
- `Assets/Scripts/SS3D/Editor/Bootstrap/SessionWorldLifecycleEditorMenus.cs` — hub rebuild menu + its type list; scene strip is Tier C (no menu)
- `Assets/Scripts/SS3D/Core/Debugging/DebugTools.cs` — named F1 debug-tool registry (usage in [inputs](inputs.md), [ui-shell](ui-shell.md))

## Extension points

- New process-wide SubSystem: add to `SystemsBootstrap.EnsureProcessWideServices`, before `ApplicationInitializerSubSystem`. Types in assemblies `SS3D.Systems` cannot reference (Networking, CommandLine, Launcher) go through `EnsureSubSystemType` by assembly-qualified name.
- New networked domain: add type to hub rebuild menu list, then append it with a tier-B recipe (`ConstructionHubRecipe`, `ActionMessageHubRecipe`: `AddComponent` + `RebuildNetworkBehaviours`) — do not edit Game.unity. Prefer that to `SS3D/Bootstrap/Rebuild NetworkSystemsHub Prefab`: the rebuild copies serialized fields from Game's system objects, which the Phase 3 strip removed, so by reading the code it would reset hub settings to defaults (unverified by a run).
- World sim: implement `IWorldReady`; notify via `WorldReadinessSubSystem` after TileMapLoaded / AreasFlooded — never treat `OnMapCreated` as ready.
- Fixed-rate sim: `SimulationClock.GetOrCreate(name, interval, pausedByWorldSuspension)` and `Subscribe`, never an `Update` accumulator. Every caller of a name must agree on its interval. Each fire runs inside an `SS3D.Clock.<name>` Profiler marker, which is where to read its per-tick GC.
- Finding live instances: register in `SceneRegistry<T>` from `OnEnabled`/`OnDisabled` (or server start/stop when only spawned ones count), never `FindObjectsByType` in gameplay code. Edit mode runs no `OnEnable`, so EditMode tests register by hand.
- Consumers: `IsReady` / `WhenReady` / `WaitUntilAsync` — not `Get` in Update.

## Pitfalls

- **Registration ≠ readiness.** Await `WorldReadyPhase` before round start / sim ticks that need flooded areas.
- **Missing Get during Disconnecting / WaitingForServer is silent** — use `TryGet`.
- **Expecting a miss? Use `TryGet` or `GetOrNull<T>()?.`, never `Get<T>()?.`:** `Get` logs an error and scans the scene on a miss. The registry drops destroyed instances, `Unregister` only removes the instance that is registered (a destroyed duplicate can't evict the live one), and quit flag and registry reset on `SubsystemRegistration` for Play Mode without a domain reload (tech-debt P2.3).
- **Bootstrap order and Test Runner cold start** are silent failures too — see [application](application.md) Pitfalls.
- **Game content prefabs may still host SubSystems** (`PlayerCameraSubSystem`, Radial/Armed interaction overlays, `MapEditorSubSystem`) that register before hub Online — Phase 3 only stripped Boot/Game systems roots. Consumers must `TryGet` / lazy-resolve; relocating those components onto bootstrap/hub is residual cleanup ([session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md) post-ship note).
- **A struct `[SyncVar]` set every tick needs `IEquatable<T>`.** FishNet compares old and new with `EqualityComparer<T>.Default`; without it Mono boxes both and reflects over every field, ~1 KB per set. Implement it with `BlittableEquality` (plain-data structs only). Hit 2026-11-11 (`HealthSnapshot`).
- **`GC.GetAllocatedBytesForCurrentThread` always returns 0 in Unity's Mono**, and `GC.GetTotalMemory` deltas don't move for small allocations. Measure per-call GC with a Profiler marker and a capture instead. Hit 2026-11-11.
- **Do not AddComponent NetworkSubSystems at runtime** — edit-time on hub prefab only (FishNet behaviour list).
- Hub despawn on disconnect unregisters via `OnDestroyed` — no extra teardown required.
- **Coimbra `GetChecked` / `EventSystem` Editor GC:** PackageCache `ServiceLocator.GetChecked` interpolates assert strings every call; deep profiles also show `String.Format` paired with every `EventSystem.Invoke`. Prefer caching `IEventService` and `SubSystems` refs on hot loops we own; do not edit PackageCache. Tracked in [TECH_DEBT.md](../TECH_DEBT.md) §2.

## Tests

- EditMode: `PlayModePrerequisiteTests`, `DebugToolsTests`, `SimulationClockTests`, `SceneRegistryTests`, `BlittableEqualityTests`. PlayMode: `GeneralTests/PlayModePrerequisites.cs` (host session reaches lobby through the real bootstrap).

## Depends on / Used by

- **Used by:** every domain SubSystem (`SubSystems.Get<T>()`); bootstrap order is consumed by [application](application.md), [networking-session](networking-session.md) and [scene-management](scene-management.md)

## Related docs

- [2026-07_session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [INDEX.md](../INDEX.md)
