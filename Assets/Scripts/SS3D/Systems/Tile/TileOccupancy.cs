namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Read-only occupancy flags for a tile cell. Populated incrementally as consumers need them.
    /// </summary>
    public struct TileOccupancy
    {
        public bool HasPlenum;
        public bool HasTurf;
        public bool HasWall;
        public bool BlocksVision;
        public bool IsAirtight;
    }
}
