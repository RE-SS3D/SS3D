using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
{
    /// <summary>
    /// Adjacency type used for objects that are not centred on a tile. Examples that use this are pipes (not the middle layer)
    /// </summary>
    [Serializable]
    public struct OffsetConnector
    {
        public enum OffsetOrientation
        {
            o,
            uNorth,
            uSouth,
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
        public Mesh uNorth;
        [Tooltip("A mesh where the south edge is connected, can be rotated to the west")]
        public Mesh uSouth;
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
            else if (cardinalInfo.IsU())
            {
                if (cardinalInfo.north > 0 || cardinalInfo.east > 0)
                {
                    mesh = uNorth;
                    orientation = OffsetOrientation.uNorth;
                    rotation = TileHelper.AngleBetween(Direction.North, cardinalInfo.GetOnlyPositive());
                }
                else
                {
                    mesh = uSouth;
                    orientation = OffsetOrientation.uSouth;
                    rotation = TileHelper.AngleBetween(Direction.South, cardinalInfo.GetOnlyPositive());
                }
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
                mesh = sides == Direction.NorthEast ? lNW
                    : sides == Direction.SouthEast ? lNE
                    : sides == Direction.SouthWest ? lSE
                    : lSW;

                orientation = sides == Direction.NorthEast ? OffsetOrientation.lNW
                    : sides == Direction.SouthEast ? OffsetOrientation.lSE
                    : sides == Direction.SouthWest ? OffsetOrientation.lSW
                    : OffsetOrientation.lNW;

                rotation = 90;
            }
            else if (cardinalInfo.IsT())
            {
                Direction notside = cardinalInfo.GetOnlyNegative();
                mesh = notside == Direction.North ? tNSE
                    : notside == Direction.East ? tSWE
                    : notside == Direction.South ? tNSW
                    : tNEW;

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