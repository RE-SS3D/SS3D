namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Server-side rules for whether two placed tiles should visually connect.
    /// </summary>
    public interface IConnectionRule
    {
        bool IsConnected(PlacedTileObject self, PlacedTileObject neighbour);
    }
}
