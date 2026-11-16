namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Something pipes connect to at its ports, and draw as a run into.
    /// </summary>
    public interface IPipePortDevice
    {
        /// <summary>The pipe lines whose neighbours must redraw when this is placed or removed.</summary>
        TileLayer[] PortLines { get; }

        bool HasPortToward(Direction direction);
    }
}
