> Code paths: Assets/Scripts/SS3D/Systems/Electricity/
> Entry points: ElectricitySubSystem
> Status: partial
> Verified: 2e2d03815 — 2026-07-18

# Electricity

## Overview

Power circuit simulation, APC channel gating, SMES storage, and tile-linked electric devices under namespace `SS3D.Systems.Electricity`. Implements `ITileMutationObserver` for [tile](tile.md) placement reactions. Machine UI in [machine-interface](machine-interface.md). **Area-scoped power:** devices in an assigned area draw from that area's APC without a cable path to each device; the APC still connects to the station grid via cables. Channel gating and load accounting use [area](area.md) `TryGetEffectiveApcForDevice`. Per-tick APC consumer membership is indexed on `ElectricitySubSystem` (invalidated on device add/remove and area rebuild). Furniture/visuals gate power via `PowerGate`.

## Unit model

- **Flow** (generators, consumers, UI): **kW** — instantaneous demand/supply per tick.
- **Storage** (SMES, APC cell): **kWh** — energy reservoir.
- **Tick interval:** `ElectricitySubSystem._tickRate` (default **0.2 s**). Conversions live in `ElectricityUnits.cs`.
- Per tick: `energy_kWh = power_kW × tickSeconds / 3600`.

## Power distribution

1. **Cable phase** — `Circuit.UpdateCableDistributionOnly`: producers feed cable-distributed consumers; surplus stored in `_pendingProducerSurplus`; SMES discharges on deficit.
2. **Area phase** — `AreaApcPowerDistribution.PowerAreaConsumers`: each APC draws grid headroom (`GetAvailableGridSupplyForArea`), then its cell covers remaining deficit (consumers from per-APC index).
3. **Charge phase** — `Circuit.ChargePendingProducerSurplus`: leftover surplus charges non-APC storages (SMES), respecting per-device charge rate.

**Load shedding:** `PowerConsumerAllocation.AllocateUnderBudget` — under insufficient supply, Equipment sheds first, then Environment, then Lighting (restore order is reverse). Applies to both cable and area distribution.

**HV cables:** underfloor Wire-layer runs connect generators, SMES units, and APCs only. Lights, vending machines, air alarms, airlocks, and other area consumers are not cable-linked; they draw from their area APC.

**Consumer visuals:** `ConsumerPowerVisual` dims emissive materials (and optional panel indicators) from `BasicPowerConsumer` / `MachinePowerConsumer` power status and APC channel gating via `PowerGate`. Wired on vendor/jukebox prefabs, air alarms, and airlock panel lights.

**Balancing defaults (prefabs):** APC cell 5 kWh / 10 kW charge & discharge; SMES 100 kWh / 50 kW, starts charged with output enabled.

## Start here

- `Assets/Scripts/SS3D/Systems/Electricity/ElectricityUnits.cs` — kW ↔ kWh conversion for tick interval
- `Assets/Scripts/SS3D/Systems/Electricity/PowerStorageMath.cs` — shared APC/SMES/battery charge/discharge helpers
- `Assets/Scripts/SS3D/Systems/Electricity/PowerGate.cs` — `IsPowered` / `IsChannelOpen` / `IsEffectivelyPowered`
- `Assets/Scripts/SS3D/Systems/Electricity/PowerConsumerAllocation.cs` — channel-priority consumer budgeting
- `Assets/Scripts/SS3D/Systems/Electricity/ElectricitySubSystem.cs` — subsystem entry point; per-APC consumer index
- `Assets/Scripts/SS3D/Systems/Electricity/AreaApcPowerDistribution.cs` — area APC powers local consumers without per-device cables
- `Assets/Scripts/SS3D/Systems/Electricity/ApcStatusDeriver.cs` — APC power/battery state derivation
- `Assets/Scripts/SS3D/Systems/Electricity/ElectricCableConnectivity.cs` — HV cable links only grid backbone devices
- `Assets/Scripts/SS3D/Systems/Electricity/Circuit.cs` — cable-grid power distribution; per-consumer channel resolver
- `Assets/Scripts/SS3D/Systems/Electricity/LightPower.cs` — fixture visuals from power + area lighting state
- `Assets/Scripts/SS3D/Systems/Electricity/ConsumerPowerVisual.cs` — emissive/panel dimming for generic consumers
- `Assets/Scripts/SS3D/Systems/Electricity/BasicPowerConsumer.cs` — constant-load consumer
- `Assets/Scripts/SS3D/Systems/Electricity/MachinePowerConsumer.cs` — idle/in-use load consumer
- `Assets/Scripts/SS3D/Systems/Tile/Connections/BasicElectricDevice.cs` — tile-placed electric device base
- `Assets/Scripts/SS3D/UI/MachineInterface/ApcController.cs` — APC façade; `IAreaApcOrigin` + storage SyncVars + MI

## Extension points

- New powered devices: attach `BasicPowerConsumer` or `MachinePowerConsumer` plus `ConsumerPowerVisual`; gate gameplay with `PowerGate.IsPowered(..., NullConsumerPolicy)` (Allow = null means powered; Deny = null means unpowered).
- Prefab examples: vendors/jukebox (`MachinePowerConsumer` + `ConsumerPowerVisual`); air alarm/airlocks (`BasicPowerConsumer`, Environment channel); light switch uses [area](area.md) `LightSwitchController` instead.
- HV cable graph: only `IPowerProducer` and `IPowerStorage` participate via `ElectricCableConnectivity.ParticipatesInCableGrid`; consumers draw from area APCs.
- Machine panels: register via [machine-interface](machine-interface.md).
- Area membership changes: Area APC register/unregister/rebuild calls `ElectricitySubSystem.InvalidateAreaConsumerIndex()`.

## Pitfalls

- **Never assign `Inactive` then `Powered` in the same tick.** `PowerStatus` is a SyncVar; OnChange fires on every real transition. Furniture (notably [furniture](furniture.md) airlocks) treats `Inactive` as a power-loss edge. Clear-then-set every ~0.2s tick restarts close timers forever. `PowerAreaConsumers` must write the final status once (and skip no-ops). Cable path in `Circuit` already does single-assignment — keep area path aligned. Test: `PowerAreaConsumers_AssignsFinalStatusOnceWithoutFlicker`.

## Depends on / Used by

- **Depends on:** [tile](tile.md), [area](area.md) (channel gating + lighting state)
- **Used by:** [machine-interface](machine-interface.md); [area](area.md) (`LightSwitchController` lighting-channel consumer)

## Related docs

- Architecture efforts: [mi-area-electricity debt](../2026-07_mi-area-electricity-debt.md), [machine-interface phase 2](../2026-07_machine-interface-phase2-apc-networking.md), [phase 3](../2026-07_machine-interface-phase3-smes-generalization.md), [area foundation](../2026-07_area-foundation.md)
- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md), [electricity_kwh_foundation_917ccdbc.plan.md](../../plans/electricity_kwh_foundation_917ccdbc.plan.md)
- Design (read-only): [Documents/design/area.md](../../design/area.md)
