using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
{
    public class MultiAdjacencyConnector : NetworkBehaviour, IAdjacencyConnector
    {
        public AdjacencyType selectedAdjacencyType;
        [Tooltip("Id that adjacent objects must be to count. If empty, any id is accepted")]
        public string type;

        public bool CrossConnectAllowed;

        [SerializeField] private SimpleConnector simpleAdjacency;
        [SerializeField] private AdvancedConnector advancedAdjacency;
        [SerializeField] private OffsetConnector offsetAdjacency;

        [SyncVar(hook = nameof(SyncAdjacentConnections))]
        private byte adjacentConnections;

        [SyncVar(hook = nameof(SyncBlockedConnections))]
        private byte blockedConnections;

        [HideInInspector]
        public byte blockedDirections;
        public byte BlockedConnections => blockedConnections;

        private AdjacencyBitmap adjacents;
        private MeshFilter filter;

        public void Awake()
        {
            EnsureInit();
            UpdateBlockedFromEditor();
        }

        public void OnEnable() => UpdateMeshAndDirection();

        private void EnsureInit()
        {
            if (adjacents == null)
                adjacents = new AdjacencyBitmap();

            if (!filter)
                filter = GetComponent<MeshFilter>();
        }

        private void SyncAdjacentConnections(byte oldConnections, byte newConnections)
        {
            EnsureInit();
            adjacentConnections = newConnections;
            adjacents.Connections = adjacentConnections;
            UpdateMeshAndDirection();
        }

        private void SyncBlockedConnections(byte oldConnections, byte newConnections)
        {
            EnsureInit();
            blockedConnections = newConnections;
            UpdateMeshAndDirection();
        }

        public override void OnStartClient()
        {
            EnsureInit();
            SyncAdjacentConnections(adjacentConnections, adjacentConnections);
            SyncBlockedConnections(blockedConnections, blockedDirections);
            base.OnStartClient();
        }

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
                    changed |= UpdateSingleConnection(dir, neighbour.GetPlacedObject(0));
                }
            }

            if (changed)
                UpdateMeshAndDirection();
        }

        public void UpdateAllOnLayer(PlacedTileObject[] neighbourObjects)
        {
            bool changed = false;
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                changed |= UpdateSingleConnection((Direction)i, neighbourObjects[i]);
            }

            if (changed)
                UpdateMeshAndDirection();
        }

        public void UpdateSingle(Direction dir, PlacedTileObject placedObject)
        {
            if (UpdateSingleConnection(dir, placedObject))
            {
                UpdateMeshAndDirection();
            }
        }

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
                    isConnected &= (placedObject.GetGenericType() == type && type != "");
                else
                    isConnected &= (placedObject.GetGenericType() == type || type == null);

                isConnected &= (AdjacencyBitmap.Adjacent(blockedConnections, dir) == 0);
            }
            bool isUpdated = adjacents.UpdateDirection(dir, isConnected, true);
            SyncAdjacentConnections(adjacents.Connections, adjacents.Connections);

            return isUpdated;
        }

        public void SetBlockedDirection(Direction dir, bool value)
        {
            byte result = AdjacencyBitmap.SetDirection(blockedConnections, dir, value);
            blockedDirections = BlockedConnections;
            SyncBlockedConnections(blockedConnections, result);
        }

        public void UpdateBlockedFromEditor()
        {
            SyncBlockedConnections(blockedConnections, blockedDirections);
        }

        private void UpdateMeshAndDirection()
        {
            MeshDirectionInfo info = new MeshDirectionInfo();
            switch (selectedAdjacencyType)
            {
                case AdjacencyType.Simple:
                    info = simpleAdjacency.GetMeshAndDirection(adjacents);
                    break;
                case AdjacencyType.Advanced:
                    info = advancedAdjacency.GetMeshAndDirection(adjacents);
                    break;
                case AdjacencyType.Offset:
                    info = offsetAdjacency.GetMeshAndDirection(adjacents);
                    break;
            }

            if (filter == null)
                filter = GetComponent<MeshFilter>();

            filter.mesh = info.mesh;

            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, info.rotation, transform.localRotation.eulerAngles.z);

            /*
            if (isServer)
                RpcUpdateMeshAndDirection(adjacents.Connections);
            */
        }

        public void CleanAdjacencies()
        {
            
        }

        /*
        [ClientRpc]
        protected void RpcUpdateMeshAndDirection(byte adjacentByte)
        {
            // Rebuild the bitmap as Mirror can only handle basic types as parameter
            MeshDirectionInfo info = new MeshDirectionInfo();
            AdjacencyBitmap adjacencyBitmap = new AdjacencyBitmap();
            adjacencyBitmap.Connections = adjacentByte;

            switch (selectedAdjacencyType)
            {
                case AdjacencyType.Simple:
                    info = simpleAdjacency.GetMeshAndDirection(adjacencyBitmap);
                    break;
                case AdjacencyType.Advanced:
                    info = advancedAdjacency.GetMeshAndDirection(adjacencyBitmap);
                    break;
                case AdjacencyType.Offset:
                    info = offsetAdjacency.GetMeshAndDirection(adjacencyBitmap);
                    break;
            }

            if (filter == null)
                filter = GetComponent<MeshFilter>();

            filter.mesh = info.mesh;
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, info.rotation, transform.localRotation.eulerAngles.z);
        }
        */
    }
}