using SS3D.Core;
using SS3D.Core.Behaviours;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for disposal furniture (bins, outlets). Triggers adjacency recompute on the
    /// disposal pipe below via <see cref="AdjacencyEngine"/>; no visual payload of its own.
    /// </summary>
    public class DisposalFurnitureConnector : NetworkActor, IAdjacencyConnector
    {
        private PlacedTileObject _placedObject;
        private bool _connectedToPipe;

        private void Setup()
        {
            if (_placedObject == null)
                _placedObject = GetComponent<PlacedTileObject>();
        }

        /// <summary>
        /// Disposal furniture connects when a disposal pipe sits below at the same origin
        /// and the pipe has fewer than two horizontal connections.
        /// </summary>
        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            if (neighbourObject == null)
                return false;

            if (!neighbourObject.TryGetComponent<DisposalPipeAdjacencyConnector>(out var pipeConnector))
                return false;

            if (neighbourObject.Origin != _placedObject.Origin)
                return false;

            return pipeConnector.HorizontalConnectionCount < 2;
        }

        public void UpdateAllConnections()
        {
            Setup();

            TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
            if (map == null)
                return;

            if (TryGetPipeBelow(map, out PlacedTileObject pipe))
                UpdateSingleConnection(Direction.North, pipe, true);
        }

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
            if (map == null || neighbourObject == null)
                return false;

            bool isConnected = IsConnected(neighbourObject);
            bool updated = _connectedToPipe != isConnected;
            _connectedToPipe = isConnected;

            if (updateNeighbour && neighbourObject.TryGetComponent<IEngineDrivenAdjacency>(out _))
            {
                map.AdjacencyEngine.QueueUpdate(neighbourObject);
                map.AdjacencyEngine.ProcessQueue();
            }

            return updated;
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            Setup();

            TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
            if (map != null && TryGetPipeBelow(map, out PlacedTileObject pipe))
                return new List<PlacedTileObject> { pipe };

            return new List<PlacedTileObject>();
        }

        private bool TryGetPipeBelow(TileMap map, out PlacedTileObject pipe)
        {
            pipe = null;
            if (map == null)
                return false;

            if (!map.TryGetTileLocation(TileLayer.Disposal, _placedObject.transform.position, out ITileLocation location))
                return false;

            if (!location.TryGetPlacedObject(out pipe, Direction.North))
                return false;

            return pipe.TryGetComponent<DisposalPipeAdjacencyConnector>(out _);
        }
    }
}
