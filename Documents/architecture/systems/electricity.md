> Code paths: Assets/Scripts/SS3D/Systems/Electricity/
> Entry points: ElectricitySubSystem
> Status: partial

# Electricity

## Overview

Power circuit simulation, APC channel gating, SMES storage, and tile-linked electric devices. Implements `ITileMutationObserver` for [tile](tile.md) placement reactions. Machine UI in [machine-interface](machine-interface.md).

## Start here

- `Assets/Scripts/SS3D/Systems/Electricity/ElectricitySubSystem.cs` — subsystem entry point
- `Assets/Scripts/SS3D/Systems/Tile/Connections/BasicElectricDevice.cs` — tile-placed electric device base

## Extension points

- New powered devices: extend electric device connectors in `Systems/Tile/Connections/`.
- Machine panels: register via [machine-interface](machine-interface.md).

## Depends on / Used by

- **Depends on:** [tile](tile.md)
- **Used by:** [machine-interface](machine-interface.md)

## Related docs

- Architecture efforts: [machine-interface phase 2](../2026-07_machine-interface-phase2-apc-networking.md), [phase 3](../2026-07_machine-interface-phase3-smes-generalization.md)
- Design (read-only): [Documents/design/area.md](../../design/area.md)
