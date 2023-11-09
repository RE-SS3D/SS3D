using System.Collections.Generic;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Interface that all adjacency connectors should use.
    /// </summary>
    public interface IAdjacencyConnector
    {
        /// <summary>
        /// Update a single connection, and eventually update the neighbour as well. 
        /// neighbour object can be null, which generally means the neighbour object is removed.
        /// dir should indicate the direction in which the neighbour object is, if it makes sense, and if
        /// it's useful. For some neighbours,in some particula cases, it might not be useful.
        /// e.g : Disposal furniture above disposal pipes, in that case direction is not useful, so it can take
        /// any value.
        /// </summary>
        bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour);

        /// <summary>
        /// Update all connections of this Connector, generally, it means update all neighbour placed tile
        /// objects of the placed object linked to this connector.
        /// </summary>
        void UpdateAllConnections();

        /// <summary>
        /// Check if a given neighbour of the placed object linked to this connector is connected to 
        /// the placed object linked to this connector. Connection conditions
        /// may vary widely from a connector to another.
        /// </summary>
        bool IsConnected(PlacedTileObject neighbourObject);

        /// <summary>
        /// Get all neighbours from the placed object this connector is attached to.
        /// The definition of neighbours may vary from one connector to another.
        /// For most, it'll simply be other placed object placed on the same layer, on adjacent tiles.
        /// Some might have more exotic definitions, such as the disposal pipes, which can consider
        /// some disposal furnitures to be their neighbour.
        /// A neighbour is a potential candidate for a connection, it means the placed object
        /// linked to this connector will not necessarily be connected with its neighbour.
        /// </summary>
        List<PlacedTileObject> GetNeighbours();
    }
}