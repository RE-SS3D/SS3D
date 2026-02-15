using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Abstract class implementing the IAdjacencyConnector interface. 
    /// Idea of this class is to provide a basic implementation for all adjacency connector working mostly
    /// on the same layer. This is because it uses the adjacencyMap, which consider only 8 possible connections.
    /// It should be generic enough to work as a base class for most IAdjacencyConnector working with same
    /// layer connections (so almost all of them).
    /// It leaves to child classes to implement isConnected method, as this may vary greatly from one connector
    /// to another.
    /// </summary>
    public abstract class AbstractHorizontalConnector : NetworkActor, IAdjacencyConnector
    {
        /// <summary>
        /// Is this a table ? A Door ? A carpet ? A pipe ?
        /// </summary>
        protected TileObjectGenericType _genericType;


        /// <summary>
        /// Is this a wooden table ? A glass table ? A poker table ?
        /// </summary>
        protected TileObjectSpecificType _specificType;

        /// <summary>
        /// Script that help with resolving the specific mesh a connectable should take.
        /// </summary>
        protected abstract IMeshAndDirectionResolver AdjacencyResolver { get; }

        /// <summary>
        /// A structure containing data regarding connection of this PlacedTileObject with all 8
        /// adjacent neighbours (cardinal and diagonal connections).
        /// </summary>
        protected AdjacencyMap _adjacencyMap;

        /// <summary>
        /// The specific mesh this connectable has.
        /// </summary>
        protected MeshFilter _filter;

        /// <summary>
        /// The Placed tile object linked to this connector. It's this placed object that this
        /// connector update.
        /// </summary>
        protected PlacedTileObject _placedObject;

        public PlacedTileObject PlacedObject => _placedObject;

        /// <summary>
        /// A byte, representing the 8 possible connections with neighbours.
        /// </summary>
        [SyncVar(OnChange = nameof(SyncAdjacencies))]
        private byte _syncedConnections;

        /// <summary>
        /// Upon Setup, this should stay true.
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// Abstract method, as from one connector to another, the code to check for connection greatly changes.
        /// </summary>
        public abstract bool IsConnected(PlacedTileObject neighbourObject);

        public EventHandler<MeshDirectionInfo> OnMeshUpdate;


        public override void OnStartClient()
        {
            base.OnStartClient();
            Setup();
        }

        /// <summary>
        /// Simply set things up, including creating new references, and fetching generic and specific type
        /// from the associated scriptable object.
        /// </summary>
        private void Setup()
        {
            if (!_initialized)
            {
                _adjacencyMap = new AdjacencyMap();
                _filter = GetComponent<MeshFilter>();

                _placedObject = GetComponentInParent<PlacedTileObject>();
                if (_placedObject == null)
                {
                    _genericType = TileObjectGenericType.None;
                    _specificType = TileObjectSpecificType.None;
                }
                else
                {
                    _genericType = _placedObject.GenericType;
                    _specificType = _placedObject.SpecificType;
                }
                _initialized = true;
            }
        }

        /// <summary>
        /// Update all connections around this connector, updating mesh and directions eventually.
        /// </summary>
        public virtual void UpdateAllConnections()
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

        /// <summary>
        /// Update a single connection, in a specific direction. Eventually also update the neighbour connection.
        /// </summary>
        public virtual bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(neighbourObject);

            bool isUpdated = _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));

            if (isUpdated)
            {
                neighbourObject?.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), _placedObject, false);
                _syncedConnections = _adjacencyMap.SerializeToByte();
                UpdateMeshAndDirection();
            }

            return isUpdated;
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
        /// Update the current mesh of the game object this connector is onto, as well
        /// as it's rotation.
        /// </summary>
        protected virtual void UpdateMeshAndDirection()
        {
            if (AdjacencyResolver == null) return;

            MeshDirectionInfo info = new();
            info = AdjacencyResolver.GetMeshAndDirection(_adjacencyMap);

            if (_filter == null)
            {
                Log.Warning(this, "Missing mesh {meshDirectionInfo}", Logs.Generic, info);
            }

            _filter.mesh = info.Mesh;

            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            localRotation = Quaternion.Euler(eulerRotation.x, info.Rotation, eulerRotation.z);

            transform.localRotation = localRotation;

            OnMeshUpdate?.Invoke(this, info);
        }

        /// <summary>
        /// Sets a given direction blocked or unblocked. 
        /// If blocked, this means that it will no longer be allowed to connect on that direction (until further update).
        /// </summary>
        public void SetBlockedDirection(Direction dir, bool value)
        {
            _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, value));
            UpdateMeshAndDirection();
        }

        /// <summary>
        /// Get all neighbour placed objects from this placed object.
        /// Return only existing neighbours. Neighbours here are adjacent (cardinal and diagonal)
        /// tile objects on the same layer.
        /// </summary>
        public List<PlacedTileObject> GetNeighbours()
        {
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            var map = tileSystem.CurrentMap;
            var neighbours = map.GetNeighbourPlacedObjects(_placedObject.Layer, _placedObject.gameObject.transform.position).ToList();
            neighbours.RemoveAll(x => x == null);
            return neighbours;
        }
    }
}
