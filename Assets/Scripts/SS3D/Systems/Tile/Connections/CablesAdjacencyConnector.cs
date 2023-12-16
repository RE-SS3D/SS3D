using SS3D.Logging;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using SS3D.Systems.Tile.Connections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object.Synchronizing;
using FishNet.Object;

namespace SS3D.Systems.Tile.Connections
{
    public class CablesAdjacencyConnector : ElectricAdjacencyConnector
    {

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
        /// Script that help with resolving the specific mesh a connectable should take.
        /// </summary>
        [SerializeField]
        private SimpleConnector _adjacencyResolver;

        /// <summary>
        /// A byte, representing the 8 possible connections with neighbours.
        /// </summary>
        [SyncVar(OnChange = nameof(SyncAdjacencies))]
        private byte _syncedConnections;

        [Server]
        protected override void Setup()
        {
            if (!_initialized)
            {
                base.Setup();
                _adjacencyMap = new AdjacencyMap();
                _filter = GetComponent<MeshFilter>();
            }
        }

        [Server]
        public override void UpdateAllConnections()
        {
            Setup();

            var neighbourObjects = GetNeighbours();

            bool changed = false;

            foreach (var neighbourObject in neighbourObjects)
            {
                if (!NeighbourIsCable(neighbourObject)) continue;

                _placedObject.NeighbourAtDirectionOf(neighbourObject, out Direction dir);
                changed |= UpdateSingleConnection(dir, neighbourObject, true);
            }

            if (changed)
            {
                UpdateMeshAndDirection();
            }
        }

        [Server]
        public override bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            if (neighbourObject != null && !NeighbourIsCable(neighbourObject)) return false;

            bool isConnected = IsConnected(neighbourObject);

            bool isUpdated = _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));

            if (isUpdated && updateNeighbour)
            {
                neighbourObject?.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), _placedObject, false);
            }

            if(isUpdated) 
            {
                _syncedConnections = _adjacencyMap.SerializeToByte();
                UpdateMeshAndDirection(); 
            }

            return isUpdated;
        }

        /// <summary>
        /// Update the current mesh of the game object this connector is onto, as well
        /// as it's rotation.
        /// </summary>
        [ServerOrClient]
        protected virtual void UpdateMeshAndDirection()
        {

            MeshDirectionInfo info = new();
            info = _adjacencyResolver.GetMeshAndDirection(_adjacencyMap);

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
        /// Sync adjacency map on client, and update mesh and direction using this new map.
        /// </summary>
        [Server]
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
        /// Cables can have as neighbour electrical device but they should not physically connect to them, as they pass below them.
        /// They only visually connect to other cables. Which is why it's useful to check if a neighbour is a cable or another electric device.
        /// </summary>>
        [Server]
        private bool NeighbourIsCable(PlacedTileObject neighbour)
        {
            return neighbour != null && neighbour.Connector is CablesAdjacencyConnector;
        }

    }
}
