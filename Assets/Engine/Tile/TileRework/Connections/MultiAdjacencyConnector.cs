using Mirror;
using SS3D.Engine.Tile.TileRework.Connections.AdjacencyTypes;
using SS3D.Engine.Tiles;
using SS3D.Engine.Tiles.Connections;
using UnityEngine;

namespace SS3D.Engine.Tile.TileRework.Connections
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
        private TileObjectGenericType genericType;

        /// <summary>
        /// Specific ID to differentiate objects when the generic is the same.
        /// </summary>
        [Tooltip("Specific ID to differentiate objects when the generic is the same.")]
        private TileObjectSpecificType specificType;

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
        [SyncVar(hook = nameof(SyncAdjacentConnections))]
        private byte adjacentConnections;

        /// <summary>
        /// Keeps track of blocked connections. Do not directly modify! Use the sync functions instead.
        /// </summary>
        [SyncVar(hook = nameof(SyncBlockedConnections))]
        private byte blockedConnections;

        /// <summary>
        /// As syncvars cannot be directly modified. This field is used by the AdjacencyEditor.
        /// </summary>
        [HideInInspector]
        public byte EditorblockedConnections;
        public byte BlockedConnections => blockedConnections;

        private AdjacencyMap adjacencyMap;
        private MeshFilter filter;

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

            if (adjacencyMap == null)
            {
                adjacencyMap = new AdjacencyMap();
            }

            if (!filter)
            {
                filter = GetComponent<MeshFilter>();
            }

            PlacedTileObject placedTileObject = GetComponent<PlacedTileObject>();
            if (placedTileObject == null)
            {
                genericType = TileObjectGenericType.None;
                specificType = TileObjectSpecificType.None;
            }
            else
            {
                genericType = placedTileObject.GetGenericType();
                specificType = placedTileObject.GetSpecificType();
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
            adjacentConnections = newConnections;
            adjacencyMap.DeserializeFromByte(adjacentConnections);
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
            blockedConnections = newConnections;
            UpdateMeshAndDirection();
        }

        /// <summary>
        /// Used for syncing late joiners.
        /// </summary>
        public override void OnStartClient()
        {
            EnsureInit();
            SyncAdjacentConnections(adjacentConnections, adjacentConnections);
            SyncBlockedConnections(blockedConnections, EditorblockedConnections);
            base.OnStartClient();
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
                var vector = TileHelper.ToCardinalVector(dir);
                TileObject neighbour = map.GetTileObject(layer, transform.position + new Vector3(vector.Item1, 0, vector.Item2));

                if ((neighbour.GetPlacedObject(0) && neighbour.GetPlacedObject(0).HasAdjacencyConnector()) || layer == ownLayer)
                {
                    bool connected = UpdateSingleConnection(dir, neighbour.GetPlacedObject(0));
                    changed |= connected;

                    // Update our neighbour as well
                    if (connected)
                        neighbour.GetPlacedObject(0)?.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), GetComponent<PlacedTileObject>());
                }
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
                    isConnected &= placedObject.GetGenericType() == genericType && genericType != TileObjectGenericType.None;
                }
                else
                {
                    isConnected &= placedObject.GetGenericType() == genericType || genericType == TileObjectGenericType.None;
                }

                // Check for specific
                isConnected &= placedObject.GetSpecificType() == specificType || specificType == TileObjectSpecificType.None;

                 
                isConnected &= ((blockedConnections >> (int) dir) & 0x1) == 0;
            }
            bool isUpdated = adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));
            byte connections = adjacencyMap.SerializeToByte();
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
            adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, value));
            SyncBlockedConnections(blockedConnections, adjacencyMap.SerializeToByte());
            EditorblockedConnections = BlockedConnections;
            adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, !value));
            UpdateMeshAndDirection();
        }

        /// <summary>
        /// Forces on an update of blocked connections. Editor only.
        /// </summary>
        public void UpdateBlockedFromEditor()
        {
            SyncBlockedConnections(blockedConnections, EditorblockedConnections);
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
                    info = simpleAdjacency.GetMeshAndDirection(adjacencyMap);
                    break;
                case AdjacencyType.Advanced:
                    info = advancedAdjacency.GetMeshAndDirection(adjacencyMap);
                    break;
                case AdjacencyType.Offset:
                    info = offsetAdjacency.GetMeshAndDirection(adjacencyMap);
                    break;
            }

            if (filter == null)
            {
                filter = GetComponent<MeshFilter>();
            }

            filter.mesh = info.Mesh;

            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, info.Rotation, transform.localRotation.eulerAngles.z);
        }

        /// <summary>
        /// Clean up all existing adjacencies when this object is removed. Gets messy really fast when cross connect is involved...
        /// </summary>
        public void CleanAdjacencies()
        {
            TileMap map = GetComponentInParent<TileMap>();

            var neighbourObjects = map.GetNeighbourObjects(GetComponent<PlacedTileObject>().GetLayer(), 0, transform.position);
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                neighbourObjects[i]?.UpdateSingleAdjacency(TileHelper.GetOpposite((Direction)i), null);
            }

            if (CrossConnectAllowed)
            {
                foreach (TileLayer layer in TileHelper.GetTileLayers())
                {
                    for (int i = 0; i < neighbourObjects.Length; i++)
                    {
                        // Get the neighbour for a given direction
                        var vector = TileHelper.ToCardinalVector((Direction)i);
                        TileObject neighbour = map.GetTileObject(layer, transform.position + new Vector3(vector.Item1, 0, vector.Item2));

                        if ((neighbour.GetPlacedObject(0) && neighbour.GetPlacedObject(0).HasAdjacencyConnector()))
                        {
                            neighbour.GetPlacedObject(0).UpdateSingleAdjacency(TileHelper.GetOpposite((Direction)i), null);
                        }
                    }
                }
            }
        }
    }
}