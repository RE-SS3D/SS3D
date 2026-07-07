using SS3D.Systems.Tile;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// Cardinal edge blocking bits: N=0, E=1, S=2, W=3.
    /// </summary>
    public static class VisionEdgeMask
    {
        public const byte AllCardinals = 0b1111;

        public static byte ForCardinal(int edgeIndex) => (byte)(1 << edgeIndex);

        public static int EdgeIndexForDirection(Direction direction)
        {
            return direction switch
            {
                Direction.North => 0,
                Direction.East => 1,
                Direction.South => 2,
                Direction.West => 3,
                _ => -1
            };
        }
    }
}
