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

        public bool IsConnected(Direction dir, PlacedTileObject neighbourObject)
        {
            if (neighbourObject == null) return false;

            TileSystem tileSystem = Subsystems.Get<TileSystem>();
            var map = tileSystem.CurrentMap;

            TryGetDisposalElementAbovePipe(out var aboveDisposalElement);

            if(neighbourObject.TryGetComponent<IDisposalElement>(out var neighbourElement) 
                && neighbourElement == aboveDisposalElement)
            {
                return true;
            }

            bool isConnected;
            isConnected = (neighbourObject && neighbourObject.HasAdjacencyConnector);
            isConnected &= neighbourObject.SpecificType == _specificType;
            return isConnected;
        }

        private bool TryGetDisposalElementAbovePipe(out IDisposalElement disposalOutlet)
        {
            TileSystem tileSystem = Subsystems.Get<TileSystem>();
            var map = tileSystem.CurrentMap;

            TileChunk currentChunk = map.GetChunk(_placedObject.gameObject.transform.position);
            var furnitureLocation = currentChunk.GetTileObject(TileLayer.FurnitureBase, _placedObject.Origin.x, _placedObject.Origin.y);
            disposalOutlet = furnitureLocation.PlacedObject?.GetComponent<IDisposalElement>();

            return disposalOutlet != null;
        }

        /// <summary>
        /// Update a single connection, in a specific direction. Eventually also update the neighbour connection.
        /// </summary>
        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(dir, neighbourObject);

            // Update our neighbour as well
            if (isConnected && updateNeighbour)
                neighbourObject.UpdateSingleAdjacency(_placedObject, TileHelper.GetOpposite(dir));

            bool isUpdated;
            bool isVertical;

            if(neighbourObject != null)
            {
                if (neighbourObject.GetComponent<IDisposalElement>() != null)
                {
                    isVertical = true;
                    isUpdated = _verticalConnection != isVertical;
                }
                else
                {
                    isVertical = false;
                    isUpdated = _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));
                }

                if (isUpdated && !isVertical)
                {
                    _syncedConnections = _adjacencyMap.SerializeToByte();
                    UpdateMeshAndDirection();
                }

                if (isUpdated && isVertical)
                {
                    _verticalConnection = isVertical;
                    UpdateMeshAndDirection();
                }

                return isUpdated;
            }
            

            isUpdated = _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));
            if (isUpdated) 
            {
                UpdateMeshAndDirection();
            }

            return isUpdated;
        }

        // TODO : maybe interface should let updateAllconnections handle retrieving neighbours
        // object, as it might not mean the same thing for different connectors.
        public void UpdateAllConnections(PlacedTileObject[] neighbourObjects)
        {
            Setup();

            bool changed = false;
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                changed |= UpdateSingleConnection((Direction)i, neighbourObjects[i], true);
            }

            if(TryGetDisposalElementAbovePipe(out IDisposalElement disposalElement))
            {
                var placedDisposal = disposalElement.GameObject.GetComponent<PlacedTileObject>();
                changed |= UpdateSingleConnection(0, placedDisposal, false);
            }

            if (changed)
            {
                UpdateMeshAndDirection();
            }
        }

        protected virtual void UpdateMeshAndDirection()
        {
            var info = _pipeAdjacency.GetMeshAndDirection(_adjacencyMap, _verticalConnection);
            transform.localRotation = Quaternion.identity;

            Vector3 pos = transform.position;
            Quaternion localRotation = _filter.transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            _filter.mesh = info.Mesh;

            if (info.Mesh == _pipeAdjacency.verticalMesh)
            {
                _isVertical= true;
                transform.position = new Vector3(pos.x, -0.67f, pos.z);
                _filter.transform.localRotation = Quaternion.Euler(eulerRotation.x, TileHelper.GetRotationAngle(_placedObject.Direction), eulerRotation.z);

                if (!_isVertical)
                {
                    var directions = TileHelper.AllDirections();
                    directions.Remove(TileHelper.GetOpposite(_placedObject.Direction));
                    SetBlockedDirection(directions, false);

                }
                
                return;
            }
            else if (_filter.mesh == _pipeAdjacency.verticalMesh)
            {
                transform.position = new Vector3(pos.x, 0f, pos.z);
            }

            if (info.Mesh != _pipeAdjacency.verticalMesh)
            {
                _isVertical = false;
            }

            localRotation = Quaternion.Euler(eulerRotation.x, info.Rotation, eulerRotation.z);
            _filter.transform.localRotation = localRotation;
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
        /// Sync adjacency map on client, and update mesh and direction using this new map.
        /// </summary>
        private void SyncVertical(bool oldValue, bool newValue, bool asServer)
        {
            if (!asServer)
            {
                Setup();

                _verticalConnection = newValue;
                UpdateMeshAndDirection();
            }
        }

        /// <summary>
        /// Sets a given direction blocked or unblocked. 
        /// If blocked, this means that it will no longer be allowed to connect on that direction (until further update).
        /// </summary>
        private void SetBlockedDirection(List<Direction> directions, bool value)
        {
            foreach(var dir in directions)
            {
                _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, value));  
            }

            UpdateMeshAndDirection();
        }
    }
}
