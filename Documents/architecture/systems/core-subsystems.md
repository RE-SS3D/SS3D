> Code paths: Assets/Scripts/SS3D/Core/, Assets/Scripts/SS3D/Systems/Bootstrap/, Assets/Scripts/SS3D/Systems/WorldReadiness/, Assets/Scripts/SS3D/Networking/NetworkSystemsHub.cs
> Entry points: SubSystem, NetworkSubSystem, SubSystems, SystemsBootstrap, WorldReadinessSubSystem, NetworkSystemsHub
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Core / SubSystems

## Overview

Base actor/subsystem pattern and runtime service locator. All gameplay domains expose a `*SubSystem` registered via `SubSystems.Get<T>()`. `NetworkSubSystem` extends FishNet `NetworkActor` for networked subsystems.

Process-wide services: `SystemsBootstrap` (DDOL, `AfterSceneLoad`; creates network session, command-line, player settings, scene, input, world readiness, screen effects, ambience/personal audio, automation, vision, server-status publisher, client main menu, then `ApplicationInitializerSubSystem` last). World/session networked systems: `NetworkSystemsHub` Resources prefab (spawned Online). Boot/Game do not place per-system SubSystem GameObjects ([session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md)).

## Start here

- `Assets/Scripts/SS3D/Core/Behaviours/SubSystem.cs` — non-networked subsystem base (bootstrap-owned)
- `Assets/Scripts/SS3D/Core/Behaviours/NetworkSubSystem.cs` — networked subsystem base (hub prefab only)
- `Assets/Scripts/SS3D/Core/Subsystems.cs` — locator; FindObject fallback skipped while quitting / WaitingForServer
- `Assets/Scripts/SS3D/Core/WorldReadiness/IWorldReady.cs` + `WorldReadyPhase.cs` — readiness contract
- `Assets/Scripts/SS3D/Systems/WorldReadiness/WorldReadinessSubSystem.cs` — phase coordinator
- `Assets/Scripts/SS3D/Systems/Bootstrap/SystemsBootstrap.cs` — DDOL process-wide services
- `Assets/Scripts/SS3D/Networking/NetworkSystemsHub.cs` + `Assets/Resources/NetworkSystemsHub.prefab` — Online hub
- `Assets/Scripts/SS3D/Editor/Bootstrap/SessionWorldLifecycleEditorMenus.cs` — hub rebuild menu + its type list; scene strip is Tier C (no menu)
- `Assets/Scripts/SS3D/Core/Debugging/DebugTools.cs` — named F1 debug-tool registry (usage in [inputs](inputs.md), [ui-shell](ui-shell.md))

## Extension points

- New process-wide SubSystem: add to `SystemsBootstrap.EnsureProcessWideServices`, before `ApplicationInitializerSubSystem`. Types in assemblies `SS3D.Systems` cannot reference (Networking, CommandLine, Launcher) go through `EnsureSubSystemType` by assembly-qualified name.
- New networked domain: add type to hub rebuild menu list, run `SS3D/Bootstrap/Rebuild NetworkSystemsHub Prefab` — do not edit Game.unity.
- World sim: implement `IWorldReady`; notify via `WorldReadinessSubSystem` after TileMapLoaded / AreasFlooded — never treat `OnMapCreated` as ready.
- Consumers: `IsReady` / `WhenReady` / `WaitUntilAsync` — not `Get` in Update.

## Pitfalls

- **Registration ≠ readiness.** Await `WorldReadyPhase` before round start / sim ticks that need flooded areas.
- **Missing Get during Disconnecting / WaitingForServer is silent** — use `TryGet`.
- **Bootstrap order and Test Runner cold start** are silent failures too — see [application](application.md) Pitfalls.
- **Game content prefabs may still host SubSystems** (`PlayerCameraSubSystem`, Radial/Armed interaction overlays, `MapEditorSubSystem`) that register before hub Online — Phase 3 only stripped Boot/Game systems roots. Consumers must `TryGet` / lazy-resolve; relocating those components onto bootstrap/hub is residual cleanup ([session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md) post-ship note).
- **Do not AddComponent NetworkSubSystems at runtime** — edit-time on hub prefab only (FishNet behaviour list).
- Hub despawn on disconnect unregisters via `OnDestroyed` — no extra teardown required.
- **Coimbra `GetChecked` / `EventSystem` Editor GC:** PackageCache `ServiceLocator.GetChecked` interpolates assert strings every call; deep profiles also show `String.Format` paired with every `EventSystem.Invoke`. Prefer caching `IEventService` and `SubSystems` refs on hot loops we own; do not edit PackageCache. Tracked in [TECH_DEBT.md](../TECH_DEBT.md) §2.

## Tests

- EditMode: `PlayModePrerequisiteTests`, `DebugToolsTests`. PlayMode: `GeneralTests/PlayModePrerequisites.cs` (host session reaches lobby through the real bootstrap).

## Depends on / Used by

- **Used by:** every domain SubSystem (`SubSystems.Get<T>()`); bootstrap order is consumed by [application](application.md), [networking-session](networking-session.md) and [scene-management](scene-management.md)

## Related docs

- [2026-07_session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [INDEX.md](../INDEX.md)
