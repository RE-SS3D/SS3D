using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
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
                north = Adjacent(bitmap, Direction.North);
                east = Adjacent(bitmap, Direction.East);
                south = Adjacent(bitmap, Direction.South);
                west = Adjacent(bitmap, Direction.West);

                numConnections = north + east + south + west;
            }

            // Checks if there are no cardinal connections
            public bool IsO() => numConnections == 0;
            // Checks for one connection
            public bool IsU() => numConnections == 1;
            // Checks for two opposite connections
            public bool IsI() => numConnections == 2 && north == south;
            // Checks for two adjacent connections
            public bool IsL() => numConnections == 2 && north != south;
            // Checks for three connections
            public bool IsT() => numConnections == 3;
            // Checks for four connections
            public bool IsX() => numConnections == 4;

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

            
            /**
             * Gets Vertical if north or south are connected, otherwise gets horizontal
             */
            
            public Orientation GetFirstOrientation()
            {
                return (Orientation)(east | west | (1 - (north | south)));
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

        public static int Adjacent(byte bitmap, Direction direction)
        {
            return (bitmap >> (int)direction) & 0x1;
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
            return Adjacent(Connections, direction);
        }
    }
}