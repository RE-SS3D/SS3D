using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Attributes;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    public abstract class AbstractHorizontalConnector : NetworkActor, IAdjacencyConnector
    {
        protected TileObjectGenericType _genericType;


        protected TileObjectSpecificType _specificType;

        protected abstract IMeshAndDirectionResolver AdjacencyResolver { get; }

        protected AdjacencyMap _adjacencyMap;

        protected MeshFilter _filter;
        protected PlacedTileObject _placedObject;

        public PlacedTileObject PlacedObject => _placedObject;

        [SyncVar(OnChange = nameof(SyncAdjacencies))]
        private byte _syncedConnections;

        private bool _initialized;

        public override void OnStartClient()
        {
            base.OnStartClient();
            Setup();
        }

        private void Setup()
        {
            if (!_initialized)
            {
                _adjacencyMap = new AdjacencyMap();
                _filter = GetComponent<MeshFilter>();

                _placedObject = GetComponent<PlacedTileObject>();
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

        public abstract bool IsConnected(Direction dir, PlacedTileObject neighbourObject);

        public virtual void UpdateAllConnections(PlacedTileObject[] neighbourObjects)
        {
            Setup();

            bool changed = false;
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                changed |= UpdateSingleConnection((Direction)i, neighbourObjects[i], true);
            }

            if (changed)
            {
                UpdateMeshAndDirection();
            }
        }

        public virtual bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            bool isConnected = IsConnected(dir, neighbourObject);

            // Update our neighbour as well
            if (isConnected && updateNeighbour)
                neighbourObject.UpdateSingleAdjacency(_placedObject, TileHelper.GetOpposite(dir));

            bool isUpdated = _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));

            if (isUpdated)
            {
                _syncedConnections = _adjacencyMap.SerializeToByte();
                UpdateMeshAndDirection();
            }

            return isUpdated;
        }

        private void SyncAdjacencies(byte oldValue, byte newValue, bool asServer)
        {
            if (!asServer)
            {
                Setup();

                _adjacencyMap.DeserializeFromByte(newValue);
                UpdateMeshAndDirection();
            }
        }


        protected void UpdateMeshAndDirection()
        {
            // Some connectors might not have to update mesh or direction at all.
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
        }

        /// <summary>
        /// Sets a given direction blocked. This means that it will no longer be allowed to connect on that direction.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="value"></param>
        public void SetBlockedDirection(Direction dir, bool value)
        {
            _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, value));
            UpdateMeshAndDirection();
        }
    }
}
