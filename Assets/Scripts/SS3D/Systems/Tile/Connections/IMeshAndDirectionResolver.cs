using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Interface for classes that help adjacency connectors to determine a given shape and direction for the mesh,
    /// given an adjacency map representing connections.
    /// </summary>
    public interface IMeshAndDirectionResolver
    {
        public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap);
    }
}
