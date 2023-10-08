using DG.Tweening;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    public class WallAdjacencyConnector : NetworkBehaviour, IAdjacencyConnector
    {
        /// <summary>
        /// A type that specifies to which objects to connect to. Must be set if cross connect is used.
        /// </summary>
        [Tooltip("Generic ID that adjacent objects must be to count. If empty, any id is accepted.")]
        [SerializeField]
        private TileObjectGenericType _genericType;

        /// <summary>
        /// Specific ID to differentiate objects when the generic is the same.
        /// </summary>
        [Tooltip("Specific ID to differentiate objects when the generic is the same.")]
        [SerializeField]
        private TileObjectSpecificType _specificType;

        [SerializeField] private AdvancedConnector _advancedAdjacency;

        private AdjacencyMap _adjacencyMap;

        [SyncVar(OnChange = nameof(SyncAdjacencies))]
        private byte _syncedConnections;

        private MeshFilter _filter;
        private bool _initialized;
        private PlacedTileObject _placedObject;

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

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();
            bool isConnected = IsConnected(dir, neighbourObject);
            bool isUpdated = false;

            if (neighbourObject != null)
            {
                isConnected = (neighbourObject && neighbourObject.HasAdjacencyConnector);

                isConnected &= neighbourObject.GenericType == TileObjectGenericType.Wall ||
    neighbourObject.GenericType == TileObjectGenericType.Door;

                if (neighbourObject.GetComponent<DoorAdjacencyConnector>() != null)
                {
                    isConnected &= IsConnectedToDoor(neighbourObject);
                }

                // Update our neighbour as well
                if (isConnected && updateNeighbour)
                    neighbourObject.UpdateSingleAdjacency(_placedObject, TileHelper.GetOpposite(dir));
            }

            isUpdated = _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));
            if (isUpdated)
            {
                _syncedConnections = _adjacencyMap.SerializeToByte();
                UpdateMeshAndDirection();
            }

            return isUpdated;
        }

        public void UpdateAllConnections(PlacedTileObject[] neighbourObjects)
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

        private void UpdateMeshAndDirection()
        {
            MeshDirectionInfo info = new();
            info = _advancedAdjacency.GetMeshAndDirection(_adjacencyMap);

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
        /// Sets a given direction blocked. This means that it will no longer be allowed to connect on that direction.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="value"></param>
        public void SetBlockedDirection(Direction dir, bool value)
        {
            _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, value));
            UpdateMeshAndDirection();
        }

        private bool IsConnectedToDoor(PlacedTileObject neighbourObject)
        {
            var doorConnector = neighbourObject.GetComponent<DoorAdjacencyConnector>();
            var door = doorConnector.PlacedObject;
            if (door != null)
            {
                if (_placedObject.IsOnLeft(door) || _placedObject.IsOnRight(door))
                    return true;
            }

            return false;
        }

        public bool IsConnected(Direction dir, PlacedTileObject neighbourObject)
        {
            if(neighbourObject == null) return false;

            bool isConnected = (neighbourObject.HasAdjacencyConnector);

            isConnected &= neighbourObject.GenericType == TileObjectGenericType.Wall ||
                neighbourObject.GenericType == TileObjectGenericType.Door;

            if (neighbourObject.GetComponent<DoorAdjacencyConnector>() != null)
            {
                isConnected &= IsConnectedToDoor(neighbourObject);
            }

            return isConnected;
        }
    }
}
