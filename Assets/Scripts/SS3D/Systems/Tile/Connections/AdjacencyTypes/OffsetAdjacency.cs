using System;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Adjacency type used for objects that are not centred on a tile. Examples that use this are pipelayers 1 & 3.
    /// </summary>
    [Serializable]
    public struct OffsetConnector : IMeshAndDirectionResolver
    {
        public enum OffsetOrientation
        {
            O,
            UNorth,
            USouth,
            I,
            LNE,
            LNW,
            LSE,
            LSW,
            TNEW,
            TNSW,
            TNSE,
            TSWE,
            X
        }

        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the North edge is connected, can be rotated to the East")]
        public Mesh uNorth;
        [Tooltip("A mesh where the South edge is connected, can be rotated to the West")]
        public Mesh uSouth;
        [Tooltip("A mesh where North & South edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the North & East edges are connected")]
        public Mesh lNE;
        [Tooltip("A mesh where the North & West edges are connected")]
        public Mesh lNW;
        [Tooltip("A mesh where the South & East edges are connected")]
        public Mesh lSE;
        [Tooltip("A mesh where the South & West edges are connected")]
        public Mesh lSW;
        [Tooltip("A mesh where the South, West, & East edges are connected")]
        public Mesh tSWE;
        [Tooltip("A mesh where the North, East, & West edges are connected")]
        public Mesh tNEW;
        [Tooltip("A mesh where the North, South, & West edges are connected")]
        public Mesh tNSW;
        [Tooltip("A mesh where the North, South, & East edges are connected")]
        public Mesh tNSE;
        [Tooltip("A mesh where all edges are connected")]
        public Mesh x;

        private OffsetOrientation _orientation;

        public OffsetOrientation GetOrientation()
        {
            return _orientation;
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
                    _orientation = OffsetOrientation.O;
                    break;
                case AdjacencyShape.UNorth:
                    mesh = uSouth;
                    _orientation = OffsetOrientation.UNorth;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleConnection());
                    break;
                case AdjacencyShape.USouth:
                    mesh = uNorth;
                    _orientation = OffsetOrientation.USouth;
                    rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleConnection());
                    break;
                case AdjacencyShape.I:
                    mesh = i;
                    _orientation = OffsetOrientation.I;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.HasConnection(Direction.North) ? Direction.North : Direction.East);
                    break;
                case AdjacencyShape.LNorthWest:
                    mesh = lSE;
                    _orientation = OffsetOrientation.LNW;
                    rotation = 90;
                    break;
                case AdjacencyShape.LNorthEast:
                    mesh = lSW;
                    _orientation = OffsetOrientation.LSE;
                    rotation = 90;
                    break;
                case AdjacencyShape.LSouthEast:
                    mesh = lNW;
                    _orientation = OffsetOrientation.LSW;
                    rotation = 90;
                    break;
                case AdjacencyShape.LSouthWest:
                    mesh = lNE;
                    _orientation = OffsetOrientation.LNW;
                    rotation = 90;
                    break;
                case AdjacencyShape.TNorthSouthEast:
                    mesh = tNSW;
                    _orientation = OffsetOrientation.TSWE;
                    rotation = 90;                      
                    break;
                case AdjacencyShape.TSouthWestEast:
                    mesh = tNEW;
                    _orientation = OffsetOrientation.TNSW;
                    rotation = 90;
                    break;
                case AdjacencyShape.TNorthSouthWest:
                    mesh = tNSE;
                    _orientation = OffsetOrientation.TNEW;
                    rotation = 90;
                    break;
                case AdjacencyShape.TNorthEastWest:
                    mesh = tSWE;
                    _orientation = OffsetOrientation.TNSE;
                    rotation = 90;
                    break;
                case AdjacencyShape.X:
                    mesh = x;
                    _orientation = OffsetOrientation.X;
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