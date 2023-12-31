namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Very simple class for electric devices that do not need their mesh to be updated when something connect to them;
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
