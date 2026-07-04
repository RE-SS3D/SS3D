using SS3D.Core;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Furniture;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connection rules for disposal pipes, including vertical furniture links and vertical pipe facing.
    /// </summary>
    public sealed class DisposalPipeConnectionRule : IConnectionRule
    {
        private readonly DisposalPipeAdjacencyConnector _connector;

        public DisposalPipeConnectionRule(DisposalPipeAdjacencyConnector connector)
        {
            _connector = connector;
        }

        public bool IsConnected(PlacedTileObject self, PlacedTileObject neighbour)
        {
            TryGetNeighbourVertical(neighbour, out bool neighbourVertical);
            return Evaluate(
                self,
                neighbour,
                _connector.VerticalConnection,
                _connector.HorizontalConnectionCount,
                neighbourVertical);
        }

        public static bool Evaluate(
            PlacedTileObject self,
            PlacedTileObject neighbour,
            bool selfVertical,
            int selfCardinalConnectionCount,
            bool neighbourVertical)
        {
            if (neighbour == null)
                return false;

            if (selfVertical && neighbour.TryGetComponent(out DisposalPipeAdjacencyConnector _))
                return IsVerticalAndNeighbourInRightPosition(self, neighbour);

            if (neighbour.TryGetComponent<IDisposalElement>(out _))
                return IsConnectedToDisposalFurniture(self, neighbour, selfCardinalConnectionCount);

            if (neighbour.TryGetComponent(out DisposalPipeAdjacencyConnector _) && neighbourVertical)
                return IsConnectedToVerticalPipe(self, neighbour);

            return neighbour.HasAdjacencyConnector && neighbour.GenericType == TileObjectGenericType.Disposal;
        }

        public static bool TryGetDisposalElementAbovePipe(TileMap map, PlacedTileObject pipe, out IDisposalElement disposalFurniture)
        {
            disposalFurniture = null;
            if (map == null || pipe == null)
                return false;

            TileChunk currentChunk = map.GetChunk(pipe.transform.position);
            if (currentChunk == null)
                return false;

            SingleTileLocation furnitureLocation = (SingleTileLocation)currentChunk.GetTileLocation(
                TileLayer.FurnitureBase, pipe.Origin.x, pipe.Origin.y);
            disposalFurniture = furnitureLocation.PlacedObject?.GetComponent<IDisposalElement>();
            return disposalFurniture != null;
        }

        private static bool IsVerticalAndNeighbourInRightPosition(PlacedTileObject self, PlacedTileObject neighbour)
        {
            bool isConnected = neighbour != null;
            isConnected &= self.NeighbourAtDirectionOf(neighbour, out Direction direction);
            isConnected &= self.Direction == direction;
            return isConnected;
        }

        private static bool IsConnectedToDisposalFurniture(
            PlacedTileObject self,
            PlacedTileObject neighbour,
            int selfCardinalConnectionCount)
        {
            TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
            if (!TryGetDisposalElementAbovePipe(map, self, out IDisposalElement aboveDisposalFurniture))
                return false;

            if (!neighbour.TryGetComponent<IDisposalElement>(out IDisposalElement disposalFurniture))
                return false;

            return disposalFurniture == aboveDisposalFurniture && selfCardinalConnectionCount < 2;
        }

        private static bool IsConnectedToVerticalPipe(PlacedTileObject self, PlacedTileObject neighbour)
        {
            if (!neighbour.TryGetComponent(out DisposalPipeAdjacencyConnector neighbourConnector))
                return false;

            if (!neighbourConnector.VerticalConnection)
                return false;

            bool isConnected = neighbourConnector.PlacedObject.NeighbourAtDirectionOf(self, out Direction direction);
            isConnected &= neighbourConnector.PlacedObject.Direction == direction;
            return isConnected;
        }

        private static bool TryGetNeighbourVertical(PlacedTileObject neighbour, out bool vertical)
        {
            if (neighbour != null && neighbour.TryGetComponent(out DisposalPipeAdjacencyConnector neighbourConnector))
            {
                vertical = neighbourConnector.VerticalConnection;
                return true;
            }

            vertical = false;
            return false;
        }
    }
}
