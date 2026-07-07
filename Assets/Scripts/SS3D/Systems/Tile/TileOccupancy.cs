using SS3D.Systems.Tile;

namespace SS3D.Systems.Vision
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
        public bool IsWindow;
        public bool IsDoor;

        /// <summary>
        /// When true the door blocks vision. Open doors will set this false once door state exists.
        /// </summary>
        public bool DoorBlocksVision;

        /// <summary>
        /// Cardinal edge blocking when exiting the cell: bit 0=N, 1=E, 2=S, 3=W.
        /// </summary>
        public byte BlockedEdges;

        public float GetDirectionalOpacity(Direction direction)
        {
            int edgeIndex = VisionEdgeMask.EdgeIndexForDirection(direction);
            if (edgeIndex < 0)
                return 0f;

            return (BlockedEdges & VisionEdgeMask.ForCardinal(edgeIndex)) != 0 ? 1f : 0f;
        }
    }
}
