using SS3D.Engine.Tiles;

namespace SS3D.Engine.Tile.TileRework.Connections
{
    /// <summary>
    /// Bitmap used for calculating and storing adjacencies.
    /// </summary>
    public class AdjacencyBitmap
    {
        public struct CardinalInfo
        {
            public int north;
            public int east;
            public int south;
            public int west;
            public int numConnections;

            public CardinalInfo(byte bitmap)
            {
                north = AdjacencyShapeResolver.Adjacent(bitmap, Direction.North);
                east = AdjacencyShapeResolver.Adjacent(bitmap, Direction.East);
                south = AdjacencyShapeResolver.Adjacent(bitmap, Direction.South);
                west = AdjacencyShapeResolver.Adjacent(bitmap, Direction.West);

                numConnections = north + east + south + west;
            }

            /**
             * Gets the direction of the only positive connection.
             * Assumes there is exactly one positive connection
             */
            public Direction GetOnlyPositive()
            {
                return (Direction)(
                    north * (int)Direction.North
                    + east * (int)Direction.East
                    + south * (int)Direction.South
                    + west * (int)Direction.West
                );
            }
            /**
             * Get the direction of the only non-1 connection.
             * Assumes there is exactly one non-1 connection
             */
            public Direction GetOnlyNegative()
            {
                return (Direction)(
                    (1 - north) * (int)Direction.North
                    + (1 - east) * (int)Direction.East
                    + (1 - south) * (int)Direction.South
                    + (1 - west) * (int)Direction.West
                );
            }

            /**
             * Assuming the cardinals have exactly two adjacent connections,
             * gets the angle between them (will therefore be a diagonal angle).
             */
            public Direction GetCornerDirection()
            {
                return south > 0 ? east > 0 ? Direction.SouthEast : Direction.SouthWest : west > 0 ? Direction.NorthWest : Direction.NorthEast;
            }

            public override string ToString()
            {
                return
                    $"North: {north}, East: {east}, South: {south}, West: {west}, Connection count: {numConnections}";
            }
        }

        /**
     * Returns a bitmap which modifies the input bitmap so that
     * the bit corresponding to the given direction is set to the given value.
     */
        public static byte SetDirection(byte bitmap, Direction direction, bool value)
        {
            // Set the direction bit to isConnected (1 or 0)
            byte output = bitmap;
            output &= (byte)~(1 << (int)direction);
            output |= (byte)((value ? 1 : 0) << (int)direction);

            return output;
        }

        public byte Connections { get; set; } = 0;

        /**
         * Updates the given bitmap using SetDirection, and returns whether the
         * bitmap changed as a result.
         * 
         * onlyCheckCardinals - If true, then only considers the bitmap has changed if N, E, S, or W has changed.
         */
        public bool UpdateDirection(Direction direction, bool value, bool onlyCheckCardinals = false)
        {
            byte output = SetDirection(Connections, direction, value);

            byte comparisonMap = onlyCheckCardinals ? (byte)0b01010101 : (byte)0b11111111;
            bool changed = (output & comparisonMap) != (Connections & comparisonMap);
            Connections = output;
            return changed;
        }

        /**
         * Gets (as 0 or 1) whether each cardinal direction is connected (N, E, S, W),
         * and the last value is the total number of cardinal connections
         */
        public CardinalInfo GetCardinalInfo()
        {
            return new CardinalInfo(Connections);
        }

        /**
         * Returns 0 if no adjacency, or 1 if there is.
         */
        public int Adjacent(Direction direction)
        {
            return AdjacencyShapeResolver.Adjacent(Connections, direction);
        }
    }
}