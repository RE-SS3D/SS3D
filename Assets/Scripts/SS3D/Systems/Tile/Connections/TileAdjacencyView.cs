using SS3D.Logging;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Local mesh/direction application for engine-driven adjacency. Not networked — sync lives on the connector.
    /// </summary>
    public class TileAdjacencyView : MonoBehaviour
    {
        private AdjacencyMap _adjacencyMap;
        private IMeshAndDirectionResolver _resolver;
        private MeshFilter _filter;

        public void Configure(IMeshAndDirectionResolver resolver)
        {
            _resolver = resolver;
        }

        public void ApplyConnections(byte connections)
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

            if (info.Mesh == null)
                return;

            _filter.mesh = info.Mesh;

            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(eulerRotation.x, info.Rotation, eulerRotation.z);
        }
    }
}
