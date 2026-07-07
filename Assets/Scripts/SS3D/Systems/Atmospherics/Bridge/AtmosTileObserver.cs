using SS3D.Systems.Tile;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Keeps the turf gas grid aligned with tilemap structure and door state.
    /// </summary>
    public sealed class AtmosTileObserver : ITileMutationObserver
    {
        private readonly AtmosSimulation _simulation;

        public AtmosTileObserver(AtmosSimulation simulation)
        {
            _simulation = simulation;
        }

        public void OnChunkCreated(TileChunkRef chunk)
        {
            _simulation.CreateChunk(chunk);
        }

        public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
        {
            _simulation.UpdateCell(coord);
        }

        public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer)
        {
            _simulation.UpdateCell(coord);
            _simulation.ActivateRegion(coord, 2);
        }

        public void OnTileStateChanged(TileCoord coord)
        {
            _simulation.UpdateCell(coord);
            _simulation.ActivateRegion(coord, 1);
        }
    }
}
