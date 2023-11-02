using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Main adjacency connector class that should fulfill the majority of the use cases. This class ensures that similar objects will have their meshes seamlessly connect to each other.
    /// </summary>
    public class MultiAdjacencyConnector : NetworkBehaviour, IAdjacencyConnector
    {
        /// <summary>
        /// Determines which adjacency type should be used.
        /// </summary>
        [FormerlySerializedAs("selectedAdjacencyType")] public AdjacencyType SelectedAdjacencyType;

        /// <summary>
        /// A type that specifies to which objects to connect to. Must be set if cross connect is used.
        /// </summary>
        [Tooltip("Generic ID that adjacent objects must be to count. If empty, any id is accepted.")]
        private TileObjectGenericType _genericType;

        /// <summary>
        /// Specific ID to differentiate objects when the generic is the same.
        /// </summary>
        [Tooltip("Specific ID to differentiate objects when the generic is the same.")]
        private TileObjectSpecificType _specificType;

        [FormerlySerializedAs("simpleAdjacency")] [SerializeField] private SimpleConnector _simpleAdjacency;
        [FormerlySerializedAs("advancedAdjacency")] [SerializeField] private AdvancedConnector _advancedAdjacency;
        [FormerlySerializedAs("offsetAdjacency")] [SerializeField] private OffsetConnector _offsetAdjacency;

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
            bool isConnected = false;
            bool isUpdated = false;

            if (neighbourObject)
            {
                isConnected = (neighbourObject && neighbourObject.HasAdjacencyConnector);
                isConnected &= neighbourObject.GenericType == _genericType || _genericType == TileObjectGenericType.None;
                isConnected &= neighbourObject.SpecificType == _specificType || _specificType == TileObjectSpecificType.None;

                // Update our neighbour as well
                if (isConnected && updateNeighbour)
                    neighbourObject.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), _placedObject, false);
            }
            
            isUpdated = _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));
            if (isUpdated)
            {
                _syncedConnections = _adjacencyMap.SerializeToByte();
                UpdateMeshAndDirection();
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

        private void UpdateMeshAndDirection()
        {
            MeshDirectionInfo info = new();
            switch (SelectedAdjacencyType)
            {
                case AdjacencyType.Simple:
                    info = _simpleAdjacency.GetMeshAndDirection(_adjacencyMap);
                    break;
                case AdjacencyType.Advanced:
                    info = _advancedAdjacency.GetMeshAndDirection(_adjacencyMap);
                    break;
                case AdjacencyType.Offset:
                    info = _offsetAdjacency.GetMeshAndDirection(_adjacencyMap);
                    break;
            }

            if (_filter == null)
            {
                Log.Warning(this, "Missing mesh {meshDirectionInfo}", Logs.Generic, info );
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

        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            throw new System.NotImplementedException();
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            TileSystem tileSystem = Subsystems.Get<TileSystem>();
            var map = tileSystem.CurrentMap;
            return map.GetNeighbourPlacedObjects(_placedObject.Layer, _placedObject.gameObject.transform.position).ToList();
        }
    }
}