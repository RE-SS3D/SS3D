using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// Derives vision-blocking occupancy from turf occupants and wall adjacency.
    /// </summary>
    public static class VisionOccupancyEvaluator
    {
        public static bool TryEvaluate(TileMap map, TileCoord coord, ITileLocation[] locations, out TileOccupancy occupancy)
        {
            occupancy = default;

            occupancy.HasPlenum = !locations[(int)TileLayer.Plenum].IsFullyEmpty();
            occupancy.HasTurf = !locations[(int)TileLayer.Turf].IsFullyEmpty();

            if (locations[(int)TileLayer.Turf] is not SingleTileLocation turfLocation || turfLocation.IsEmpty())
                return true;

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
                    occupancy.IsDoor = true;
                    occupancy.DoorBlocksVision = true;
                    occupancy.BlocksVision = true;
                    occupancy.BlockedEdges = VisionEdgeMask.AllCardinals;
                    occupancy.IsAirtight = true;
                    break;
            }

            return true;
        }

        public static bool IsWindow(PlacedTileObject placed)
        {
            return placed.NameString.Contains("Window");
        }

        public static byte ComputeWallBlockedEdges(PlacedTileObject wall, TileMap map)
        {
            if (map == null || wall.Layer != TileLayer.Turf || wall.GenericType != TileObjectGenericType.Wall)
                return VisionEdgeMask.AllCardinals;

            AdjacencyMap adjacencyMap = ResolveAdjacencyMap(wall, map);
            byte blockedEdges = 0;
            int edgeIndex = 0;

            foreach (Direction direction in TileHelper.CardinalDirections())
            {
                if (!adjacencyMap.HasConnection(direction))
                    blockedEdges |= VisionEdgeMask.ForCardinal(edgeIndex);

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
