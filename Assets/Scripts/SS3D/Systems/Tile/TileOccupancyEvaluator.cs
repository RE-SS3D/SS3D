using SS3D.Systems.Tile.Connections;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Derives occupancy flags (walls, doors, windows, per-edge blocking) from turf occupants and wall adjacency.
    /// </summary>
    public static class TileOccupancyEvaluator
    {
        private const byte AllCardinalEdges = 0b1111;

        public static void Evaluate(TileMap map, ITileLocation[] locations, ref TileOccupancy occupancy)
        {
            if (locations[(int)TileLayer.Turf] is not SingleTileLocation turfLocation || turfLocation.IsEmpty())
                return;

            PlacedTileObject placed = turfLocation.PlacedObject;
            switch (placed.GenericType)
            {
                case TileObjectGenericType.Wall:
                    if (IsWindow(placed))
                    {
                        occupancy.IsWindow = true;
                        occupancy.BlocksVision = false;
                        occupancy.BlockedEdges = 0;
                    }
                    else
                    {
                        occupancy.HasWall = true;
                        occupancy.BlocksVision = true;
                        occupancy.BlockedEdges = ComputeWallBlockedEdges(placed, map);
                        occupancy.IsAirtight = true;
                    }

                    break;

                case TileObjectGenericType.Door:
                    bool open = placed.TryGetComponent(out IDynamicTileOccupant dynamic) && dynamic.IsOpen;
                    occupancy.IsDoor = true;
                    occupancy.DoorBlocksVision = !open;
                    occupancy.BlocksVision = !open;
                    occupancy.BlockedEdges = open ? (byte)0 : AllCardinalEdges;
                    occupancy.IsAirtight = !open;
                    break;
            }
        }

        public static bool IsWindow(PlacedTileObject placed)
        {
            return placed.NameString.Contains("Window");
        }

        public static byte ComputeWallBlockedEdges(PlacedTileObject wall, TileMap map)
        {
            if (map == null || wall.Layer != TileLayer.Turf || wall.GenericType != TileObjectGenericType.Wall)
                return AllCardinalEdges;

            AdjacencyMap adjacencyMap = ResolveAdjacencyMap(wall, map);
            byte blockedEdges = 0;
            int edgeIndex = 0;

            foreach (Direction direction in TileHelper.CardinalDirections())
            {
                if (!adjacencyMap.HasConnection(direction))
                    blockedEdges |= (byte)(1 << edgeIndex);

                edgeIndex++;
            }

            return blockedEdges;
        }

        private static AdjacencyMap ResolveAdjacencyMap(PlacedTileObject wall, TileMap map)
        {
            if (wall.Connector is IEngineDrivenAdjacency engineDriven && engineDriven.ConnectionRule != null)
                return AdjacencyEngine.ComputeAdjacencyMap(wall, engineDriven.ConnectionRule, map);

            return new AdjacencyMap();
        }
    }
}
