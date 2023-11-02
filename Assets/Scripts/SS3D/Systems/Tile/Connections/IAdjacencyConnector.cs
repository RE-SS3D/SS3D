using System.Collections.Generic;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Interface that all adjacency connectors should use.
    /// </summary>
    /// TODO : add getneighbours method, remove neighbour parameters.
    public interface IAdjacencyConnector
    {
        bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour);
        void UpdateAllConnections();
        bool IsConnected(PlacedTileObject neighbourObject);
        List<PlacedTileObject> GetNeighbours();
    }
}