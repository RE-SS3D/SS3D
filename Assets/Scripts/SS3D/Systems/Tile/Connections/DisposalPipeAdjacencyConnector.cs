using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Furniture;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections

{
    public class DisposalPipeAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {
        [SerializeField] private DisposalPipeConnector _pipeAdjacency;

        protected AdjacencyMap _adjacencyMap;

        [SyncVar(OnChange = nameof(SyncVertical))]
        protected bool _verticalConnection;

        protected MeshFilter _filter;

        protected PlacedTileObject _placedObject;

        public PlacedTileObject PlacedObject => _placedObject;

        [SyncVar(OnChange = nameof(SyncAdjacencies))]
        private byte _syncedConnections;

        private bool _initialized;

        protected TileObjectSpecificType _specificType;

        private bool _isVertical = false;

        public int HorizontalConnectionCount => _adjacencyMap.CardinalConnectionCount;

        private AdjacencyShape _currentShape;


        private void Setup()
        {
            if (!_initialized)
            {
                _adjacencyMap = new AdjacencyMap();
                _filter = GetComponent<MeshFilter>();

                _placedObject = GetComponent<PlacedTileObject>();
                if (_placedObject == null)
                {
                    _specificType = TileObjectSpecificType.None;
                }
                else
                {
                    _specificType = _placedObject.SpecificType;
                }
                _initialized = true;
            }
        }

        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            if (neighbourObject == null) return false;

            if (_verticalConnection == true 
                    && neighbourObject.TryGetComponent(out DisposalPipeAdjacencyConnector neighbourConnector))
                return IsVerticalAndNeighbourInRightPosition(neighbourObject);

            if (neighbourObject.TryGetComponent<IDisposalElement>(out var disposalFurniture))
                return IsConnectedToDisposalFurniture(neighbourObject);

            if(neighbourObject.TryGetComponent(out DisposalPipeAdjacencyConnector neighbourConnectorAgain)
                && neighbourConnectorAgain._verticalConnection == true)
                return IsConnectedToVerticalPipe(neighbourObject);

            bool isConnected;
            isConnected = neighbourObject.HasAdjacencyConnector;
            isConnected &= neighbourObject.SpecificType == _specificType;
            return isConnected;
        }

        /// <summary>
        /// check if the neighbour disposal pipe of this is connected to this, when this is a vertical pipe.
        /// Basically check if the neighbour is positionned in front of the vertical pipe.
        /// </summary>
        private bool IsVerticalAndNeighbourInRightPosition(PlacedTileObject neighbourObject)
        {
            bool isConnected = neighbourObject != null;
            isConnected &= _placedObject.NeighbourAtDirectionOf(neighbourObject, out Direction direction);
            isConnected &= _placedObject.Direction == direction;
            return isConnected;
        }

        /// <summary>
        /// Check if neighbour is the disposal furniture just above this disposal pipe.
        /// </summary>
        private bool IsConnectedToDisposalFurniture(PlacedTileObject neighbourObject)
        {
            // check if neighbour is the disposal furniture just above this.
            bool IsDisposalFurnitureAbove = false;
            TryGetDisposalElementAbovePipe(out var aboveDisposalFurniture);

            if (neighbourObject.TryGetComponent<IDisposalElement>(out var disposalFurniture)
                && disposalFurniture == aboveDisposalFurniture)
            {
                IsDisposalFurnitureAbove = true;
            }

            return IsDisposalFurnitureAbove && HorizontalConnectionCount < 2;
        }

        /// <summary>
        /// Check if this disposal pipe is connected to its neighbour disposal pipe, when the neighbour is a vertical pipe.
        /// </summary>
        private bool IsConnectedToVerticalPipe(PlacedTileObject neighbourObject)
        {
            bool isConnected = neighbourObject != null;
            isConnected &= neighbourObject.TryGetComponent(out DisposalPipeAdjacencyConnector neighbourConnector);
            isConnected &= neighbourConnector._verticalConnection == true;
            isConnected &= neighbourConnector._placedObject.NeighbourAtDirectionOf(_placedObject, out Direction direction);
            isConnected &= neighbourConnector._placedObject.Direction == direction;
            return isConnected;
        }

        /// <summary>
        /// Try to get the Disposal furniture (e.g : disposal bin) just above this disposal pipe, if it exists.
        /// </summary>
        private bool TryGetDisposalElementAbovePipe(out IDisposalElement disposalFurniture)
        {
            TileSystem tileSystem = Subsystems.Get<TileSystem>();
            var map = tileSystem.CurrentMap;

            TileChunk currentChunk = map.GetChunk(_placedObject.gameObject.transform.position);
            var furnitureLocation = currentChunk.GetTileObject(TileLayer.FurnitureBase, _placedObject.Origin.x, _placedObject.Origin.y);
            disposalFurniture = furnitureLocation.PlacedObject?.GetComponent<IDisposalElement>();

            return disposalFurniture != null;
        }

        /// <summary>
        /// Update a single connection, in a specific direction. Eventually also update the neighbour connection.
        /// </summary>
        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(neighbourObject);
            bool isUpdated;

            // If neighbour is a disposal furniture and this disposal pipe is connected to it
            if (isConnected && IsConnectedToDisposalFurniture(neighbourObject))
            {
                isUpdated = _verticalConnection != true;
                _verticalConnection = true;
            }

            // If neighbour is a removed disposal furniture which used to be connected to this disposal pipe.
            else if (neighbourObject == null 
                && !TryGetDisposalElementAbovePipe(out IDisposalElement disposalFurniture) 
                && _verticalConnection == true)
            {
                isUpdated = true;
                _verticalConnection = false;
            }

            // Else it's not a disposal furniture and we simply use the adjacency map for horizontal connections
            // with other Disposal pipes.
            else
            {
                isUpdated = _adjacencyMap.SetConnection(dir,
                    new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));
            }

            if (isUpdated)
            {
                UpdateMeshAndDirection();
            }

            if (isUpdated && updateNeighbour)
            {
                var connector = neighbourObject?.GetComponent<DisposalPipeAdjacencyConnector>();
                if(connector != null)
                {
                    connector.UpdateAllConnections(new PlacedTileObject[] { });
                }
            }
                
            return isUpdated;
        }

        // TODO : maybe interface should let updateAllconnections handle retrieving neighbours
        // object, as it might not mean the same thing for different connectors.
        public void UpdateAllConnections(PlacedTileObject[] neighbourObjects)
        {
            Setup();

            neighbourObjects = GetNeighbours().ToArray();

            bool changed = false;

            foreach (var neighbourObject in neighbourObjects)
            {
                _placedObject.NeighbourAtDirectionOf(neighbourObject, out Direction dir);
                changed |= UpdateSingleConnection(dir, neighbourObject, true);
            }

            if (changed)
            {
                UpdateMeshAndDirection();
            }
        }

        protected virtual void UpdateMeshAndDirection()
        {
            var info = _pipeAdjacency.GetMeshRotationShape(_adjacencyMap, _verticalConnection);
            transform.localRotation = Quaternion.identity;

            Vector3 pos = transform.position;
            Quaternion localRotation = _filter.transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            _filter.mesh = info.Item1;


            if (info.Item3 == AdjacencyShape.Vertical)
            {
                // remove some y so the vertical model is at the right place in the ground.
                transform.position = new Vector3(pos.x, -0.67f, pos.z); 
                _filter.transform.localRotation = Quaternion.Euler(eulerRotation.x, TileHelper.GetRotationAngle(_placedObject.Direction), eulerRotation.z);
            }
            else
            {
                // reinitialize y to 0 if the model is non vertical.
                transform.position = new Vector3(pos.x, 0f, pos.z);
                _filter.transform.localRotation = Quaternion.Euler(eulerRotation.x, info.Item2, eulerRotation.z);
            }

            _currentShape = info.Item3;
        }

        /// <summary>
        /// Sync adjacency map on client, and update mesh and direction using this new map.
        /// </summary>
        private void SyncAdjacencies(byte oldValue, byte newValue, bool asServer)
        {
            if (!asServer)
            {
                Setup();

                _adjacencyMap.DeserializeFromByte(newValue);
                UpdateMeshAndDirection();
            }
        }

        /// <summary>
        /// Sync vertical on client, and update mesh and direction.
        /// </summary>
        private void SyncVertical(bool oldValue, bool newValue, bool asServer)
        {
            if (!asServer)
            {
                Setup();
                UpdateMeshAndDirection();
            }
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            PlacedTileObject placedDisposal = null;
            List<PlacedTileObject> neighbours = new();
            if (TryGetDisposalElementAbovePipe(out IDisposalElement disposalElement))
            {
                placedDisposal = disposalElement.GameObject.GetComponent<PlacedTileObject>();
            }
            if (placedDisposal != null)
                neighbours.Add(placedDisposal);

            TileSystem tileSystem = Subsystems.Get<TileSystem>();
            var map = tileSystem.CurrentMap;
            neighbours.AddRange(map.GetNeighbourPlacedObjects(_placedObject.Layer, _placedObject.gameObject.transform.position));
            neighbours.RemoveAll(x => x == null);
            return neighbours;
        }
    }
}
