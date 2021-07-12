using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
{

    public struct MeshDirectionInfo
    {
        public Mesh mesh;
        public float rotation;
    }

    /// Just... don't scroll down...
    [Serializable]
    public struct SimpleConnector
    {
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the north edge is connected")]
        public Mesh c;
        [Tooltip("A mesh where the north & south edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the north & east edges are connected")]
        public Mesh l;
        [Tooltip("A mesh where the north, east, and west edges are connected")]
        public Mesh t;
        [Tooltip("A mesh where all edges are connected")]
        public Mesh x;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyBitmap adjacents)
        {
            // Count number of connections along cardinal (which is all that we use atm)
            var cardinalInfo = adjacents.GetCardinalInfo();

            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            if (cardinalInfo.IsO())
                mesh = o;
            else if (cardinalInfo.IsC())
            {
                mesh = c;
                rotation = TileHelper.AngleBetween(Direction.North, cardinalInfo.GetOnlyPositive());
            }
            else if (cardinalInfo.IsI())
            {
                mesh = i;
                rotation = TileHelper.AngleBetween(Orientation.Vertical, cardinalInfo.GetFirstOrientation());
            }
            else if (cardinalInfo.IsL())
            {
                mesh = l;
                rotation = TileHelper.AngleBetween(Direction.NorthEast, cardinalInfo.GetCornerDirection());
            }
            else if (cardinalInfo.IsT())
            {
                mesh = t;
                rotation = TileHelper.AngleBetween(Direction.South, cardinalInfo.GetOnlyNegative());
            }
            else // Must be X
                mesh = x;

            return new MeshDirectionInfo { mesh = mesh, rotation = rotation };
        }
    }

    [Serializable]
    public struct AdvancedConnector
    {
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;

        [Tooltip("A mesh where north connects to same type")]
        public Mesh u;

        [Tooltip("A mesh where north and south edges are connected")]
        public Mesh i;

        [Tooltip("A mesh where the north and east edges are connected, no corners")]
        public Mesh lNone;
        [Tooltip("A mesh where the north and east edges are connected, and NE is a corner")]
        public Mesh lSingle;

        [Tooltip("A mesh where the north, east, and west edges are connected, no corners")]
        public Mesh tNone;
        [Tooltip("A mesh where the north, east, and west edges are connected, NW is a corner")]
        public Mesh tSingleRight;
        [Tooltip("A mesh where the north, east, and west edges are connected, NE is a corner")]
        public Mesh tSingleLeft;
        [Tooltip("A mesh where north, east, and west edges are connected, NW & NE are corners")]
        public Mesh tDouble;

        [Tooltip("A mesh where all edges are connected, no corners")]
        public Mesh xNone;
        [Tooltip("A mesh where all edges are connected, SW is a corner")]
        public Mesh xSingle;
        [Tooltip("A mesh where all edges are connected, SW & SW are corners")]
        public Mesh xSide;
        [Tooltip("A mesh where all edges are connected, NW & SE are corners")]
        public Mesh xOpposite;
        [Tooltip("A mesh where all edges are connected, all but NE are corners")]
        public Mesh xTriple;
        [Tooltip("A mesh where all edges are connected, all corners")]
        public Mesh xQuad;

        public GameObject[] viewObstacles;
        public bool opaque;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyBitmap adjacents)
        {
            var cardinalInfo = adjacents.GetCardinalInfo();

            float rotation = 0.0f;
            Mesh mesh;
            if (cardinalInfo.IsO())
            {
                mesh = o;
            }
            else if (cardinalInfo.IsU())
            {
                mesh = u;
                rotation = TileHelper.AngleBetween(Direction.North, cardinalInfo.GetOnlyPositive());

                if (opaque)
                {
                    viewObstacles[0].SetActive(false);
                    viewObstacles[1].SetActive(false);
                    viewObstacles[2].SetActive(true);
                    viewObstacles[3].SetActive(false);
                }
            }
            else if (cardinalInfo.IsI())
            {
                mesh = i;
                rotation = TileHelper.AngleBetween(Orientation.Vertical, cardinalInfo.GetFirstOrientation());

                if (opaque)
                {
                    viewObstacles[0].SetActive(false);
                    viewObstacles[1].SetActive(false);
                    viewObstacles[2].SetActive(true);
                    viewObstacles[3].SetActive(true);
                }
            }
            else if (cardinalInfo.IsL())
            {
                // Determine lSolid or lCorner by finding whether the area between the two connections is filled
                // We check for if any of the following bitfields matches the connection bitfield
                // N+NE+E = 0/1/2, E+SE+S = 2/3/4, S+SW+W = 4/5/6, W+NW+N = 6/7/0
                bool isFilled = (adjacents.Connections & 0b00000111) == 0b00000111 || (adjacents.Connections & 0b00011100) == 0b00011100 || (adjacents.Connections & 0b01110000) == 0b01110000 || (adjacents.Connections & 0b11000001) == 0b11000001;
                mesh = isFilled ? lSingle : lNone;
                rotation = TileHelper.AngleBetween(Direction.NorthEast, cardinalInfo.GetCornerDirection());

                if (opaque)
                {
                    viewObstacles[0].SetActive(false);
                    viewObstacles[1].SetActive(true);
                    viewObstacles[2].SetActive(true);
                    viewObstacles[3].SetActive(false);
                }
            }
            else if (cardinalInfo.IsT())
            {
                // We make another bitfield (noticing a pattern?). 0x0 means no fills, 0x1 means right corner filled, 0x2 means left corner filled,
                // therefore both corners filled = 0x3.
                int corners = ((1 - cardinalInfo.north) * 2 | (1 - cardinalInfo.east)) * adjacents.Adjacent(Direction.SouthWest)
                            + ((1 - cardinalInfo.east) * 2 | (1 - cardinalInfo.south)) * adjacents.Adjacent(Direction.NorthWest)
                            + ((1 - cardinalInfo.south) * 2 | (1 - cardinalInfo.west)) * adjacents.Adjacent(Direction.NorthEast)
                            + ((1 - cardinalInfo.west) * 2 | (1 - cardinalInfo.north)) * adjacents.Adjacent(Direction.SouthEast);
                mesh = corners == 0 ? tNone
                    : corners == 1 ? tSingleLeft
                    : corners == 2 ? tSingleRight
                    : tDouble;

                rotation = TileHelper.AngleBetween(Direction.South, cardinalInfo.GetOnlyNegative());

                if (opaque)
                {
                    viewObstacles[0].SetActive(true);
                    viewObstacles[1].SetActive(true);
                    viewObstacles[2].SetActive(true);
                    viewObstacles[3].SetActive(corners >= 3);
                }
            }
            else
            {
                // This sneaky piece of code uses the cardinal info to store diagonals by rotating everything -45 degrees
                // NE -> N, SW -> S, etc.
                var diagonals = new AdjacencyBitmap.CardinalInfo((byte)(adjacents.Connections >> 1));

                switch (diagonals.numConnections)
                {
                    case 0:
                        mesh = xNone;
                        break;
                    case 1:
                        mesh = xSingle;
                        rotation = TileHelper.AngleBetween(Direction.South, diagonals.GetOnlyPositive());
                        break;
                    case 2:
                        if (diagonals.north == diagonals.south)
                        {
                            mesh = xOpposite;
                            rotation = TileHelper.AngleBetween(Orientation.Horizontal, diagonals.GetFirstOrientation());
                        }
                        else
                        {
                            mesh = xSide;
                            rotation = TileHelper.AngleBetween(Direction.SouthEast, diagonals.GetCornerDirection());
                        }
                        break;
                    case 3:
                        mesh = xTriple;
                        rotation = TileHelper.AngleBetween(Direction.North, diagonals.GetOnlyNegative());
                        break;
                    default:
                        mesh = xQuad;
                        break;

                }
                if (opaque)
                {
                    viewObstacles[0].SetActive(true);
                    viewObstacles[1].SetActive(true);
                    viewObstacles[2].SetActive(true);
                    viewObstacles[3].SetActive(true);
                }
            }

            return new MeshDirectionInfo { mesh = mesh, rotation = rotation };
        }
    }

    [Serializable]
    public struct OffsetConnector
    {
        public enum OffsetOrientation
        {
            o,
            cNorth,
            cSouth,
            i,
            lNE,
            lNW,
            lSE,
            lSW,
            tNEW,
            tNSW,
            tNSE,
            tSWE,
            x
        }


        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the north edge is connected, can be rotated to the east")]
        public Mesh cNorth;
        [Tooltip("A mesh where the south edge is connected, can be rotated to the west")]
        public Mesh cSouth;
        [Tooltip("A mesh where north & south edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the north & east edges are connected")]
        public Mesh lNE;
        [Tooltip("A mesh where the north & west edges are connected")]
        public Mesh lNW;
        [Tooltip("A mesh where the south & east edges are connected")]
        public Mesh lSE;
        [Tooltip("A mesh where the south & west edges are connected")]
        public Mesh lSW;
        [Tooltip("A mesh where the north, east, & west edges are connected")]
        public Mesh tNEW;
        [Tooltip("A mesh where the north, south, & west edges are connected")]
        public Mesh tNSW;
        [Tooltip("A mesh where the north, south, & east edges are connected")]
        public Mesh tNSE;
        [Tooltip("A mesh where the South, west, & east edges are connected")]
        public Mesh tSWE;
        [Tooltip("A mesh where all edges are connected")]
        public Mesh x;

        private OffsetOrientation orientation;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyBitmap adjacents)
        {
            // Count number of connections along cardinal (which is all that we use atm)
            var cardinalInfo = adjacents.GetCardinalInfo();

            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            if (cardinalInfo.IsO())
            {
                mesh = o;
                orientation = OffsetOrientation.o;
            }
            else if (cardinalInfo.IsC())
            {
                if (cardinalInfo.north > 0 || cardinalInfo.east > 0)
                {
                    mesh = cNorth;
                    orientation = OffsetOrientation.cNorth;
                }
                else
                {
                    mesh = cSouth;
                    orientation = OffsetOrientation.cSouth;
                }
                rotation = TileHelper.AngleBetween(Direction.North, cardinalInfo.GetOnlyPositive());
            }
            else if (cardinalInfo.IsI())
            {
                mesh = i;
                orientation = OffsetOrientation.i;
                rotation = TileHelper.AngleBetween(Orientation.Vertical, cardinalInfo.GetFirstOrientation());
            }
            else if (cardinalInfo.IsL())
            {
                Direction sides = cardinalInfo.GetCornerDirection();
                mesh = sides == Direction.NorthEast ? lNE
                    : sides == Direction.SouthEast ? lSE
                    : sides == Direction.SouthWest ? lSW
                    : lNW;

                orientation = sides == Direction.NorthEast ? OffsetOrientation.lNE
                    : sides == Direction.SouthEast ? OffsetOrientation.lSE
                    : sides == Direction.SouthWest ? OffsetOrientation.lSW
                    : OffsetOrientation.lNW;

                rotation = 90;
            }
            else if (cardinalInfo.IsT())
            {
                Direction notside = cardinalInfo.GetOnlyNegative();
                mesh = notside == Direction.North ? tSWE
                    : notside == Direction.East ? tNSW
                    : notside == Direction.South ? tNEW
                    : tNSE;

                orientation = notside == Direction.North ? OffsetOrientation.tSWE
                    : notside == Direction.East ? OffsetOrientation.tNSW
                    : notside == Direction.South ? OffsetOrientation.tNEW
                    : OffsetOrientation.tNSE;

                rotation = 90;
            }
            else // Must be X
            {
                mesh = x;
                orientation = OffsetOrientation.x;

                rotation = 90;
            }

            return new MeshDirectionInfo { mesh = mesh, rotation = rotation };
        }
    }
}