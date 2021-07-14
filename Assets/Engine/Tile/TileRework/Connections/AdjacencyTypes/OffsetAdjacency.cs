using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
{
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