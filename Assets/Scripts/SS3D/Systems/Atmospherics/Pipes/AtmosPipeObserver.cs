using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Rebuilds gas pipe network topology when pipe segments are placed or removed.
    /// </summary>
    public sealed class AtmosPipeObserver : ITileMutationObserver
    {
        private readonly TileMap _map;
        private readonly GasPipeNetworkRegistry _registry;
        private readonly HashSet<TileCoord> _pendingRebuild = new();

        public AtmosPipeObserver(TileMap map, GasPipeNetworkRegistry registry)
        {
            _map = map;
            _registry = registry;
        }

        public void OnChunkCreated(TileChunkRef chunk)
        {
        }

        public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
        {
            if (occupant is PlacedTileObject placed && PipeConnectionRule.ParticipatesInGasNetwork(placed))
                _registry.RebuildAround(_map, coord);
        }

        public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer)
        {
            if (AtmosPipeConnectivity.IsGasPipeLayer(layer))
                _pendingRebuild.Add(coord);
        }

        public void OnTileStateChanged(TileCoord coord)
        {
        }

        public void FlushPendingRebuilds()
        {
            if (_pendingRebuild.Count == 0)
                return;

            foreach (TileCoord coord in _pendingRebuild)
                _registry.RebuildAround(_map, coord);

            _pendingRebuild.Clear();
        }
    }
}
