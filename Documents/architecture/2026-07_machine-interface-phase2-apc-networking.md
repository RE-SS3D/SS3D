> Implements: Documents/design/area.md §power distribution assumptions, Documents/design/main-hud.md §operator workflows
> Touches systems: electricity backend, APC interaction flow, FishNet snapshots, machine-interface host
> Status: shipped

# Machine Interface Phase 2 APC Networking

## Goal

Convert the Phase 1 local APC panel into a server-authoritative, networked interface backed by live electricity data and channel controls.

## Scope

- Extend electricity model with channel-aware load accounting.
- Add APC-specific control/state contracts for server-side gating and diagnostics.
- Introduce snapshot serialization and server->client panel refresh loop.
- Wire interaction entry point from world APC object.
- Ensure UI panel ownership and close/toggle actions route through server RPC boundaries.

## Delivery order

1. Electricity primitives:
   - `PowerChannel`
   - `ApcControlFlags`
   - `CircuitStats`
   - `IApcChannelSource`
2. Circuit integration:
   - Gate consumer power by enabled APC channels.
   - Expose per-channel load stats and total demand.
3. APC network model:
   - `ApcInterfaceSnapshot`
   - serializer/mapper
   - periodic push cadence for open viewers.
4. Runtime behavior:
   - `MachineInterfaceBehaviour` viewer session management.
   - `ApcController` server-owned channel toggles and battery/diagnostic snapshot builder.
   - UI bridge for close/toggle actions.
5. World integration:
   - APC interaction target (`OpenMachineInterfaceInteraction`).
   - APC prefab and scene wiring.

## Validation checklist

- Interacting with APC opens panel for the requesting client.
- Readouts refresh while open and stop after close.
- Channel toggles are server-authoritative and visible to all viewers.
- Circuit behavior reflects channel enable/disable state.
- Input blocking while panel is open does not break core player controls.

## Known implementation hardening from recovered plan

- Input map handling had to align with existing `Movement` and `Camera` maps.
- FishNet serializer methods used `WriteByte`/`ReadByte` compatibility path.
- Drag positioning required conversion from centered translate positioning to pixel coordinates to avoid top-snap behavior.
