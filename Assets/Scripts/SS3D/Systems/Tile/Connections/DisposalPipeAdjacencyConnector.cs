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
    /// <summary>
    /// Connector for disposal pipes only.
    /// </summary>
    public class DisposalPipeAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {
        /// <summary>
        /// Structure helping to determine disposal pipes shape and rotation.
        /// </summary>
        [SerializeField] private DisposalPipeConnector _pipeAdjacency;

        /// <summary>
        /// Adjacency map for all horizontal connections.
        /// </summary>
        private AdjacencyMap _adjacencyMap;

        /// <summary>
        /// true if connected to a disposal furniture.
        /// </summary>
        [SyncVar(OnChange = nameof(SyncVertical))]
        private bool _verticalConnection;

        /// <summary>
        /// Direction of the disposal pipe. Useful to update it on client.
        /// </summary>
        [SyncVar(OnChange = nameof(SyncDirection))]
        private Direction _direction;

        /// <summary>
        /// The mesh of the disposal pipe.
        /// </summary>
        private MeshFilter _filter;

        /// <summary>
        /// The placed object for this disposal pipe.
        /// </summary>
        private PlacedTileObject _placedObject;


        /// <summary>
        /// A byte, representing the 8 possible connections with other pipes neighbours.
        /// </summary>
        [SyncVar(OnChange = nameof(SyncAdjacencies))]
        private byte _syncedConnections;

        /// <summary>
        /// Upon Setup, this should stay true.
        /// </summary>
        private bool _initialized;


        private TileObjectGenericType _disposalType = TileObjectGenericType.Disposal;

        /// <summary>
        /// Number of horizontal connections of the disposal pipe, only count cardinal connections.
        /// </summary>
        public int HorizontalConnectionCount => _adjacencyMap.CardinalConnectionCount;


        private void Setup()
        {
            if (!_initialized)
            {
                _adjacencyMap = new AdjacencyMap();
                _filter = GetComponent<MeshFilter>();
                _placedObject = GetComponent<PlacedTileObject>();
                _initialized = true;
            }
        }

        /// <summary>
        /// Connection for disposal pipes is not completely trivial. Disposal pipes should
        /// not connect to neighbour pipe if they are vertical, unless they are in front of them.
        /// If there is a disposal furniture above them, they should connected to it if they have a single
        /// connection or none. They otherwise connect only to other disposal pipes.
        /// </summary>
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

            return neighbourObject.HasAdjacencyConnector && neighbourObject.GenericType == _disposalType;
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
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            var map = tileSystem.CurrentMap;

            TileChunk currentChunk = map.GetChunk(_placedObject.gameObject.transform.position);
            SingleTileLocation furnitureLocation = (SingleTileLocation) currentChunk.GetTileLocation(TileLayer.FurnitureBase, _placedObject.Origin.x, _placedObject.Origin.y);
            disposalFurniture = furnitureLocation.PlacedObject?.GetComponent<IDisposalElement>();

            return disposalFurniture != null;
        }

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(neighbourObject);
            bool isUpdated;
            bool neighbourIsRemovedDisposalFurniture = false;

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
                neighbourIsRemovedDisposalFurniture = true;
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
                _syncedConnections = _adjacencyMap.SerializeToByte();
                _direction = _placedObject.Direction;
                UpdateMeshAndDirection();

                // Update neighbour connector if they are pipes,
                // and itself in all directions if it just removed a furniture above
                var connector = neighbourObject?.GetComponent<DisposalPipeAdjacencyConnector>();
                if(connector != null)
                {
                    connector.UpdateAllConnections();
                }
                if (neighbourIsRemovedDisposalFurniture)
                {
                    UpdateAllConnections();
                }
            }
                
            return isUpdated;
        }

        public void UpdateAllConnections()
        {
            Setup();

            var neighbourObjects = GetNeighbours();

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
                _filter.transform.localRotation = Quaternion.Euler(eulerRotation.x, TileHelper.GetRotationAngle(_direction), eulerRotation.z);
            }
            else
            {
                // reinitialize y to 0 if the model is non vertical.
                transform.position = new Vector3(pos.x, 0f, pos.z);
                _filter.transform.localRotation = Quaternion.Euler(eulerRotation.x, info.Item2, eulerRotation.z);
            }
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

        /// <summary>
        /// Sync direction on the client, and update mesh and direction.
        /// </summary>
        private void SyncDirection(Direction oldValue, Direction newValue, bool asServer)
        {
            if (!asServer)
            {
                Setup();
                UpdateMeshAndDirection();
            }
        }

        /// <summary>
        /// Get the neighbours of this disposal pipe. This include all disposal pipes
        /// adjacents (cardinal and diagonal) and eventually the disposal furniture above it.
        /// </summary>
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

            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            var map = tileSystem.CurrentMap;
            neighbours.AddRange(map.GetNeighbourPlacedObjects(_placedObject.Layer, _placedObject.gameObject.transform.position));
            neighbours.RemoveAll(x => x == null);
            return neighbours;
        }
    }
}
