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
            StructuralIntegrityStage integrity = placed.IntegrityStage;
            bool leaks = integrity == StructuralIntegrityStage.Cracked
                || integrity == StructuralIntegrityStage.Destroyed;

            switch (placed.GenericType)
            {
                case TileObjectGenericType.Wall:
                    if (IsWindow(placed))
                    {
                        occupancy.IsWindow = true;
                        occupancy.HasWall = true;
                        occupancy.BlocksVision = false;
                        occupancy.BlockedEdges = ComputeWallBlockedEdges(placed, map);
                        occupancy.IsAirtight = !leaks;
                    }
                    else if (placed.tileObjectSO != null && !placed.tileObjectSO.airtight)
                    {
                        // Girder and unwelded plated girder: block movement, but air flows through
                        // their seams as if open (construction.md §6). Atmos flow reads edges.
                        occupancy.HasWall = true;
                        occupancy.BlocksVision = true;
                        occupancy.BlockedEdges = 0;
                        occupancy.IsAirtight = false;
                    }
                    else
                    {
                        occupancy.HasWall = true;
                        occupancy.BlocksVision = true;
                        occupancy.BlockedEdges = ComputeWallBlockedEdges(placed, map);
                        occupancy.IsAirtight = !leaks;
                    }

                    break;

                case TileObjectGenericType.Door:
                    bool open = placed.TryGetComponent(out IDynamicTileOccupant dynamic) && dynamic.IsOpen;
                    occupancy.IsDoor = true;
                    occupancy.DoorBlocksVision = !open;
                    occupancy.BlocksVision = !open;
                    occupancy.BlockedEdges = open ? (byte)0 : AllCardinalEdges;
                    occupancy.IsAirtight = !open && !leaks;
                    break;
            }
        }

        public static bool IsWindow(PlacedTileObject placed)
        {
            return placed != null && IsWindowName(placed.NameString);
        }

        public static bool IsWindowName(string nameString)
        {
            return !string.IsNullOrEmpty(nameString) && nameString.Contains("Window");
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
