using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Replicated adjacency visual state for a placed tile. Applies mesh/direction from a resolver.
    /// </summary>
    public class TileAdjacencyView : NetworkBehaviour
    {
        [SyncVar(OnChange = nameof(SyncHorizontalConnections))]
        private byte _syncedHorizontalConnections;

        private AdjacencyMap _adjacencyMap;
        private IMeshAndDirectionResolver _resolver;
        private MeshFilter _filter;
        private byte _pendingHorizontalConnections;
        private bool _hasPendingConnections;

        public void Configure(IMeshAndDirectionResolver resolver)
        {
            _resolver = resolver;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_hasPendingConnections)
                PublishConnections(_pendingHorizontalConnections);
        }

        public void SetHorizontalConnections(byte connections)
        {
            _pendingHorizontalConnections = connections;
            _hasPendingConnections = true;
            ApplyConnections(connections);

            if (IsServer)
                PublishConnections(connections);
        }

        private void PublishConnections(byte connections)
        {
            _syncedHorizontalConnections = connections;
            _hasPendingConnections = false;
        }

        private void SyncHorizontalConnections(byte _, byte newValue, bool asServer)
        {
            if (!asServer)
                ApplyConnections(newValue);
        }

        private void ApplyConnections(byte connections)
        {
            EnsureInitialized();
            _adjacencyMap.DeserializeFromByte(connections);
            UpdateMeshAndDirection();
        }

        private void EnsureInitialized()
        {
            if (_adjacencyMap != null)
                return;

            _adjacencyMap = new AdjacencyMap();
            _filter = GetComponent<MeshFilter>();
        }

        private void UpdateMeshAndDirection()
        {
            if (_resolver == null)
                return;

            EnsureInitialized();

            MeshDirectionInfo info = _resolver.GetMeshAndDirection(_adjacencyMap);

            if (_filter == null)
            {
                Log.Warning(this, "Missing mesh filter for adjacency view", Logs.Generic);
                return;
            }

            _filter.mesh = info.Mesh;

            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(eulerRotation.x, info.Rotation, eulerRotation.z);
        }
    }
}
