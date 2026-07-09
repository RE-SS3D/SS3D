using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for booths, benches, and other direction-facing connectable furniture.
    /// Geometry is evaluated by <see cref="DirectionalConfigurationEvaluator"/>.
    /// </summary>
    public class DirectionalAdjacencyConnector : NetworkActor, IAdjacencyConnector, ICustomAdjacencyRecompute
    {
        public DirectionnalShapeResolver AdjacencyResolver;

        protected MeshFilter _filter;
        protected PlacedTileObject _placedObject;

        private bool _initialized;
        private float _currentRotation;
        private int _currentConnections;
        private AdjacencyShape _currentShape;
        private PlacedTileObject _firstNeighbour;
        private PlacedTileObject _secondNeighbour;

        [SyncVar(OnChange = nameof(SyncRotation))]
        private float _syncedRotation;

        [SyncVar(OnChange = nameof(SyncShape))]
        private AdjacencyShape _syncedShape;

        private float _pendingRotation;
        private AdjacencyShape _pendingShape;
        private bool _hasPendingVisual;

        public IConnectionRule ConnectionRule => null;

        public void SetAdjacencyConnections(byte horizontalConnections)
        {
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_hasPendingVisual)
                PublishVisualState(_pendingRotation, _pendingShape);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Setup();

            if (!IsServer)
                ApplyVisualState(_syncedRotation, _syncedShape);
        }

        public void RecomputeAdjacency(TileMap map)
        {
            UpdateAllAndNeighbours(map);
        }

        private void Setup()
        {
            if (_initialized)
                return;

            _filter = GetComponent<MeshFilter>();
            _placedObject = GetComponent<PlacedTileObject>();
            _initialized = true;
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            Setup();
            TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
            return GetNeighbours(map);
        }

        private List<PlacedTileObject> GetNeighbours(TileMap map)
        {
            Setup();

            if (map == null || _placedObject == null)
                return new List<PlacedTileObject>();

            IEnumerable<PlacedTileObject> neighbours = map.GetCardinalNeighbourPlacedObjects(
                _placedObject.Layer, _placedObject.transform.position);

            return neighbours.Where(x => x != null &&
                x.TryGetComponent<DirectionalAdjacencyConnector>(out _)).ToList();
        }

        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            return neighbourObject == _firstNeighbour || neighbourObject == _secondNeighbour;
        }

        public void UpdateAllConnections()
        {
            Setup();

            TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
            if (map == null)
                return;

            map.AdjacencyEngine.QueueCascadeFrom(_placedObject);
            map.AdjacencyEngine.ProcessQueue();
        }

        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            Setup();

            TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
            if (map == null)
                return false;

            map.AdjacencyEngine.QueueUpdate(_placedObject);

            if (updateNeighbour && neighbourObject != null && neighbourObject.TryGetComponent<IEngineDrivenAdjacency>(out _))
                map.AdjacencyEngine.QueueUpdate(neighbourObject);

            map.AdjacencyEngine.ProcessQueue();
            return true;
        }

        private void UpdateAllAndNeighbours(TileMap map)
        {
            Setup();

            List<PlacedTileObject> neighbours = GetNeighbours(map);
            DirectionalAdjacencyResult result = DirectionalConfigurationEvaluator.Evaluate(
                _placedObject,
                neighbours,
                CreateNeighbourState,
                AdjacencyResolver);

            _firstNeighbour = result.FirstNeighbour;
            _secondNeighbour = result.SecondNeighbour;

            bool updated = UpdateMeshRotationDirection(
                result.Mesh,
                result.Rotation,
                result.Facing,
                result.Shape,
                result.ConnectionCount);

            if (updated)
                PublishVisualState(result.Rotation, result.Shape);

            // Only cascade when this tile's shape changed. Initial neighbour updates are handled
            // by AdjacencyEngine.QueueCascadeFrom; unconditional re-queue causes infinite loops.
            if (updated)
            {
                foreach (PlacedTileObject adjacent in neighbours)
                    map.AdjacencyEngine.QueueUpdate(adjacent);
            }
        }

        private DirectionalNeighbourState CreateNeighbourState(PlacedTileObject neighbour)
        {
            DirectionalAdjacencyConnector connector = neighbour.GetComponent<DirectionalAdjacencyConnector>();
            return new DirectionalNeighbourState(
                neighbour,
                neighbour.Direction,
                connector._currentShape,
                connector._currentConnections,
                connector._firstNeighbour,
                connector._secondNeighbour);
        }

        private bool UpdateMeshRotationDirection(
            Mesh mesh,
            float rotation,
            Direction direction,
            AdjacencyShape shape,
            int connectionNumbers)
        {
            bool updated = direction != _placedObject.Direction;
            updated |= rotation != _currentRotation;
            updated |= shape != _currentShape;
            updated |= connectionNumbers != _currentConnections;

            _currentShape = shape;
            _currentRotation = rotation;
            _currentConnections = connectionNumbers;

            if (direction != _placedObject.Direction)
                _placedObject.SetDirection(direction);

            if (_filter != null)
                _filter.mesh = mesh;

            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            localRotation = Quaternion.Euler(eulerRotation.x, rotation, eulerRotation.z);
            transform.localRotation = localRotation;

            return updated;
        }

        private void PublishVisualState(float rotation, AdjacencyShape shape)
        {
            _pendingRotation = rotation;
            _pendingShape = shape;
            _hasPendingVisual = true;

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                _syncedRotation = rotation;
                _syncedShape = shape;
                _hasPendingVisual = false;
            }
        }

        private void SyncRotation(float _, float __, bool asServer)
        {
            if (!asServer)
                ApplyVisualState(_syncedRotation, _syncedShape);
        }

        private void SyncShape(AdjacencyShape _, AdjacencyShape __, bool asServer)
        {
            if (!asServer)
                ApplyVisualState(_syncedRotation, _syncedShape);
        }

        private void ApplyVisualState(float rotation, AdjacencyShape shape)
        {
            Setup();

            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            localRotation = Quaternion.Euler(eulerRotation.x, rotation, eulerRotation.z);
            transform.localRotation = localRotation;
            Mesh mesh = AdjacencyResolver.ShapeToMesh(shape);
            _filter.mesh = mesh;
        }
    }
}
