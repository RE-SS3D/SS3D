> Code paths: Assets/Scripts/SS3D/Systems/Furniture/Disposal/, Assets/Scripts/SS3D/Systems/Furniture/DisposalBin.cs, DisposalOutlet.cs, Assets/Scripts/SS3D/Systems/Tile/Connections/Disposal*
> Entry points: DisposalSubSystem, DisposalBin, DisposalOutlet, DisposalPipeConnectivity
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Disposal

## Overview

Server-authoritative **item** disposal network: pipe segments on `TileLayer.Disposal` form BFS-connected components; chutes inject held items as capsules that travel a routed path to a tagged or main outlet. Mid-transit tile clear spills the capsule at the cut. Player pipe craft recipes, Cargo export, and player-body transit are deferred — see [2026-07_disposal-item-network.md](../../archive/architecture/2026-07_disposal-item-network.md).

## Start here

- `Assets/Scripts/SS3D/Systems/Furniture/Disposal/DisposalSubSystem.cs` — registry owner; `IWorldReady`; awaits `TileMapLoaded` then rebuilds + notifies `DisposalReady`; capsule tick (`CapsulesPaused` for Map Editor); spill-on-cut
- `Assets/Scripts/SS3D/Systems/Furniture/Disposal/DisposalPipeConnectivity.cs` — BFS walk + `TryFindRoute`
- `Assets/Scripts/SS3D/Systems/Furniture/Disposal/DisposalPipeObserver.cs` — rebuilds topology on pipe or disposal-furniture place/clear; `SuspendRebuilds` queues coords during bulk edits
- `Assets/Scripts/SS3D/Systems/Furniture/DisposalBin.cs` — chute; `AcceptsSize` / `MaxSizeClass`; drop-in + tagger interactions
- `Assets/Scripts/SS3D/Systems/Furniture/DisposalOutlet.cs` — arrival hold / main-outlet grace (eject only if `_spaceEjectionPoint` or `IDisposalSweepable` is wired)
- `Assets/Scripts/SS3D/Systems/Furniture/Disposal/DisposalDropInInteraction.cs` — Combine-tier dispose
- `Assets/Scripts/SS3D/Systems/Tile/Connections/DisposalPipeAdjacencyConnector.cs` — pipe mesh adjacency
- Prefabs: `Assets/Content/WorldObjects/Furniture/Machines/Supply/DisposalBin.prefab`, `DisposalOutlet.prefab`; pipes under `.../Structures/Pipes/Disposals/`; open clips/controllers beside the prefabs
- Scene: disposal furniture on station maps; `DisposalSubSystem` on `NetworkSystemsHub` (not Game.unity)

## Extension points

- New outlet destinations: place `DisposalOutlet` with `TargetDepartment` set; ensure a pipe under it joins the network.
- Cargo hook: implement `IDisposalSweepable` on the main outlet (or sibling) — no appraisal yet.
- Size-restricted chutes: lower `DisposalBin._maxSizeClass` in the inspector (same ceiling pattern as `AttachedContainer`).

## Pitfalls

- **Do not poll `CurrentMap != null` for network rebuild.** Await `TileMapLoaded`, rebuild, notify `DisposalReady` (epoch reset re-inits).
- **Dispose fails silently if `DisposalSubSystem` missing:** `DisposalBin.TryEnterDisposalNetwork` returns false when `SubSystems.TryGet` misses — Disposal lives on `NetworkSystemsHub` (spawned Online), not Game.unity.
- **Chute click drops instead of disposing:** keep `DisposalDropInInteraction.Priority` (40) and the tagger (20) above Drop (5).
- **Bin or outlet never joins the network:** a terminal placed after its pipes only registers because `DisposalPipeObserver` also rebuilds on `FurnitureBase`/`FurnitureTop` place/clear. Keep that path; a pipe-only rebuild leaves `Terminals` empty and Dispose finds no route.
- **Outlet arrivals spawn inside the mesh:** arrivals are spat along `transform.forward` by `_spitDistance` (default 0.85m), not at the outlet origin.
- **Main-outlet items vanished after grace:** prefab defaults to `Department.None` with a 30s grace, then `EjectIntoSpace` despawned in place when `_spaceEjectionPoint` was unset. Grace/eject now only runs if an ejection point or `IDisposalSweepable` is present; otherwise arrivals sit for pickup.
- **Lid/door anim never plays:** controllers need an Idle default (Open-as-default plays on spawn). Drive `NetworkAnimator.Play` from successful chute enter / outlet arrive — not raw `Animator.Play` alone if clients should see it. NetworkAnimator must be in `NetworkObject._networkBehaviours` with `_clientAuthoritative: 0`.
- **Items visible mid-pipe:** design wants visible transit later (glass sections). Until then capsules hide via `Item.SetVisibility` (ObserversRpc); enable `DisposalSubSystem._debugShowTransitItems` to watch routes. Reveal on spit / pipe-cut spill.
- **`Object.Destroy` on items:** Coimbra forbids it — use FishNet `Despawn` when `ServerManager` exists, else `gameObject.Dispose(true)` (`using Coimbra`).
- **Pipe place/cut in play:** no player recipes yet; clearing a disposal tile (map editor / `TryClearTile`) is what triggers sabotage spill.
- **DMM import NRE on disposal pipes:** `DisposalPipeConnectionRule.IsConnected` used `_connector.PlacedObject` before `Setup()` (null during bulk place). Use `self.Direction`; `ConnectionRule` getter calls `Setup()`. Map import defers topology via `BeginDeferredNetworkRebuild` / `EndDeferredNetworkRebuild` and rebuilds once after adjacency. Hit 2026-07-28.

## Tests

- EditMode: `Disposal/DisposalPipeTests` (network split/join, bin terminal routing, size ceiling, main-outlet grace); `AdjacencyEngineTests` covers the bulk-place connection rule. No automated test drives a full chute-to-outlet transit.

## Depends on / Used by

- **Depends on:** [tile](tile.md), [inventory](inventory.md) (`Item.SizeClass`, hands), [interactions-framework](interactions-framework.md), [examine](examine.md) (tag readout), [id-access](id-access.md) (optional chute gate)
- **Used by:** station maps with disposal furniture; future Cargo export

## Related docs

- Effort: [2026-07_disposal-item-network.md](../../archive/architecture/2026-07_disposal-item-network.md)
- Effort: [2026-07_session-world-lifecycle.md](../../archive/architecture/2026-07_session-world-lifecycle.md)
- Design (read-only): [disposal.md](../../design/disposal.md)
- [INDEX.md](../INDEX.md)
