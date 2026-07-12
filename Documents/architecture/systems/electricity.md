> Code paths: Assets/Scripts/SS3D/Systems/Electricity/
> Entry points: ElectricitySubSystem
> Status: partial

# Electricity

## Overview

Power circuit simulation, APC channel gating, SMES storage, and tile-linked electric devices. Implements `ITileMutationObserver` for [tile](tile.md) placement reactions. Machine UI in [machine-interface](machine-interface.md). **Area-scoped power:** devices in an assigned area draw from that area's APC without a cable path to each device; the APC still connects to the station grid via cables. Channel gating and load accounting use [area](area.md) `TryGetEffectiveApcForDevice`.

## Unit model

- **Flow** (generators, consumers, UI): **kW** — instantaneous demand/supply per tick.
- **Storage** (SMES, APC cell): **kWh** — energy reservoir.
- **Tick interval:** `ElectricitySubSystem._tickRate` (default **0.2 s**). Conversions live in `ElectricityUnits.cs`.
- Per tick: `energy_kWh = power_kW × tickSeconds / 3600`.

## Power distribution

1. **Cable phase** — `Circuit.UpdateCableDistributionOnly`: producers feed cable-distributed consumers; surplus stored in `_pendingProducerSurplus`; SMES discharges on deficit.
2. **Area phase** — `AreaApcPowerDistribution.PowerAreaConsumers`: each APC draws grid headroom (`GetAvailableGridSupplyForArea`), then its cell covers remaining deficit.
3. **Charge phase** — `Circuit.ChargePendingProducerSurplus`: leftover surplus charges non-APC storages (SMES), respecting per-device charge rate.

**Load shedding:** `PowerConsumerAllocation.AllocateUnderBudget` — under insufficient supply, Equipment sheds first, then Environment, then Lighting (restore order is reverse). Applies to both cable and area distribution.

**HV cables:** underfloor Wire-layer runs connect generators, SMES units, and APCs only. Lights, vending machines, air alarms, airlocks, and other area consumers are not cable-linked; they draw from their area APC.

**Consumer visuals:** `ConsumerPowerVisual` dims emissive materials (and optional panel indicators) from `BasicPowerConsumer` / `MachinePowerConsumer` power status and APC channel gating. Wired on vendor/jukebox prefabs, air alarms, and airlock panel lights.

**Balancing defaults (prefabs):** APC cell 5 kWh / 10 kW charge & discharge; SMES 100 kWh / 50 kW, starts charged with output enabled.

## Start here

- `Assets/Scripts/SS3D/Systems/Electricity/ElectricityUnits.cs` — kW ↔ kWh conversion for tick interval
- `Assets/Scripts/SS3D/Systems/Electricity/PowerConsumerAllocation.cs` — channel-priority consumer budgeting
- `Assets/Scripts/SS3D/Systems/Electricity/ElectricitySubSystem.cs` — subsystem entry point
- `Assets/Scripts/SS3D/Systems/Electricity/AreaApcPowerDistribution.cs` — area APC powers local consumers without per-device cables
- `Assets/Scripts/SS3D/Systems/Electricity/ElectricCableConnectivity.cs` — HV cable links only grid backbone devices (`ParticipatesInCableGrid`: producers + storage)
- `Assets/Scripts/Tests/EditMode/ElectricityTests/ElectricCableConnectivityTests.cs` — HV cable eligibility tests
- `Assets/Scripts/SS3D/Systems/Electricity/ElectricityDebugGizmoDrawer.cs` — Scene-view circuit/device diagnostics (`SS3D → Dev → Electricity → Show Grid Gizmos`)
- `Assets/Scripts/SS3D/Systems/Electricity/Circuit.cs` — cable-grid power distribution; per-consumer channel resolver
- `Assets/Scripts/SS3D/Systems/Electricity/LightPower.cs` — fixture visuals from power + area lighting state
- `Assets/Scripts/SS3D/Systems/Electricity/ConsumerPowerVisual.cs` — emissive/panel dimming for generic consumers (vendors, jukebox, air alarms, airlocks)
- `Assets/Scripts/SS3D/Systems/Electricity/BasicPowerConsumer.cs` — constant-load consumer (lights, switches, airlocks)
- `Assets/Scripts/SS3D/Systems/Electricity/MachinePowerConsumer.cs` — idle/in-use load consumer (vendors, jukebox); fires initial power status on client start
- `Assets/Scripts/SS3D/Systems/Tile/Connections/BasicElectricDevice.cs` — tile-placed electric device base
- `Assets/Scripts/SS3D/UI/MachineInterface/ApcController.cs` — APC; implements `IAreaApcOrigin` for area registration

## Extension points

- New powered devices: extend electric device connectors in `Systems/Tile/Connections/`; attach `BasicPowerConsumer` or `MachinePowerConsumer` plus `ConsumerPowerVisual` for emissive meshes (optional `_panelIndicators` for status-light materials).
- Prefab examples: vendors/jukebox (`MachinePowerConsumer` + `ConsumerPowerVisual`); air alarm/airlocks (`BasicPowerConsumer`, Environment channel); light switch uses [area](area.md) `LightSwitchController` instead.
- HV cable graph: only `IPowerProducer` and `IPowerStorage` participate via `ElectricCableConnectivity.ParticipatesInCableGrid`; consumers draw from area APCs.
- Machine panels: register via [machine-interface](machine-interface.md).
- Area-scoped APC channels: resolved in `ElectricitySubSystem` via [area](area.md) `TryGetEffectiveApcForDevice` (circuit-wide OR fallback for unassigned tiles).
- Area-scoped power: `AreaApcPowerDistribution` — APC cell + grid supply powers all consumers in the APC's area; cables only needed for grid backbone to the APC.

## Depends on / Used by

- **Depends on:** [tile](tile.md), [area](area.md) (channel gating + lighting state)
- **Used by:** [machine-interface](machine-interface.md); [area](area.md) (`LightSwitchController` lighting-channel consumer)

## Related docs

- Architecture efforts: [machine-interface phase 2](../2026-07_machine-interface-phase2-apc-networking.md), [phase 3](../2026-07_machine-interface-phase3-smes-generalization.md), [area foundation](../2026-07_area-foundation.md)
- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md), [electricity_kwh_foundation_917ccdbc.plan.md](../../plans/electricity_kwh_foundation_917ccdbc.plan.md)
- Design (read-only): [Documents/design/area.md](../../design/area.md)
