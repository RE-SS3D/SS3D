> Code paths: Assets/Scripts/SS3D/Systems/Electricity/
> Entry points: ElectricitySubSystem
> Status: partial
> Verified: 654199cdc — 2026-11-09

# Electricity

## Overview

Power circuit simulation, APC channel gating, SMES storage, and tile-linked electric devices under namespace `SS3D.Systems.Electricity`. Implements `ITileMutationObserver` for [tile](tile.md) placement reactions. Machine UI in [machine-interface](machine-interface.md). **Area-scoped power:** devices in an assigned area draw from that area's APC without a cable path to each device; the APC still connects to the station grid via cables. Channel gating and load accounting use [area](area.md) `TryGetEffectiveApcForDevice`. Per-tick APC consumer membership is indexed on `ElectricitySubSystem` (invalidated on device add/remove and area rebuild). Furniture/visuals gate power via `PowerGate`. **Solar:** `SolarPanel` producers on the HV cable grid with aim × stub `SolarCycle` output; `SolarTrackingBeacon` auto-aims nearby panels (world Toggle, no MI).

### Unit model

- **Flow** (generators, consumers, UI): **kW** — instantaneous demand/supply per tick.
- **Storage** (SMES, APC cell): **kWh** — energy reservoir.
- **Tick interval:** `ElectricitySubSystem._tickRate` (default **0.2 s**). Conversions live in `ElectricityUnits.cs`.
- Per tick: `energy_kWh = power_kW × tickSeconds / 3600`.

### Power distribution

1. **Cable phase** — `Circuit.UpdateCableDistributionOnly`: producers feed cable-distributed consumers; surplus stored in `_pendingProducerSurplus`; SMES discharges on deficit.
2. **Area phase** — `AreaApcPowerDistribution.PowerAreaConsumers`: each APC draws grid headroom (`GetAvailableGridSupplyForArea`), then its cell covers remaining deficit (consumers from per-APC index).
3. **Charge phase** — `Circuit.ChargePendingProducerSurplus`: leftover surplus charges non-APC storages (SMES), respecting per-device charge rate.

**Load shedding:** `PowerConsumerAllocation.AllocateUnderBudget` — under insufficient supply, Equipment sheds first, then Environment, then Lighting (restore order is reverse). Applies to both cable and area distribution.

**HV cables:** underfloor Wire-layer runs connect generators, SMES units, and APCs only. Lights, vending machines, air alarms, airlocks, and other area consumers are not cable-linked; they draw from their area APC.

**Consumer visuals:** `ConsumerPowerVisual` dims emissive materials (and optional panel indicators) from `BasicPowerConsumer` / `MachinePowerConsumer` power status and APC channel gating via `PowerGate`. Wired on vendor/jukebox prefabs, air alarms, and airlock panel lights.

**Power loss, two batteries:** an area's APC cell carries it first: when the grid can't cover the load the area goes Emergency (dim red) and sheds Equipment, then Environment, then Lighting as the cell drains. Under that, every emergency-capable fixture has its own small cell (`FixtureEmergencyCell`, 120 s run, 60 s recharge on mains). Whenever its APC can't light it — APC cell flat, lighting shed or switched off at the APC, APC broken or gone with its wall — the fixture runs on its own cell in Emergency until that runs down, then goes dark. The wall light switch is a choice, not a power cut: it turns lights off without the cell. A cell only arms once the fixture has been lit from mains, so round start and Map Editor placement stay dark.

**Breakable APC:** the APC carries a `BreakableDevice` (60 integrity: five M4 rounds, two crowbar swings). Rounds and swings that reach its casing hit it instead of the wall behind; a blast hitting its wall tile damages it too. A broken APC stays on the wall with its panel dark, reports every channel off, neither charges nor discharges its cell and offers nothing to open. Its room drops straight to the fixture cells. Repair is a later pass (explosives-destruction.md §9).

**Breakable bulbs:** each bulb and tube fixture carries a `LightFixtureBulb` with a synced socket state (Intact / Broken / Empty). A ballistic or energy round with brute or burn damage that crosses an intact bulb's glass before it lands breaks it: the fixture swaps in its `*Broken` mesh, plays a small glass break (`LightFixtureAudioTrackIds`) and `LightPower` stays Off whatever the power. The round carries on through the glass. An empty hand removes whatever is in the socket (a whole bulb, or a broken one as rubbish). A held `LightBulb` / `LightTube` item of the fixture's kind inserts into an empty socket, or replaces a broken one, which drops to the floor. Spares are in the YouTool vendor (10 each). Socket state is round-scoped and not saved with the station template.

**Balancing defaults (prefabs):** APC cell 5 kWh / 10 kW charge & discharge; SMES 100 kWh / 50 kW, starts charged with output enabled; solar panel peak 2 kW.

## Start here

- `Assets/Scripts/SS3D/Systems/Electricity/ElectricityUnits.cs` — kW ↔ kWh conversion for tick interval
- `Assets/Scripts/SS3D/Systems/Electricity/PowerStorageMath.cs` — shared APC/SMES/battery charge/discharge helpers
- `Assets/Scripts/SS3D/Systems/Electricity/PowerGate.cs` — `IsPowered` / `IsChannelOpen` / `IsEffectivelyPowered`
- `Assets/Scripts/SS3D/Systems/Electricity/PowerConsumerAllocation.cs` — channel-priority consumer budgeting
- `Assets/Scripts/SS3D/Systems/Electricity/ElectricitySubSystem.cs` — subsystem entry point; `IWorldReady`; awaits `AreasFlooded` then notifies `ElectricityReady`; per-APC consumer index; `_circuitByDevice` O(1) lookup
- `Assets/Scripts/SS3D/Systems/Electricity/AreaApcPowerDistribution.cs` — area APC powers local consumers without per-device cables
- `Assets/Scripts/SS3D/Systems/Electricity/ApcStatusDeriver.cs` — APC power/battery state derivation
- `Assets/Scripts/SS3D/Systems/Electricity/ElectricCableConnectivity.cs` — HV cable links only grid backbone devices
- `Assets/Scripts/SS3D/Systems/Electricity/Circuit.cs` — cable-grid power distribution; per-consumer channel resolver
- `Assets/Scripts/SS3D/Systems/Electricity/GameplayLightGuard.cs` — station startup disables orphan lights; preserves `LightPower` fixtures and isolated `CharacterPreviewBooth` lights
- `Assets/Scripts/SS3D/Systems/Electricity/LightPower.cs` — fixture visuals from power + area lighting state; client path uses floor-cache area id + lighting snapshot
- `Assets/Scripts/SS3D/Systems/Electricity/FixtureEmergencyCell.cs` — a fixture's own battery (arm on mains, drain while it carries the light); `LightPower` ticks it on the electricity tick
- `Assets/Scripts/SS3D/Systems/StructuralDamage/BreakableDevice.cs` — machine integrity → broken (APC); `BreakableDeviceResolver` finds one along a shot or swing
- `Assets/Scripts/SS3D/Systems/Electricity/LightFixtureBulb.cs` — bulb socket SyncVar, glass hit box (`TryFindStruck`), break / remove / insert; `LightBulbRules.cs` holds the pure rules
- `Assets/Scripts/SS3D/Systems/Electricity/LightBulbItem.cs` — loose bulb/tube item; offers `InsertLightBulbInteraction`. Empty hands get `RemoveLightBulbInteraction` from the fixture
- `Assets/Scripts/SS3D/Systems/Electricity/Editor/LightFixturePrefabSetup.cs` — builds the four bulb items from `Lights.fbx`, wires both fixtures, stocks YouTool
- `Assets/Scripts/SS3D/Systems/Electricity/ConsumerPowerVisual.cs` — emissive/panel dimming for generic consumers
- `Assets/Scripts/SS3D/Systems/Electricity/BasicPowerConsumer.cs` — constant-load consumer
- `Assets/Scripts/SS3D/Systems/Electricity/MachinePowerConsumer.cs` — idle/in-use load consumer
- `Assets/Scripts/SS3D/Systems/Electricity/FuelPowerGenerator.cs` — Pacman generator (`IPowerProducer` + toggle FX; running loop via buffered `RpcSyncRunningSound`)
- `Assets/Scripts/SS3D/Systems/Electricity/SolarCycle.cs` — stub sun azimuth / intensity / eclipse for solar output
- `Assets/Scripts/SS3D/Systems/Electricity/SolarPanel.cs` — solar `IPowerProducer` + Rotate aim
- `Assets/Scripts/SS3D/Systems/Electricity/SolarTrackingBeacon.cs` — auto-aim nearby panels (Toggle, no MI)
- `Assets/Scripts/SS3D/Systems/Electricity/Editor/ElectricityContentPrefabRecipes.cs` — tier-B solar and light-fixture prefab wiring aggregator
- `Assets/Scripts/SS3D/Systems/Electricity/MachineVibrate.cs` — client vibrate FX; capture rest rotation on enable (not `OnStart`)
- `Assets/Scripts/SS3D/Systems/Tile/Connections/BasicElectricDevice.cs` — tile-placed electric device base
- `Assets/Scripts/SS3D/UI/MachineInterface/ApcController.cs` — APC façade; `IAreaApcOrigin` + storage SyncVars + MI

## Extension points

- New powered devices: attach `BasicPowerConsumer` or `MachinePowerConsumer` plus `ConsumerPowerVisual`; gate gameplay with `PowerGate.IsPowered(..., NullConsumerPolicy)` (Allow = null means powered; Deny = null means unpowered).
- Prefab examples: vendors/jukebox (`MachinePowerConsumer` + `ConsumerPowerVisual`); air alarm/airlocks (`BasicPowerConsumer`, Environment channel); light switch uses [area](area.md) `LightSwitchController` instead.
- HV cable graph: only `IPowerProducer` and `IPowerStorage` participate via `ElectricCableConnectivity.ParticipatesInCableGrid`; consumers draw from area APCs. Solar panels follow the Pacman producer path (FurnitureBase + `ElectricDeviceAdjacencyConnector` + Wire cable to SMES/APC).
- Solar content prefabs: re-run `SS3D/Electricity/Run Content Prefab Recipes` (`SolarPrefabSetup`) after merge conflicts — do not hand-edit mega-prefab YAML. Recipe nests the full `SolarPanel.fbx` (base/arm/armature/panel) under a `Model` child; do not extract a single mesh.
- Breakable machines: add `BreakableDevice` and gate the machine on `IsBroken` (see `ApcController.Channels`); `ApcPrefabSetup` is the example recipe.
- Light fixtures and bulb items: re-run `SS3D/Electricity/Run Content Prefab Recipes` (`LightFixturePrefabSetup`). A new fixture kind needs `*` and `*Broken` socket meshes, a `LightBulbKind`, and a matching item pair.
- Machine panels: register via [machine-interface](machine-interface.md). Solar tracker/panels intentionally have **no** MI (diegetic Toggle / Rotate only).
- Area membership changes: Area APC register/unregister/rebuild calls `ElectricitySubSystem.InvalidateAreaConsumerIndex()`.
- Powered-device init: await `ElectricityReady` / `WorldReady` (`IWorldReady` / `WorldReadinessSubSystem`) — obsolete `IsSetUp` is an `IsReady` shim only.

## Pitfalls

- **Deleted APC kept powering its room:** `ApcController` / `BasicElectricDevice` unregistered in `OnDestroyed` behind `if (IsServer)`. FishNet clears `IsServer` during despawn, before Unity's `OnDestroy`, so the unregister never ran: lights stayed on until a later graph rebuild pruned the device, and a replacement APC hit "A room can have only one APC". Unregister server state in `OnStopServer`; `AreaPaintingService` also skips Unity-destroyed APCs. Hit 2026-11-01.
- **Powered machines never glowed (host and client):** the July MPB conversion of `ConsumerPowerVisual` kept one renderer-wide block seeded from the *first* emissive material. Multi-material machines (vendors, jukebox: `Palette` + `PaletteEmission`) got `Palette`'s black emission written onto the glow submesh. All emissive power visuals (`ConsumerPowerVisual`, `LightSwitchController`, `AirAlarmController`, `LightPower`) now go through `EmissiveMaterialSlots`: per-material blocks, each material's own powered values scaled/tinted. Do not reintroduce a renderer-wide emissive block. Hit 2026-10-31.
- **Server-only events subscribed in `OnStartClient`:** `GenericToggleInteractionTarget.OnToggle` is raised on the server. `FuelPowerGenerator` and `SolarTrackingBeacon` subscribed in `OnStartClient`, which a dedicated server never runs — the toggle animated but the PACman never produced power. Subscribe server handlers in `OnStartServer`. Host play hides this. Hit 2026-10-31.
- **Early lobby character appears black:** the orphan-light sweep in `AreaSubSystem.CompleteSetup` runs after the preview booth can be created. Exempt `CharacterPreviewBooth`-owned lights as well as `LightPower`; enabling `Light.enabled` alone cannot restore a deactivated light GameObject.

- **`CircuitsTick` GC at SS13 scale:** per-tick `new List<>` / LINQ `ToList` in area APC power and `Circuit` cable distribute (~25 MB / 76 ticks on Metastation). Hot path must reuse scratch buffers: `FillActiveConsumers` / `AllocateUnderBudget(..., results)` / `PowerAreaConsumers(..., poweredScratch, poweredSetScratch)` and `Circuit` instance scratches. Do not restore allocating helpers on the 0.2 s tick — including `TryGetApcCircuitStats` (MI path). **Also:** `AreaRegistry.GetAllAreas()` must not allocate a fresh List each tick (cache until Register/Unregister); `FillActiveConsumers` must take `IReadOnlyList` + index — foreach over `IEnumerable` boxes List's enumerator (~40 B/APC). Hit 2026-07-28 / 2026-07-29.
- **`TryGetCircuitForDevice` must stay O(1):** scanning `_circuits` × `ContainsDevice` was ~507 membership tests per APC lookup × two calls/area/tick on Metastation. Rebuild `_circuitByDevice` in `UpdateAllCircuitsTopology`; do not reintroduce linear scans on the tick or MI stats path. Hit 2026-07-29 (deep profile).
- **Cable filter must not re-resolve area each consumer:** `FillActiveConsumers` used to call `IsAreaScopedConsumer` + channel resolver (each → `TryGetEffectiveApcForDevice`) per consumer per tick. Rebuild `_areaScopedConsumers` / `_apcByConsumer` in `RebuildApcConsumerIndex` and use those in the circuit lambdas. Hit 2026-07-29.
- **`SS3D.Electricity.FixedUpdate` / OnTick fan-out:** do not subscribe every `ConsumerPowerVisual` to `ElectricitySubSystem.OnTick` — at Metastation door counts that rewrites emissives (and used to alloc `Renderer.materials`) every 0.2 s. Drive visuals from `OnPowerStatusUpdated` (+ one-shot `WhenReady`); cache material refs in `CacheVisuals`. Hit 2026-07-28.
- **`FixedUpdate` ≫ `CircuitsTick` self-time:** the FixedUpdate marker wraps CircuitsTick **and** `OnTick` subscriber fan-out. Zoom-out hitches often coincide with a 0.2 s tick: CircuitsTick may be ~6 ms while OnTick was the rest. Marker `SS3D.Electricity.OnTick` splits the bucket. Hit 2026-07-28.
- **Lighting bypass ignores manually placed fixtures:** `LightPower.ShouldBeLit` returned false when `!_hasArea` before applying `LightingDevBypass`. No-APC / Map-Editor place stays dark with **SS3D/Dev/Lighting/Always Power Light Fixtures** on. Bypass still respects area Dark + channel policy when an area exists. Hit 2026-07-28.
- **`RemoveElectricalElement` must not require a live `TileObject`.** `BasicElectricDevice.TileObject` is null during `OnDestroyed` (Unity fake-null). Bailing on null left zombies in `_registeredDevices` → NRE in `RebuildElectricGraph`/`ToCoordinates` after `TileMap.Clear` (Map Editor load, DMM import with Clear). Unregister by device reference; prune null-`TileObject` entries on rebuild. Hit 2026-07-28.
- **`TileMap.Clear` vs FishNet despawn:** Clear empties `_chunks` before despawn finishes. Orphan cables still report `TileObject` but `GetChunk` is null → NRE in `ElectricNeighbourLookup.GetElectricDevicesOnSameTile`. Neighbour lookup must null-check map/chunk; rebuild prunes chunkless devices. Hit on MetaStation DMM import 2026-07-28.
- **Interface-typed destroyed devices throw, not null.** `IElectricDevice device?.TileObject` does **not** Unity-null-check — a destroyed `ApcController` still invokes `get_TileObject` → `MissingReferenceException`. Use `device is Object u && u` before `TileObject`, and keep `ApcController.TileObject` as `this ? GetComponent… : null` (same as `BasicElectricDevice`). Full DMM import suspends circuit ticks + clears the registry around Clear/place. Hit 2026-07-28.
- **Walls invisible until adjacency refresh finishes:** DMM import places with `skipAdjacency`, then `RefreshAllAdjacencies`. Airlocks are full meshes so they appear first; walls/windows need the adjacency pass. Mid-import electricity spam can make a full MetaStation import look “doors only” until the apply finishes cleanly.
- **Never assign `Inactive` then `Powered` in the same tick.** `PowerStatus` is a SyncVar; OnChange fires on every real transition. Furniture (notably [furniture](furniture.md) airlocks) treats `Inactive` as a power-loss edge. Clear-then-set every ~0.2s tick restarts close timers forever. `PowerAreaConsumers` must write the final status once (and skip no-ops). Cable path in `Circuit` already does single-assignment — keep area path aligned. Test: `PowerAreaConsumers_AssignsFinalStatusOnceWithoutFlicker`.
- **`PowerStatus` setter must allow EditMode/offline.** Guard pure clients with `NetworkObject != null && NetworkObject.IsSpawned && !IsServer` (not bare `!IsServer`). Bare `IsServer` NREs when `_networkObjectCache` is null, and treating all non-server as skip leaves Circuit EditMode tests stuck at `Inactive`. Same pattern as [tile](tile.md) adjacency SyncVar publishes / `PlacedTileObject.CanWriteIntegritySyncVars`.
- **Client light fixtures ignore APC / wall-switch toggles:** Host `LightPower` can read live APC channels from the area registry; pure clients cannot. Fixture lit mode is a **server SyncVar** (`LightPower._fixtureVisual`); clients only apply it. Do not re-derive emit on clients from area/obsolete `IsSetUp`. `ApcController.OnChannelsChanged` refreshes fixtures on the server so the SyncVar updates immediately.
- **Machine "Turn on" snaps facing to prefab/default:** `MachineVibrate` used to cache rest rotation in `OnStart`, then force it when Enable becomes true. Tile-placed machines (Pacman / `FuelPowerGenerator`) often get their final yaw later (spawn sync, `PlacedTileObject` direction). Capture rest pose when vibration starts, restore it when stopping — never from a stale Start snapshot.
- **Solar aim visual vs placement yaw:** `SolarPanel` SyncVar aim drives world yaw independently of tile placement direction. Never assign `rotation = Euler(0, yaw, 0)` on an FBX armature bone — that zeros Blender rest pitch/roll and tumbles the rig toward north. Capture rest, strip world-Y, then `AngleAxis(yaw, up) * restNoYaw`. Prefer aim transform = `Model` (FBX root), not a skinned bone.

- **`OnTick` handlers subscribed in `OnStartClient` must unsubscribe in `OnStopClient`.** A client that leaves an object's observer range and comes back runs `OnStartClient` again. `SmesBattery` unsubscribed only in `OnDestroyed`, so the first handler outlived the SMES and threw `MissingReferenceException` on its destroyed renderer every tick after a round reload. Hit 2026-11-03 by the space ragdoll scenario.
- **Generator silent on remote clients and late joiners:** `AudioSubSystem.PlayAudioSource`/`StopAudioSource` are `[Server]`, so a client SyncVar hook calling them only logs and returns, and a one-shot play reaches only observers present at that moment. Start/stop looping machine sounds from the server through a `BufferLast` ObserversRpc that calls the `*Local` variants (`FuelPowerGenerator.RpcSyncRunningSound`). Hit 2026-11-05 (#153).

- **Fixture collider on its own layer:** fixtures need a collider for reach checks and the interaction point, but `LineOfSight` raycasts hit triggers, so a trigger on the fixture's layer stopped rounds before the glass and could block cover checks. The trigger lives on a `ReachVolume` child on Ignore Raycast; range checks and `SelectionTargetUtility` still find it. Hit 2026-11-09.
- **Wall mounts sit inside their wall's collider:** walls collide as full tiles, so the structural hit lands about 0.16 m in front of a mounted bulb's glass and the round never reached it. A wall hit tests bulbs up to `LightFixtureBulb.WallInset` (0.35 m) past the landing point; a fixture on the wall's far side is about 0.8 m in, so it stays safe. Hit 2026-11-09.

- **APC collider was under the panel, inside the wall:** the prefab's box sat at floor height in the wall's middle while the panel is drawn at chest height on its face, so rounds and swings never reached it. `ApcPrefabSetup` fits it to the mesh; `WallMountColliderSetup` does the same for the light switch and air alarm, which had the same mis-authored box. `WallMountColliderTests` checks every wall mount's colliders sit on its panel. The air alarm also lacked a baked `PlacedTileObject` (so remote clients never had it in their tilemap); re-saving it through the recipe bakes it and rebuilds its NetworkBehaviour list. Hit 2026-11-09.

## Tests

`Assets/Scripts/Tests/EditMode/ElectricityTests/`: circuit distribution, cable connectivity, consumer allocation, `PowerGate`, APC status/consumer index, area APC distribution and lighting state/fixture policy, solar generation, bulb rules and fixture wiring (`LightFixtureBulbTests`), fixture cells and the broken APC (`PowerLossLightingTests`). Run `Testing/ui/validate.sh --steps compile,editmode`. PlayMode `LightBulbScenarios` (Combat category) fires the server path at a test-room fixture, then removes and replaces the bulb by hand. `PowerLossScenarios` shoots the test room's APC until it breaks and blows its wall away, and checks the room runs on fixture cells, then goes dark.

## Depends on / Used by

- **Depends on:** [tile](tile.md), [area](area.md) (channel gating + lighting state), [rounds-lobby](rounds-lobby.md) (preview-booth light ownership)
- **Used by:** [combat](combat.md) (ranged rounds break bulbs); [machine-interface](machine-interface.md); [area](area.md) (`LightSwitchController` lighting-channel consumer)

## Related docs

- Architecture efforts: [solar-generation](../../archive/architecture/2026-07_solar-generation.md), [mi-area-electricity debt](../../archive/architecture/2026-07_mi-area-electricity-debt.md), [machine-interface phase 2](../../archive/architecture/2026-07_machine-interface-phase2-apc-networking.md), [phase 3](../../archive/architecture/2026-07_machine-interface-phase3-smes-generalization.md), [area foundation](../../archive/architecture/2026-07_area-foundation.md), [session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md)
- Plan: [areas_implementation_plan_c0639343.plan.md](../../archive/plans/areas_implementation_plan_c0639343.plan.md), [electricity_kwh_foundation_917ccdbc.plan.md](../../archive/plans/electricity_kwh_foundation_917ccdbc.plan.md)
- Design (read-only): [Documents/design/electricity.md](../../design/electricity.md)
