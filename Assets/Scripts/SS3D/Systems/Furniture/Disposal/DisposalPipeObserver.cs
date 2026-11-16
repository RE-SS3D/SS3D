using SS3D.Systems.Furniture;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// Rebuilds disposal pipe network topology when pipe segments or disposal furniture (bin/outlet)
    /// are placed or removed, mirroring <c>AtmosPipeObserver</c>. Also reports cut segments immediately
    /// (before the topology rebuild) so <c>DisposalSubSystem</c> can spill in-transit capsules —
    /// sabotage must be immediate, never deferred (design doc §7).
    /// </summary>
    public sealed class DisposalPipeObserver : ITileMutationObserver
    {
        private readonly TileMap _map;
        private readonly DisposalNetworkRegistry _registry;
        private readonly Action<PlacedTileObject> _onSegmentCut;
        private readonly HashSet<TileCoord> _pendingRebuild = new();

        /// <summary>
        /// When true, place/clear only queues coords; call <see cref="FlushPendingRebuilds"/> or
        /// <see cref="ClearPendingRebuilds"/> + registry RebuildAll after bulk edits (map import).
        /// </summary>
        public bool SuspendRebuilds { get; set; }

        public DisposalPipeObserver(TileMap map, DisposalNetworkRegistry registry, Action<PlacedTileObject> onSegmentCut)
        {
            _map = map;
            _registry = registry;
            _onSegmentCut = onSegmentCut;
        }

        public void OnChunkCreated(TileChunkRef chunk)
        {
        }

        public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
        {
            if (occupant is not PlacedTileObject placed)
                return;

            // Pipes form topology; bins/outlets are terminals discovered only during CollectNetwork.
            // Furniture placed after pipes must trigger a rebuild or Dispose finds no destination.
            if (DisposalPipeConnectivity.ParticipatesInDisposalNetwork(placed)
                || placed.GetComponent<IDisposalElement>() != null)
            {
                if (SuspendRebuilds)
                {
                    _pendingRebuild.Add(coord);
                    return;
                }

                _registry.RebuildAround(_map, coord);
            }
        }

        public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer)
        {
            if (occupant is PlacedTileObject placed
                && DisposalPipeConnectivity.ParticipatesInDisposalNetwork(placed))
            {
                if (!SuspendRebuilds)
                    _onSegmentCut?.Invoke(placed);
                _pendingRebuild.Add(coord);
                return;
            }

            // Clearing a bin/outlet must drop it from network.Terminals.
            if ((layer == TileLayer.FurnitureBase || layer == TileLayer.FurnitureTop)
                && occupant is PlacedTileObject furniture
                && furniture.GetComponent<IDisposalElement>() != null)
            {
                _pendingRebuild.Add(coord);
            }
        }

        public void OnTileStateChanged(TileCoord coord)
        {
        }

        public void ClearPendingRebuilds() => _pendingRebuild.Clear();

        public void FlushPendingRebuilds()
        {
            if (SuspendRebuilds || _pendingRebuild.Count == 0)
                return;

            foreach (TileCoord coord in _pendingRebuild)
                _registry.RebuildAround(_map, coord);

            _pendingRebuild.Clear();
        }
    }
}
