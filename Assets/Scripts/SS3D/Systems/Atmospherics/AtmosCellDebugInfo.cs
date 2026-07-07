using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;

namespace SS3D.Systems.Atmospherics
{
    public struct AtmosCellDebugInfo
    {
        public bool Exists;
        public TileCoord Coord;
        public AtmosCellState State;
        public float Temperature;
        public float Volume;
        public float Pressure;
        public AtmosNeighbours Neighbours;
        public TileOccupancy Occupancy;
    }
}
