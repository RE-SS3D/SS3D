﻿using System;
using SS3D.Tilemaps;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Adjacency type used for objects that are not centred on a tile. Examples that use this are pipes (not the middle layer)
    /// </summary>
    [Serializable]
    public struct OffsetConnector
    {
        public enum OffsetOrientation
        {
            O,
            UNorth,
            USouth,
            I,
            LNe,
            LNw,
            LSe,
            LSW,
            TNEW,
            TNSW,
            TNSE,
            TSWE,
            X
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

        private OffsetOrientation _orientation;

        public OffsetOrientation Orientation
        {
            get => _orientation;
            set => _orientation = value;
        }

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap)
        {
            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            AdjacencyShape shape = AdjacencyShapeResolver.GetOffsetShape(adjacencyMap);
            switch (shape)
            {
                case AdjacencyShape.O:
                    mesh = o;
                    Orientation = OffsetOrientation.O;
                    break;
                case AdjacencyShape.UNorth:
                    mesh = uNorth;
                    Orientation = OffsetOrientation.UNorth;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleConnection());
                    break;
                case AdjacencyShape.USouth:
                    mesh = uSouth;
                    Orientation = OffsetOrientation.USouth;
                    rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.I:
                    mesh = i;
                    Orientation = OffsetOrientation.I;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.HasConnection(Direction.North) ? Direction.North : Direction.East);
                    break;
                case AdjacencyShape.LNorthWest:
                    mesh = lNW;
                    Orientation = OffsetOrientation.LNw;
                    rotation = 90;
                    break;
                case AdjacencyShape.LNorthEast:
                    mesh = lNE;
                    Orientation = OffsetOrientation.LSe;
                    rotation = 90;
                    break;
                case AdjacencyShape.LSouthEast:
                    mesh = lSE;
                    Orientation = OffsetOrientation.LSW;
                    rotation = 90;
                    break;
                case AdjacencyShape.LSouthWest:
                    mesh = lSW;
                    Orientation = OffsetOrientation.LNw;
                    rotation = 90;
                    break;
                case AdjacencyShape.TNorthSouthEast:
                    mesh = tNSE;
                    Orientation = OffsetOrientation.TSWE;
                    rotation = 90;                      
                    break;
                case AdjacencyShape.TSouthWestEast:
                    mesh = tSWE;
                    Orientation = OffsetOrientation.TNSW;
                    rotation = 90;
                    break;
                case AdjacencyShape.TNorthSouthWest:
                    mesh = tNSW;
                    Orientation = OffsetOrientation.TNEW;
                    rotation = 90;
                    break;
                case AdjacencyShape.TNorthEastWest:
                    mesh = tNEW;
                    Orientation = OffsetOrientation.TNSE;
                    rotation = 90;
                    break;
                case AdjacencyShape.X:
                    mesh = x;
                    Orientation = OffsetOrientation.X;
                    rotation = 90;
                    break;
                default:
                    Debug.LogError($"Received unexpected shape from offset shape resolver: {shape}");
                    mesh = o;
                    break;
            }

            return new MeshDirectionInfo { Mesh = mesh, Rotation = rotation };
        }
    }
}