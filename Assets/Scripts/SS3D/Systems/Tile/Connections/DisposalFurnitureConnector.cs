using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Furniture;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for disposal furniture. e.g disposal outlets and disposal bins.
    /// It doesn't do much, except checking for an available pipe just below itself, and 
    /// signaling this pipe to update itself upon adding or clearing the disposal furniture.
    /// </summary>
    public class DisposalFurnitureConnector : NetworkActor, IAdjacencyConnector
    {
        private PlacedTileObject _placedObject;

        private bool _connectedToPipe;

        private void Setup()
        {
            _placedObject = GetComponent<PlacedTileObject>();
        }

        /// <summary>
        /// Disposal furnitures are connected when they have a disposal pipe just below them
        /// and this pipe has less than two connections already.
        /// </summary>
        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            if(neighbourObject == null) return false;

            if (!neighbourObject.TryGetComponent<DisposalPipeAdjacencyConnector>(out var pipeConnector))
                return false;

            Vector2Int neighbourPosition = neighbourObject.Origin;

            if(neighbourPosition != _placedObject.Origin) return false;

            if (pipeConnector.HorizontalConnectionCount >= 2) return false;

            return true;
        }

        public void UpdateAllConnections()
        {
            
            Setup();

            // update pipe just below.
            if (TryGetPipeBelow(out PlacedTileObject pipe))
            {
                UpdateSingleConnection(Direction.North, pipe, true);
            };
        }

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(neighbourObject);
            bool updated = _connectedToPipe != isConnected;

            if (updated)
                neighbourObject.UpdateAdjacencies();

            return updated;
        }

        /// <summary>
        /// Just get the pipe below the disposal furniture if it exists.
        /// </summary>
        private bool TryGetPipeBelow(out PlacedTileObject pipe)
        {
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            var map = tileSystem.CurrentMap;

            TileChunk currentChunk = map.GetChunk(_placedObject.gameObject.transform.position);
            SingleTileLocation pipeLocation = (SingleTileLocation) currentChunk.GetTileLocation(TileLayer.Disposal,
                _placedObject.Origin.x, _placedObject.Origin.y);
            pipe = pipeLocation.PlacedObject;

            return pipe != null;
        }

        /// <summary>
        /// The only neighbour of a disposal furniture is the eventual disposal pipe just below it.
        /// </summary>
        public List<PlacedTileObject> GetNeighbours()
        {
            bool hasNeighbour = TryGetPipeBelow(out PlacedTileObject pipe);
            if (hasNeighbour) return new List<PlacedTileObject> { pipe };
            else return new List<PlacedTileObject>();  
        }
    }
}
