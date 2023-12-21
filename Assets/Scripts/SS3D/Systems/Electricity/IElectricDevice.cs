using SS3D.Systems.Tile;

/// <summary>
/// IElectricDevice are tile objects that can be connected to an electric circuit in some way.
/// </summary>
public interface IElectricDevice
{
    /// <summary>
    /// Tile object linked to this electric device.
    /// </summary>
    public PlacedTileObject TileObject { get; }
}
