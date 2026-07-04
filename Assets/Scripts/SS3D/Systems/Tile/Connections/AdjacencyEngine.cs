using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Server-side adjacency recompute queue. Replaces recursive neighbour ping-pong for engine-driven connectors.
    /// </summary>
    public sealed class AdjacencyEngine
    {
        private readonly TileMap _map;
        private readonly Queue<PlacedTileObject> _queue = new();
        private readonly HashSet<PlacedTileObject> _enqueued = new();

        public AdjacencyEngine(TileMap map)
        {
            _map = map;
        }

        public void QueueUpdate(PlacedTileObject tile)
        {
            if (tile == null || !_enqueued.Add(tile))
                return;

            _queue.Enqueue(tile);
        }

        public void QueueCascadeFrom(PlacedTileObject tile)
        {
            if (tile == null)
                return;

            QueueUpdate(tile);

            if (!tile.TryGetComponent(out IAdjacencyConnector connector))
                return;

            foreach (PlacedTileObject neighbour in connector.GetNeighbours())
                QueueUpdate(neighbour);
        }

        public void ProcessQueue()
        {
            while (_queue.Count > 0)
            {
                PlacedTileObject tile = _queue.Dequeue();
                _enqueued.Remove(tile);

                if (tile == null)
                    continue;

                UpdateTile(tile);
            }
        }

        private void UpdateTile(PlacedTileObject tile)
        {
            if (!tile.TryGetComponent(out IEngineDrivenAdjacency engineDriven))
                return;

            IConnectionRule rule = engineDriven.ConnectionRule;
            TileAdjacencyView view = engineDriven.AdjacencyView;
            IMeshAndDirectionResolver resolver = engineDriven.MeshResolver;

            if (rule == null || view == null || resolver == null)
                return;

            view.Configure(resolver);

            AdjacencyMap adjacencyMap = ComputeAdjacencyMap(tile, rule, _map);
            view.SetHorizontalConnections(adjacencyMap.SerializeToByte());
        }

        public static AdjacencyMap ComputeAdjacencyMap(PlacedTileObject tile, IConnectionRule rule, TileMap map)
        {
            AdjacencyMap adjacencyMap = new();
            PlacedTileObject[] neighbours = map.GetNeighbourPlacedObjects(tile.Layer, tile.transform.position);

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                PlacedTileObject neighbour = neighbours[(int)direction];
                bool connected = rule.IsConnected(tile, neighbour);
                adjacencyMap.SetConnection(direction,
                    new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, connected));
            }

            return adjacencyMap;
        }
    }
}
