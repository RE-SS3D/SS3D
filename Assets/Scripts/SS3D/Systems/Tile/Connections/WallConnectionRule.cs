namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Walls connect to other walls and to doors on their left or right, with an edge-case guard
    /// when walls surround a door.
    /// </summary>
    public sealed class WallConnectionRule : IConnectionRule
    {
        private readonly TileMap _map;

        public WallConnectionRule(TileMap map)
        {
            _map = map;
        }

        public bool IsConnected(PlacedTileObject self, PlacedTileObject neighbour)
        {
            if (neighbour == null || !neighbour.HasAdjacencyConnector)
                return false;

            bool isConnected = neighbour.GenericType == TileObjectGenericType.Wall
                || neighbour.GenericType == TileObjectGenericType.Door;

            if (neighbour.TryGetComponent(out DoorAdjacencyConnector _))
                isConnected &= IsOnLeftOrRightOf(self, neighbour);

            if (TryGetOnLeftOrRightDoor(self, out PlacedTileObject door)
                && neighbour.TryGetComponent(out WallAdjacencyConnector _)
                && (neighbour.IsInFront(door) || neighbour.IsBehind(door)))
            {
                isConnected = false;
            }

            return isConnected;
        }

        private static bool IsOnLeftOrRightOf(PlacedTileObject self, PlacedTileObject door)
        {
            return self.IsOnLeft(door) || self.IsOnRight(door);
        }

        private bool TryGetOnLeftOrRightDoor(PlacedTileObject self, out PlacedTileObject door)
        {
            door = null;
            if (_map == null || self == null)
                return false;

            PlacedTileObject[] neighbours = _map.GetNeighbourPlacedObjects(self.Layer, self.transform.position);
            foreach (PlacedTileObject neighbour in neighbours)
            {
                if (neighbour == null || !neighbour.TryGetComponent(out DoorAdjacencyConnector _))
                    continue;

                if (self.IsOnLeft(neighbour) || self.IsOnRight(neighbour))
                {
                    door = neighbour;
                    return true;
                }
            }

            return false;
        }
    }
}
