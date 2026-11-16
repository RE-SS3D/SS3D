using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.WorldReadiness;
using SS3D.Logging;
using SS3D.Systems.Furniture;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using SS3D.Systems.WorldReadiness;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// Server-authoritative disposal network coordinator — owns pipe topology, routes items dropped
    /// into a chute, and ticks in-transit capsules along their route (design doc §3-§7).
    /// Mirrors <c>ElectricitySubSystem</c>/<c>AtmosSubSystem</c>'s role for their own networks.
    /// </summary>
    public sealed class DisposalSubSystem : NetworkSubSystem, IWorldReady
    {
        [SerializeField]
        [Tooltip("When on, items stay visible while riding pipes (useful for debugging routes). Off hides them until spit/spill — opaque pipes make mid-transit meshes look wrong.")]
        private bool _debugShowTransitItems;

        private DisposalNetworkRegistry _registry;
        private DisposalPipeObserver _observer;
        private TileMap _map;
        private CancellationTokenSource _readinessCts;

        private readonly List<DisposalCapsule> _activeCapsules = new();

        public event Action WhenReady;

        public bool IsReady { get; private set; }

        public DisposalNetworkRegistry Registry => _registry;

        /// <summary>
        /// Queue disposal topology mutations without rebuilding (bulk map import).
        /// Pair with <see cref="EndDeferredNetworkRebuild"/>.
        /// </summary>
        public void BeginDeferredNetworkRebuild()
        {
            if (_observer != null)
                _observer.SuspendRebuilds = true;
        }

        /// <summary>
        /// Drop queued per-tile rebuilds and rebuild the whole disposal topology once.
        /// </summary>
        public void EndDeferredNetworkRebuild()
        {
            if (_observer != null)
            {
                _observer.SuspendRebuilds = false;
                _observer.ClearPendingRebuilds();
            }

            if (_registry != null && _map != null)
                _registry.RebuildAll(_map);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (SubSystems.TryGet(out WorldReadinessSubSystem readiness))
            {
                readiness.PhaseChanged += HandleReadinessPhaseChanged;
            }

            InitializeWhenMapReady().Forget();
        }

        private void HandleReadinessPhaseChanged(WorldReadyPhase phase)
        {
            if (phase != WorldReadyPhase.None || !IsServer)
            {
                return;
            }

            IsReady = false;
            InitializeWhenMapReady().Forget();
        }

        private async UniTaskVoid InitializeWhenMapReady()
        {
            _readinessCts?.Cancel();
            _readinessCts?.Dispose();
            _readinessCts = new CancellationTokenSource();
            CancellationToken ct = _readinessCts.Token;

            WorldReadinessSubSystem readiness = null;
            await UniTask.WaitUntil(() => SubSystems.TryGet(out readiness), cancellationToken: ct);
            await readiness.WaitUntilAsync(WorldReadyPhase.TileMapLoaded, ct);

            if (!IsServer || ct.IsCancellationRequested)
                return;

            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
            if (tileSubSystem?.CurrentMap == null)
                return;

            _map = tileSubSystem.CurrentMap;

            if (_registry == null)
            {
                _registry = new DisposalNetworkRegistry();
                _observer = new DisposalPipeObserver(_map, _registry, OnSegmentCut);
                tileSubSystem.RegisterTileMutationObserver(_observer);
            }

            _registry.RebuildAll(_map);

            IsReady = true;
            WhenReady?.Invoke();
            readiness.NotifyDisposalReady();

            Log.Information(this, $"Disposal network started with {_registry.NetworkCount} network(s).");
        }

        protected override void OnDestroyed()
        {
            _readinessCts?.Cancel();
            _readinessCts?.Dispose();
            _readinessCts = null;

            if (SubSystems.TryGet(out WorldReadinessSubSystem readiness))
            {
                readiness.PhaseChanged -= HandleReadinessPhaseChanged;
            }

            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
            if (_observer != null)
                tileSubSystem?.UnregisterTileMutationObserver(_observer);

            _observer = null;
            _registry = null;
            base.OnDestroyed();
        }

        private void Update()
        {
            if (!IsServer || _registry == null)
                return;

            _observer?.FlushPendingRebuilds();

            TickCapsules(Time.deltaTime);
        }

        private void TickCapsules(float deltaTime)
        {
            if (_activeCapsules.Count == 0)
                return;

            float distance = DisposalConstants.TransitSpeed * deltaTime;

            for (int i = _activeCapsules.Count - 1; i >= 0; i--)
            {
                DisposalCapsule capsule = _activeCapsules[i];
                if (capsule.Item == null)
                {
                    _activeCapsules.RemoveAt(i);
                    continue;
                }

                bool arrived = capsule.Advance(distance);
                capsule.Item.transform.position = capsule.CurrentPosition;

                if (arrived)
                {
                    _activeCapsules.RemoveAt(i);
                    HandleArrival(capsule);
                }
            }
        }

        private void HandleArrival(DisposalCapsule capsule)
        {
            if (capsule.Destination is DisposalOutlet outlet)
            {
                // Stay hidden through the outlet spit delay; FinishArrival reveals.
                outlet.OnItemArrived(capsule.Item);
                return;
            }

            RevealItem(capsule.Item);
            capsule.Item.Unfreeze();
        }

        /// <summary>
        /// Entry point for the chute drop-in interaction (design doc §2/§4). Resolves a route from the
        /// bin's pipe to the tagged destination (or the main outlet if untagged/unreachable) and spawns
        /// a capsule. Returns false if the bin isn't connected to any disposal network.
        /// </summary>
        public bool TryEnterNetwork(DisposalBin bin, Item item, Department destinationTag)
        {
            if (_registry == null || _map == null || bin == null || item == null)
            {
                Log.Warning(this, "Dispose rejected: disposal subsystem not ready.");
                return false;
            }

            if (!TryGetPipeBelow(bin, out PlacedTileObject entrySegment))
            {
                Log.Warning(this, "Dispose rejected: no disposal pipe under {bin}.", Logs.Generic, bin.name);
                return false;
            }

            DisposalSegmentKey entryKey = DisposalSegmentKey.From(entrySegment);
            if (!_registry.TryGetNetworkForSegment(entryKey, out DisposalNetworkId networkId, out DisposalNetworkRecord network))
            {
                // Pipes may have been placed before the observer registered, or furniture after pipes
                // without a rebuild — recover by rebuilding around the entry tile.
                _registry.RebuildAround(_map, entryKey.Coord);
                if (!_registry.TryGetNetworkForSegment(entryKey, out networkId, out network))
                {
                    Log.Warning(this, "Dispose rejected: pipe under {bin} is not in any disposal network.", Logs.Generic, bin.name);
                    return false;
                }
            }

            if (!TryResolveDestination(network, destinationTag, out IDisposalElement destination))
            {
                // Terminals can be stale if the outlet was placed after the pipe network formed.
                _registry.RebuildAround(_map, entryKey.Coord);
                if (!_registry.TryGetNetworkForSegment(entryKey, out networkId, out network)
                    || !TryResolveDestination(network, destinationTag, out destination))
                {
                    Log.Warning(
                        this,
                        "Dispose rejected: no outlet on the network for {bin} (tag {tag}). Place a main outlet (Department.None) on a connected pipe.",
                        Logs.Generic,
                        bin.name,
                        destinationTag);
                    return false;
                }
            }

            if (!DisposalPipeConnectivity.TryFindRoute(_map, entrySegment, destination, out List<PlacedTileObject> path))
            {
                Log.Warning(this, "Dispose rejected: no route from {bin} to outlet {outlet}.", Logs.Generic, bin.name, destination.GameObject.name);
                return false;
            }

            item.Freeze();
            SetTransitVisibility(item, visible: _debugShowTransitItems);
            DisposalCapsule capsule = new(item, destination, networkId, path);
            _activeCapsules.Add(capsule);
            return true;
        }

        /// <summary>
        /// Makes a transit item visible again (outlet spit, pipe-cut spill, or failed destination).
        /// </summary>
        public void RevealItem(Item item)
        {
            SetTransitVisibility(item, visible: true);
        }

        private void SetTransitVisibility(Item item, bool visible)
        {
            if (item == null || item.NetworkObject == null)
            {
                return;
            }

            ObserversSetItemVisibility(item.NetworkObject, visible);
        }

        [ObserversRpc(RunLocally = true)]
        private void ObserversSetItemVisibility(NetworkObject itemObject, bool visible)
        {
            if (itemObject != null && itemObject.TryGetComponent(out Item item))
            {
                item.SetVisibility(visible);
            }
        }

        private static bool TryResolveDestination(DisposalNetworkRecord network, Department destinationTag, out IDisposalElement destination)
        {
            destination = null;
            IDisposalElement mainOutlet = null;

            foreach (IDisposalElement terminal in network.Terminals)
            {
                if (terminal is not DisposalOutlet outlet)
                    continue;

                if (destinationTag != Department.None && outlet.TargetDepartment == destinationTag)
                {
                    destination = outlet;
                    return true;
                }

                if (outlet.IsMainOutlet)
                    mainOutlet = outlet;
            }

            // No reachable outlet matches the tag (or it was untagged) — fall back to the main outlet (§4).
            destination = mainOutlet;
            return destination != null;
        }

        private bool TryGetPipeBelow(IDisposalElement element, out PlacedTileObject pipe)
        {
            pipe = null;
            if (_map == null)
                return false;

            if (!_map.TryGetTileLocation(TileLayer.Disposal, element.GameObject.transform.position, out ITileLocation location))
                return false;

            if (!location.TryGetPlacedObject(out pipe, Direction.North))
                return false;

            return DisposalPipeConnectivity.ParticipatesInDisposalNetwork(pipe);
        }

        /// <summary>
        /// Pipe-cut sabotage (design doc §7): immediately spills any capsule that hasn't yet passed the
        /// cut segment, right where the cut happened — never a hidden roll.
        /// </summary>
        private void OnSegmentCut(PlacedTileObject cutSegment)
        {
            DisposalSegmentKey key = DisposalSegmentKey.From(cutSegment);
            Vector3 spillPosition = cutSegment.transform.position;

            for (int i = _activeCapsules.Count - 1; i >= 0; i--)
            {
                DisposalCapsule capsule = _activeCapsules[i];
                if (!capsule.HasNotYetPassed(key))
                    continue;

                _activeCapsules.RemoveAt(i);

                if (capsule.Item == null)
                    continue;

                RevealItem(capsule.Item);
                capsule.Item.Unfreeze();
                capsule.Item.transform.position = spillPosition;
            }
        }
    }
}
