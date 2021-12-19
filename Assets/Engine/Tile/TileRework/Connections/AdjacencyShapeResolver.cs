using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Engine.Tile.TileRework.Connections
{
    /// <summary>
    /// Class used for picking the correct connector shape based on adjacent tiles.
    /// </summary>
    public static class AdjacencyShapeResolver
    {
        public static AdjacencyShape GetSimpleShape(AdjacencyBitmap.CardinalInfo cardinalInfo)
        {
            int connectionCount = cardinalInfo.numConnections;
            if (connectionCount == 0)
            {
                return AdjacencyShape.O;
            }

            if (connectionCount == 1)
            {
                return AdjacencyShape.U;
            }

            //When two connections, checks if they're opposite or adjacent
            if (connectionCount == 2)
            {
                return cardinalInfo.north == cardinalInfo.south ? AdjacencyShape.I : AdjacencyShape.L;
            }

            if (connectionCount == 3)
            {
                return AdjacencyShape.T;
            }

            if (cardinalInfo.numConnections == 4)
            {
                return AdjacencyShape.X;
            }

            Debug.LogError($"Could not resolve Simple Adjacency Shape for given Cardinal Info - {cardinalInfo}");
            return AdjacencyShape.O;
        }

        //TODO: Simplify when diagonal support for CardinalInfo is added. Probably will be able to get rid of all the bitfields
        public static AdjacencyShape GetAdvancedShape(AdjacencyBitmap.CardinalInfo cardinalInfo, byte connections)
        {
            int connectionCount = cardinalInfo.numConnections;
            if (connectionCount == 0)
            {
                return AdjacencyShape.O;
            }

            if (connectionCount == 1)
            {
                return AdjacencyShape.U;
            }

            //When two connections and they're opposite
            if (connectionCount == 2 && cardinalInfo.north == cardinalInfo.south)
            {
                return AdjacencyShape.I;
            }

            //When two connections and they're adjacent
            if (connectionCount == 2 && cardinalInfo.north != cardinalInfo.south)
            {
                // Determine lSolid or lCorner by finding whether the area between the two connections is filled
                // We check for if any of the following bitfields matches the connection bitfield
                // N+NE+E = 0/1/2, E+SE+S = 2/3/4, S+SW+W = 4/5/6, W+NW+N = 6/7/0
                bool isFilled = (connections & 0b00000111) == 0b00000111 ||
                                (connections & 0b00011100) == 0b00011100 ||
                                (connections & 0b01110000) == 0b01110000 ||
                                (connections & 0b11000001) == 0b11000001;
                return isFilled ? AdjacencyShape.LSingle : AdjacencyShape.LNone;
            }

            if (connectionCount == 3)
            {
                // We make another bitfield (noticing a pattern?). 0x0 means no fills, 0x1 means right corner filled, 0x2 means left corner filled,
                // therefore both corners filled = 0x3.
                int corners = ((1 - cardinalInfo.north) * 2 | (1 - cardinalInfo.east)) *
                              Adjacent(connections, Direction.SouthWest)
                              + ((1 - cardinalInfo.east) * 2 | (1 - cardinalInfo.south)) *
                              Adjacent(connections, Direction.NorthWest)
                              + ((1 - cardinalInfo.south) * 2 | (1 - cardinalInfo.west)) *
                              Adjacent(connections, Direction.NorthEast)
                              + ((1 - cardinalInfo.west) * 2 | (1 - cardinalInfo.north)) *
                              Adjacent(connections, Direction.SouthEast);
                return corners == 0 ? AdjacencyShape.TNone
                    : corners == 1 ? AdjacencyShape.TSingleLeft
                    : corners == 2 ? AdjacencyShape.TSingleRight
                    : AdjacencyShape.TDouble;
            }

            //This sneaky piece of code uses the cardinal info to store diagonals by rotating everything -45 degrees
            //NE -> N, SW -> S, etc.
            var diagonals = new AdjacencyBitmap.CardinalInfo((byte) (connections >> 1));

            switch (diagonals.numConnections)
            {
                case 0:
                    return AdjacencyShape.XNone;
                case 1:
                    return AdjacencyShape.XSingle;
                case 2:
                    return diagonals.north == diagonals.south ? AdjacencyShape.XOpposite : AdjacencyShape.XSide;
                case 3:
                    return AdjacencyShape.XTriple;
                case 4:
                    return AdjacencyShape.XQuad;
                default:
                    Debug.LogError(
                        $"Could not resolve Advanced Adjacency Shape for given Cardinal Info - {cardinalInfo} and Connections map - {connections}");
                    return AdjacencyShape.XQuad;
            }
        }

        public static AdjacencyShape GetOffsetShape(AdjacencyBitmap.CardinalInfo cardinalInfo)
        {
            int connectionCount = cardinalInfo.numConnections;
            if (connectionCount == 0)
            {
                return AdjacencyShape.O;
            }
            
            if (connectionCount == 1)
            {
                return cardinalInfo.north > 0 || cardinalInfo.east > 0 ? AdjacencyShape.UNorth : AdjacencyShape.USouth;
            }
            
            //When two connections and they're opposite
            if (connectionCount == 2 && cardinalInfo.north == cardinalInfo.south)
            {
                return AdjacencyShape.I;
            }
            
            //When two connections and they're adjacent
            if (connectionCount == 2 && cardinalInfo.north != cardinalInfo.south)
            {
                Direction sides = cardinalInfo.GetCornerDirection();
                return sides == Direction.NorthEast ? AdjacencyShape.LNorthWest
                    : sides == Direction.SouthEast ? AdjacencyShape.LNorthEast
                    : sides == Direction.SouthWest ? AdjacencyShape.LSouthEast
                    : AdjacencyShape.LSouthWest;
            }

            if (connectionCount == 3)
            {
                Direction notside = cardinalInfo.GetOnlyNegative();
                return notside == Direction.North ? AdjacencyShape.TNorthSouthEast
                    : notside == Direction.East ? AdjacencyShape.TSouthWestEast
                    : notside == Direction.South ? AdjacencyShape.TNorthSouthWest
                    : AdjacencyShape.TNorthEastWest;
            }

            if (connectionCount == 4)
            {
                return AdjacencyShape.X;
            }
            
            Debug.LogError(
                $"Could not resolve Offset Adjacency Shape for given Cardinal Info - {cardinalInfo}");
            return AdjacencyShape.X;
        }

        public static int Adjacent(byte bitmap, Direction direction)
        {
            return (bitmap >> (int) direction) & 0x1;
        }
    }
}