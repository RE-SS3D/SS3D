using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Horizontal connectors using <see cref="AdjacencyEngine"/> for server updates,
    /// <see cref="TileAdjacencyView"/> for client mesh sync, and a single engine connections SyncVar.
    /// </summary>
    public abstract class EngineDrivenHorizontalConnector : AbstractHorizontalConnector, IEngineDrivenAdjacency
    {
        private TileAdjacencyView _adjacencyView;

        [SyncVar(OnChange = nameof(SyncEngineConnections))]
        private byte _syncedEngineConnections;

        private byte _pendingEngineConnections;
        private bool _hasPendingEngineConnections;

        public abstract IConnectionRule ConnectionRule { get; }

        /// <summary>
        /// Mesh resolver driving <see cref="TileAdjacencyView"/>. Override to null when visuals use a custom path (e.g. doors).
        /// </summary>
        protected virtual IMeshAndDirectionResolver EngineMeshResolver => AdjacencyResolver;

        protected byte SyncedEngineConnections => _syncedEngineConnections;

        protected virtual void Awake()
        {
            ConfigureAdjacencyView();
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
            ConfigureAdjacencyView();

            if (!IsServer)
                ApplyEngineConnections(_syncedEngineConnections);
        }

        public virtual void SetAdjacencyConnections(byte horizontalConnections)
        {
            _pendingEngineConnections = horizontalConnections;
            _hasPendingEngineConnections = true;
            ApplyEngineConnections(horizontalConnections);

            // NetworkObject first — IsServer NREs when _networkObjectCache is null (EditMode).
            if (NetworkObject != null && NetworkObject.IsSpawned && IsServer)
                PublishEngineConnections(horizontalConnections);
        }

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            Setup();
            return ConnectionRule.IsConnected(PlacedObject, neighbourObject);
        }

        public override void UpdateAllConnections()
        {
            Setup();

            TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
            if (map == null)
                return;

            map.AdjacencyEngine.QueueCascadeFrom(PlacedObject);
            map.AdjacencyEngine.ProcessQueue();
        }

        public override bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

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

        protected virtual void ApplyEngineConnections(byte connections)
        {
            if (EngineMeshResolver == null)
                return;

            GetOrCreateAdjacencyView().ApplyConnections(connections);
        }

        protected virtual void ConfigureAdjacencyView()
        {
            if (EngineMeshResolver == null)
                return;

            GetOrCreateAdjacencyView().Configure(EngineMeshResolver);
        }

        private void PublishEngineConnections(byte connections)
        {
            _syncedEngineConnections = connections;
            _hasPendingEngineConnections = false;
        }

        private void SyncEngineConnections(byte _, byte newValue, bool asServer)
        {
            if (!asServer)
                ApplyEngineConnections(newValue);
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
