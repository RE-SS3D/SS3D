namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Tile occupant whose physical state can change at runtime (e.g. open/closed doors).
    /// The tilemap translates this into occupancy flags such as <see cref="TileOccupancy.BlocksVision"/>.
    /// </summary>
    public interface IDynamicTileOccupant
    {
        /// <summary>
        /// Whether the occupant is currently open (passable). Closed when false.
        /// </summary>
        bool IsOpen { get; }
    }
}
