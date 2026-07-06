namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Electric devices share the circuit via <see cref="ElectricNeighbourLookup"/> but have no visual adjacency mesh to sync.
    /// Intentionally not engine-driven — cable neighbours trigger graph updates through <see cref="ElectricitySubSystem"/>.
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
