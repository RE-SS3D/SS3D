> Code paths: Assets/Scripts/SS3D/Systems/Furniture/, Assets/Content/WorldObjects/Furniture/
> Entry points: (various world object behaviours); AirLockProximityService
> Status: partial
> Verified: bf0356444 — 2026-11-04

# Furniture / world objects

## Overview

Station furniture and interactable world objects — airlocks, lockers, disposal units, vending machines, jukebox, and related prefab behaviours. Steel/wood/glass tables, three locker variants, thirteen opted-in machines, and civilian/security airlocks share [structural damage](structural-destruction.md): tabletop dents/tears for steel, grain-following gouges/splits for wood, branching fractures for glass, side-casing damage on machines, and marks on moving door leaves, plus material-specific contact bursts. Destruction removes furniture without its floor; lockers/machines release stored items first, including through locked removal gates. Power consumption and visuals delegate to [electricity](electricity.md) consumers; airlock opening respects area APC power. Vending machines open a diegetic panel via [machine-interface](machine-interface.md) instead of a direct dispense interaction. Disposal chute/outlet gameplay lives under [disposal](disposal.md).

Steel tables leave procedural collapsed tops and legs through [structural-destruction](structural-destruction.md); remains do not block movement and clear when rebuilt.

## Start here

- `Assets/Scripts/SS3D/Systems/Furniture/Locker.cs` — door + ID lock; implements `IStorageAccessGate` so view/store only while open; closing door closes storage panels; door petal shows Open/Close by state, lock/unlock use the `Lock`/`Unlock` padlock icons
- `Assets/Scripts/SS3D/Systems/Furniture/AirLockProximityService.cs` — server HashGrid index; player-centric proximity tick (not per-door FixedUpdate); hoists MapEditor/EntitySubSystem + passes `SpawnedPlayers` into each door
- `Assets/Scripts/SS3D/Systems/Furniture/AirLockOpener.cs` — padded proximity auto-open; click Open/Close (`AirLockDoorInteraction`); access-denied SFX + door-light blink; SyncVar→local Animator (no NetworkAnimator); power-gated; `IDynamicTileOccupant` notifies [atmospherics](atmospherics.md)
- `Assets/Scripts/SS3D/Systems/Furniture/AirLockDoorInteraction.cs` — Open requires ID; Close does not; manual open still auto-closes
- `Assets/Scripts/SS3D/Systems/Furniture/AirlockAudioTrackIds.cs` — `AirlockDeny` clip id ([audio](audio.md))
- `Assets/Scripts/SS3D/Systems/Furniture/AirlockStateMachine.cs` — open/close SFX + door-light colors via `AirLockOpener.SetDoorLightColor`
- `Assets/Scripts/SS3D/UI/MachineInterface/VendingMachineController.cs` — vending machines (machine-interface controller)
- `Assets/Scripts/SS3D/Systems/Audio/Boombox.cs` — jukebox toggle; stops audio on power loss
- `Assets/Content/WorldObjects/Furniture/Machines/Vendors/` — vending machine prefabs
- Disposal: see [disposal](disposal.md) (`DisposalBin`, `DisposalOutlet`, `DisposalSubSystem`)

- `Assets/Scripts/SS3D/Systems/StructuralDamage/Editor/StructuralIntegrityPrefabSetup.cs` — tier-B presentation/examine recipe for supported tables, lockers, machines and civilian/security airlocks

## Extension points

- New furniture interactions: add `InteractionTargetNetworkBehaviour` (or `IInteractionTarget` on an existing networked behaviour) + domain interactions per [interactions-framework](interactions-framework.md).
- Powered machines: attach `MachinePowerConsumer` or `BasicPowerConsumer` + `ElectricDeviceAdjacencyConnector`; add `ConsumerPowerVisual` when emissive meshes should dim unpowered (see [electricity](electricity.md)).
- Panel-driven machines: subclass `MachineInterfaceBehaviour` and register UI per [machine-interface](machine-interface.md) extension recipe.
- Wall light switches: use [area](area.md) `LightSwitchController`, not furniture scripts.

## Pitfalls

- **Room access edits do not affect doors:** civilian/security skins serialized fixed masks and disabled area defaults. `AirLockAccessGate` now resolves assigned-room explicit access, then department defaults; legacy skin masks (migrated via `FormerlySerializedAs`) are only fallbacks for unassigned doors. Public assigned rooms stay public.


- **SS13-scale airlock FixedUpdate:** hundreds of doors each calling `SpawnedPlayers.ToList()` + `GetComponent` every physics tick (~814× on Metastation). Proximity is owned by `AirLockProximityService` (HashGrid neighborhood of players only). Empty close passes use `_pendingEmptyPass` (doors with occupants), not a walk of all `_openerCells` — `EmptyPassSweep` must stay O(pending). Do not re-add per-door `FixedUpdate`. Hit 2026-07-28 (perf capture).
- **Airlock proximity GC per tick:** do not prune `_deniedOccupants` with `RemoveWhere` capturing `_proximityScratch` (allocates a display class + delegate every call). Use a list prune. Do not `TryGet` MapEditor/EntitySubSystem inside each door — hoist in the service and pass `SpawnedPlayers`. Do not read `renderer.sharedMaterials` on door-light updates (new array every call); resolve color property once and set via MPB material index. Hit 2026-07-29.
- **Airlock panel SMR vs bones:** `AirlockPanel` uses `SkinnedMeshRenderer` for blend shapes (`Confirm`/`On`), not skeletal bones. That path breaks SRP Batcher grouping vs `MeshRenderer`. Replacing with MeshRenderer + MPB/emissive is a content change, not a zero-cost swap. Hit 2026-07-29.
- **Door animator — no NetworkAnimator:** open state is already a SyncVar (`_isOpen` → `OnOpenChanged` → `_animator.SetBool`). A FishNet `NetworkAnimator` on every airlock re-polled that bool every network tick on the host (~1.5k doors × Metastation) for a result the SyncVar already produced. Do **not** re-add `NetworkAnimator` to airlock prefabs. Keep prefab `Animator.cullingMode = CullCompletely`. Do not AOI-toggle `Animator.enabled` (`KeepAnimatorStateOnDisable: 0` desyncs Open). DisposalBin/Outlet still use `NetworkAnimator.Play` for one-shots — leave those. Hit 2026-07-29 (deep profile).
- **Airlock `CreateTargetInteractions` alloc on hover:** returning `new IInteraction[] { new AirLockDoorInteraction(...) }` every outline probe GC-spikes when looking at doors. Cache the interaction instance + array on the opener; only refresh `Name`. Hit 2026-07-28.
- **Client airlocks never open:** remotes are NetworkTransformed on the server — `CharacterController.Move` never runs for them, so Unity triggers / CC Overlap miss. Disable CC when `!IsOwner` so NT can drive the server transform. Probe `EntitySubSystem.SpawnedPlayers` with `Collider.ClosestPoint` + padding (~0.85m): closed door solids stop characters just outside the trigger OBB, so a strict contains check never fires. Drive `Animator.Open` from the `_isOpen` SyncVar OnChange. Hit 2026-07-26.
- **Access-denied spam at a locked door:** unauthorized players in the padded proximity volume must only trigger deny once per approach (`_deniedOccupants` edge). Calling `ServerPlayAccessDenied` every FixedUpdate floods SFX/blinks. Clear the denied set when they leave or on power loss. Hit 2026-07-26 (design).
- **Security cannot open civilian airlocks:** Security role preset omitted `AccessLevel.Civilian` (bit 128). Civilian doors require that bit; host and client both fail access. SecurityOfficer preset + `Security.asset` include Civilian. Hit 2026-07-26.
- **Airlocks stuck open after leaving the trigger:** `AirLockOpener` closes on `OnPowerStatusUpdated(Inactive)`. If [electricity](electricity.md) `PowerAreaConsumers` writes `Inactive` then `Powered` every tick (~0.2s), the SyncVar OnChange restarts the 2s close timer forever. Assign final `PowerStatus` once per consumer; never clear-then-set. Defense: `ScheduleCloseAfterDelay` must not restart an already-running timer. Regression test: `PowerAreaConsumers_AssignsFinalStatusOnceWithoutFlicker`. Hit three times (91b053c1d, Jul 16 uncommitted, 2026-07-18).

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [interactions-runtime](interactions-runtime.md), [tile](tile.md), [electricity](electricity.md), [area](area.md) (airlock/switch area resolution), [atmospherics](atmospherics.md) (airlock passability), [audio](audio.md) (deny/open/close SFX), [id-access](id-access.md), [machine-interface](machine-interface.md) (vending), [disposal](disposal.md) (chute/outlet behaviours)
- **Used by:** world scenes and construction content

## Related docs

- [disposal](disposal.md) — disposal item network
- [machine-interface](machine-interface.md) — vending UI and diegetic shell
- Effort: [2026-07_metastation-scale-perf.md](../../archive/architecture/2026-07_metastation-scale-perf.md) — airlock proximity invert + Map Editor sim suspend
- Plan: [areas_implementation_plan_c0639343.plan.md](../../archive/plans/areas_implementation_plan_c0639343.plan.md)
- [INDEX.md](../INDEX.md)
