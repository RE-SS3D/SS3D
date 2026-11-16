using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Resolved mesh, facing, and neighbour links for a directional tile.
    /// </summary>
    public sealed class DirectionalAdjacencyResult
    {
        public DirectionalAdjacencyResult(
            Mesh mesh,
            float rotation,
            Direction facing,
            AdjacencyShape shape,
            int connectionCount,
            PlacedTileObject firstNeighbour,
            PlacedTileObject secondNeighbour)
        {
            Mesh = mesh;
            Rotation = rotation;
            Facing = facing;
            Shape = shape;
            ConnectionCount = connectionCount;
            FirstNeighbour = firstNeighbour;
            SecondNeighbour = secondNeighbour;
        }

        public Mesh Mesh { get; }

        public float Rotation { get; }

        public Direction Facing { get; }

        public AdjacencyShape Shape { get; }

        public int ConnectionCount { get; }

        public PlacedTileObject FirstNeighbour { get; }

        public PlacedTileObject SecondNeighbour { get; }
    }
}
