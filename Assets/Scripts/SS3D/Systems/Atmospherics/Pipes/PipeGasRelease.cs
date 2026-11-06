using SS3D.Systems.Tile;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// A removed pipe segment's share of its network's gas, on its way to the segment's tile.
    /// </summary>
    public readonly struct PipeGasRelease
    {
        public readonly TileCoord Coord;
        public readonly float[] Moles;
        public readonly float Temperature;

        public PipeGasRelease(TileCoord coord, float[] moles, float temperature)
        {
            Coord = coord;
            Moles = moles;
            Temperature = temperature;
        }
    }
}
