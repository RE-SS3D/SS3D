using System;
using FishNet.Object;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using SS3D.Tilemaps;
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
        public AdjacencyType selectedAdjacencyType;

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

        /// <summary>
        /// Bool that determines if objects on different layers are allowed to connect to each other.
        /// </summary>
        [Tooltip("Bool that determines if objects on different layers are allowed to connect to each other.")]
        public bool CrossConnectAllowed;

        [SerializeField] private SimpleConnector simpleAdjacency;
        [SerializeField] private AdvancedConnector advancedAdjacency;
        [SerializeField] private OffsetConnector offsetAdjacency;

        /// <summary>
        /// Keeps track of adjacenc connections. Do not directly modify! Use the sync functions instead.
        /// </summary>
        //[SyncVar(hook = nameof(SyncAdjacentConnections))]
        private byte _adjacentConnections;

        /// <summary>
        /// As syncvars cannot be directly modified. This field is used by the AdjacencyEditor.
        /// </summary>
        [FormerlySerializedAs("EditorblockedConnections")] [HideInInspector]
        public byte EditorBlockedConnections;

        /// <summary>
        /// Keeps track of blocked connections. Do not directly modify! Use the sync functions instead.
        /// </summary>
        private byte BlockedConnections { get; set; }

        private AdjacencyMap _adjacencyMap;
        private MeshFilter _filter;

        public void Awake()
        {
            EnsureInit();
            UpdateBlockedFromEditor();
        }

        public void OnEnable() => UpdateMeshAndDirection();

        /// <summary>
        /// Ensures that the object is properly initialized.
        /// </summary>
        private void EnsureInit()
        {
            if (!this)
            {
                return;
            }

            if (_adjacencyMap == null)
            {
                _adjacencyMap = new AdjacencyMap();
            }

            if (!_filter)
            {
                _filter = GetComponent<MeshFilter>();
            }

            PlacedTileObject placedTileObject = GetComponent<PlacedTileObject>();
            if (placedTileObject == null)
            {
                _genericType = TileObjectGenericType.None;
                _specificType = TileObjectSpecificType.None;
            }
            else
            {
                _genericType = placedTileObject.GetGenericType();
                _specificType = placedTileObject.GetSpecificType();
            }
        }

        /// <summary>
        /// Syncs adjacent connections with clients. Use this function to modify the attribute.
        /// </summary>
        /// <param name="oldConnections"></param>
        /// <param name="newConnections"></param>
        private void SyncAdjacentConnections(byte oldConnections, byte newConnections)
        {
            EnsureInit();
            _adjacentConnections = newConnections;
            _adjacencyMap.DeserializeFromByte(_adjacentConnections);
            UpdateMeshAndDirection();
        }

        /// <summary>
        /// Syncs blocked connections with clients. Use this function to modify the attribute.
        /// </summary>
        /// <param name="oldConnections"></param>
        /// <param name="newConnections"></param>
        private void SyncBlockedConnections(byte oldConnections, byte newConnections)
        {
            EnsureInit();
            BlockedConnections = newConnections;
            UpdateMeshAndDirection();
        }

        /// <summary>
        /// Used for syncing late joiners.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            EnsureInit();
            SyncAdjacentConnections(_adjacentConnections, _adjacentConnections);
            SyncBlockedConnections(BlockedConnections, EditorBlockedConnections);
        }

        /// <summary>
        /// Updates the object based on the given neighbours. If cross connect is used, it will determine it's own neighbours.
        /// </summary>
        /// <param name="neighbourObjects"></param>
        public void UpdateAll(PlacedTileObject[] neighbourObjects)
        {
            if (CrossConnectAllowed)
            {
                // Hacky way to find which layer the tileObjectSO is
                TileLayer layer = GetComponent<PlacedTileObject>().GetLayer();

                for (int i = 0; i < neighbourObjects.Length; i++)
                {
                    UpdateAllOnDirection((Direction)i, layer);
                }
            }
            else
            {
                UpdateAllOnLayer(neighbourObjects);
            }
        }

        /// <summary>
        /// Summerizes all layers together for a given direction to see if a connection can be made. Only used for cross connect.
        /// Needs to know it's own layer to see if the value can be set to null.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="ownLayer"></param>
        private void UpdateAllOnDirection(Direction dir, TileLayer ownLayer)
        {
            TileMap map = GetComponentInParent<TileMap>();

            bool changed = false;
            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                // Get the neighbour for a given direction
                Tuple<int, int> vector = TileHelper.ToCardinalVector(dir);
                TileObject neighbour = map.GetTileObject(layer, transform.position + new Vector3(vector.Item1, 0, vector.Item2));

                if ((!neighbour.GetPlacedObject(0) || !neighbour.GetPlacedObject(0).HasAdjacencyConnector()) && layer != ownLayer)
                {
                    continue;
                }

                bool connected = UpdateSingleConnection(dir, neighbour.GetPlacedObject(0));
                changed |= connected;

                // Update our neighbour as well
                if (connected)
                    neighbour.GetPlacedObject(0)?.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), GetComponent<PlacedTileObject>());
            }

            if (changed)
            {
                UpdateMeshAndDirection();
            }
        }

        /// <summary>
        /// Goes through all directions for a given layer.
        /// </summary>
        /// <param name="neighbourObjects"></param>
        public void UpdateAllOnLayer(PlacedTileObject[] neighbourObjects)
        {
            bool changed = false;
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                changed |= UpdateSingleConnection((Direction)i, neighbourObjects[i]);
            }

            if (changed)
            {
                UpdateMeshAndDirection();
            }
        }

        /// <summary>
        /// Update a single direction. Updates meshes if changed.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="placedObject"></param>
        public void UpdateSingle(Direction dir, PlacedTileObject placedObject)
        {
            if (!this)
                return;

            if (UpdateSingleConnection(dir, placedObject))
            {
                UpdateMeshAndDirection();
            }
        }

        /// <summary>
        /// Update a single direction.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="placedObject"></param>
        /// <returns></returns>
        private bool UpdateSingleConnection(Direction dir, PlacedTileObject placedObject)
        {
            // For some reason called before OnAwake()
            EnsureInit();
            UpdateBlockedFromEditor();

            bool isConnected = false;
            if (placedObject)
            {
                isConnected = (placedObject && placedObject.HasAdjacencyConnector());

                // If cross connect is allowed, we only allow it to connect when the object type matches the connector type
                if (CrossConnectAllowed)
                {
                    isConnected &= placedObject.GetGenericType() == _genericType && _genericType != TileObjectGenericType.None;
                }
                else
                {
                    isConnected &= placedObject.GetGenericType() == _genericType || _genericType == TileObjectGenericType.None;
                }

                // Check for specific
                isConnected &= placedObject.GetSpecificType() == _specificType || _specificType == TileObjectSpecificType.None;

                 
                isConnected &= ((BlockedConnections >> (int) dir) & 0x1) == 0;
            }
            bool isUpdated = _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));
            byte connections = _adjacencyMap.SerializeToByte();
            SyncAdjacentConnections(connections, connections);

            // Cross connect will override adjacents for other layers, so return isConnected instead.
            return CrossConnectAllowed ? isConnected : isUpdated;
        }

        /// <summary>
        /// Sets a given direction blocked. This means that it will no longer be allowed to connect on that direction.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="value"></param>
        public void SetBlockedDirection(Direction dir, bool value)
        {
            _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, value));
            SyncBlockedConnections(BlockedConnections, _adjacencyMap.SerializeToByte());
            EditorBlockedConnections = BlockedConnections;
            _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, !value));
            UpdateMeshAndDirection();
        }

        /// <summary>
        /// Forces on an update of blocked connections. Editor only.
        /// </summary>
        public void UpdateBlockedFromEditor()
        {
            SyncBlockedConnections(BlockedConnections, EditorBlockedConnections);
        }

        /// <summary>
        /// Updates the meshes and direction based on the used Adjacency type.
        /// </summary>
        private void UpdateMeshAndDirection()
        {
            MeshDirectionInfo info = new MeshDirectionInfo();
            switch (selectedAdjacencyType)
            {
                case AdjacencyType.Simple:
                    info = simpleAdjacency.GetMeshAndDirection(_adjacencyMap);
                    break;
                case AdjacencyType.Advanced:
                    info = advancedAdjacency.GetMeshAndDirection(_adjacencyMap);
                    break;
                case AdjacencyType.Offset:
                    info = offsetAdjacency.GetMeshAndDirection(_adjacencyMap);
                    break;
            }

            if (_filter == null)
            {
                _filter = GetComponent<MeshFilter>();
            }

            _filter.mesh = info.Mesh;

            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, info.Rotation, transform.localRotation.eulerAngles.z);
        }

        /// <summary>
        /// Clean up all existing adjacencies when this object is removed. Gets messy really fast when cross connect is involved...
        /// </summary>
        public void CleanAdjacencies()
        {
            TileMap map = GetComponentInParent<TileMap>();

            PlacedTileObject[] neighbourObjects = map.GetNeighbourObjects(GetComponent<PlacedTileObject>().GetLayer(), 0, transform.position);
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                neighbourObjects[i]?.UpdateSingleAdjacency(TileHelper.GetOpposite((Direction)i), null);
            }

            if (!CrossConnectAllowed)
            {
                return;
            }

            {
                foreach (TileLayer layer in TileHelper.GetTileLayers())
                {
                    for (int i = 0; i < neighbourObjects.Length; i++)
                    {
                        // Get the neighbour for a given direction
                        Tuple<int, int> vector = TileHelper.ToCardinalVector((Direction)i);
                        TileObject neighbour = map.GetTileObject(layer, transform.position + new Vector3(vector.Item1, 0, vector.Item2));

                        if (neighbour.GetPlacedObject(0) && neighbour.GetPlacedObject(0).HasAdjacencyConnector())
                        {
                            neighbour.GetPlacedObject(0).UpdateSingleAdjacency(TileHelper.GetOpposite((Direction)i), null);
                        }
                    }
                }
            }
        }
    }
}