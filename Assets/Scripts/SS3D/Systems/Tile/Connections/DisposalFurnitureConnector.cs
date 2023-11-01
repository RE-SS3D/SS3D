﻿using FishNet.Object.Synchronizing;
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
    public class DisposalFurnitureConnector : NetworkActor, IAdjacencyConnector
    {
        private PlacedTileObject _placedObject;

        private bool _connectedToPipe;

        private void Setup()
        {
            _placedObject = GetComponent<PlacedTileObject>();
        }

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

        public void UpdateAllConnections(PlacedTileObject[] neighbourObjects)
        {
            // update pipe just below.
            Setup();

            if(TryGetPipeBelow(out PlacedTileObject pipe))
            {
                UpdateSingleConnection(Direction.North, pipe, true);
            };
        }

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(neighbourObject);
            bool updated = _connectedToPipe != isConnected;

            // Update our neighbour as well
            if (isConnected && updateNeighbour)
                neighbourObject.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), _placedObject, true);

            return updated;
        }

        private bool TryGetPipeBelow(out PlacedTileObject pipe)
        {
            TileSystem tileSystem = Subsystems.Get<TileSystem>();
            var map = tileSystem.CurrentMap;

            TileChunk currentChunk = map.GetChunk(_placedObject.gameObject.transform.position);
            var pipeLocation = currentChunk.GetTileObject(TileLayer.Disposal, _placedObject.Origin.x, _placedObject.Origin.y);
            pipe = pipeLocation.PlacedObject;

            return pipe != null;
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            bool hasNeighbour = TryGetPipeBelow(out PlacedTileObject pipe);
            if (hasNeighbour) return new List<PlacedTileObject> { pipe };
            else return new List<PlacedTileObject>();  
        }
    }
}
