using SS3D.Systems.Tile.Enums;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Class used for picking the correct connector shape based on adjacent tiles.
    /// </summary>
    public static class AdjacencyShapeResolver
    {
        public static AdjacencyShape GetSimpleShape(AdjacencyMap adjacencyMap)
        {
            int connectionCount = adjacencyMap.CardinalConnectionCount;
            switch (connectionCount)
            {
                case 0:
                    return AdjacencyShape.O;
                case 1:
                    return AdjacencyShape.U;
                case 2:
                    return adjacencyMap.HasConnection(Direction.North) == adjacencyMap.HasConnection(Direction.South) ? AdjacencyShape.I : AdjacencyShape.L;
                case 3:
                    return AdjacencyShape.T;
                case 4:
                    return AdjacencyShape.X;
                default:
                {
                    Debug.LogError($"Could not resolve Simple Adjacency Shape for given Adjacency Map - {adjacencyMap}");
                    return AdjacencyShape.O;
                }
            }
        }

        public static AdjacencyShape GetAdvancedShape(AdjacencyMap adjacencyMap)
        {
            int cardinalConnectionCount = adjacencyMap.CardinalConnectionCount;
            int diagonalConnectionCount = adjacencyMap.DiagonalConnectionCount;
            if (cardinalConnectionCount == 0)
            {
                return AdjacencyShape.O;
            }

            if (cardinalConnectionCount == 1)
            {
                return AdjacencyShape.U;
            }

            // When two connections and they're opposite
            if (cardinalConnectionCount == 2 && adjacencyMap.HasConnection(Direction.North) == adjacencyMap.HasConnection(Direction.South))
            {
                return AdjacencyShape.I;
            }

            // When two connections and they're adjacent
            if (cardinalConnectionCount == 2 && adjacencyMap.HasConnection(Direction.North) != adjacencyMap.HasConnection(Direction.South))
            {
                // Determine lSolid or lCorner by finding whether the area between the two connections is filled
                // We check if any of the following adjacency maps matches
                // N+NE+E, E+SE+S, S+SW+W, W+NW+N
                bool isFilled = (adjacencyMap.HasConnection(Direction.North) &&
                                 adjacencyMap.HasConnection(Direction.NorthEast) &&
                                 adjacencyMap.HasConnection(Direction.East)) ||
                                (adjacencyMap.HasConnection(Direction.East) &&
                                 adjacencyMap.HasConnection(Direction.SouthEast) &&
                                 adjacencyMap.HasConnection(Direction.South)) ||
                                (adjacencyMap.HasConnection(Direction.South) &&
                                 adjacencyMap.HasConnection(Direction.SouthWest) &&
                                 adjacencyMap.HasConnection(Direction.West)) ||
                                (adjacencyMap.HasConnection(Direction.West) &&
                                 adjacencyMap.HasConnection(Direction.NorthWest) &&
                                 adjacencyMap.HasConnection(Direction.North));

                return isFilled ? AdjacencyShape.LSingle : AdjacencyShape.LNone;
            }

            if (cardinalConnectionCount == 3)
            {
                int hasNorth = adjacencyMap.HasConnection(Direction.North) ? 1 : 0;
                int hasNorthEast = adjacencyMap.HasConnection(Direction.NorthEast) ? 1 : 0;
                int hasEast = adjacencyMap.HasConnection(Direction.East) ? 1 : 0;
                int hasSouthEast = adjacencyMap.HasConnection(Direction.SouthEast) ? 1 : 0;
                int hasSouth = adjacencyMap.HasConnection(Direction.South) ? 1 : 0;
                int hasSouthWest = adjacencyMap.HasConnection(Direction.SouthWest) ? 1 : 0;
                int hasWest = adjacencyMap.HasConnection(Direction.West) ? 1 : 0;
                int hasNorthWest = adjacencyMap.HasConnection(Direction.NorthWest) ? 1 : 0;

                // TODO: Someone smarter than me needs to refactor the bitwise comparison for this piece. I couldn't figure it out. Hats off to original author.
                // We make another bitfield. 0x0 means no fills, 0x1 means right corner filled, 0x2 means left corner filled,
                // therefore both corners filled = 0x3.
                int corners = (((1 - hasNorth) * 2 | (1 - hasEast)) * hasSouthWest)
                              + (((1 - hasEast) * 2 | (1 - hasSouth)) * hasNorthWest)
                              + (((1 - hasSouth) * 2 | (1 - hasWest)) * hasNorthEast)
                              + (((1 - hasWest) * 2 | (1 - hasNorth)) * hasSouthEast);

                return corners switch
                {
                    0 => AdjacencyShape.TNone,
                    1 => AdjacencyShape.TSingleLeft,
                    2 => AdjacencyShape.TSingleRight,
                    _ => AdjacencyShape.TDouble,
                };
            }

            switch (diagonalConnectionCount)
            {
                case 0:
                    return AdjacencyShape.XNone;
                case 1:
                    return AdjacencyShape.XSingle;
                case 2:
                    return adjacencyMap.HasConnection(Direction.NorthEast) == adjacencyMap.HasConnection(Direction.SouthWest) ? AdjacencyShape.XOpposite : AdjacencyShape.XSide;
                case 3:
                    return AdjacencyShape.XTriple;
                case 4:
                    return AdjacencyShape.XQuad;
                default:
                {
                    Debug.LogError($"Could not resolve Advanced Adjacency Shape for given Adjacency Map - {adjacencyMap}");
                    return AdjacencyShape.XQuad;
                }
            }
        }

        public static AdjacencyShape GetOffsetShape(AdjacencyMap adjacencyMap)
        {
            int cardinalConnectionCount = adjacencyMap.CardinalConnectionCount;
            if (cardinalConnectionCount == 0)
            {
                return AdjacencyShape.O;
            }

            if (cardinalConnectionCount == 1)
            {
                return adjacencyMap.HasConnection(Direction.North) || adjacencyMap.HasConnection(Direction.East) ?
                    AdjacencyShape.UNorth : AdjacencyShape.USouth;
            }

            // When two connections and they're opposite
            if (cardinalConnectionCount == 2 && adjacencyMap.HasConnection(Direction.North) == adjacencyMap.HasConnection(Direction.South))
            {
                return AdjacencyShape.I;
            }

            // When two connections and they're adjacent
            if (cardinalConnectionCount == 2 && adjacencyMap.HasConnection(Direction.North) != adjacencyMap.HasConnection(Direction.South))
            {
                Direction diagonal = adjacencyMap.GetDirectionBetweenTwoConnections();
                return diagonal switch
                {
                    Direction.NorthEast => AdjacencyShape.LNorthWest,
                    Direction.SouthEast => AdjacencyShape.LNorthEast,
                    Direction.SouthWest => AdjacencyShape.LSouthEast,
                    _ => AdjacencyShape.LSouthWest,
                };
            }

            if (cardinalConnectionCount == 3)
            {
                Direction missingConnection = adjacencyMap.GetSingleNonConnection();
                return missingConnection switch
                {
                    Direction.North => AdjacencyShape.TNorthSouthEast,
                    Direction.East => AdjacencyShape.TSouthWestEast,
                    Direction.South => AdjacencyShape.TNorthSouthWest,
                    _ => AdjacencyShape.TNorthEastWest,
                };
            }

            if (cardinalConnectionCount == 4)
            {
                return AdjacencyShape.X;
            }

            Debug.LogError(
                $"Could not resolve Offset Adjacency Shape for given Adjacency Map - {adjacencyMap}");
            return AdjacencyShape.X;
        }
    }
}