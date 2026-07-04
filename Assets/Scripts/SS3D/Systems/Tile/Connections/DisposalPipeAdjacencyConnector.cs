using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Furniture;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for disposal pipes only.
    /// </summary>
    public class DisposalPipeAdjacencyConnector : NetworkActor, IAdjacencyConnector, ICustomAdjacencyRecompute
    {
        [SerializeField] private DisposalPipeConnector _pipeAdjacency;

        private AdjacencyMap _adjacencyMap;
        private MeshFilter _filter;
        private PlacedTileObject _placedObject;
        private DisposalPipeConnectionRule _connectionRule;
        private bool _initialized;

        [SyncVar(OnChange = nameof(SyncEngineConnections))]
        private byte _syncedEngineConnections;

        [SyncVar(OnChange = nameof(SyncVertical))]
        private bool _verticalConnection;

        [SyncVar(OnChange = nameof(SyncDirection))]
        private Direction _direction;

        private byte _pendingEngineConnections;
        private bool _pendingVerticalConnection;
        private Direction _pendingDirection;
        private bool _hasPendingAdjacency;

        public PlacedTileObject PlacedObject => _placedObject;

        public bool VerticalConnection => _verticalConnection;

        /// <summary>
        /// Horizontal facing used for vertical mesh rotation and in-front pipe links.
        /// </summary>
        public Direction FacingDirection => _direction;

        public int HorizontalConnectionCount => _adjacencyMap?.CardinalConnectionCount ?? 0;

        public IConnectionRule ConnectionRule => _connectionRule ??= new DisposalPipeConnectionRule(this);

        private void Setup()
        {
            if (_initialized)
                return;

            _adjacencyMap = new AdjacencyMap();
            _filter = GetComponent<MeshFilter>();
            _placedObject = GetComponent<PlacedTileObject>();
            _initialized = true;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_hasPendingAdjacency)
                PublishAdjacency(_pendingEngineConnections, _pendingVerticalConnection, _pendingDirection);
        }

        public void SetAdjacencyConnections(byte horizontalConnections)
        {
            PublishAdjacency(horizontalConnections, _verticalConnection, _placedObject.Direction);
        }

        public void RecomputeAdjacency(TileMap map)
        {
            Setup();

            AdjacencyMap preliminaryMap = BuildHorizontalMap(map, vertical: false, _placedObject.Direction);
            bool vertical = DisposalPipeConnectionRule.TryGetDisposalElementAbovePipe(map, _placedObject, out _)
                && preliminaryMap.CardinalConnectionCount < 2;

            Direction facing = vertical
                ? DisposalPipeConnectionRule.ResolveVerticalFacing(preliminaryMap, _placedObject.Direction)
                : _placedObject.Direction;

            AdjacencyMap horizontalMap = vertical
                ? BuildHorizontalMap(map, vertical: true, facing)
                : preliminaryMap;

            PublishAdjacency(
                horizontalMap.SerializeToByte(),
                vertical,
                facing);
        }

        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            Setup();
            return ConnectionRule.IsConnected(_placedObject, neighbourObject);
        }

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
            if (map == null)
                return false;

            map.AdjacencyEngine.QueueUpdate(_placedObject);

            if (updateNeighbour && neighbourObject != null)
            {
                if (neighbourObject.TryGetComponent<IEngineDrivenAdjacency>(out _))
                    map.AdjacencyEngine.QueueUpdate(neighbourObject);
            }

            map.AdjacencyEngine.ProcessQueue();
            return true;
        }

        public void UpdateAllConnections()
        {
            Setup();

            TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
            if (map == null)
                return;

            map.AdjacencyEngine.QueueCascadeFrom(_placedObject);
            map.AdjacencyEngine.ProcessQueue();
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            Setup();

            List<PlacedTileObject> neighbours = new();
            TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
            if (DisposalPipeConnectionRule.TryGetDisposalElementAbovePipe(map, _placedObject, out IDisposalElement disposalElement))
            {
                PlacedTileObject placedDisposal = disposalElement.GameObject.GetComponent<PlacedTileObject>();
                if (placedDisposal != null)
                    neighbours.Add(placedDisposal);
            }

            if (map != null)
            {
                neighbours.AddRange(map.GetNeighbourPlacedObjects(_placedObject.Layer, _placedObject.transform.position));
                neighbours.RemoveAll(x => x == null);
            }

            return neighbours;
        }

        private AdjacencyMap BuildHorizontalMap(TileMap map, bool vertical, Direction facingDirection)
        {
            AdjacencyMap horizontalMap = new();
            PlacedTileObject[] neighbours = map.GetNeighbourPlacedObjects(_placedObject.Layer, _placedObject.transform.position);
            Direction selfFacing = vertical ? facingDirection : _placedObject.Direction;

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                PlacedTileObject neighbour = neighbours[(int)direction];
                bool neighbourVertical = neighbour != null
                    && neighbour.TryGetComponent(out DisposalPipeAdjacencyConnector neighbourConnector)
                    && neighbourConnector.VerticalConnection;

                Direction neighbourFacing = DisposalPipeConnectionRule.ResolveNeighbourFacing(neighbour);

                bool isConnected = DisposalPipeConnectionRule.Evaluate(
                    _placedObject,
                    neighbour,
                    vertical,
                    horizontalMap.CardinalConnectionCount,
                    neighbourVertical,
                    selfFacing,
                    neighbourFacing);

                horizontalMap.SetConnection(direction,
                    new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));
            }

            return horizontalMap;
        }

        private void PublishAdjacency(byte connections, bool vertical, Direction direction)
        {
            _adjacencyMap.DeserializeFromByte(connections);
            _verticalConnection = vertical;
            _direction = direction;
            UpdateMeshAndDirection();

            _pendingEngineConnections = connections;
            _pendingVerticalConnection = vertical;
            _pendingDirection = direction;
            _hasPendingAdjacency = true;

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                _syncedEngineConnections = connections;
                _hasPendingAdjacency = false;
            }
        }

        private void UpdateMeshAndDirection()
        {
            if (_filter == null || _adjacencyMap == null)
                return;

            var info = _pipeAdjacency.GetMeshRotationShape(_adjacencyMap, _verticalConnection);
            transform.localRotation = Quaternion.identity;

            Vector3 pos = transform.position;
            Quaternion localRotation = _filter.transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            _filter.mesh = info.Item1;

            if (info.Item3 == AdjacencyShape.Vertical)
            {
                transform.position = new Vector3(pos.x, -0.67f, pos.z);
                // verticalMesh opens opposite its connection-facing direction (model south vs world north)
                Direction meshRotation = TileHelper.GetOpposite(_direction);
                _filter.transform.localRotation = Quaternion.Euler(
                    eulerRotation.x, TileHelper.GetRotationAngle(meshRotation), eulerRotation.z);
            }
            else
            {
                transform.position = new Vector3(pos.x, 0f, pos.z);
                _filter.transform.localRotation = Quaternion.Euler(eulerRotation.x, info.Item2, eulerRotation.z);
            }
        }

        private void SyncEngineConnections(byte _, byte newValue, bool asServer)
        {
            if (!asServer)
            {
                Setup();
                _adjacencyMap.DeserializeFromByte(newValue);
                UpdateMeshAndDirection();
            }
        }

        private void SyncVertical(bool _, bool newValue, bool asServer)
        {
            if (!asServer)
            {
                Setup();
                UpdateMeshAndDirection();
            }
        }

        private void SyncDirection(Direction _, Direction newValue, bool asServer)
        {
            if (!asServer)
            {
                Setup();
                UpdateMeshAndDirection();
            }
        }
    }
}
