using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Logging;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections.Generic;
using UnityEngine;

namespace System.Electricity
{
    /// <summary>
    /// Cables have their own connector logic because they behave in a particular way. They can "connect" to any electric devices,
    /// but no connection will be display. Connections with other cables are the only one being displayed.
    /// </summary>
    public class CablesAdjacencyConnector : ElectricAdjacencyConnector
    {
        /// <summary>
        /// A structure containing data regarding connection of this PlacedTileObject with all 8
        /// adjacent neighbours (cardinal and diagonal connections).
        /// </summary>
        private AdjacencyMap _adjacencyMap;

        /// <summary>
        /// The specific mesh this connectable has.
        /// </summary>
        private MeshFilter _filter;

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
        public override void UpdateAllConnections()
        {
            Setup();

            List<PlacedTileObject> neighbourObjects = GetNeighbours();

            bool changed = false;

            foreach (PlacedTileObject neighbourObject in neighbourObjects)
            {
                if (!NeighbourIsCable(neighbourObject))
                {
                    continue;
                }

                PlacedObject.NeighbourAtDirectionOf(neighbourObject, out Direction dir);
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

            // should not return here if neighbour object is null, otherwise it would prevent update if a neighbour is removed.
            if (neighbourObject != null && !NeighbourIsCable(neighbourObject))
            {
                return false;
            }

            bool isConnected = IsConnected(neighbourObject);

            bool isUpdated = _adjacencyMap.SetConnection(dir, isConnected);

            if (isUpdated && updateNeighbour && neighbourObject)
            {
                neighbourObject.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), PlacedObject, false);
            }

            if (isUpdated)
            {
                _syncedConnections = _adjacencyMap.SerializeToByte();
                UpdateMeshAndDirection();
            }

            return isUpdated;
        }

        [Server]
        protected override void Setup()
        {
            if (!Initialized)
            {
                base.Setup();
                _adjacencyMap = new AdjacencyMap();
                _filter = GetComponent<MeshFilter>();
            }
        }

        /// <summary>
        /// Update the current mesh of the game object this connector is onto, as well
        /// as it's rotation.
        /// </summary>
        [ServerOrClient]
        protected virtual void UpdateMeshAndDirection()
        {
            MeshDirectionInfo info = _adjacencyResolver.GetMeshAndDirection(_adjacencyMap);

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
