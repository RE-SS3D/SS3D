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
        private bool _verticalConnection;
        private Direction _direction;

        [SyncVar(OnChange = nameof(SyncAdjacencyPayload))]
        private uint _syncedAdjacencyPayload;

        private uint _pendingAdjacencyPayload;
        private bool _hasPendingAdjacency;

        public PlacedTileObject PlacedObject => _placedObject;

        public bool VerticalConnection => _verticalConnection;

        /// <summary>
        /// Horizontal facing used for vertical mesh rotation and in-front pipe links.
        /// </summary>
        public Direction FacingDirection => _direction;

        public int HorizontalConnectionCount => _adjacencyMap?.CardinalConnectionCount ?? 0;

        public IConnectionRule ConnectionRule
        {
            get
            {
                Setup();
                return _connectionRule ??= new DisposalPipeConnectionRule(this);
            }
        }

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
                PublishAdjacency(AdjacencyPayload.UnpackDisposal(_pendingAdjacencyPayload));
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Setup();

            if (!IsServer)
                ApplyAdjacency(AdjacencyPayload.UnpackDisposal(_syncedAdjacencyPayload));
        }

        public void SetAdjacencyConnections(byte horizontalConnections)
        {
            PublishAdjacency(AdjacencyPayload.ForDisposal(horizontalConnections, _verticalConnection, _direction));
        }

        public void RecomputeAdjacency(TileMap map)
        {
            Setup();

            Direction previousFacing = _direction;

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
                AdjacencyPayload.ForDisposal(
                    horizontalMap.SerializeToByte(),
                    vertical,
                    facing));

            if (vertical && facing != previousFacing)
                QueueCardinalDisposalNeighbours(map);
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

                bool isConnected = DisposalPipeConnectionRule.Evaluate(
                    _placedObject,
                    neighbour,
                    vertical,
                    horizontalMap.CardinalConnectionCount,
                    selfFacing);

                horizontalMap.SetConnection(direction,
                    new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));
            }

            return horizontalMap;
        }

        private void QueueCardinalDisposalNeighbours(TileMap map)
        {
            PlacedTileObject[] neighbours = map.GetNeighbourPlacedObjects(_placedObject.Layer, _placedObject.transform.position);
            foreach (Direction direction in TileHelper.CardinalDirections())
            {
                PlacedTileObject neighbour = neighbours[(int)direction];
                if (neighbour != null && neighbour.TryGetComponent<IEngineDrivenAdjacency>(out _))
                    map.AdjacencyEngine.QueueUpdate(neighbour);
            }
        }

        private void PublishAdjacency(AdjacencyPayload payload)
        {
            ApplyAdjacency(payload);

            _pendingAdjacencyPayload = payload.PackDisposal();
            _hasPendingAdjacency = true;

            // NetworkObject first — IsServer NREs when _networkObjectCache is null (EditMode).
            if (NetworkObject != null && NetworkObject.IsSpawned && IsServer)
            {
                _syncedAdjacencyPayload = _pendingAdjacencyPayload;
                _hasPendingAdjacency = false;
            }
        }

        private void ApplyAdjacency(AdjacencyPayload payload)
        {
            _adjacencyMap.DeserializeFromByte(payload.HorizontalConnections);
            _verticalConnection = payload.VerticalConnection;
            _direction = payload.Facing;
            UpdateMeshAndDirection();
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
            _filter.sharedMesh = info.Item1;

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

        private void SyncAdjacencyPayload(uint _, uint newValue, bool asServer)
        {
            if (!asServer)
            {
                Setup();
                ApplyAdjacency(AdjacencyPayload.UnpackDisposal(newValue));
            }
        }
    }
}
