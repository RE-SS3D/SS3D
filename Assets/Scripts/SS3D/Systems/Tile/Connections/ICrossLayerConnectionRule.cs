namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// A connection rule that can also connect to something on another layer of the neighbouring tile.
    /// </summary>
    public interface ICrossLayerConnectionRule : IConnectionRule
    {
        bool IsConnectedAcrossLayers(PlacedTileObject self, Direction direction, TileMap map);
    }
}
