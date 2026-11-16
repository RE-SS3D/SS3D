namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Electric devices share the circuit via <see cref="ElectricNeighbourLookup"/> but have no visual adjacency mesh to sync.
    /// Cable topology changes refresh device edges through <see cref="SS3D.Systems.Electricity.ElectricitySubSystem"/>.
    /// </summary>
    public class ElectricDeviceAdjacencyConnector : ElectricAdjacencyConnector
    {
        public override void UpdateAllConnections() { }

        public override bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            return true;
        }
    }
}
