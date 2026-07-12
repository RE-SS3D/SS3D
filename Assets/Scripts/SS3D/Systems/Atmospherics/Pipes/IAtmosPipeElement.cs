using SS3D.Systems.Tile;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Marker for atmos devices that interface with a gas pipe network (vents, scrubbers, pumps).
    /// </summary>
    public interface IAtmosPipeElement
    {
        TileCoord OriginTile { get; }

        bool TryGetConnectedNetwork(out GasPipeNetworkId networkId);
    }
}
