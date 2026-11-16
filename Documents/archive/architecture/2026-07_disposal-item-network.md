> Implements: Documents/design/disposal.md §2–§7 (item network); §8 player transit deferred
> Touches systems: disposal, furniture, tile, inventory, examine, id-access, interactions
> Status: shipped

# Disposal item network

## Goal

Ship the physical disposal pipe network for **items**: BFS topology, chute drop-in, destination tagging, visible transit capsules, outlet grace/eject, and mid-transit spill when a pipe tile is cleared — per [disposal.md](../../design/disposal.md) §2–§7.

## Shipped

1. **Topology** — `DisposalPipeConnectivity` / `DisposalNetworkRegistry` / `DisposalNetworkRecord` (atmos gas-pipe registry pattern); incremental `RebuildAround` on tile mutation.
2. **Coordinator** — `DisposalSubSystem` on `Game.unity` (`DisposalSystem`); ticks capsules; spills on segment cut.
3. **Chute** — `DisposalBin` + `DisposalDropInInteraction` (`InteractionTier.Combine`); optional `AirLockAccessGate`; **SizeClass** accept ceiling (`MaxSizeClass`, default `Huge`) per inventory-storage §4.
4. **Tagging** — `DisposalTaggerInteraction`, `DisposalTag` / `DisposalTagExaminable` (examine-readable destination).
5. **Transit** — `DisposalCapsule` per item along a waypoint path at `DisposalConstants.TransitSpeed`.
6. **Outlets** — `DisposalOutlet`: tagged arrivals sit for world pickup; main outlet grace window then despawn (or `IDisposalSweepable` claim). Grace skips items already in a container or moved away.
7. **Tests** — `DisposalPipeTests` (network form/split/route, SizeClass ceiling, grace skip-when-moved).
8. **Adjacency scaffolding** — existing `DisposalPipeAdjacencyConnector` / tile assets (map-editor placeable).

## Deferred

- Player pipe-coil place + wirecutter cut recipes (same unfinished path as HV cable; map-editor / `TryClearTile` drives sabotage spill today)
- Cargo export consumer of `IDisposalSweepable` (Cargo still design-only)
- Player transit ([disposal.md](../../design/disposal.md) §8)
- Design-doc companion wording on SizeClass in `disposal.md` §2 (owner-only; code already uses `Item.SizeClass`)

## Documented fork notes

- Design cites Tier 3 combine for chute drop-in — this is the first live `InteractionTier.Combine` consumer; crafting place/cut remains Instant→menu, not Combine.
- Space eject despawns the item (FishNet `Despawn` / Coimbra `Dispose`); it does not leave a world object at `_spaceEjectionPoint`.

## Related docs

- System map: [systems/disposal.md](../../architecture/systems/disposal.md)
- Design (read-only): [disposal.md](../../design/disposal.md)
- Inventory SizeClass: [inventory-storage.md](../../design/inventory-storage.md) §4
