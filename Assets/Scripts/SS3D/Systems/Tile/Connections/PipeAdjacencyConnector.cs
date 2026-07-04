using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for pipes with a possible offset, such as atmos pipes.
    /// </summary>
    public class PipeAdjacencyConnector : AbstractHorizontalConnector, IAdjacencyConnector, IEngineDrivenAdjacency
    {
        [SerializeField] private OffsetConnector _connector;
        private TileAdjacencyView _adjacencyView;

        [SyncVar(OnChange = nameof(SyncEngineConnections))]
        private byte _syncedEngineConnections;

        private byte _pendingEngineConnections;
        private bool _hasPendingEngineConnections;

        protected override IMeshAndDirectionResolver AdjacencyResolver => _connector;

        public IConnectionRule ConnectionRule
        {
            get
            {
                PlacedTileObject placed = GetComponentInParent<PlacedTileObject>();
                return new PipeConnectionRule(placed.GenericType, placed.SpecificType);
            }
        }

        public IMeshAndDirectionResolver MeshResolver => _connector;

        private void Awake()
        {
            GetOrCreateAdjacencyView().Configure(_connector);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_hasPendingEngineConnections)
                PublishEngineConnections(_pendingEngineConnections);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            GetOrCreateAdjacencyView().Configure(_connector);
        }

        public void SetAdjacencyConnections(byte horizontalConnections)
        {
            _pendingEngineConnections = horizontalConnections;
            _hasPendingEngineConnections = true;
            GetOrCreateAdjacencyView().ApplyConnections(horizontalConnections);

            if (NetworkObject != null && NetworkObject.IsSpawned)
                PublishEngineConnections(horizontalConnections);
        }

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            return ConnectionRule.IsConnected(GetComponentInParent<PlacedTileObject>(), neighbourObject);
        }

        public override void UpdateAllConnections()
        {
            TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
            if (map == null)
                return;

            map.AdjacencyEngine.QueueCascadeFrom(PlacedObject);
            map.AdjacencyEngine.ProcessQueue();
        }

        public override bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
            if (map == null)
                return false;

            map.AdjacencyEngine.QueueUpdate(PlacedObject);

            if (updateNeighbour && neighbourObject != null && neighbourObject.TryGetComponent<IEngineDrivenAdjacency>(out _))
                map.AdjacencyEngine.QueueUpdate(neighbourObject);

            map.AdjacencyEngine.ProcessQueue();
            return true;
        }

        /// <summary>
        /// Engine-driven connectors use <see cref="TileAdjacencyView"/> instead of the legacy SyncVar path.
        /// </summary>
        protected override void UpdateMeshAndDirection()
        {
        }

        private void PublishEngineConnections(byte connections)
        {
            _syncedEngineConnections = connections;
            _hasPendingEngineConnections = false;
        }

        private void SyncEngineConnections(byte _, byte newValue, bool asServer)
        {
            if (!asServer)
                GetOrCreateAdjacencyView().ApplyConnections(newValue);
        }

        private TileAdjacencyView GetOrCreateAdjacencyView()
        {
            if (_adjacencyView == null)
                _adjacencyView = GetComponent<TileAdjacencyView>();

            if (_adjacencyView == null)
                _adjacencyView = gameObject.AddComponent<TileAdjacencyView>();

            return _adjacencyView;
        }
    }
}
